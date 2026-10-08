using RobotPlanet.Core;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Res = RobotPlanet.Wpf.Properties.Resources;
namespace RobotPlanet.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly RobotWorld _world = new();
        private readonly ObservableCollection<string> _log = new();

        public MainWindow()
        {
            InitializeComponent();

            TypeBox.ItemsSource = Enum.GetValues<RobotKind>();
            RobotList.ItemsSource = _world.Robots;
            TargetBox.ItemsSource = _world.Robots;
            LogList.ItemsSource = _log;

            // Demo-andmed
            _world.TryAdd(RobotKind.Cleaner, 1, "Sädel", 100, 100, out _);
            _world.TryAdd(RobotKind.Explorer, 2, "Kosmos", 80, 100, out _);
            _world.TryAdd(RobotKind.Repair, 3, "Mutrivõti", 100, 60, out _);
            _world.TryAdd(RobotKind.Guard, 4, "Vaht", 100, 100, out _);
            _world.TryAdd(RobotKind.Jester, 5, "Naljahammas", 90, 100, out _);
        }

        private Robot? Selected => RobotList.SelectedItem as Robot;

        private void Log(string message) =>
            _log.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (TypeBox.SelectedItem is not RobotKind kind)
            { Log(Res.Err_No_Type); return; }
            if (!int.TryParse(IdBox.Text, out int id))
            { Log(Res.Err_Id_NotNumber); return; }
            if (!int.TryParse(BatteryBox.Text, out int battery))
            { Log(Res.Err_Battery_NotNumber); return; }
            if (!int.TryParse(IntegrityBox.Text, out int integrity))
            { Log(Res.Err_Integrity_NotNumber); return; }

            _world.TryAdd(kind, id, NameBox.Text, battery, integrity, out string message);
            Log(message);
        }

        private void Remove_Click(object sender, RoutedEventArgs e) =>
            Log(_world.Remove(Selected));

        private void Work_Click(object sender, RoutedEventArgs e) =>
            Log(Selected?.DoWork() ?? Res.Err_No_Selection);

        private void Crazy_Click(object sender, RoutedEventArgs e) =>
            Log(Selected?.CrazyAction() ?? Res.Err_No_Selection);

        private void Charge_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ChargeBox.Text, out int amount))
            { Log(Res.Err_Amount_NotNumber); return; }
            Log(_world.Charge(Selected, amount));
        }

        private void Scan_Click(object sender, RoutedEventArgs e) =>
            Log(_world.Scan(Selected));

        private void Repair_Click(object sender, RoutedEventArgs e) =>
            Log(_world.Repair(Selected, TargetBox.SelectedItem as Robot));

        private void ClearLog_Click(object sender, RoutedEventArgs e) => _log.Clear();

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            bool narrow = e.NewSize.Width < 860;

            Grid.SetColumnSpan(LeftPanel, narrow ? 2 : 1);
            LeftPanel.Margin = narrow ? new Thickness(0, 0, 0, 12) : new Thickness(0, 0, 8, 12);

            Grid.SetRow(RightPanel, narrow ? 2 : 1);
            Grid.SetColumn(RightPanel, narrow ? 0 : 1);
            Grid.SetColumnSpan(RightPanel, narrow ? 2 : 1);
            RightPanel.Margin = narrow ? new Thickness(0, 0, 0, 12) : new Thickness(8, 0, 0, 12);
        }
    }
}