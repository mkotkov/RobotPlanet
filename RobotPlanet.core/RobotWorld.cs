using RobotPlanet.Core.entities;
using RobotPlanet.Core.@interface;
using RobotPlanet.Core.Properties;
using System.Collections.ObjectModel;

namespace RobotPlanet.Core
{
    public class RobotWorld
    {
        private static readonly Dictionary<RobotKind, Func<int, string, int, int, Robot>> Factories = new()
        {
            [RobotKind.Cleaner] = (id, n, b, i) => new CleanerBot(id, n, b, i),
            [RobotKind.Explorer] = (id, n, b, i) => new ExplorerBot(id, n, b, i),
            [RobotKind.Repair] = (id, n, b, i) => new RepairBot(id, n, b, i),
            [RobotKind.Guard] = (id, n, b, i) => new GuardBot(id, n, b, i),
        };

        public ObservableCollection<Robot> Robots { get; } = new();

        public bool TryAdd(RobotKind kind, int id, string name, int battery, int integrity, out string message)
        {
            if (Robots.Any(r => r.Id == id))
            {
                message = string.Format(Resources.World_DuplicateId, id);
                return false;
            }
            try
            {
                Robot robot = Factories[kind](id, name, battery, integrity);
                Robots.Add(robot);
                message = string.Format(Resources.World_Added, robot);
                return true;
            }
            catch (ArgumentException ex)
            {
                message = ex.Message.Split(" (Parameter", 2)[0];
                return false;
            }
        }

        public string Remove(Robot? robot)
        {
            if (robot == null) return Resources.World_NoRobot;
            Robots.Remove(robot);
            return string.Format(Resources.World_Removed, robot);
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
    }
}