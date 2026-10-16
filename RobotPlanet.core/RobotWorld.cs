using RobotPlanet.Core.entities;
using RobotPlanet.Core.events;
using RobotPlanet.Core.@interface;
using RobotPlanet.Core.Properties;
using RobotPlanet.Core.repository;
using System.Collections.ObjectModel;

namespace RobotPlanet.Core
{
    /// <summary>
    /// The world owns ONE repository per robot type and forwards all reactions
    /// through a single event, so the ViewModel only has to subscribe once.
    /// </summary>
    public class RobotWorld
    {
        private readonly Dictionary<RobotKind, IRobotType> _types = new();
        private readonly ObservableCollection<Robot> _robots = new();
        private readonly ObservableCollection<IRobotRepository> _repositories = new();

        public RobotWorld()
        {
            Robots = new ReadOnlyObservableCollection<Robot>(_robots);
            Repositories = new ReadOnlyObservableCollection<IRobotRepository>(_repositories);

            // One line per robot type. A new type = new class + new RobotKind value + one line here.
            Register(RobotKind.Cleaner, (id, name, battery, integrity) => new CleanerBot(id, name, battery, integrity));
            Register(RobotKind.Explorer, (id, name, battery, integrity) => new ExplorerBot(id, name, battery, integrity));
            Register(RobotKind.Repair, (id, name, battery, integrity) => new RepairBot(id, name, battery, integrity));
            Register(RobotKind.Guard, (id, name, battery, integrity) => new GuardBot(id, name, battery, integrity));
            Register(RobotKind.Jester, (id, name, battery, integrity) => new JesterBot(id, name, battery, integrity));

            // three new robots: Delivery, Inspector, Rescue
            Register(RobotKind.Delivery, (id, name, battery, integrity) => new DeliveryBot(id, name, battery, integrity));
            Register(RobotKind.Inspector, (id, name, battery, integrity) => new InspectorBot(id, name, battery, integrity));
            Register(RobotKind.Rescue, (id, name, battery, integrity) => new RescueBot(id, name, battery, integrity));
        }

        /// <summary>All robots of all types (handy for repair targets and summaries).</summary>
        public ReadOnlyObservableCollection<Robot> Robots { get; }

        /// <summary>One read-only repository view per robot type.</summary>
        public ReadOnlyObservableCollection<IRobotRepository> Repositories { get; }

        /// <summary>Every reaction of every repository, for the log.</summary>
        public event EventHandler<ReactionEventArgs>? ReactionOccurred;

        // ---- Registration ----
        private void Register<T>(RobotKind kind, Func<int, string, int, int, T> factory) where T : Robot
        {
            var type = new RobotType<T>(factory);
            type.Repository.ReactionOccurred += (_, e) => ReactionOccurred?.Invoke(this, e);
            _types.Add(kind, type);
            _repositories.Add(type.Repository);
        }

        // ---- Domain operations ----
        public bool TryAdd(RobotKind kind, int id, string name, int battery, int integrity, out string message)
        {
            if (!_types.TryGetValue(kind, out IRobotType? type))
            {
                message = Resources.World_UnknownKind;
                return false;
            }
            if (_robots.Any(r => r.Id == id))
            {
                message = string.Format(Resources.World_DuplicateId, id);
                return false;
            }

            try
            {
                Robot robot = type.CreateAndAdd(id, name, battery, integrity);
                _robots.Add(robot);
                message = string.Format(Resources.World_Added, robot);
                return true;
            }
            catch (ArgumentException ex)
            {
                // ArgumentException appends " (Parameter 'x')", so cut it off.
                message = ex.Message.Split(" (Parameter", 2)[0];
                return false;
            }
            catch (InvalidOperationException ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public string Remove(Robot? robot)
        {
            if (robot == null) return Resources.World_NoRobot;

            IRobotType? type = FindType(robot);
            if (type == null || !type.Remove(robot))
                return string.Format(Resources.Repo_NotInRepository, robot.Name);

            _robots.Remove(robot);
            return string.Format(Resources.World_Removed, robot);
        }

        /// <summary>Runs the robot's CrazyAction through its repository so the others can react.</summary>
        public string CrazyAction(Robot? robot)
        {
            if (robot == null) return Resources.World_NoRobot;

            IRobotType? type = FindType(robot);
            return type == null
                ? string.Format(Resources.Repo_NotInRepository, robot.Name)
                : type.TriggerCrazyAction(robot);
        }

        public string Charge(Robot? robot, int amount)
        {
            if (robot == null) return Resources.World_NoRobot;
            if (robot is IChargeable chargeable)
                return chargeable.Charge(amount);
            return string.Format(Resources.World_NotChargeable, robot.Name);
        }

        public string Scan(Robot? robot)
        {
            if (robot == null) return Resources.World_NoRobot;
            IScan? scanner = robot as IScan;
            return scanner != null
                ? scanner.Scan()
                : string.Format(Resources.World_NotScanner, robot.Name);
        }

        public string Repair(Robot? healer, Robot? target)
        {
            if (healer == null) return Resources.World_NoRobot;
            if (target == null) return Resources.World_NoTarget;
            if (healer is IRepair repairer)
                return repairer.Repair(target);
            return string.Format(Resources.World_NotRepairer, healer.Name);
        }

        private IRobotType? FindType(Robot robot) => _types.Values.FirstOrDefault(t => t.Owns(robot));

        // ---- Hides the generic parameter so different Repository<T> can live in one dictionary ----
        private interface IRobotType
        {
            bool Owns(Robot robot);
            Robot CreateAndAdd(int id, string name, int battery, int integrity);
            bool Remove(Robot robot);
            string TriggerCrazyAction(Robot robot);
        }

        private sealed class RobotType<T> : IRobotType where T : Robot
        {
            private readonly Func<int, string, int, int, T> _factory;

            public RobotType(Func<int, string, int, int, T> factory)
            {
                _factory = factory;
            }

            public Repository<T> Repository { get; } = new();

            // Exact type match, so a CleanerBot never ends up in another type's repository.
            public bool Owns(Robot robot) => robot.GetType() == typeof(T);

            public Robot CreateAndAdd(int id, string name, int battery, int integrity)
            {
                T robot = _factory(id, name, battery, integrity);   // typed: no cast needed
                Repository.Add(robot);
                return robot;
            }

            public bool Remove(Robot robot) => Owns(robot) && Repository.Remove((T)robot);

            public string TriggerCrazyAction(Robot robot) => Repository.TriggerCrazyAction((T)robot);
        }
    }
}