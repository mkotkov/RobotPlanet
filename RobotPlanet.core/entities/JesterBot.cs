using RobotPlanet.Core.@interface;
using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core.entities
{
    public class JesterBot : Robot, IScan, IChargeable
    {
        public JesterBot(int id, string name, int battery = MaxLevel, int integrity = MaxLevel)
            : base(id, name, battery, integrity) { }

        public override string DoWork()
        {
            if (!TrySpendBattery(8))
                return string.Format(Resources.Jester_DoWork_LowBattery, Name);

            return string.Format(Resources.Jester_DoWork_Ok, Name, Battery);
        }

        public override string CrazyAction()
        {
            if (Battery < 20)
                return string.Format(Resources.Jester_Crazy_LowBattery, Name);

            TrySpendBattery(20);
            return string.Format(Resources.Jester_Crazy_Ok, Name, Battery, Integrity);
        }

        public string Scan()
        {
            return string.Format(Resources.Jester_Scan, Name);
        }

        public string Charge(int amount)
        {
            if (!TryAddBattery(amount))
                return string.Format(Resources.Msg_Charge_Invalid, Name, amount);
            return string.Format(Resources.Jester_Charge_Ok, Name, Battery);
        }
    }
}