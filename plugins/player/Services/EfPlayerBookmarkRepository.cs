using Listenarr.Plugins.Player.Data;
using Microsoft.EntityFrameworkCore;

namespace Listenarr.Plugins.Player.Services;

public sealed class EfPlayerBookmarkRepository(PlayerDbContext db) : IBookmarkRepository
{
    public async Task<IReadOnlyList<PlayerBookmark>> GetByAudiobookIdAsync(int audiobookId, CancellationToken ct = default) =>
        await db.Bookmarks
            .Where(b => b.AudiobookId == audiobookId)
            .OrderBy(b => b.FileIndex).ThenBy(b => b.PositionSeconds)
            .ToListAsync(ct);

    public async Task<PlayerBookmark> AddAsync(PlayerBookmark bookmark, CancellationToken ct = default)
    {
        db.Bookmarks.Add(bookmark);
        await db.SaveChangesAsync(ct);
        return bookmark;
    }

    public async Task<bool> DeleteAsync(int audiobookId, int bookmarkId, CancellationToken ct = default)
    {
        var existing = await db.Bookmarks.FirstOrDefaultAsync(b => b.Id == bookmarkId && b.AudiobookId == audiobookId, ct);
        if (existing is null)
        {
            return false;
        }

        db.Bookmarks.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
