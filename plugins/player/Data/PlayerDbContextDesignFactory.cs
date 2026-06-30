using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Listenarr.Plugins.Player.Data;

/// <summary>
/// Design-time factory so `dotnet ef migrations` can build the model without the app's DI.
/// The connection string is irrelevant for scaffolding migrations — only the model matters.
/// </summary>
public sealed class PlayerDbContextDesignFactory : IDesignTimeDbContextFactory<PlayerDbContext>
{
    public PlayerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlayerDbContext>()
            .UseSqlite("Data Source=player-design.db")
            .Options;
        return new PlayerDbContext(options);
    }
}
