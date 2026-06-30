using System.ComponentModel.DataAnnotations;

namespace Listenarr.Plugins.Player.Data;

/// <summary>A user bookmark at a position within an audiobook. Owned by the player plugin.</summary>
public sealed class PlayerBookmark
{
    [Key]
    public int Id { get; set; }

    public int AudiobookId { get; set; }

    public int FileIndex { get; set; }

    public double PositionSeconds { get; set; }

    public string? Label { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
