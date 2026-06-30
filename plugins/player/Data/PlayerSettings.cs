namespace Listenarr.Plugins.Player.Data;

/// <summary>
/// Single-row (Id = 1) player settings table, owned by the plugin.
/// ponytail: one toggle, seeded on migrate; a settings UI can come later.
/// </summary>
public sealed class PlayerSettings
{
    public int Id { get; set; } = 1;

    /// <summary>When true, mark a book unmonitored once the listener finishes it.</summary>
    public bool AutoUnmonitorOnFinish { get; set; } = true;
}
