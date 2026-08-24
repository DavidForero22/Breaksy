using System.Runtime.InteropServices;
using System.Windows.Input;
using Breaksy.Native;

namespace Breaksy.Services;

/// <summary>
/// Registra el hook global WH_KEYBOARD_LL y expone cada pulsación como evento.
/// </summary>
public class KeyboardHookService : IDisposable
{
    // Supresión de teclado (temporal, Fase 0)
    // true  -> las pulsaciones detectadas se suprimen y no llegan a ninguna otra app.
    // false -> comportamiento original: solo se detectan, nunca se bloquean.
    // TODO: cuando se integre con la máquina de estados, esta constante debería sustituirse por una condición real (p. ej. "estado actual es Bloqueado1 o Bloqueado2").
    private const bool HideKeys = false;

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

                // Supresión de teclado
                if (HideKeys)
                {
                    // Un valor != 0 le indica a Windows que la pulsación queda "consumida".
                    // IMPORTANTE: no llamar a CallNextHookEx en este camino, o la tecla igualmente pasaría.
                    return (IntPtr)1;
                }
            }
        }

        return KeyboardHookNative.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();
}