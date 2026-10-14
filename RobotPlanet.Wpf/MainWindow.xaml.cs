using System.Windows;
using System.Windows.Controls;

namespace RobotPlanet.Wpf
{
    /// <summary>
    /// The View has no domain logic and no event handlers for buttons.
    /// The DataContext (MainViewModel) is set in MainWindow.xaml.
    /// The only code here is pure layout.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // On a narrow window the right column moves below the left one.
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