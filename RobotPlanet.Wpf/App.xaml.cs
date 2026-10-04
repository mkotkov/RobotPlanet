using System.Windows;

namespace RobotPlanet.Wpf
{
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show(e.Exception.ToString(), "Viga");
                e.Handled = true;
            };
        }
    }
}