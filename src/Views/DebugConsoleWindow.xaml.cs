using System.Windows;
using Breaksy.Services;

namespace Breaksy.Views;

/// <summary>
/// Muestra el registro de LogService. Cerrarla no cierra la aplicación.
/// </summary>
public partial class DebugConsoleWindow : Window
{
    public DebugConsoleWindow()
    {
        InitializeComponent();

        foreach (var line in LogService.GetHistory())
            LogText.AppendText(line + Environment.NewLine);
        LogText.ScrollToEnd();

        LogService.EntryAdded += OnEntryAdded;
        Closed += (_, _) => LogService.EntryAdded -= OnEntryAdded;
    }

    private void OnEntryAdded(string line)
    {
        Dispatcher.BeginInvoke(() =>
        {
            LogText.AppendText(line + Environment.NewLine);
            LogText.ScrollToEnd();
        });
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
