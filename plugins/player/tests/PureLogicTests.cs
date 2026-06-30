using Listenarr.Plugins.Player.Services;
using Xunit;

namespace Listenarr.Plugins.Player.Tests;

public class AudioContentTypeTests
{
    [Theory]
    [InlineData("m4b", "audio/mp4")]
    [InlineData(".M4A", "audio/mp4")]
    [InlineData("mp3", "audio/mpeg")]
    [InlineData("opus", "audio/ogg")]
    [InlineData("flac", "audio/flac")]
    [InlineData("wav", "audio/wav")]
    [InlineData(null, "application/octet-stream")]
    [InlineData("weird", "application/octet-stream")]
    public void ForContainer_maps_expected(string? container, string expected) =>
        Assert.Equal(expected, AudioContentType.ForContainer(container));
}

public class ChapterProbeParseTests
{
    [Fact]
    public void Parses_chapters_with_titles()
    {
        const string json = """
        { "chapters": [
          { "start_time": "0.000000", "end_time": "10.5", "tags": { "title": "Intro" } },
          { "start_time": 10.5, "end_time": 20.0 }
        ]}
        """;
        var result = ChapterProbe.ParseChapters(json);
        Assert.Equal(2, result.Count);
        Assert.Equal("Intro", result[0].Title);
        Assert.Equal(10.5, result[0].EndSeconds);
        Assert.Equal(20.0, result[1].EndSeconds);
        Assert.Equal(string.Empty, result[1].Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"chapters\": {}}")]
    public void Returns_empty_on_missing_or_bad_input(string json) =>
        Assert.Empty(ChapterProbe.ParseChapters(json));
}

public class NaturalSortTests
{
    [Fact]
    public void Sorts_numeric_segments_by_value()
    {
        var input = new[] { "Part 10.mp3", "Part 2.mp3", "Part 1.mp3" };
        Array.Sort(input, PlaybackService.NaturalSortComparer.Instance);
        Assert.Equal(new[] { "Part 1.mp3", "Part 2.mp3", "Part 10.mp3" }, input);
    }
}
