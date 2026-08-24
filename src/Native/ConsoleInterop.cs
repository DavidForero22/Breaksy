using System.Runtime.InteropServices;

namespace Breaksy.Native;

/// <summary>
/// Consola de depuración.
/// </summary>
internal static class ConsoleInterop
{
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    // Crea y asocia una consola nueva al proceso actual.
    public static void EnsureConsole()
    {
        AllocConsole();
    }
}
