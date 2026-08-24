using System.Windows.Input;

namespace Breaksy.Services;

/// <summary>
/// Datos de una pulsación detectada por KeyboardHookService.
/// </summary>
public class KeyboardKeyEventArgs : EventArgs
{
    public Key Key { get; }
    public bool IsKeyDown { get; }

    public KeyboardKeyEventArgs(Key key, bool isKeyDown)
    {
        Key = key;
        IsKeyDown = isKeyDown;
    }
}
