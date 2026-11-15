using RobotPlanet.Core;

namespace RobotPlanet.Wpf.Services
{
    /// <summary>
    /// The robots the application starts with. It used to be a private method of MainViewModel;
    /// now it is a separate factory, so the ViewModel only receives a finished world from dependency injection.
    /// </summary>
    public static class DemoWorld
    {
        public static RobotWorld Create()
        {
            var world = new RobotWorld();
            world.TryAdd(RobotKind.Cleaner, 1, "Sädel", 100, 100, out _);
            world.TryAdd(RobotKind.Cleaner, 2, "Läige", 100, 100, out _);
            world.TryAdd(RobotKind.Explorer, 3, "Kosmos", 80, 100, out _);
            world.TryAdd(RobotKind.Repair, 4, "Mutrivõti", 100, 60, out _);
            world.TryAdd(RobotKind.Guard, 5, "Vaht", 100, 100, out _);
            world.TryAdd(RobotKind.Jester, 6, "Naljahammas", 90, 100, out _);

            // three new robots: Delivery, Inspector, Rescue
            world.TryAdd(RobotKind.Delivery, 7, "Tarnija", 67, 89, out _);
            world.TryAdd(RobotKind.Inspector, 8, "Inspektor 3000", 78, 56, out _);
            world.TryAdd(RobotKind.Rescue, 9, "Superpäästja", 67, 52, out _);

            return world;
        }
    }
}
