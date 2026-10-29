using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core.computation
{
    /// <summary>
    /// Template method: the base class checks the common precondition (the robot must be operational),
    /// then each subclass runs its own CPU-heavy algorithm in Compute.
    /// </summary>
    public abstract class RobotCalculation : IRobotCalculation
    {
        public abstract string Title { get; }

        public CalculationOutput Run(RobotSnapshot snapshot, int workload, CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            // A broken robot cannot be simulated. This is the "failure" case of the report.
            if (!snapshot.IsOperational)
                throw new InvalidOperationException(string.Format(Resources.Calc_NotOperational, snapshot.Name));

            token.ThrowIfCancellationRequested();
            return Compute(snapshot, workload, token);
        }

        protected abstract CalculationOutput Compute(RobotSnapshot snapshot, int workload, CancellationToken token);

        /// <summary>Every robot gets its own random generator, so results are identical in both run modes.</summary>
        protected static Random CreateRandom(RobotSnapshot snapshot, int workload) =>
            new(snapshot.Id * 7919 + workload);
    }

    /// <summary>ExplorerBot: compares many random route variants through waypoints and keeps the shortest.</summary>
    public sealed class RouteComparisonCalculation : RobotCalculation
    {
        public override string Title => Resources.Calc_Cleaning_Title;

        protected override CalculationOutput Compute(RobotSnapshot snapshot, int workload, CancellationToken token)
        {
            int points = 8 + snapshot.Battery / 20;              // 8..13 waypoints: depends on the snapshot
            long variants = (long)workload * 300_000;
            Random random = CreateRandom(snapshot, workload);

            var x = new double[points];
            var y = new double[points];
            for (int i = 0; i < points; i++)
            {
                x[i] = random.NextDouble() * 100;
                y[i] = random.NextDouble() * 100;
            }

            var distance = new double[points, points];
            for (int i = 0; i < points; i++)
                for (int j = 0; j < points; j++)
                    distance[i, j] = Math.Sqrt((x[i] - x[j]) * (x[i] - x[j]) + (y[i] - y[j]) * (y[i] - y[j]));

            var order = new int[points];
            for (int i = 0; i < points; i++) order[i] = i;

            double best = double.MaxValue;
            for (long variant = 0; variant < variants; variant++)
            {
                token.ThrowIfCancellationRequested();

                // Fisher-Yates shuffle = one random route variant
                for (int i = points - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }

                double length = 0;
                for (int i = 1; i < points; i++)
                    length += distance[order[i - 1], order[i]];

                if (length < best) best = length;
            }

            best = Math.Round(best, 2);
            return new CalculationOutput(best,
                string.Format(Resources.Calc_Cleaning_Title, points, best.ToString("F2"), variants.ToString("N0")));
        }
    }

    /// <summary>CleanerBot: repeated simulation of a random cleaning walk on a grid; the battery limits the steps.</summary>
    public sealed class CleaningSimulationCalculation : RobotCalculation
    {
        private const int Size = 20;
        private static readonly int[] Dx = { 1, -1, 0, 0 };
        private static readonly int[] Dy = { 0, 0, 1, -1 };

        public override string Title => Resources.Calc_Cleaning_Title;

        protected override CalculationOutput Compute(RobotSnapshot snapshot, int workload, CancellationToken token)
        {
            int simulations = workload * 2_500;
            int maxSteps = snapshot.Battery * 12;                // more battery = longer cleaning
            int targetCells = Size * Size * 60 / 100;            // goal: clean 60 % of the room
            Random random = CreateRandom(snapshot, workload);
            var visited = new bool[Size * Size];

            double totalCoverage = 0;
            long totalSteps = 0;

            for (int run = 0; run < simulations; run++)
            {
                token.ThrowIfCancellationRequested();

                Array.Clear(visited);
                int x = Size / 2, y = Size / 2;
                visited[y * Size + x] = true;
                int covered = 1, steps = 0;

                while (steps < maxSteps && covered < targetCells)
                {
                    token.ThrowIfCancellationRequested();

                    int direction = random.Next(4);
                    x = Math.Clamp(x + Dx[direction], 0, Size - 1);
                    y = Math.Clamp(y + Dy[direction], 0, Size - 1);
                    steps++;

                    if (!visited[y * Size + x])
                    {
                        visited[y * Size + x] = true;
                        covered++;
                    }
                }

                totalCoverage += covered * 100.0 / (Size * Size);
                totalSteps += steps;
            }

            double averageCoverage = Math.Round(totalCoverage / simulations, 2);
            long averageSteps = totalSteps / simulations;
            return new CalculationOutput(averageCoverage,
                string.Format(Resources.Calc_Cleaning_Result, averageCoverage.ToString("F2"), averageSteps, simulations.ToString("N0")));
        }
    }

    /// <summary>GuardBot: simulates patrol ticks on a ring of checkpoints; integrity decides the detection chance.</summary>
    public sealed class PatrolSimulationCalculation : RobotCalculation
    {
        private const int Checkpoints = 16;
        private const int IntruderStays = 12;

        public override string Title => Resources.Calc_Patrol_Title;

        protected override CalculationOutput Compute(RobotSnapshot snapshot, int workload, CancellationToken token)
        {
            long ticks = (long)workload * 4_000_000;
            double detectChance = snapshot.Integrity / 100.0;     // damaged guards notice less
            Random random = CreateRandom(snapshot, workload);

            int guard = 0, intruder = 0, ticksLeft = 0;
            long intruders = 0, detected = 0;
            bool spotted = false;

            for (long tick = 0; tick < ticks; tick++)
            {
                token.ThrowIfCancellationRequested();

                guard = (guard + 1) % Checkpoints;

                if (ticksLeft == 0)
                {
                    if (random.NextDouble() < 0.01)
                    {
                        intruder = random.Next(Checkpoints);
                        ticksLeft = IntruderStays;
                        spotted = false;
                        intruders++;
                    }
                }
                else
                {
                    ticksLeft--;
                    int gap = Math.Abs(guard - intruder);
                    gap = Math.Min(gap, Checkpoints - gap);
                    if (!spotted && gap <= 1 && random.NextDouble() < detectChance)
                    {
                        spotted = true;
                        detected++;
                        ticksLeft = 0;
                    }
                }
            }

            double rate = intruders == 0 ? 0 : Math.Round(detected * 100.0 / intruders, 2);
            return new CalculationOutput(rate,
                string.Format(Resources.Calc_Patrol_Result, rate.ToString("F2"), intruders.ToString("N0"), ticks.ToString("N0")));
        }
    }

    /// <summary>
    /// RepairBot and the default for every other robot type (also types added later):
    /// a Monte Carlo wear forecast. How likely is the robot to fall below 20 integrity in 200 days?
    /// </summary>
    public sealed class WearForecastCalculation : RobotCalculation
    {
        private const int Days = 200;

        public override string Title => Resources.Calc_Wear_Title;

        protected override CalculationOutput Compute(RobotSnapshot snapshot, int workload, CancellationToken token)
        {
            int forecasts = workload * 15_000;
            double maxDailyWear = 0.5 + (100 - snapshot.Battery) / 80.0;   // low battery wears the robot faster
            Random random = CreateRandom(snapshot, workload);

            long failures = 0;
            double totalEnd = 0;

            for (int forecast = 0; forecast < forecasts; forecast++)
            {
                token.ThrowIfCancellationRequested();

                double integrity = snapshot.Integrity;
                bool failed = false;

                for (int day = 0; day < Days; day++)
                {
                    token.ThrowIfCancellationRequested();

                    integrity -= random.NextDouble() * maxDailyWear;
                    if (random.NextDouble() < 0.02) integrity += 5;          // maintenance visit
                    integrity = Math.Clamp(integrity, 0, 100);

                    if (integrity < 20 && !failed)
                    {
                        failed = true;
                        failures++;
                    }
                }

                totalEnd += integrity;
            }

            double probability = Math.Round(failures * 100.0 / forecasts, 2);
            double averageEnd = Math.Round(totalEnd / forecasts, 1);
            return new CalculationOutput(probability,
                string.Format(Resources.Calc_Wear_Result, Days, probability.ToString("F2"), averageEnd.ToString("F1"), forecasts.ToString("N0")));
        }
    }
}
