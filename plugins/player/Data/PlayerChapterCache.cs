namespace Listenarr.Plugins.Player.Data;

/// <summary>
/// Cached ffprobe chapter data per audiobook file, keyed by the core audiobook-file id.
/// ChaptersJson is a serialized ChapterDto list; "[]" means probed with no chapters (or
/// ffprobe failed) so we don't re-probe every load.
/// </summary>
public sealed class PlayerChapterCache
{
    public int AudiobookFileId { get; set; }

    public string ChaptersJson { get; set; } = "[]";
}
