using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Listenarr.Plugins.Player.Data;

/// <summary>
/// Applies the player plugin's own migrations on startup (against its isolated history table)
/// and seeds the single PlayerSettings row. Runs once at app start, before serving requests.
/// </summary>
public sealed class PlayerMigrationRunner : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PlayerMigrationRunner(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlayerDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        if (!await db.Settings.AnyAsync(cancellationToken))
        {
            db.Settings.Add(new PlayerSettings { Id = 1 });
            await db.SaveChangesAsync(cancellationToken);
        }

        Console.WriteLine("[player] migrations applied");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
