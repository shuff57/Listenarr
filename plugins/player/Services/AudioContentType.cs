namespace Listenarr.Plugins.Player.Services;

/// <summary>Maps an audio container/format to a browser-playable MIME type.</summary>
public static class AudioContentType
{
    public static string ForContainer(string? container) =>
        (container ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant() switch
        {
            "m4b" or "m4a" or "mp4" or "aac" => "audio/mp4",
            "mp3" => "audio/mpeg",
            "ogg" or "oga" or "opus" => "audio/ogg",
            "flac" => "audio/flac",
            "wav" => "audio/wav",
            _ => "application/octet-stream",
        };
}
