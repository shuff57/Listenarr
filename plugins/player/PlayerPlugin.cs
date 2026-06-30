using Listenarr.Api.Plugins;
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
        // Phase B+ wires DbContext, migration runner, and player services here.
        Console.WriteLine("[player] plugin loaded");
    }
}
