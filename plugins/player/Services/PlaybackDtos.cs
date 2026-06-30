namespace Listenarr.Plugins.Player.Services;

public record PlaybackFileDto(int Index, double? DurationSeconds, string ContentType);
public record ChapterDto(int Index, int FileIndex, double StartSeconds, double EndSeconds, string Title);
public record PlaybackStateDto(int AudiobookId, string? Title, string? Asin, IReadOnlyList<PlaybackFileDto> Files, int FileIndex, double PositionSeconds, bool Finished, IReadOnlyList<ChapterDto> Chapters);
public record SavePlaybackRequest(int FileIndex, double PositionSeconds, bool Finished);

/// <summary>Lightweight continue-listening row; the frontend merges it with library data by id.</summary>
public record ContinueListeningItem(int AudiobookId, int FileIndex, double PositionSeconds, bool Finished, DateTime? UpdatedUtc);

public record BookmarkDto(int Id, int FileIndex, double PositionSeconds, string? Label, DateTime CreatedUtc);
public record CreateBookmarkRequest(int FileIndex, double PositionSeconds, string? Label);
