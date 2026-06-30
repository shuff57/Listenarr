using Listenarr.Application.Audiobooks.Contracts.Repositories;
using Listenarr.Domain.Audiobooks;
using Listenarr.Plugins.Player.Data;
using Listenarr.Plugins.Player.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Listenarr.Plugins.Player.Tests;

public sealed class PlaybackServiceTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly PlayerDbContext _db;

    public PlaybackServiceTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var options = new DbContextOptionsBuilder<PlayerDbContext>().UseSqlite(_conn).Options;
        _db = new PlayerDbContext(options);
        _db.Database.EnsureCreated();
    }

    private static Audiobook BookWithFiles(int id, int fileCount)
    {
        var files = Enumerable.Range(0, fileCount)
            .Select(i => new AudiobookFile { Id = id * 100 + i, Path = $"/audio/part {i + 1}.mp3" })
            .ToList();
        return new Audiobook { Id = id, Title = "Test", Monitored = true, Files = files };
    }

    private PlaybackService Service(Mock<IAudiobookRepository> repo)
    {
        // ChapterProbe is only exercised by GetState; these tests don't hit ffprobe.
        var probe = new ChapterProbe(Mock.Of<Listenarr.Application.SystemDiagnostics.Contracts.IFfmpegService>(), NullLogger<ChapterProbe>.Instance);
        return new PlaybackService(repo.Object, _db, probe, NullLogger<PlaybackService>.Instance);
    }

    [Fact]
    public async Task SaveAsync_clamps_out_of_range_values()
    {
        var repo = new Mock<IAudiobookRepository>();
        repo.Setup(r => r.GetByIdsWithFilesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Audiobook> { BookWithFiles(1, 3) });

        var ok = await Service(repo).SaveAsync(1, new SavePlaybackRequest(FileIndex: 99, PositionSeconds: -5, Finished: false));

        Assert.True(ok);
        var state = await _db.PlaybackStates.SingleAsync();
        Assert.Equal(2, state.FileIndex);           // clamped to last index (3 files → max 2)
        Assert.Equal(0, state.PositionSeconds);     // clamped up from negative
        Assert.NotNull(state.UpdatedUtc);
    }

    [Fact]
    public async Task SaveAsync_finished_unmonitors_when_setting_on()
    {
        _db.Settings.Add(new PlayerSettings { Id = 1, AutoUnmonitorOnFinish = true });
        await _db.SaveChangesAsync();

        var book = BookWithFiles(7, 2);
        var repo = new Mock<IAudiobookRepository>();
        repo.Setup(r => r.GetByIdsWithFilesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Audiobook> { book });
        repo.Setup(r => r.UpdateAsync(It.IsAny<Audiobook>())).ReturnsAsync(true);

        await Service(repo).SaveAsync(7, new SavePlaybackRequest(0, 123, Finished: true));

        Assert.False(book.Monitored);
        repo.Verify(r => r.UpdateAsync(It.Is<Audiobook>(b => b.Id == 7 && !b.Monitored)), Times.Once);
    }

    [Fact]
    public async Task GetContinueListening_returns_unfinished_recent_first()
    {
        _db.PlaybackStates.AddRange(
            new PlayerPlaybackState { AudiobookId = 1, UpdatedUtc = new DateTime(2026, 1, 1), Finished = false },
            new PlayerPlaybackState { AudiobookId = 2, UpdatedUtc = new DateTime(2026, 2, 1), Finished = false },
            new PlayerPlaybackState { AudiobookId = 3, UpdatedUtc = new DateTime(2026, 3, 1), Finished = true });
        await _db.SaveChangesAsync();

        var repo = new Mock<IAudiobookRepository>();
        var list = await Service(repo).GetContinueListeningAsync();

        Assert.Equal(new[] { 2, 1 }, list.Select(x => x.AudiobookId).ToArray());
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
