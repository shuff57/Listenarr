using System.Text.Json;
using System.Text.RegularExpressions;
using Listenarr.Application.Audiobooks.Contracts.Repositories;
using Listenarr.Domain.Audiobooks;
using Listenarr.Plugins.Player.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Listenarr.Plugins.Player.Services;

/// <summary>
/// Player playback service. State, chapter cache, and settings live in the plugin's own
/// <see cref="PlayerDbContext"/>; core is consumed only through the existing
/// <see cref="IAudiobookRepository"/> (to read books/files and toggle monitoring on finish).
/// </summary>
public sealed partial class PlaybackService(
    IAudiobookRepository audiobookRepository,
    PlayerDbContext db,
    ChapterProbe chapterProbe,
    ILogger<PlaybackService> logger) : IPlaybackService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<PlaybackStateDto?> GetStateAsync(int audiobookId, CancellationToken ct = default)
    {
        var book = await LoadBookWithFilesAsync(audiobookId, ct);
        if (book is null)
        {
            return null;
        }

        var ordered = OrderFiles(book.Files);
        var fileDtos = ordered
            .Select((f, i) => new PlaybackFileDto(i, f.DurationSeconds, AudioContentType.ForContainer(f.Container ?? f.Format)))
            .ToList();

        var state = await db.PlaybackStates.AsNoTracking().FirstOrDefaultAsync(s => s.AudiobookId == audiobookId, ct);
        var chapters = await BuildChaptersAsync(ordered, ct);

        return new PlaybackStateDto(
            book.Id,
            book.Title,
            book.Asin,
            fileDtos,
            state?.FileIndex ?? 0,
            state?.PositionSeconds ?? 0,
            state?.Finished ?? false,
            chapters);
    }

    public async Task<(string Path, string ContentType)?> ResolveFileAsync(int audiobookId, int fileIndex, CancellationToken ct = default)
    {
        var book = await LoadBookWithFilesAsync(audiobookId, ct);
        if (book is null)
        {
            return null;
        }

        var ordered = OrderFiles(book.Files);
        if (fileIndex < 0 || fileIndex >= ordered.Count)
        {
            return null;
        }

        var file = ordered[fileIndex];
        return (file.Path!, AudioContentType.ForContainer(file.Container ?? file.Format));
    }

    public async Task<bool> SaveAsync(int audiobookId, SavePlaybackRequest req, CancellationToken ct = default)
    {
        var book = await LoadBookWithFilesAsync(audiobookId, ct);
        if (book is null)
        {
            return false;
        }

        var state = await db.PlaybackStates.FirstOrDefaultAsync(s => s.AudiobookId == audiobookId, ct);
        if (state is null)
        {
            state = new PlayerPlaybackState { AudiobookId = audiobookId };
            db.PlaybackStates.Add(state);
        }

        // Clamp client-supplied values: never persist negative/NaN/out-of-range resume state.
        var fileCount = book.Files?.Count ?? 0;
        state.FileIndex = fileCount == 0 ? 0 : Math.Clamp(req.FileIndex, 0, fileCount - 1);
        state.PositionSeconds = double.IsFinite(req.PositionSeconds) ? Math.Max(0, req.PositionSeconds) : 0;
        state.UpdatedUtc = DateTime.UtcNow;
        state.Finished = req.Finished;

        if (req.Finished)
        {
            var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(ct);
            if (settings?.AutoUnmonitorOnFinish == true && book.Monitored)
            {
                book.Monitored = false;
                await audiobookRepository.UpdateAsync(book);
            }
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<ContinueListeningItem>> GetContinueListeningAsync(CancellationToken ct = default)
    {
        var rows = await db.PlaybackStates.AsNoTracking()
            .Where(s => s.UpdatedUtc != null && !s.Finished)
            .OrderByDescending(s => s.UpdatedUtc)
            .Take(20)
            .ToListAsync(ct);

        return rows
            .Select(r => new ContinueListeningItem(r.AudiobookId, r.FileIndex, r.PositionSeconds, r.Finished, r.UpdatedUtc))
            .ToList();
    }

    public async Task<IReadOnlyList<ContinueListeningItem>> GetStatesAsync(CancellationToken ct = default)
    {
        var rows = await db.PlaybackStates.AsNoTracking()
            .Where(s => s.UpdatedUtc != null)
            .OrderByDescending(s => s.UpdatedUtc)
            .ToListAsync(ct);

        return rows
            .Select(r => new ContinueListeningItem(r.AudiobookId, r.FileIndex, r.PositionSeconds, r.Finished, r.UpdatedUtc))
            .ToList();
    }

    // ── Chapter extraction & aggregation ─────────────────────────────────────

    private async Task<IReadOnlyList<ChapterDto>> BuildChaptersAsync(List<AudiobookFile> ordered, CancellationToken ct)
    {
        var result = new List<ChapterDto>();
        var chapterIndex = 0;

        for (var fileIdx = 0; fileIdx < ordered.Count; fileIdx++)
        {
            var file = ordered[fileIdx];

            var cache = await db.ChapterCaches.FirstOrDefaultAsync(c => c.AudiobookFileId == file.Id, ct);
            if (cache is null)
            {
                cache = new PlayerChapterCache { AudiobookFileId = file.Id, ChaptersJson = await ProbeAndSerializeAsync(file, ct) };
                db.ChapterCaches.Add(cache);
                try
                {
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex) when (ex is not (OperationCanceledException or OutOfMemoryException or StackOverflowException))
                {
                    logger.LogWarning(ex, "Failed to persist chapter cache for AudiobookFile {Id}", file.Id);
                    db.ChapterCaches.Entry(cache).State = EntityState.Detached;
                }
            }

            var embedded = DeserializeChapters(cache.ChaptersJson);

            if (embedded.Count > 0)
            {
                foreach (var ch in embedded)
                {
                    var title = string.IsNullOrWhiteSpace(ch.Title) ? $"Chapter {chapterIndex + 1}" : ch.Title;
                    result.Add(new ChapterDto(chapterIndex++, fileIdx, ch.StartSeconds, ch.EndSeconds, title));
                }
            }
            else
            {
                var title = !string.IsNullOrWhiteSpace(file.Path)
                    ? Path.GetFileNameWithoutExtension(file.Path)
                    : $"Part {fileIdx + 1}";
                result.Add(new ChapterDto(chapterIndex++, fileIdx, 0, file.DurationSeconds ?? 0, title));
            }
        }

        return result;
    }

    private async Task<string> ProbeAndSerializeAsync(AudiobookFile file, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(file.Path))
        {
            return "[]";
        }

        var chapters = await chapterProbe.RunAsync(file.Path, ct);
        if (chapters.Count == 0)
        {
            return "[]";
        }

        var entries = chapters.Select(c => new ChapterCacheEntry(c.StartSeconds, c.EndSeconds, c.Title)).ToList();
        return JsonSerializer.Serialize(entries, JsonOptions);
    }

    private static IReadOnlyList<ChapterCacheEntry> DeserializeChapters(string? json)
    {
        if (string.IsNullOrEmpty(json) || json == "[]")
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<ChapterCacheEntry>>(json, JsonOptions) ?? (IReadOnlyList<ChapterCacheEntry>)[];
        }
        catch
        {
            return [];
        }
    }

    // ponytail: simple POD for chapter JSON cache; no version field until the schema changes.
    private record ChapterCacheEntry(double StartSeconds, double EndSeconds, string Title);

    private async Task<Audiobook?> LoadBookWithFilesAsync(int id, CancellationToken ct)
    {
        var books = await audiobookRepository.GetByIdsWithFilesAsync([id], ct);
        return books.FirstOrDefault();
    }

    internal static List<AudiobookFile> OrderFiles(List<AudiobookFile>? files)
    {
        if (files is null or { Count: 0 })
        {
            return [];
        }

        // AudiobookFile carries no track-number field; fall back to natural sort on Path.
        return [.. files.OrderBy(f => f.Path ?? string.Empty, NaturalSortComparer.Instance)];
    }

    // ── Natural-sort comparer ─────────────────────────────────────────────────
    // ponytail: simple token-split comparer; use a dedicated lib if multi-locale or Unicode
    // edge-cases matter.

    /// <summary>Sorts strings so that "Part 2" &lt; "Part 10" (numeric segments compared by value).</summary>
    public sealed partial class NaturalSortComparer : IComparer<string>
    {
        public static readonly NaturalSortComparer Instance = new();

        [GeneratedRegex(@"(\d+)")]
        private static partial Regex Tokenizer();

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var xParts = Tokenizer().Split(x);
            var yParts = Tokenizer().Split(y);

            for (var i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
            {
                int cmp;
                if (int.TryParse(xParts[i], out var xn) && int.TryParse(yParts[i], out var yn))
                {
                    cmp = xn.CompareTo(yn);
                }
                else
                {
                    cmp = string.Compare(xParts[i], yParts[i], StringComparison.OrdinalIgnoreCase);
                }

                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return xParts.Length.CompareTo(yParts.Length);
        }
    }
}
