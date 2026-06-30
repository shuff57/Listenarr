using Listenarr.Application.Audiobooks.Contracts.Repositories;
using Listenarr.Application.Common.Contracts;
using Listenarr.Domain.Common;
using Listenarr.Plugins.Player.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Listenarr.Plugins.Player.Controllers;

/// <summary>
/// Player HTTP API, served from the plugin assembly (discovered as an MVC application part).
/// Routes match the original built-in feature so the frontend is unchanged.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/audiobooks")]
public sealed class ListenController(
    IPlaybackService playback,
    IBookmarkService bookmarkService,
    ILogger<ListenController> logger,
    IFileSystem fileSystem,
    IRootFolderRepository rootFolderRepository) : ControllerBase
{
    /// <summary>Get the current playback state for an audiobook.</summary>
    [HttpGet("{id:int}/playback")]
    public async Task<IActionResult> GetPlayback(int id, CancellationToken ct)
    {
        var state = await playback.GetStateAsync(id, ct);
        return state is null ? NotFound() : Ok(state);
    }

    /// <summary>Stream an audiobook file by index. Supports HTTP range requests (206).</summary>
    [HttpGet("{id:int}/files/{index:int}/stream")]
    public async Task<IActionResult> Stream(int id, int index, CancellationToken ct)
    {
        var resolved = await playback.ResolveFileAsync(id, index, ct);
        if (resolved is null)
        {
            return NotFound();
        }

        var (path, contentType) = resolved.Value;

        // Trust boundary: reject path traversal before touching the filesystem.
        if (!await IsUnderConfiguredRootAsync(path))
        {
            logger.LogWarning(
                "Blocked stream request for audiobook {AudiobookId} file {FileIndex}: path outside configured roots",
                id, index);
            return Forbid();
        }

        if (!fileSystem.FileExists(path))
        {
            return NotFound();
        }

        // IFileSystem has no stream-open method; System.IO.File.OpenRead is intentional here.
        var stream = System.IO.File.OpenRead(path);
        return File(stream, contentType, enableRangeProcessing: true);
    }

    /// <summary>Save playback progress for an audiobook.</summary>
    [HttpPut("{id:int}/playback")]
    public async Task<IActionResult> SavePlayback(int id, [FromBody] SavePlaybackRequest req, CancellationToken ct)
    {
        var ok = await playback.SaveAsync(id, req, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>Get in-progress audiobooks, most-recently-played first, capped at 20.</summary>
    [HttpGet("continue-listening")]
    public async Task<IActionResult> GetContinueListening(CancellationToken ct) =>
        Ok(await playback.GetContinueListeningAsync(ct));

    /// <summary>List all bookmarks for an audiobook.</summary>
    [HttpGet("{id:int}/bookmarks")]
    public async Task<IActionResult> GetBookmarks(int id, CancellationToken ct) =>
        Ok(await bookmarkService.GetAsync(id, ct));

    /// <summary>Create a bookmark for an audiobook.</summary>
    [HttpPost("{id:int}/bookmarks")]
    public async Task<IActionResult> CreateBookmark(int id, [FromBody] CreateBookmarkRequest request, CancellationToken ct)
    {
        var dto = await bookmarkService.AddAsync(id, request, ct);
        return StatusCode(201, dto);
    }

    /// <summary>Delete a bookmark.</summary>
    [HttpDelete("{id:int}/bookmarks/{bookmarkId:int}")]
    public async Task<IActionResult> DeleteBookmark(int id, int bookmarkId, CancellationToken ct)
    {
        var deleted = await bookmarkService.DeleteAsync(id, bookmarkId, ct);
        return deleted ? NoContent() : NotFound();
    }

    private async Task<bool> IsUnderConfiguredRootAsync(string path)
    {
        var roots = await rootFolderRepository.GetAllAsync();
        return roots.Any(r => FileUtils.IsPathSameOrInside(path, r.Path));
    }
}
