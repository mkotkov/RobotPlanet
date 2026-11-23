using System.Xml.Serialization;

namespace RobotPlanet.Core.storage
{
    /// <summary>
    /// The saved form of the whole world: plain data only.
    /// No windows, no commands and no event subscriptions are ever saved.
    /// Every value is nullable on purpose: a file that lacks a field must be reported
    /// as an error, not silently read as 0 or an empty text.
    /// </summary>
    [XmlRoot("World")]
    public sealed class WorldDto
    {
        public const int CurrentVersion = 1;

        public int? Version { get; set; }

        [XmlArray("Robots")]
        [XmlArrayItem("Robot")]
        public List<RobotDto>? Robots { get; set; }
    }

    /// <summary>The saved form of one robot: identity, type, name, changeable fields and group.</summary>
    public sealed class RobotDto
    {
        public int? Id { get; set; }

        /// <summary>The name of the RobotKind value, e.g. "Cleaner".</summary>
        public string? Type { get; set; }

        public string? Name { get; set; }
        public int? Battery { get; set; }
        public int? Integrity { get; set; }

        /// <summary>The name of the RobotGroup value, e.g. "Alfa".</summary>
        public string? Group { get; set; }
    }
}
