using RobotPlanet.Core.@interface;
using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core.entities
{
    public class CleanerBot : Robot, IChargeable
    {
        public CleanerBot(int id, string name, int battery = MaxLevel, int integrity = MaxLevel)
            : base(id, name, battery, integrity) { }

        public override string DoWork()
        {
            if (!TrySpendBattery(5))
                return string.Format(Resources.Cleaner_DoWork_LowBattery, Name);
            return string.Format(Resources.Cleaner_DoWork_Ok, Name, Battery);
        }

        public override string CrazyAction()
        {
            if (Battery < 15)
                return string.Format(Resources.Cleaner_Crazy_LowBattery, Name);

            TrySpendBattery(15);
            TryDamage(10);
            return string.Format(Resources.Cleaner_Crazy_Ok, Name, Battery, Integrity);
        }

        public string Charge(int amount)
        {
            if (!TryAddBattery(amount))
                return string.Format(Resources.Msg_Charge_Invalid, Name, amount);
            return string.Format(Resources.Cleaner_Charge_Ok, Name, Battery);
        }
    }
}