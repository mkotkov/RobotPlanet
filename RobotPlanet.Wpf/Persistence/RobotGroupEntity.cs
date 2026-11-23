using RobotPlanet.Core.entities;

namespace RobotPlanet.Wpf.Persistence
{
    /// <summary>
    /// Normalized lookup table entity for RobotGroup in EF Core LocalDB schema.
    /// </summary>
    public sealed class RobotGroupEntity
    {
        public RobotGroup Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
