using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RobotPlanet.Wpf.Services;

namespace RobotPlanet.Wpf.Persistence
{
    /// <summary>
    /// Enables `dotnet ef migrations add` and `dotnet ef database update` at design time
    /// without launching the WPF application window.
    /// </summary>
    public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RobotPlanetDbContext>
    {
        public RobotPlanetDbContext CreateDbContext(string[] args)
        {
            AppSettings settings = AppSettings.Load(args);
            var optionsBuilder = new DbContextOptionsBuilder<RobotPlanetDbContext>();
            optionsBuilder.UseSqlServer(settings.LocalDbConnectionString);
            return new RobotPlanetDbContext(optionsBuilder.Options);
        }
    }
}
