using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RobotPlanet.Core.storage;
using RobotPlanet.Wpf.Persistence;

namespace RobotPlanet.Wpf.Services
{
    /// <summary>
    /// Registers the active IWorldStorage implementation in the DI container based on AppSettings.
    /// Keeps composition logic clean and decoupled from MainViewModel.
    /// </summary>
    public static class StorageProviderFactory
    {
        public static void RegisterStorage(IServiceCollection services, AppSettings settings)
        {
            if (settings.UseEfCore)
            {
                services.AddDbContextFactory<RobotPlanetDbContext>(options =>
                    options.UseSqlServer(settings.LocalDbConnectionString));

                services.AddSingleton<IWorldStorage, EfCoreWorldStorage>();
            }
            else if (settings.UseLocalDb)
            {
                services.AddSingleton<IWorldStorage>(_ =>
                    new LocalDbWorldStorage(
                        connectionString => new SqlConnection(connectionString),
                        settings.LocalDbConnectionString));
            }
            else
            {
                services.AddSingleton<JsonWorldStorage>();
                services.AddSingleton<XmlWorldStorage>();
                services.AddSingleton<IWorldStorage, FileWorldStorage>();
            }
        }
    }
}
