namespace RobotPlanet.Wpf.Persistence
{
    /// <summary>
    /// Tracks schema/world version and last UTC save timestamp in EF Core LocalDB schema.
    /// </summary>
    public sealed class WorldMetadataEntity
    {
        public int Id { get; set; }
        public int Version { get; set; }
        public DateTime SavedAtUtc { get; set; }
    }
}
