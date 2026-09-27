namespace Breaksy.Models;

/// <summary>
/// Nivel de interrupción aplicado cuando se agota el tiempo de trabajo.
/// </summary>
public enum InterruptionLevel
{
    /// <summary>El personaje cambia de estado y avisa por voz.</summary>
    Light,
    /// <summary>Además, se bloquea el teclado durante los bloqueos y el descanso.</summary>
    Intermediate,
    /// <summary>Además, aparece una ventana de advertencia en el centro de la pantalla.</summary>
    Strict
}
