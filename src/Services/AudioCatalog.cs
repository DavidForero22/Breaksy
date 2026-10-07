using Breaksy.Models;

namespace Breaksy.Services;

/// <summary>
/// Tipo que tiene de serie cada evento de audio. Las claves son la carpeta dentro de audio\ (por ejemplo
/// "warning\step_1"). El usuario puede cambiar el tipo de cada evento en el editor de personaje.
/// </summary>
public static class AudioCatalog
{
    private static readonly HashSet<string> DefaultSounds = new(StringComparer.OrdinalIgnoreCase)
    {
        @"awaken\from_disabled",
        @"paused\pause",
        @"paused\resume",
        @"disabled\disable"
    };

    public static AudioType DefaultType(string key) =>
        DefaultSounds.Contains(key) ? AudioType.Sound : AudioType.Voice;
}
