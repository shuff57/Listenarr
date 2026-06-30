namespace Listenarr.Plugins.Player.Data;

/// <summary>
/// Per-audiobook resume state, owned by the player plugin (keyed by the core audiobook id,
/// by convention — no enforced cross-context FK).
/// </summary>
public sealed class PlayerPlaybackState
{
    public int AudiobookId { get; set; }

    /// <summary>Index of the file in the natural-ordered playlist last played.</summary>
    public int FileIndex { get; set; }

    /// <summary>Resume position within that file, in seconds.</summary>
    public double PositionSeconds { get; set; }

    /// <summary>When this was last saved (UTC). Null = never played.</summary>
    public DateTime? UpdatedUtc { get; set; }

    /// <summary>True once the listener finished the book.</summary>
    public bool Finished { get; set; }
}
