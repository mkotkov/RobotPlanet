using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RobotPlanet.Core;
using RobotPlanet.Core.storage;
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

            var services = new ServiceCollection();
            ConfigureServices(services);
            _services = services.BuildServiceProvider();

            // The container builds the window, which needs the ViewModel, which needs the storage and the dialog service.
            MainWindow window = _services.GetRequiredService<MainWindow>();
            window.Show();
        }

        /// <summary>
        /// The composition root: the one place where the application decides which class stands behind each interface.
        /// </summary>
        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_ => DemoWorld.Create());

            // The storage implementation. The ViewModel only asks for IWorldStorage.
            services.AddSingleton<JsonWorldStorage>();
            services.AddSingleton<XmlWorldStorage>();
            services.AddSingleton<IWorldStorage, FileWorldStorage>();       // <- the chosen implementation (JSON or XML by file extension)

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
