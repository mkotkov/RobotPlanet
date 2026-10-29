using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using RobotPlanet.Core;
using RobotPlanet.Core.computation;
using RobotPlanet.Core.entities;
using RobotPlanet.Wpf.Properties;

namespace RobotPlanet.Wpf.ViewModels
{
    public enum RunKind
    {
        Sequential,
        Parallel,
        Both
    }

    /// <summary>
    /// Vol.3: the user picks objects and a workload, runs the CPU calculations sequentially and/or in parallel,
    /// can cancel, and sees a report per mode and a comparison.
    /// The heavy work runs in Core on thread-pool threads. This class only starts it and shows the results,
    /// and it only ever changes its collections and properties on the UI thread.
    /// </summary>
    public class CalculationViewModel : ViewModelBase
    {
        public const int MinWorkload = 1;
        public static int MaxWorkload => RobotCalculationRunner.MaxWorkload;

        private readonly RobotWorld _world;
        private readonly RobotCalculationRunner _runner;
        private readonly ObservableCollection<CalculationTarget> _targets = new();

        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private bool _isCancelling;
        private int _workload = 10;
        private string _statusText = Resources.Run_Status_Idle;
        private string _elapsedText = "";
        private string _currentModeText = "";
        private int _progressValue;
        private int _progressMaximum = 1;
        private string _comparisonText = Resources.Compare_Hint;
        private string _speedupText = "\u2013";
        private RunReport? _sequentialRun;
        private RunReport? _parallelRun;
        private RunMode? _liveMode;

        public CalculationViewModel(RobotWorld world) : this(world, new RobotCalculationRunner())
        {
        }

        public CalculationViewModel(RobotWorld world, RobotCalculationRunner runner)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));

            Targets = new ReadOnlyObservableCollection<CalculationTarget>(_targets);
            SequentialReport = new ModeReportViewModel(Resources.Mode_Sequential);
            ParallelReport = new ModeReportViewModel(Resources.Mode_Parallel);

            foreach (Robot robot in _world.Robots)
                _targets.Add(new CalculationTarget(robot, OnSelectionChanged));
            ((INotifyCollectionChanged)_world.Robots).CollectionChanged += OnRobotsChanged;

            // CanExecute: you can only start when objects are chosen and nothing is running;
            // you can only cancel while something is running.
            RunSequentialCommand = new RelayCommand(() => _ = RunAsync(RunKind.Sequential), CanStart);
            RunParallelCommand = new RelayCommand(() => _ = RunAsync(RunKind.Parallel), CanStart);
            RunBothCommand = new RelayCommand(() => _ = RunAsync(RunKind.Both), CanStart);
            CancelCommand = new RelayCommand(Cancel, () => IsRunning && !IsCancelling);
            SelectAllCommand = new RelayCommand(() => SetAllSelected(true), () => !IsRunning);
            ClearSelectionCommand = new RelayCommand(() => SetAllSelected(false), () => !IsRunning);
        }

        // ---------------- What the View shows ----------------

        /// <summary>Slider limits. XAML binds to these, so the number 50 lives only in Core.</summary>
        public int WorkloadMin => MinWorkload;
        public int WorkloadMax => MaxWorkload;

        public ReadOnlyObservableCollection<CalculationTarget> Targets { get; }
        public ModeReportViewModel SequentialReport { get; }
        public ModeReportViewModel ParallelReport { get; }

        public int Workload
        {
            get => _workload;
            set => SetProperty(ref _workload, Math.Clamp(value, MinWorkload, MaxWorkload));
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (!SetProperty(ref _isRunning, value)) return;
                OnPropertyChanged(nameof(CanEdit));
                RaiseCommandsChanged();
            }
        }

        public bool IsCancelling
        {
            get => _isCancelling;
            private set
            {
                if (SetProperty(ref _isCancelling, value))
                    CancelCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>The selection and the workload can only be changed while nothing is running.</summary>
        public bool CanEdit => !IsRunning;

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        /// <summary>Live stopwatch for the whole run. It keeps ticking, which shows that the UI is not frozen.</summary>
        public string ElapsedText
        {
            get => _elapsedText;
            private set => SetProperty(ref _elapsedText, value);
        }

        public string CurrentModeText
        {
            get => _currentModeText;
            private set => SetProperty(ref _currentModeText, value);
        }

        public int ProgressValue
        {
            get => _progressValue;
            private set => SetProperty(ref _progressValue, value);
        }

        public int ProgressMaximum
        {
            get => _progressMaximum;
            private set => SetProperty(ref _progressMaximum, value);
        }

        public string ComparisonText
        {
            get => _comparisonText;
            private set => SetProperty(ref _comparisonText, value);
        }

        public string SpeedupText
        {
            get => _speedupText;
            private set => SetProperty(ref _speedupText, value);
        }

        // ---------------- Commands ----------------

        public RelayCommand RunSequentialCommand { get; }
        public RelayCommand RunParallelCommand { get; }
        public RelayCommand RunBothCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand SelectAllCommand { get; }
        public RelayCommand ClearSelectionCommand { get; }

        private bool CanStart() => !IsRunning && _targets.Any(t => t.IsSelected);

        private void Cancel()
        {
            if (_cts == null || IsCancelling) return;

            IsCancelling = true;
            StatusText = Resources.Run_Status_Cancelling;
            _cts.Cancel();      // ONE token reaches every calculation
        }

        private void SetAllSelected(bool selected)
        {
            foreach (CalculationTarget target in _targets)
                target.IsSelected = selected;
        }

        // ---------------- Running ----------------

        /// <summary>
        /// Starts the chosen mode(s). Everything is caught inside, so the buttons can safely
        /// fire and forget (no exception can escape from this async method and crash the app).
        /// </summary>
        public async Task RunAsync(RunKind kind)
        {
            if (IsRunning) return;

            List<Robot> robots = _targets.Where(t => t.IsSelected).Select(t => t.Robot).ToList();
            if (robots.Count == 0) return;

            // Take the snapshots NOW, on the UI thread. The background threads will only see these copies,
            // never the live Robot objects the screen is bound to. Both modes use the SAME snapshots and workload.
            List<RobotCalculationJob> jobs = robots.Select(RobotCalculationJob.From).ToList();
            int workload = Workload;
            RunMode[] modes = kind switch
            {
                RunKind.Sequential => new[] { RunMode.Sequential },
                RunKind.Parallel => new[] { RunMode.Parallel },
                _ => new[] { RunMode.Sequential, RunMode.Parallel }
            };

            _cts = new CancellationTokenSource();
            var tickerStop = new CancellationTokenSource();
            var watch = Stopwatch.StartNew();
            ElapsedText = FormatElapsed(watch);
            IsCancelling = false;
            IsRunning = true;
            Task ticker = TickAsync(watch, tickerStop.Token);

            try
            {
                foreach (RunMode mode in modes)
                {
                    BeginMode(mode, jobs.Count);

                    // Progress<T> was created on the UI thread, so its callback runs on the UI thread too.
                    var progress = new Progress<CalculationOutcome>(outcome => OnProgress(mode, outcome));
                    RunReport report = await _runner.RunAsync(jobs, workload, mode, _cts.Token, progress);

                    FinishMode(report);
                    if (report.WasCancelled) break;      // no point in starting the next mode
                }

                StatusText = _cts.IsCancellationRequested ? Resources.Run_Status_Cancelled : Resources.Run_Status_Done;
            }
            catch (Exception ex)
            {
                StatusText = string.Format(Resources.Run_Status_Error, ex.Message);
            }
            finally
            {
                tickerStop.Cancel();
                await ticker;
                tickerStop.Dispose();
                watch.Stop();
                ElapsedText = FormatElapsed(watch);

                _liveMode = null;
                _cts.Dispose();
                _cts = null;
                IsCancelling = false;
                IsRunning = false;
            }
        }

        private void BeginMode(RunMode mode, int total)
        {
            _liveMode = mode;
            ProgressMaximum = total;
            ProgressValue = 0;
            CurrentModeText = ModeTitle(mode);
            StatusText = string.Format(Resources.Run_Status_Running, CurrentModeText, 0, total);

            ReportOf(mode).Reset();
            if (mode == RunMode.Sequential) _sequentialRun = null; else _parallelRun = null;
            UpdateComparison();
        }

        private void OnProgress(RunMode mode, CalculationOutcome outcome)
        {
            if (_liveMode != mode) return;      // a late callback of a mode that is already finished

            ProgressValue++;
            ReportOf(mode).AddLive(OutcomeRow.From(outcome));
            if (!IsCancelling)
                StatusText = string.Format(Resources.Run_Status_Running, ModeTitle(mode), ProgressValue, ProgressMaximum);
        }

        private void FinishMode(RunReport report)
        {
            _liveMode = null;
            ReportOf(report.Mode).ShowFinal(report);
            if (report.Mode == RunMode.Sequential) _sequentialRun = report; else _parallelRun = report;
            UpdateComparison();
        }

        private async Task TickAsync(Stopwatch watch, CancellationToken stop)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            try
            {
                while (await timer.WaitForNextTickAsync(stop))
                    ElapsedText = FormatElapsed(watch);
            }
            catch (OperationCanceledException)
            {
                // the run is over
            }
        }

        // ---------------- Comparison ----------------

        private void UpdateComparison()
        {
            if (_sequentialRun == null || _parallelRun == null)
            {
                ComparisonText = Resources.Compare_Hint;
                SpeedupText = "\u2013";
                return;
            }

            var comparison = new RunComparison(_sequentialRun, _parallelRun);
            if (!comparison.SameInput)
            {
                ComparisonText = Resources.Compare_Note_Input;
                SpeedupText = "\u2013";
            }
            else if (!comparison.IsComparable)
            {
                ComparisonText = Resources.Compare_Note_Cancelled;
                SpeedupText = "\u2013";
            }
            else
            {
                string speedup = comparison.Speedup.ToString("F2");
                SpeedupText = "\u00D7" + speedup;
                ComparisonText = string.Join(Environment.NewLine,
                    string.Format(Resources.Compare_Sequential, Fmt.Seconds(_sequentialRun.TotalDuration)),
                    string.Format(Resources.Compare_Parallel, Fmt.Seconds(_parallelRun.TotalDuration)),
                    string.Format(Resources.Compare_Speedup, speedup),
                    comparison.ResultsMatch ? Resources.Compare_Match_Yes : Resources.Compare_Match_No);
            }
        }

        // ---------------- Helpers ----------------

        private ModeReportViewModel ReportOf(RunMode mode) =>
            mode == RunMode.Sequential ? SequentialReport : ParallelReport;

        private static string ModeTitle(RunMode mode) =>
            mode == RunMode.Sequential ? Resources.Mode_Sequential : Resources.Mode_Parallel;

        private static string FormatElapsed(Stopwatch watch) =>
            string.Format(Resources.Run_Elapsed, watch.Elapsed.TotalSeconds.ToString("F1"));

        private void OnSelectionChanged() => RaiseCommandsChanged();

        private void RaiseCommandsChanged()
        {
            RunSequentialCommand.RaiseCanExecuteChanged();
            RunParallelCommand.RaiseCanExecuteChanged();
            RunBothCommand.RaiseCanExecuteChanged();
            CancelCommand.RaiseCanExecuteChanged();
            SelectAllCommand.RaiseCanExecuteChanged();
            ClearSelectionCommand.RaiseCanExecuteChanged();
        }

        /// <summary>Keeps the list of choosable objects in step with the world (robots added or removed).</summary>
        private void OnRobotsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (Robot removed in e.OldItems.OfType<Robot>())
                {
                    CalculationTarget? target = _targets.FirstOrDefault(t => ReferenceEquals(t.Robot, removed));
                    if (target != null) _targets.Remove(target);
                }
            }

            if (e.NewItems != null)
            {
                foreach (Robot added in e.NewItems.OfType<Robot>())
                    _targets.Add(new CalculationTarget(added, OnSelectionChanged));
            }

            RaiseCommandsChanged();
        }
    }
}
