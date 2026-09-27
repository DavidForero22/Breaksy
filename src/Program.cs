using Breaksy.Services;
using Velopack;

namespace Breaksy;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack debe ejecutarse lo primero: gestiona los ganchos de instalación,
        // actualización y desinstalación, y puede terminar el proceso en esos casos.
        VelopackApp.Build()
            .SetArgs(args)
            // Al desinstalar, quitar el arranque automático para no dejar una entrada rota en el registro
            .OnBeforeUninstallFastCallback(_ => StartupService.SetEnabled(false))
            .Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
