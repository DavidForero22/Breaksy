using System.Windows;
using System.Windows.Threading;
using Breaksy.Services;

namespace Breaksy;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        base.OnStartup(e);
    }

    // Errores en el hilo de la interfaz: se registran y se avisa, sin cerrar la aplicación
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogService.Log($"[App] ERROR no controlado: {e.Exception}");

        MessageBox.Show(
            $"Se ha producido un error inesperado:\n\n{e.Exception.Message}\n\n" +
            "Breaksy intentará seguir funcionando. Si el problema persiste, reinicia la aplicación.",
            "Breaksy - Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    // Errores en otros hilos: no se pueden recuperar, solo registrarlos antes de que el proceso termine
    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogService.Log($"[App] ERROR fatal: {e.ExceptionObject}");
    }
}
