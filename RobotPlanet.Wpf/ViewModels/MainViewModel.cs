using RobotPlanet.Core;
using RobotPlanet.Core.entities;
using RobotPlanet.Core.@interface;
using RobotPlanet.Core.storage;
using RobotPlanet.Wpf.Properties;
using RobotPlanet.Wpf.Services;
using System.Collections.ObjectModel;

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
        private readonly IWorldStorage _storage;
        private readonly IFileDialogService _dialogs;

        // A database has no file, so a storage that is not file based gets this fixed location label.
        private const string DatabaseLocation = "LocalDB";

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
        private RobotGroup _selectedGroup = RobotGroup.Alfa;
        private bool _isBusy;

        /// <summary>
        /// Everything the ViewModel needs comes in through the constructor (dependency injection, see App.xaml.cs).
        /// It only knows the IWorldStorage interface: which implementation it is was decided when the application started.
        /// </summary>
        public MainViewModel(RobotWorld world, IWorldStorage storage, IFileDialogService dialogs)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            StorageText = string.Format(Resources.Storage_Label, _storage.GetType().Name);

            Repositories = new ReadOnlyCollection<RepositoryItem>(
                _world.Repositories.Select(r => new RepositoryItem(r)).ToList());
            VisibleRobots = new ReadOnlyObservableCollection<Robot>(_visibleRobots);
            Summary = new ReadOnlyObservableCollection<GroupSummary>(_summary);
            Log = new ReadOnlyObservableCollection<string>(_log);
            AllRobots = _world.Robots;
            Kinds = Enum.GetValues<RobotKind>();
            Groups = Enum.GetValues<RobotGroup>();
            Calculation = new CalculationViewModel(_world);

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
            ChangeGroupCommand = new RelayCommand(ChangeGroup, () => SelectedRobot != null && !IsBusy);
            SaveCommand = new RelayCommand(() => _ = SaveAsync(), () => !IsBusy);
            // Loading replaces the robots, so it waits until no calculation is running.
            LoadCommand = new RelayCommand(() => _ = LoadAsync(), () => !IsBusy && !Calculation.IsRunning);
            Calculation.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(CalculationViewModel.IsRunning))
                    LoadCommand.RaiseCanExecuteChanged();
            };

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
        public CalculationViewModel Calculation { get; }

        // ---- Vol.4 & Vol.5: group, saving and loading ----

        public IReadOnlyList<RobotGroup> Groups { get; }

        /// <summary>Tells the user which storage implementation the application was started with (chosen by DI).</summary>
        public string StorageText { get; }

        public RobotGroup SelectedGroup
        {
            get => _selectedGroup;
            set => SetProperty(ref _selectedGroup, value);
        }

        /// <summary>True while data is being saved or loaded. The buttons are disabled meanwhile.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (!SetProperty(ref _isBusy, value)) return;
                SaveCommand.RaiseCanExecuteChanged();
                LoadCommand.RaiseCanExecuteChanged();
                ChangeGroupCommand.RaiseCanExecuteChanged();
            }
        }

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
                {
                    if (value != null)
                        SelectedGroup = value.Group;
                    RaiseRobotCommandsChanged();
                }
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
            set { if (SetProperty(ref _newBattery, value ?? "")) AddCommand.RaiseCanExecuteChanged(); }
        }

        public string NewIntegrity
        {
            get => _newIntegrity;
            set { if (SetProperty(ref _newIntegrity, value ?? "")) AddCommand.RaiseCanExecuteChanged(); }
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
        public RelayCommand ChangeGroupCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand LoadCommand { get; }

        private bool CanAdd() =>
            NewKind != null &&
            !string.IsNullOrWhiteSpace(NewId) &&
            !string.IsNullOrWhiteSpace(NewName);

        private void Add()
        {
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

        // ---------------- Group, saving and loading (Vol.4 & Vol.5) ----------------

        private void ChangeGroup() => Run(() => _world.ChangeGroup(SelectedRobot, SelectedGroup));

        /// <summary>
        /// Saves the world through the injected storage (File, ADO.NET LocalDB, or EF Core TPH).
        /// </summary>
        private async Task SaveAsync()
        {
            string? path = AskPath(save: true);
            if (path == null) return;

            IsBusy = true;
            try
            {
                if (_storage is IEntityWorldStorage entityStorage)
                {
                    IReadOnlyList<Robot> snapshot = _world.CreateRobotsSnapshot();
                    await entityStorage.SaveWorldAsync(snapshot);
                    AddLogLines(string.Format(Resources.File_Saved, snapshot.Count, $"{DatabaseLocation} (EF Core TPH)"));
                }
                else
                {
                    WorldDto dto = _world.CreateDto();
                    await _storage.SaveAsync(dto, path);
                    AddLogLines(string.Format(Resources.File_Saved, dto.Robots?.Count ?? 0, path));
                }
            }
            catch (WorldStorageException ex)
            {
                AddLogLines(ex.Message);
            }
            catch (Exception ex)
            {
                AddLogLines(string.Format(Resources.File_Unexpected, ex.Message));
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Loads a world. The current world is replaced only AFTER the data was read successfully and validated.
        /// </summary>
        private async Task LoadAsync()
        {
            string? path = AskPath(save: false);
            if (path == null) return;

            IsBusy = true;
            try
            {
                if (_storage is IEntityWorldStorage entityStorage)
                {
                    IReadOnlyList<Robot> robots = await entityStorage.LoadWorldAsync();
                    if (_world.TryLoadRobots(robots, out string message))
                    {
                        Run(() => string.Format(Resources.File_Loaded, message, $"{DatabaseLocation} (EF Core TPH)"));
                        NewId = SuggestNextId();
                    }
                    else
                    {
                        AddLogLines(message, Resources.File_WorldKept);
                    }
                }
                else
                {
                    WorldDto dto = await _storage.LoadAsync(path);
                    if (_world.TryLoad(dto, out string message))
                    {
                        Run(() => string.Format(Resources.File_Loaded, message, path));
                        NewId = SuggestNextId();
                    }
                    else
                    {
                        AddLogLines(message, Resources.File_WorldKept);
                    }
                }
            }
            catch (WorldStorageException ex)
            {
                AddLogLines(ex.Message, Resources.File_WorldKept);
            }
            catch (Exception ex)
            {
                AddLogLines(string.Format(Resources.File_Unexpected, ex.Message), Resources.File_WorldKept);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>A file based storage needs a file from the dialog; a database storage does not.</summary>
        private string? AskPath(bool save)
        {
            if (_storage is IFileWorldStorage fileStorage)
            {
                return save
                    ? _dialogs.PickSavePath(fileStorage.FileFilter, Resources.Dialog_DefaultName)
                    : _dialogs.PickOpenPath(fileStorage.FileFilter);
            }

            return DatabaseLocation;
        }

        /// <summary>Writes lines to the log in reading order (newest block on top) without running an action.</summary>
        private void AddLogLines(params string[] messages)
        {
            for (int i = messages.Length - 1; i >= 0; i--)
                _log.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {messages[i]}");
        }

        // ---------------- Log ----------------

        private void Run(Func<string> action)
        {
            _pendingReactions.Clear();
            string message = action();

            var lines = new List<string> { $"[{DateTime.Now:HH:mm:ss}] {message}" };
            lines.AddRange(_pendingReactions.Select(r => " ↳ " + r));
            _pendingReactions.Clear();

            for (int i = lines.Count - 1; i >= 0; i--)
                _log.Insert(0, lines[i]);

            AfterAction();
        }

        private void AfterAction()
        {
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
            ChangeGroupCommand.RaiseCanExecuteChanged();
        }

        // ---------------- LINQ ----------------

        private void RefreshVisible()
        {
            IEnumerable<Robot> query = _selectedRepository?.Repository.Robots ?? Enumerable.Empty<Robot>();

            string text = SearchText.Trim();
            if (text.Length > 0)
            {
                query = query.Where(r =>
                    r.Name.Value.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
                    r.Id.ToString().Contains(text));
            }

            query = SelectedSort.Option switch
            {
                SortOption.Battery => SortDescending ? query.OrderByDescending(r => r.Battery) : query.OrderBy(r => r.Battery),
                SortOption.Integrity => SortDescending ? query.OrderByDescending(r => r.Integrity) : query.OrderBy(r => r.Integrity),
                SortOption.Id => SortDescending ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id),
                _ => SortDescending ? query.OrderByDescending(r => r.Name.Value) : query.OrderBy(r => r.Name.Value)
            };

            Sync(_visibleRobots, query.ToList());
        }

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

        private string SuggestNextId() =>
            (_world.Robots.Select(r => r.Id).DefaultIfEmpty(0).Max() + 1).ToString();
    }
}
