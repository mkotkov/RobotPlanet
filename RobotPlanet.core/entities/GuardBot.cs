using RobotPlanet.Core.Properties;
using RobotPlanet.Core.@interface;

namespace RobotPlanet.Core.entities
{
    public class GuardBot : Robot, IScan
    {
        public GuardBot(int id, string name, int battery = MaxLevel, int integrity = MaxLevel)
            : base(id, name, battery, integrity) { }

        public override string DoWork() => string.Format(Resources.Guard_DoWork, Name);

        public override string CrazyAction()
        {
            if (Integrity < 30)
                return string.Format(Resources.Guard_Crazy_Sleep, Name);

            TryDamage(5);
            return string.Format(Resources.Guard_Crazy_Ok, Name, Integrity);
        }

        public string Scan() => string.Format(Resources.Guard_Scan, Name);
    }
}