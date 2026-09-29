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
        Native.WindowAnimationHelper.Enable(this);

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

    private async void OnCopyClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(LogText.Text);
            CopyButton.Content = "¡Copiado!";
        }
        catch (Exception ex)
        {
            // El portapapeles puede estar bloqueado momentáneamente por otra aplicación
            LogService.Log($"[DebugConsole] ERROR al copiar al portapapeles: {ex.Message}");
            CopyButton.Content = "Error al copiar";
        }

        await Task.Delay(1500);
        CopyButton.Content = "Copiar todo";
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
