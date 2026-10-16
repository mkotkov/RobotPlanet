using RobotPlanet.Core.entities;
using RobotPlanet.Core.@interface;
using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core.@interface
{
    public class InspectorBot : Robot, IChargeable, IScan
    {
        public InspectorBot(int id, string name, int battery = MaxLevel, int integrity = MaxLevel)
            : base(id, name, battery, integrity) { }

        public string Scan() => string.Format(Resources.Inspector_Scan, Name);
        public override string DoWork()
        {
            if (!TrySpendBattery(5))
                return string.Format(Resources.Inspector_DoWork_LowBattery, Name);
            return string.Format(Resources.Inspector_DoWork_Ok, Name, Battery);
        }

        public override string CrazyAction()
        {
            if (Integrity < 20)
                return string.Format(Resources.Inspector_Crazy_TooDamaged, Name);
            if (!TrySpendBattery(10))
                return string.Format(Resources.Inspector_Crazy_LowBattery, Name);

            TryDamage(5);
            return string.Format(Resources.Inspector_Crazy_Ok, Name, Battery, Integrity);
        }

        public string Charge(int amount)
        {
            if (!TryAddBattery(amount))
                return string.Format(Resources.Msg_Charge_Invalid, Name, amount);
            return string.Format(Resources.Inspector_Charge_Ok, Name, Battery);
        }

        public override string ReactToCrazyAction(Robot actor)
        {
            TryRestoreIntegrity(3);
            return string.Format(Resources.Inspector_React_Crazy, Name, actor.Name, Integrity);
        }

        public override string ReactToRemoved(Robot departed) =>
            string.Format(Resources.Inspector_React_Removed, Name, departed.Name);
    }
}