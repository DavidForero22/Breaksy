using System.Runtime.InteropServices;

namespace Breaksy.Native;

public static class ConsoleInterop
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    public static void EnsureConsole()
    {
        AllocConsole();
    }

    public static void SetConsoleVisibility(bool visible)
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, visible ? SW_SHOW : SW_HIDE);
        }
    }
}