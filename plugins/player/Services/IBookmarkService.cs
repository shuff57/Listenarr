namespace Listenarr.Plugins.Player.Services;

public interface IBookmarkService
{
    Task<IReadOnlyList<BookmarkDto>> GetAsync(int audiobookId, CancellationToken ct = default);
    Task<BookmarkDto> AddAsync(int audiobookId, CreateBookmarkRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int audiobookId, int bookmarkId, CancellationToken ct = default);
}

public interface IBookmarkRepository
{
    Task<IReadOnlyList<Data.PlayerBookmark>> GetByAudiobookIdAsync(int audiobookId, CancellationToken ct = default);
    Task<Data.PlayerBookmark> AddAsync(Data.PlayerBookmark bookmark, CancellationToken ct = default);
    Task<bool> DeleteAsync(int audiobookId, int bookmarkId, CancellationToken ct = default);
}
