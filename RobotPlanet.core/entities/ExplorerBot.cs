using RobotPlanet.Core.@interface;
using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core.entities
{
    public class ExplorerBot : Robot, IChargeable, IScan
    {
        public ExplorerBot(int id, string name, int battery = MaxLevel, int integrity = MaxLevel)
            : base(id, name, battery, integrity) { }

        public override string DoWork()
        {
            if (!TrySpendBattery(10))
                return string.Format(Resources.Explorer_DoWork_LowBattery, Name);
            return string.Format(Resources.Explorer_DoWork_Ok, Name, Battery);
        }

        public override string CrazyAction()
        {
            if (!TrySpendBattery(20))
                return string.Format(Resources.Explorer_Crazy_LowBattery, Name);
            return string.Format(Resources.Explorer_Crazy_Ok, Name, Battery);
        }

        public string Scan() =>
            Battery < 20
                ? string.Format(Resources.Explorer_Scan_LowBattery, Name)
                : string.Format(Resources.Explorer_Scan_Ok, Name, 0, 0);

        public string Charge(int amount)
        {
            if (!TryAddBattery(amount))
                return string.Format(Resources.Msg_Charge_Invalid, Name, amount);
            return string.Format(Resources.Explorer_Charge_Ok, Name, Battery);
        }
    }
}