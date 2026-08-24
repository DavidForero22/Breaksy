using System.Runtime.InteropServices;
using System.Windows.Input;
using Breaksy.Native;

namespace Breaksy.Services;

/// <summary>
/// Registra el hook global WH_KEYBOARD_LL y expone cada pulsación como evento.
/// Fase 0: solo detecta, nunca suprime ninguna tecla (siempre llama a CallNextHookEx).
/// La supresión se añadirá en una fase posterior, una vez validado que la detección es fiable.
/// </summary>
public class KeyboardHookService : IDisposable
{
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;

    public event EventHandler<KeyboardKeyEventArgs>? KeyPressed;

    public KeyboardHookService()
    {
        // Se guarda la referencia al delegado para que el GC no lo recoja mientras el hook está activo.
        _proc = HookCallback;
    }

    // Instala el hook en el proceso actual. Lanza si Windows lo rechaza.
    public void Start()
    {
        if (_hookId != IntPtr.Zero)
        {
            return;
        }

        using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule
            ?? throw new InvalidOperationException("No se pudo obtener el módulo principal del proceso.");

        _hookId = KeyboardHookNative.SetWindowsHookEx(
            KeyboardHookNative.WH_KEYBOARD_LL,
            _proc,
            KeyboardHookNative.GetModuleHandle(currentModule.ModuleName),
            0);

        if (_hookId == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"SetWindowsHookEx falló (error de Win32: {error}).");
        }
    }

    // Libera el hook. Llamarlo varias veces no da error.
    public void Stop()
    {
        if (_hookId == IntPtr.Zero)
        {
            return;
        }

        KeyboardHookNative.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var message = wParam.ToInt32();
            var isKeyDown = message is KeyboardHookNative.WM_KEYDOWN or KeyboardHookNative.WM_SYSKEYDOWN;
            var isKeyUp = message is KeyboardHookNative.WM_KEYUP or KeyboardHookNative.WM_SYSKEYUP;

            if (isKeyDown || isKeyUp)
            {
                var hookStruct = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
                var key = KeyInterop.KeyFromVirtualKey((int)hookStruct.VkCode);
                KeyPressed?.Invoke(this, new KeyboardKeyEventArgs(key, isKeyDown));
            }
        }

        // Fase 0: la pulsación siempre continúa su camino normal, nunca se bloquea aquí.
        return KeyboardHookNative.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();
}
