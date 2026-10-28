using RobotPlanet.Core.entities;

namespace RobotPlanet.Core.computation
{
    /// <summary>
    /// An immutable copy of a robot's data taken at one moment (hetktõmmis).
    /// Background threads only ever read this copy and never touch the live Robot object,
    /// which the UI is bound to and which changes while the computation runs.
    /// </summary>
    public sealed record RobotSnapshot(int Id, string Name, string Kind, int Battery, int Integrity)
    {
        public bool IsOperational => Battery > 0 && Integrity > 0;

        public static RobotSnapshot From(Robot robot)
        {
            ArgumentNullException.ThrowIfNull(robot);
            return new RobotSnapshot(robot.Id, robot.Name, robot.Kind, robot.Battery, robot.Integrity);
        }
    }

    /// <summary>The result of one calculation: a number (for comparing the two modes) and a readable text.</summary>
    public sealed record CalculationOutput(double Value, string Description);

    /// <summary>One CPU-heavy calculation. It must be pure: same snapshot + same workload = same output.</summary>
    public interface IRobotCalculation
    {
        string Title { get; }

        /// <summary>Must check <paramref name="token"/> in every loop and throw OperationCanceledException.</summary>
        CalculationOutput Run(RobotSnapshot snapshot, int workload, CancellationToken token);
    }

    /// <summary>What the runner executes: the snapshot plus the calculation this robot type has chosen.</summary>
    public sealed record RobotCalculationJob(RobotSnapshot Snapshot, IRobotCalculation Calculation)
    {
        /// <summary>Takes the snapshot NOW (call it on the UI thread, before starting any background work).</summary>
        public static RobotCalculationJob From(Robot robot) =>
            new(RobotSnapshot.From(robot), robot.CreateCalculation());
    }
}