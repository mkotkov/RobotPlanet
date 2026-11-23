using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RobotPlanet.Core.storage;
using AppResources = RobotPlanet.Wpf.Properties.Resources;
using RobotPlanet.Wpf.Services;
using RobotPlanet.Wpf.ViewModels;

namespace RobotPlanet.Wpf
{
    public partial class App : Application
    {
        private ServiceProvider? _services;

        public App()
        {
            DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show(e.Exception.ToString(), "Viga");
                e.Handled = true;
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // appsettings.json and the command line decide which storage is used (--storage=EfCore | LocalDb | File).
            AppSettings settings = AppSettings.Load(e.Args);
            if (settings.Warning != null)
                MessageBox.Show(settings.Warning, AppResources.Settings_Title);

            var services = new ServiceCollection();
            ConfigureServices(services, settings);
            _services = services.BuildServiceProvider();

            // The container builds the window, which needs the ViewModel, which needs the storage and the dialog service.
            MainWindow window = _services.GetRequiredService<MainWindow>();
            window.Show();
        }

        /// <summary>
        /// The composition root: the one place where the application decides which class stands behind each interface.
        /// </summary>
        private static void ConfigureServices(IServiceCollection services, AppSettings settings)
        {
            services.AddSingleton(_ => DemoWorld.Create());

            // THE choice of the storage implementation (File, LocalDb ADO.NET, or EfCore TPH).
            StorageProviderFactory.RegisterStorage(services, settings);

            services.AddSingleton<IFileDialogService, WpfFileDialogService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _services?.Dispose();
            base.OnExit(e);
        }
    }
}
