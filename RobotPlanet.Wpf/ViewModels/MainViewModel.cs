using RobotPlanet.Core;
using RobotPlanet.Core.entities;
using RobotPlanet.Core.events;
using RobotPlanet.Core.@interface;
using RobotPlanet.Wpf.Properties;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;

namespace RobotPlanet.Wpf.ViewModels
{
    /// <summary>
    /// Holds what the View shows (selected repository and robot, query results, log)
    /// and what the buttons do (commands). It contains NO domain rules: those live in Core.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly RobotWorld _world;
        private readonly ObservableCollection<Robot> _visibleRobots = new();
        private readonly ObservableCollection<GroupSummary> _summary = new();
        private readonly ObservableCollection<string> _log = new();
        private readonly List<string> _pendingReactions = new();

        private RepositoryItem? _selectedRepository;
        private Robot? _selectedRobot;
        private Robot? _repairTarget;
        private string _searchText = "";
        private SortChoice _selectedSort;
        private bool _sortDescending;
        private RobotKind? _newKind;
        private string _newId;
        private string _newName = "";
        private string _newBattery = "100";
        private string _newIntegrity = "100";
        private string _chargeAmount = "30";

        public MainViewModel() : this(CreateDemoWorld())
        {
        }

        public MainViewModel(RobotWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));

            Repositories = new ReadOnlyCollection<RepositoryItem>(
                _world.Repositories.Select(r => new RepositoryItem(r)).ToList());
            VisibleRobots = new ReadOnlyObservableCollection<Robot>(_visibleRobots);
            Summary = new ReadOnlyObservableCollection<GroupSummary>(_summary);
            Log = new ReadOnlyObservableCollection<string>(_log);
            AllRobots = _world.Robots;
            Kinds = Enum.GetValues<RobotKind>();

            SortOptions = new[]
            {
                new SortChoice(SortOption.Name, Resources.Sort_Name),
                new SortChoice(SortOption.Battery, Resources.Sort_Battery),
                new SortChoice(SortOption.Integrity, Resources.Sort_Integrity),
                new SortChoice(SortOption.Id, Resources.Sort_Id)
            };
            _selectedSort = SortOptions[0];
            _newId = SuggestNextId();

            // Commands: the second argument is CanExecute (decides if the button is enabled).
            AddCommand = new RelayCommand(Add, CanAdd);
            RemoveCommand = new RelayCommand(Remove, () => SelectedRobot != null);
            WorkCommand = new RelayCommand(Work, () => SelectedRobot != null);
            CrazyCommand = new RelayCommand(Crazy, () => SelectedRobot != null);
            ChargeCommand = new RelayCommand(Charge, () => SelectedRobot is IChargeable);
            ScanCommand = new RelayCommand(Scan, () => SelectedRobot is IScan);
            RepairCommand = new RelayCommand(Repair, () => SelectedRobot is IRepair && RepairTarget != null);
            ClearLogCommand = new RelayCommand(() => _log.Clear());

            // The world forwards every reaction of every repository through this one event.
            _world.ReactionOccurred += (_, e) => _pendingReactions.Add(e.Message);

            SelectedRepository = Repositories.FirstOrDefault();
            RefreshSummary();
        }

        // ---------------- What the View shows ----------------

        public IReadOnlyList<RepositoryItem> Repositories { get; }
        public ReadOnlyObservableCollection<Robot> VisibleRobots { get; }
        public ReadOnlyObservableCollection<Robot> AllRobots { get; }
        public ReadOnlyObservableCollection<GroupSummary> Summary { get; }
        public ReadOnlyObservableCollection<string> Log { get; }
        public IReadOnlyList<RobotKind> Kinds { get; }
        public IReadOnlyList<SortChoice> SortOptions { get; }

        public RepositoryItem? SelectedRepository
        {
            get => _selectedRepository;
            set
            {
                if (!SetProperty(ref _selectedRepository, value)) return;
                SelectedRobot = null;
                RefreshVisible();
            }
        }

        public Robot? SelectedRobot
        {
            get => _selectedRobot;
            set
            {
                if (SetProperty(ref _selectedRobot, value))
                    RaiseRobotCommandsChanged();
            }
        }

        public Robot? RepairTarget
        {
            get => _repairTarget;
            set
            {
                if (SetProperty(ref _repairTarget, value))
                    RepairCommand.RaiseCanExecuteChanged();
            }
        }

        // ---- Search and sort (the LINQ query is rebuilt whenever one of these changes) ----

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value ?? ""))
                    RefreshVisible();
            }
        }

        public SortChoice SelectedSort
        {
            get => _selectedSort;
            set
            {
                if (value != null && SetProperty(ref _selectedSort, value))
                    RefreshVisible();
            }
        }

        public bool SortDescending
        {
            get => _sortDescending;
            set
            {
                if (SetProperty(ref _sortDescending, value))
                    RefreshVisible();
            }
        }

        // ---- "Add robot" form ----

        public RobotKind? NewKind
        {
            get => _newKind;
            set { if (SetProperty(ref _newKind, value)) AddCommand.RaiseCanExecuteChanged(); }
        }

        public string NewId
        {
            get => _newId;
            set { if (SetProperty(ref _newId, value ?? "")) AddCommand.RaiseCanExecuteChanged(); }
        }

        public string NewName
        {
            get => _newName;
            set { if (SetProperty(ref _newName, value ?? "")) AddCommand.RaiseCanExecuteChanged(); }
        }

        public string NewBattery
        {
            get => _newBattery;
            set => SetProperty(ref _newBattery, value ?? "");
        }

        public string NewIntegrity
        {
            get => _newIntegrity;
            set => SetProperty(ref _newIntegrity, value ?? "");
        }

        public string ChargeAmount
        {
            get => _chargeAmount;
            set => SetProperty(ref _chargeAmount, value ?? "");
        }

        // ---------------- Commands ----------------

        public RelayCommand AddCommand { get; }
        public RelayCommand RemoveCommand { get; }
        public RelayCommand WorkCommand { get; }
        public RelayCommand CrazyCommand { get; }
        public RelayCommand ChargeCommand { get; }
        public RelayCommand ScanCommand { get; }
        public RelayCommand RepairCommand { get; }
        public RelayCommand ClearLogCommand { get; }

        private bool CanAdd() =>
            NewKind != null &&
            !string.IsNullOrWhiteSpace(NewId) &&
            !string.IsNullOrWhiteSpace(NewName);

        private void Add()
        {
            // The View only gives us text. Turning it into numbers is the ViewModel's job;
            // the rules (ranges, unique id, name length) are checked by Core.
            if (NewKind is not RobotKind kind)
            { Run(() => Resources.Err_No_Type); return; }
            if (!int.TryParse(NewId, out int id))
            { Run(() => Resources.Err_Id_NotNumber); return; }
            if (!int.TryParse(NewBattery, out int battery))
            { Run(() => Resources.Err_Battery_NotNumber); return; }
            if (!int.TryParse(NewIntegrity, out int integrity))
            { Run(() => Resources.Err_Integrity_NotNumber); return; }

            bool added = false;
            Run(() =>
            {
                added = _world.TryAdd(kind, id, NewName, battery, integrity, out string message);
                return message;
            });

            if (!added) return;

            // Show the new robot: switch to its repository and make sure no filter hides it.
            Robot newRobot = _world.Robots.Last();
            SelectedRepository = Repositories.First(r => r.Repository.Robots.Contains(newRobot));
            SearchText = "";
            SelectedRobot = newRobot;

            NewId = SuggestNextId();
            NewName = "";
        }

        private void Remove() => Run(() => _world.Remove(SelectedRobot));

        private void Work() => Run(() => SelectedRobot?.DoWork() ?? Resources.Err_No_Selection);

        private void Crazy() => Run(() => _world.CrazyAction(SelectedRobot));

        private void Charge()
        {
            if (!int.TryParse(ChargeAmount, out int amount))
            { Run(() => Resources.Err_Amount_NotNumber); return; }
            Run(() => _world.Charge(SelectedRobot, amount));
        }

        private void Scan() => Run(() => _world.Scan(SelectedRobot));

        private void Repair() => Run(() => _world.Repair(SelectedRobot, RepairTarget));

        // ---------------- Log ----------------

        /// <summary>
        /// Runs one action, then writes its message and every reaction it caused to the log
        /// (newest on top, but each action stays above its own reactions), and refreshes the lists.
        /// </summary>
        private void Run(Func<string> action)
        {
            _pendingReactions.Clear();
            string message = action();

            var lines = new List<string> { $"[{DateTime.Now:HH:mm:ss}] {message}" };
            lines.AddRange(_pendingReactions.Select(r => "      \u21B3 " + r));
            _pendingReactions.Clear();

            for (int i = lines.Count - 1; i >= 0; i--)
                _log.Insert(0, lines[i]);

            AfterAction();
        }

        private void AfterAction()
        {
            // Forget robots that no longer exist.
            if (SelectedRobot != null && !_world.Robots.Contains(SelectedRobot))
                SelectedRobot = null;
            if (RepairTarget != null && !_world.Robots.Contains(RepairTarget))
                RepairTarget = null;

            RefreshVisible();
            RefreshSummary();
            RaiseRobotCommandsChanged();
        }

        private void RaiseRobotCommandsChanged()
        {
            RemoveCommand.RaiseCanExecuteChanged();
            WorkCommand.RaiseCanExecuteChanged();
            CrazyCommand.RaiseCanExecuteChanged();
            ChargeCommand.RaiseCanExecuteChanged();
            ScanCommand.RaiseCanExecuteChanged();
            RepairCommand.RaiseCanExecuteChanged();
        }

        // ---------------- LINQ ----------------

        /// <summary>LINQ 1 (Where) and LINQ 2 (OrderBy / OrderByDescending) on the selected repository.</summary>
        private void RefreshVisible()
        {
            IEnumerable<Robot> query = _selectedRepository?.Repository.Robots ?? Enumerable.Empty<Robot>();

            // Filtering: name or id contains the search text
            string text = SearchText.Trim();
            if (text.Length > 0)
            {
                query = query.Where(r =>
                    r.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
                    r.Id.ToString().Contains(text));
            }

            // Sorting
            query = SelectedSort.Option switch
            {
                SortOption.Battery => SortDescending ? query.OrderByDescending(r => r.Battery) : query.OrderBy(r => r.Battery),
                SortOption.Integrity => SortDescending ? query.OrderByDescending(r => r.Integrity) : query.OrderBy(r => r.Integrity),
                SortOption.Id => SortDescending ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id),
                _ => SortDescending ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name)
            };

            Sync(_visibleRobots, query.ToList());
        }

        /// <summary>LINQ 3 (GroupBy): how many robots of each type, and their average battery and integrity.</summary>
        private void RefreshSummary()
        {
            var groups = _world.Robots
                .GroupBy(r => r.Kind)
                .Select(g => new GroupSummary(
                    g.Key,
                    g.Count(),
                    (int)Math.Round(g.Average(r => r.Battery)),
                    (int)Math.Round(g.Average(r => r.Integrity))))
                .OrderBy(s => s.Title)
                .ToList();

            _summary.Clear();
            foreach (var group in groups)
                _summary.Add(group);
        }

        /// <summary>
        /// Makes 'target' equal to 'desired' using Remove / Move / Insert instead of Clear + Add,
        /// so the selected robot stays selected after the list is refreshed.
        /// </summary>
        private static void Sync(ObservableCollection<Robot> target, IList<Robot> desired)
        {
            for (int i = target.Count - 1; i >= 0; i--)
            {
                if (!desired.Contains(target[i]))
                    target.RemoveAt(i);
            }

            for (int i = 0; i < desired.Count; i++)
            {
                if (i < target.Count && ReferenceEquals(target[i], desired[i]))
                    continue;

                int current = target.IndexOf(desired[i]);
                if (current >= 0)
                    target.Move(current, i);
                else
                    target.Insert(i, desired[i]);
            }
        }

        // ---------------- Helpers ----------------

        private string SuggestNextId() =>
            (_world.Robots.Select(r => r.Id).DefaultIfEmpty(0).Max() + 1).ToString();

        private static RobotWorld CreateDemoWorld()
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
