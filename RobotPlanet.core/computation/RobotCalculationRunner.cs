using System.Diagnostics;
using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core.computation
{
    public enum RunMode
    {
        Sequential,
        Parallel
    }

    /// <summary>Final state of one object's calculation.</summary>
    public enum CalculationStatus
    {
        Success,
        Failed,
        Cancelled
    }

    /// <summary>One row of the report: object name, final state, result or error text, duration.</summary>
    public sealed record CalculationOutcome(
        string Name,
        string Kind,
        string CalculationTitle,
        CalculationStatus Status,
        string Message,
        double? Value,
        TimeSpan Duration);

    public sealed record RunReport(
        RunMode Mode,
        int Workload,
        string InputSignature,
        IReadOnlyList<CalculationOutcome> Outcomes,
        TimeSpan TotalDuration,
        bool WasCancelled)
    {
        public int SuccessCount => Outcomes.Count(o => o.Status == CalculationStatus.Success);
        public int FailedCount => Outcomes.Count(o => o.Status == CalculationStatus.Failed);
        public int CancelledCount => Outcomes.Count(o => o.Status == CalculationStatus.Cancelled);
    }

    /// <summary>Compares the sequential and the parallel report of the SAME input.</summary>
    public sealed record RunComparison(RunReport Sequential, RunReport Parallel)
    {
        public bool SameInput => Sequential.InputSignature == Parallel.InputSignature;

        /// <summary>A cancelled run is incomplete, so comparing it with a complete one makes no sense.</summary>
        public bool IsComparable => SameInput && !Sequential.WasCancelled && !Parallel.WasCancelled;

        public bool ResultsMatch =>
            IsComparable &&
            Sequential.Outcomes.Count == Parallel.Outcomes.Count &&
            Sequential.Outcomes.Zip(Parallel.Outcomes).All(pair =>
                pair.First.Name == pair.Second.Name &&
                pair.First.Status == pair.Second.Status &&
                pair.First.Value == pair.Second.Value &&
                pair.First.Message == pair.Second.Message);

        public double Speedup =>
            Parallel.TotalDuration.TotalSeconds > 0
                ? Sequential.TotalDuration.TotalSeconds / Parallel.TotalDuration.TotalSeconds
                : 0;
    }

    public sealed class RobotCalculationRunner
    {
        public const int MaxWorkload = 50;

        /// <summary>
        /// Runs the same jobs one by one (Sequential) or all at once (Parallel).
        /// One shared CancellationToken reaches every calculation. A failing object never stops the others.
        /// </summary>
        public async Task<RunReport> RunAsync(
            IReadOnlyList<RobotCalculationJob> jobs,
            int workload,
            RunMode mode,
            CancellationToken token,
            IProgress<CalculationOutcome>? progress = null)
        {
            ArgumentNullException.ThrowIfNull(jobs);
            if (jobs.Count == 0)
                throw new ArgumentException(Resources.Calc_NoJobs, nameof(jobs));
            if (workload < 1 || workload > MaxWorkload)
                throw new ArgumentOutOfRangeException(nameof(workload), string.Format(Resources.Calc_Workload_Invalid, MaxWorkload));

            var total = Stopwatch.StartNew();
            CalculationOutcome[] outcomes;

            if (mode == RunMode.Sequential)
            {
                var list = new List<CalculationOutcome>(jobs.Count);
                foreach (RobotCalculationJob job in jobs)
                {
                    // Wait for this object to finish before the next one starts.
                    // The work runs on a thread-pool thread, so the caller (the UI) stays free.
                    CalculationOutcome outcome = await Task.Run(() => RunOne(job, workload, token, progress)).ConfigureAwait(false);
                    list.Add(outcome);
                }
                outcomes = list.ToArray();
            }
            else
            {
                // Start every calculation at once with Task.Run and wait for all of them with Task.WhenAll.
                Task<CalculationOutcome>[] tasks = jobs
                    .Select(job => Task.Run(() => RunOne(job, workload, token, progress)))
                    .ToArray();
                outcomes = await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            total.Stop();
            return new RunReport(
                mode,
                workload,
                CreateSignature(jobs, workload),
                outcomes,
                total.Elapsed,
                token.IsCancellationRequested || outcomes.Any(o => o.Status == CalculationStatus.Cancelled));
        }

        /// <summary>
        /// Runs ONE job and turns success, failure and cancellation into an outcome.
        /// No exception leaves this method, so Task.WhenAll never fails and the other jobs carry on.
        /// </summary>
        private static CalculationOutcome RunOne(
            RobotCalculationJob job,
            int workload,
            CancellationToken token,
            IProgress<CalculationOutcome>? progress)
        {
            RobotSnapshot snapshot = job.Snapshot;
            var watch = Stopwatch.StartNew();
            CalculationOutcome outcome;

            if (token.IsCancellationRequested)
            {
                outcome = Create(CalculationStatus.Cancelled, Resources.Calc_Cancelled_BeforeStart, null);
            }
            else
            {
                try
                {
                    CalculationOutput output = job.Calculation.Run(snapshot, workload, token);
                    outcome = Create(CalculationStatus.Success, output.Description, output.Value);
                }
                catch (OperationCanceledException)
                {
                    outcome = Create(CalculationStatus.Cancelled, Resources.Calc_Cancelled_Running, null);
                }
                catch (Exception ex)
                {
                    outcome = Create(CalculationStatus.Failed, ex.Message, null);
                }
            }

            progress?.Report(outcome);
            return outcome;

            CalculationOutcome Create(CalculationStatus status, string message, double? value) =>
                new(snapshot.Name, snapshot.Kind, job.Calculation.Title, status, message, value, watch.Elapsed);
        }

        /// <summary>Identifies the input (objects, their data and the workload) so two reports can be compared.</summary>
        private static string CreateSignature(IReadOnlyList<RobotCalculationJob> jobs, int workload) =>
            workload + "|" + string.Join(";", jobs.Select(j => $"{j.Snapshot.Id}:{j.Snapshot.Battery}:{j.Snapshot.Integrity}"));
    }
}
