using Listenarr.Plugins.Player.Data;

namespace Listenarr.Plugins.Player.Services;

public sealed class BookmarkService(IBookmarkRepository repository) : IBookmarkService
{
    public async Task<IReadOnlyList<BookmarkDto>> GetAsync(int audiobookId, CancellationToken ct = default)
    {
        var bookmarks = await repository.GetByAudiobookIdAsync(audiobookId, ct);
        return bookmarks.Select(ToDto).ToList();
    }

    public async Task<BookmarkDto> AddAsync(int audiobookId, CreateBookmarkRequest request, CancellationToken ct = default)
    {
        var bookmark = new PlayerBookmark
        {
            AudiobookId = audiobookId,
            FileIndex = request.FileIndex,
            PositionSeconds = request.PositionSeconds,
            Label = request.Label,
            CreatedUtc = DateTime.UtcNow,
        };
        var saved = await repository.AddAsync(bookmark, ct);
        return ToDto(saved);
    }

    public Task<bool> DeleteAsync(int audiobookId, int bookmarkId, CancellationToken ct = default) =>
        repository.DeleteAsync(audiobookId, bookmarkId, ct);

    private static BookmarkDto ToDto(PlayerBookmark b) =>
        new(b.Id, b.FileIndex, b.PositionSeconds, b.Label, b.CreatedUtc);
}
