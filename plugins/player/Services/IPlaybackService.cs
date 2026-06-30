namespace Listenarr.Plugins.Player.Services;

public interface IPlaybackService
{
    Task<PlaybackStateDto?> GetStateAsync(int audiobookId, CancellationToken ct = default);
    Task<(string Path, string ContentType)?> ResolveFileAsync(int audiobookId, int fileIndex, CancellationToken ct = default);
    Task<bool> SaveAsync(int audiobookId, SavePlaybackRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<ContinueListeningItem>> GetContinueListeningAsync(CancellationToken ct = default);

    /// <summary>All started playback rows (finished and in-progress) so the UI can build
    /// Continue-Listening / Finished sections and per-book status badges.</summary>
    Task<IReadOnlyList<ContinueListeningItem>> GetStatesAsync(CancellationToken ct = default);
}
