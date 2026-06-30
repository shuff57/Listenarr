using Microsoft.EntityFrameworkCore;

namespace Listenarr.Plugins.Player.Data;

/// <summary>
/// The player plugin's own EF context. Points at the SAME SQLite file as core but keeps its
/// schema isolated via a distinct migrations history table (see ConfigureServices), so plugin
/// and core migrations never collide.
/// </summary>
public sealed class PlayerDbContext : DbContext
{
    public PlayerDbContext(DbContextOptions<PlayerDbContext> options) : base(options)
    {
    }

    public DbSet<PlayerPlaybackState> PlaybackStates => Set<PlayerPlaybackState>();
    public DbSet<PlayerBookmark> Bookmarks => Set<PlayerBookmark>();
    public DbSet<PlayerChapterCache> ChapterCaches => Set<PlayerChapterCache>();
    public DbSet<PlayerSettings> Settings => Set<PlayerSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerPlaybackState>(e =>
        {
            e.ToTable("PlayerPlaybackStates");
            e.HasKey(x => x.AudiobookId);
            e.Property(x => x.AudiobookId).ValueGeneratedNever();
            e.HasIndex(x => x.UpdatedUtc);
        });

        modelBuilder.Entity<PlayerBookmark>(e =>
        {
            e.ToTable("PlayerBookmarks");
            e.HasIndex(x => x.AudiobookId);
        });

        modelBuilder.Entity<PlayerChapterCache>(e =>
        {
            e.ToTable("PlayerChapterCaches");
            e.HasKey(x => x.AudiobookFileId);
            e.Property(x => x.AudiobookFileId).ValueGeneratedNever();
        });

        modelBuilder.Entity<PlayerSettings>(e =>
        {
            e.ToTable("PlayerSettings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });

        // ponytail: cross-context FK to core audiobook ids by convention; orphan sweep later.
    }
}
