using Listenarr.Api.Plugins;
using Listenarr.Infrastructure.Persistence;
using Listenarr.Plugins.Player.Data;
using Listenarr.Plugins.Player.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Listenarr.Plugins.Player;

/// <summary>
/// The built-in audiobook player, packaged as a Listenarr plugin. Owns its own data
/// (<c>PlayerDbContext</c>, isolated migrations history) and consumes core only through
/// existing repositories — core carries no player code or schema.
/// </summary>
public sealed class PlayerPlugin : IListenarrPlugin
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Reuse core's exact SQLite connection (resolved once, cached) so the plugin opens the
        // SAME database file — never re-deriving the path and risking drift from core.
        string? connection = null;
        services.AddDbContext<PlayerDbContext>((sp, options) =>
        {
            if (connection is null)
            {
                using var core = sp.GetRequiredService<IDbContextFactory<ListenArrDbContext>>().CreateDbContext();
                connection = core.Database.GetConnectionString();
            }

            options.UseSqlite(connection, sql => sql.MigrationsHistoryTable("__PlayerMigrations"));
        });

        services.AddHostedService<PlayerMigrationRunner>();

        services.AddScoped<ChapterProbe>();
        services.AddScoped<IPlaybackService, PlaybackService>();
        services.AddScoped<IBookmarkService, BookmarkService>();
        services.AddScoped<IBookmarkRepository, EfPlayerBookmarkRepository>();

        Console.WriteLine("[player] plugin loaded");
    }
}
