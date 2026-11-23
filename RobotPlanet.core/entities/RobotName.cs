using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core.entities
{
    /// <summary>
    /// Value Object representing a validated robot name (2-20 trimmed characters, non-empty).
    /// Encapsulates domain validation rules inside the domain model (DDD).
    /// </summary>
    public readonly record struct RobotName
    {
        public const int MinLength = 2;
        public const int MaxLength = 20;

        public string Value { get; }

        public RobotName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(Resources.Err_Invalid_name, nameof(value));

            string trimmed = value.Trim();
            if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
                throw new ArgumentException(Resources.Err_Invalid_name, nameof(value));

            Value = trimmed;
        }

        public static RobotName Create(string? value) => new(value);

        public static bool TryCreate(string? value, out RobotName result, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = default;
                error = Resources.Err_Invalid_name;
                return false;
            }

            string trimmed = value.Trim();
            if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
            {
                result = default;
                error = Resources.Err_Invalid_name;
                return false;
            }

            result = new RobotName(trimmed);
            error = string.Empty;
            return true;
        }

        public override string ToString() => Value ?? string.Empty;

        public static implicit operator string(RobotName name) => name.Value ?? string.Empty;
    }
}
