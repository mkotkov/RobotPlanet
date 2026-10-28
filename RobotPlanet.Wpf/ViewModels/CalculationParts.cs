using System.Collections.ObjectModel;
using RobotPlanet.Core.computation;
using RobotPlanet.Core.entities;
using RobotPlanet.Wpf.Properties;

namespace RobotPlanet.Wpf.ViewModels
{
    internal static class Fmt
    {
        public static string Seconds(TimeSpan time) => time.TotalSeconds.ToString("F2");
    }

    /// <summary>One robot in the "choose objects" list: the robot plus a tick box.</summary>
    public sealed class CalculationTarget : ViewModelBase
    {
        private readonly Action _selectionChanged;
        private bool _isSelected;

        public CalculationTarget(Robot robot, Action selectionChanged)
        {
            Robot = robot ?? throw new ArgumentNullException(nameof(robot));
            _selectionChanged = selectionChanged ?? throw new ArgumentNullException(nameof(selectionChanged));
        }

        public Robot Robot { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                    _selectionChanged();
            }
        }
    }

    /// <summary>One report row, already formatted for the screen.</summary>
    public sealed record OutcomeRow(
        string Name,
        string CalculationTitle,
        CalculationStatus Status,
        string StatusText,
        string Message,
        string DurationText)
    {
        public static OutcomeRow From(CalculationOutcome outcome) => new(
            outcome.Name,
            outcome.CalculationTitle,
            outcome.Status,
            StatusLabel(outcome.Status),
            outcome.Message,
            Fmt.Seconds(outcome.Duration) + " s");

        public static string StatusLabel(CalculationStatus status) => status switch
        {
            CalculationStatus.Success => Resources.Status_Success,
            CalculationStatus.Failed => Resources.Status_Failed,
            _ => Resources.Status_Cancelled
        };
    }

    /// <summary>The report table of ONE mode (sequential or parallel).</summary>
    public sealed class ModeReportViewModel : ViewModelBase
    {
        private readonly ObservableCollection<OutcomeRow> _rows = new();
        private string _totalText = "";
        private string _countsText = "";

        public ModeReportViewModel(string title)
        {
            Title = title;
            Rows = new ReadOnlyObservableCollection<OutcomeRow>(_rows);
        }

        public string Title { get; }
        public ReadOnlyObservableCollection<OutcomeRow> Rows { get; }
        public bool HasRows => _rows.Count > 0;

        public string TotalText
        {
            get => _totalText;
            private set => SetProperty(ref _totalText, value);
        }

        public string CountsText
        {
            get => _countsText;
            private set => SetProperty(ref _countsText, value);
        }

        public void Reset()
        {
            _rows.Clear();
            TotalText = "";
            CountsText = "";
            OnPropertyChanged(nameof(HasRows));
        }

        /// <summary>While the run is going: show each object as soon as it finishes.</summary>
        public void AddLive(OutcomeRow row)
        {
            _rows.Add(row);
            OnPropertyChanged(nameof(HasRows));
        }

        /// <summary>When the run is over: replace the live rows with the final report in the original order.</summary>
        public void ShowFinal(RunReport report)
        {
            _rows.Clear();
            foreach (CalculationOutcome outcome in report.Outcomes)
                _rows.Add(OutcomeRow.From(outcome));

            TotalText = string.Format(Resources.Report_Total, Fmt.Seconds(report.TotalDuration));
            CountsText = string.Format(Resources.Report_Counts, report.SuccessCount, report.FailedCount, report.CancelledCount);
            OnPropertyChanged(nameof(HasRows));
        }
    }
}
