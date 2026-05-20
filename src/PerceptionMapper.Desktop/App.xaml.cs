using System.Windows;
using System.Windows.Threading;

namespace PerceptionMapper.Desktop;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                ShowFatalError(ex.Message);
        };

        base.OnStartup(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowFatalError(e.Exception.Message);
        e.Handled = true;
    }

    private static void ShowFatalError(string message) =>
        MessageBox.Show(
            $"Unexpected error:\n{message}",
            "Perception Mapper",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
}
