namespace Breaksy.Models;

/// <summary>Tipo de un audio: decide qué interruptor de Aplicación lo controla.</summary>
public enum AudioType
{
    /// <summary>Línea que dice el personaje ("Reproducir voces").</summary>
    Voice,
    /// <summary>Efecto que no es una voz ("Reproducir sonidos").</summary>
    Sound
}
