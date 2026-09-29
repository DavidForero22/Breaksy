using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Breaksy.Native;

/// <summary>
/// Con WindowStyle=None Windows quita las animaciones de minimizar/restaurar.
/// Volver a activar los estilos de sistema del menú y el botón de minimizar las recupera.
/// </summary>
public static class WindowAnimationHelper
{
    private const int GWL_STYLE = -16;
    private const int WS_SYSMENU = 0x00080000;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_CAPTION = 0x00C00000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    public static void Enable(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var style = GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();
            SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(style | WS_SYSMENU | WS_MINIMIZEBOX | WS_CAPTION));
        };
    }
}
