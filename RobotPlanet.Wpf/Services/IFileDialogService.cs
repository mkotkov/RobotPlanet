namespace RobotPlanet.Wpf.Services
{
    /// <summary>
    /// Lets the user choose a file. The ViewModel only knows this interface, so it stays free of WPF
    /// dialogs and can be tested with a fake. Both methods return null when the user closes the dialog.
    /// </summary>
    public interface IFileDialogService
    {
        string? PickSavePath(string filter, string defaultFileName);

        string? PickOpenPath(string filter);
    }
}
