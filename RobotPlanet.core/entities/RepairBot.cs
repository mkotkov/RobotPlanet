using RobotPlanet.Core.@interface;
using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core.@interface
{
    public class RepairBot : Robot, IChargeable, IRepair
    {
        public RepairBot(int id, string name, int battery = MaxLevel, int integrity = MaxLevel)
            : base(id, name, battery, integrity) { }

        public override string DoWork() => string.Format(Resources.Repair_DoWork, Name);

        public override string CrazyAction()
        {
            if (Integrity < 20)
                return string.Format(Resources.Repair_Crazy_TooDamaged, Name);
            if (!TrySpendBattery(10))
                return string.Format(Resources.Repair_Crazy_LowBattery, Name);

            TryDamage(5);
            return string.Format(Resources.Repair_Crazy_Ok, Name, Battery, Integrity);
        }

        public string Repair(Robot target)
        {
            if (target == null)
                return string.Format(Resources.Repair_Repair_NoTarget, Name);
            if (ReferenceEquals(target, this))
                return string.Format(Resources.Repair_Repair_Self, Name);
            if (!TrySpendBattery(10))
                return string.Format(Resources.Repair_Repair_LowBattery, Name);

            return string.Format(Resources.Repair_Repair_Start, Name, target.ReceiveRepair(20));
        }

        public string Charge(int amount)
        {
            if (!TryAddBattery(amount))
                return string.Format(Resources.Msg_Charge_Invalid, Name, amount);
            return string.Format(Resources.Repair_Charge_Ok, Name, Battery);
        }
    }
}