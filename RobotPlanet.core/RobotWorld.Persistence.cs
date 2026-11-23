using RobotPlanet.Core.entities;
using RobotPlanet.Core.Properties;
using RobotPlanet.Core.storage;

namespace RobotPlanet.Core
{
    // Vol.4 & Vol.5: saving and loading the world (both DTO-based and direct EF Core TPH domain entities).
    public partial class RobotWorld
    {
        // ---- members added to the private nested types declared in RobotWorld.cs ----

        private partial interface IRobotType
        {
            Robot Create(int id, string name, int battery, int integrity);
            void Restore(Robot robot);
            void Clear();
        }

        private sealed partial class RobotType<T> where T : Robot
        {
            /// <summary>Creates a robot WITHOUT adding it anywhere (used to check the data before loading).</summary>
            public Robot Create(int id, string name, int battery, int integrity) => _factory(id, name, battery, integrity);

            public void Restore(Robot robot) => Repository.Restore((T)robot);

            public void Clear() => Repository.ClearSilently();
        }

        // ---- saving ----

        /// <summary>
        /// Copies the current state into a WorldDto: plain data, nothing live.
        /// Call it on the UI thread and give the result to the storage, which may run on another thread.
        /// </summary>
        public WorldDto CreateDto() => new()
        {
            Version = WorldDto.CurrentVersion,
            Robots = _robots.Select(robot => new RobotDto
            {
                Id = robot.Id,
                Type = KindOf(robot).ToString(),
                Name = robot.Name.Value,
                Battery = robot.Battery,
                Integrity = robot.Integrity,
                Group = robot.Group.ToString()
            }).ToList()
        };

        /// <summary>
        /// Creates a snapshot list of domain Robot entities on the UI thread (for EF Core TPH persistence).
        /// </summary>
        public IReadOnlyList<Robot> CreateRobotsSnapshot() => _robots.ToList();

        // ---- loading ----

        /// <summary>
        /// Replaces the world with the data of a DTO, but ONLY if every part of it is valid.
        /// The whole file is checked first; when anything is wrong the current world stays exactly as it was.
        /// The right robot types, their repositories and the reaction links are rebuilt,
        /// and no reaction is triggered while loading.
        /// </summary>
        public bool TryLoad(WorldDto? dto, out string message)
        {
            if (dto == null)
            {
                message = Resources.Load_NoData;
                return false;
            }
            if (dto.Version == null)
            {
                message = Resources.Load_NoData;
                return false;
            }
            if (dto.Version != WorldDto.CurrentVersion)
            {
                message = string.Format(Resources.Load_Version, dto.Version, WorldDto.CurrentVersion);
                return false;
            }
            if (dto.Robots == null)
            {
                message = Resources.Load_NoRobots;
                return false;
            }

            // 1. Build and check everything. The current world is not touched yet.
            var built = new List<(IRobotType Entry, Robot Robot)>();
            var usedIds = new HashSet<int>();

            for (int i = 0; i < dto.Robots.Count; i++)
            {
                int number = i + 1;
                RobotDto? item = dto.Robots[i];

                if (item == null)
                {
                    message = string.Format(Resources.Load_RobotEmpty, number);
                    return false;
                }
                if (item.Id is not int id)
                    return Missing(number, nameof(RobotDto.Id), out message);
                if (string.IsNullOrWhiteSpace(item.Type))
                    return Missing(number, nameof(RobotDto.Type), out message);
                if (item.Name == null)
                    return Missing(number, nameof(RobotDto.Name), out message);
                if (item.Battery is not int battery)
                    return Missing(number, nameof(RobotDto.Battery), out message);
                if (item.Integrity is not int integrity)
                    return Missing(number, nameof(RobotDto.Integrity), out message);
                if (string.IsNullOrWhiteSpace(item.Group))
                    return Missing(number, nameof(RobotDto.Group), out message);

                if (!TryParseName(item.Type, out RobotKind kind) || !_types.TryGetValue(kind, out IRobotType? entry))
                {
                    message = string.Format(Resources.Load_UnknownType, number, item.Type);
                    return false;
                }
                if (!TryParseName(item.Group, out RobotGroup group))
                {
                    message = string.Format(Resources.Load_UnknownGroup, number, item.Group);
                    return false;
                }

                Robot robot;
                try
                {
                    // The constructor checks the rules (id range, name length, battery and integrity limits).
                    robot = entry.Create(id, item.Name, battery, integrity);
                }
                catch (ArgumentException ex)
                {
                    message = string.Format(Resources.Load_InvalidRobot, number, ex.Message.Split(" (Parameter", 2)[0]);
                    return false;
                }

                if (!usedIds.Add(robot.Id))
                {
                    message = string.Format(Resources.Load_DuplicateId, number, robot.Id);
                    return false;
                }

                robot.ChangeGroup(group);
                built.Add((entry, robot));
            }

            // 2. Everything is valid: replace the world.
            ApplyLoadedRobots(built);

            message = string.Format(Resources.Load_Done, built.Count);
            return true;

            static bool Missing(int number, string field, out string text)
            {
                text = string.Format(Resources.Load_FieldMissing, number, field);
                return false;
            }
        }

        /// <summary>
        /// Replaces the world directly with materialized domain entities (e.g., from EF Core TPH).
        /// Validates invariants and unique IDs atomically before replacing the current world.
        /// </summary>
        public bool TryLoadRobots(IEnumerable<Robot>? robots, out string message)
        {
            if (robots == null)
            {
                message = Resources.Load_NoRobots;
                return false;
            }

            var list = robots.ToList();
            var built = new List<(IRobotType Entry, Robot Robot)>();
            var usedIds = new HashSet<int>();

            for (int i = 0; i < list.Count; i++)
            {
                int number = i + 1;
                Robot? robot = list[i];
                if (robot == null)
                {
                    message = string.Format(Resources.Load_RobotEmpty, number);
                    return false;
                }

                IRobotType? entry = FindType(robot);
                if (entry == null)
                {
                    message = string.Format(Resources.Load_UnknownType, number, robot.GetType().Name);
                    return false;
                }

                if (!Enum.IsDefined(robot.Group))
                {
                    message = string.Format(Resources.Load_UnknownGroup, number, robot.Group);
                    return false;
                }

                if (robot.Id < 0 || robot.Id > 999999)
                {
                    message = string.Format(Resources.Load_InvalidRobot, number, Resources.Err_Invalid_id);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(robot.Name.Value) ||
                    robot.Name.Value.Length < RobotName.MinLength ||
                    robot.Name.Value.Length > RobotName.MaxLength)
                {
                    message = string.Format(Resources.Load_InvalidRobot, number, Resources.Err_Invalid_name);
                    return false;
                }

                if (robot.Battery < 0 || robot.Battery > Robot.MaxLevel)
                {
                    message = string.Format(Resources.Load_InvalidRobot, number, Resources.Err_Invalid_battery);
                    return false;
                }

                if (robot.Integrity < 0 || robot.Integrity > Robot.MaxLevel)
                {
                    message = string.Format(Resources.Load_InvalidRobot, number, Resources.Err_Invalid_integrity);
                    return false;
                }

                if (!usedIds.Add(robot.Id))
                {
                    message = string.Format(Resources.Load_DuplicateId, number, robot.Id);
                    return false;
                }

                built.Add((entry, robot));
            }

            ApplyLoadedRobots(built);
            message = string.Format(Resources.Load_Done, built.Count);
            return true;
        }

        private void ApplyLoadedRobots(List<(IRobotType Entry, Robot Robot)> built)
        {
            foreach (IRobotType entry in _types.Values)
                entry.Clear();
            _robots.Clear();

            foreach ((IRobotType entry, Robot robot) in built)
            {
                entry.Restore(robot);
                _robots.Add(robot);
            }
        }

        private RobotKind KindOf(Robot robot)
        {
            foreach (var pair in _types)
            {
                if (pair.Value.Owns(robot))
                    return pair.Key;
            }
            throw new InvalidOperationException(Resources.World_UnknownKind);
        }

        private static bool TryParseName<TEnum>(string text, out TEnum value) where TEnum : struct, Enum
        {
            value = default;
            string trimmed = text.Trim();
            if (trimmed.Length == 0 || int.TryParse(trimmed, out _))
                return false;
            return Enum.TryParse(trimmed, ignoreCase: true, out value) && Enum.IsDefined(value);
        }

        public string ChangeGroup(Robot? robot, RobotGroup group)
        {
            if (robot == null) return Resources.World_NoRobot;
            return robot.ChangeGroup(group);
        }
    }
}
