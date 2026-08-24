using System.Windows;
using Breaksy.Native;

namespace Breaksy;

public partial class App : Application
{
    // Fase 0: abrir una consola para poder depurar las teclas detectadas por el hook.
    // Más adelante esto se podrá quitar o condicionar a un modo debug.
    protected override void OnStartup(StartupEventArgs e)
    {
        ConsoleInterop.EnsureConsole();
        Console.WriteLine("=== Breaksy — Fase 0: prueba de hook de teclado ===");
        Console.WriteLine("Cierra la ventana del personaje para terminar.\n");

        base.OnStartup(e);
    }
}
