using Microsoft.Win32;
using RobotPlanet.Wpf.Properties;

namespace RobotPlanet.Wpf.Services
{
    /// <summary>The real dialogs of Windows. Only this class knows about SaveFileDialog and OpenFileDialog.</summary>
    public sealed class WpfFileDialogService : IFileDialogService
    {
        public string? PickSavePath(string filter, string defaultFileName)
        {
            var dialog = new SaveFileDialog
            {
                Title = Resources.Dialog_SaveTitle,
                Filter = filter,
                FileName = defaultFileName,
                DefaultExt = ".json",
                AddExtension = true,
                OverwritePrompt = true
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? PickOpenPath(string filter)
        {
            var dialog = new OpenFileDialog
            {
                Title = Resources.Dialog_OpenTitle,
                Filter = filter,
                CheckFileExists = true
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
