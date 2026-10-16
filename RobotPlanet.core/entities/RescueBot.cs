using RobotPlanet.Core.entities;
using RobotPlanet.Core.@interface;
using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core.@interface
{
    public class RescueBot : Robot, IChargeable, IScan
    {
        public RescueBot(int id, string name, int battery = MaxLevel, int integrity = MaxLevel)
            : base(id, name, battery, integrity) { }

        public string Scan() => string.Format(Resources.Rescue_Scan, Name);
        public override string DoWork()
        {
            if (!TrySpendBattery(7))
                return string.Format(Resources.Rescue_DoWork_LowBattery, Name);
            return string.Format(Resources.Rescue_DoWork_Ok, Name, Battery);
        }

        public override string CrazyAction()
        {
            if (Integrity < 23)
                return string.Format(Resources.Rescue_Crazy_TooDamaged, Name);
            if (!TrySpendBattery(12))
                return string.Format(Resources.Rescue_Crazy_LowBattery, Name);

            TryDamage(8);
            return string.Format(Resources.Rescue_Crazy_Ok, Name, Battery, Integrity);
        }

        public string Charge(int amount)
        {
            if (!TryAddBattery(amount))
                return string.Format(Resources.Msg_Charge_Invalid, Name, amount);
            return string.Format(Resources.Rescue_Charge_Ok, Name, Battery);
        }

        public override string ReactToCrazyAction(Robot actor)
        {
            TryRestoreIntegrity(6);
            return string.Format(Resources.Rescue_React_Crazy, Name, actor.Name, Integrity);
        }

        public override string ReactToRemoved(Robot departed) =>
            string.Format(Resources.Rescue_React_Removed, Name, departed.Name);
    }
}