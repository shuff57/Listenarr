using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Listenarr.Application.SystemDiagnostics.Contracts;
using Microsoft.Extensions.Logging;

namespace Listenarr.Plugins.Player.Services;

/// <summary>
/// Extracts chapter markers by shelling out to ffprobe directly, reusing the binary core
/// already installs (via <see cref="IFfmpegService.GetFfprobePathAsync"/>). Replaces the
/// method the player feature used to add to core's IFfmpegService — so core stays untouched.
/// Never throws: any failure yields an empty list (treated as chapter-less).
/// </summary>
public sealed class ChapterProbe(IFfmpegService ffmpeg, ILogger<ChapterProbe> logger)
{
    public record ProbedChapter(double StartSeconds, double EndSeconds, string Title);

    public async Task<IReadOnlyList<ProbedChapter>> RunAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return [];
            }

            var ffprobe = await ffmpeg.GetFfprobePathAsync();
            if (string.IsNullOrEmpty(ffprobe) || !File.Exists(ffprobe))
            {
                logger.LogWarning("ffprobe binary unavailable; treating file as chapter-less");
                return [];
            }

            var psi = new ProcessStartInfo
            {
                FileName = ffprobe,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-v");
            psi.ArgumentList.Add("quiet");
            psi.ArgumentList.Add("-print_format");
            psi.ArgumentList.Add("json");
            psi.ArgumentList.Add("-show_chapters");
            psi.ArgumentList.Add(Path.GetFullPath(filePath));

            using var proc = Process.Start(psi);
            if (proc is null)
            {
                return [];
            }

            var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
            try
            {
                await proc.WaitForExitAsync(linked.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                try { proc.Kill(true); } catch { /* best effort */ }
                logger.LogWarning("ffprobe timed out; treating file as chapter-less");
                return [];
            }

            var json = await stdoutTask;
            return ParseChapters(json);
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or OutOfMemoryException or StackOverflowException))
        {
            logger.LogWarning(ex, "ffprobe chapters failed; treating as chapter-less");
            return [];
        }
    }

    /// <summary>Pure parser for ffprobe -show_chapters JSON. Testable without a binary.</summary>
    public static IReadOnlyList<ProbedChapter> ParseChapters(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("chapters", out var chapters) || chapters.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<ProbedChapter>();
            foreach (var ch in chapters.EnumerateArray())
            {
                var start = ParseTime(ch, "start_time");
                var end = ParseTime(ch, "end_time");
                var title = string.Empty;
                if (ch.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Object
                    && tags.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                {
                    title = t.GetString() ?? string.Empty;
                }

                result.Add(new ProbedChapter(start, end, title));
            }

            return result;
        }
        catch
        {
            return [];
        }
    }

    private static double ParseTime(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.String
                && double.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                return d;
            }

            if (prop.ValueKind == JsonValueKind.Number)
            {
                return prop.GetDouble();
            }
        }

        return 0.0;
    }
}
