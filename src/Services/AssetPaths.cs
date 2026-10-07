using System.IO;

namespace Breaksy.Services;

/// <summary>
/// Resuelve dónde buscar imágenes y voces.
/// Primero se mira en la carpeta de personalización del usuario (%AppData%\Breaksy\assets), que las
/// actualizaciones no tocan, y si ahí no hay nada se usan los archivos de serie junto al ejecutable.
/// </summary>
public static class AssetPaths
{
    /// <summary>Carpeta de personalización del usuario.</summary>
    public static string UserRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Breaksy", "assets");

    /// <summary>Archivos de serie, junto al ejecutable (se sustituyen en cada actualización).</summary>
    public static string BundledRoot { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");

    // Estructura que se crea en la carpeta de personalización para que el usuario vea dónde va cada archivo
    private static readonly string[] CustomizableFolders =
    [
        @"character\disabled", @"character\idle", @"character\awaken", @"character\paused",
        @"character\warning", @"character\serious_warning", @"character\blocked_1",
        @"character\blocked_2", @"character\sleeping", @"character\waiting",
        @"audio\awaken\from_idle", @"audio\awaken\from_disabled", @"audio\awaken\from_blocked1",
        @"audio\awaken\from_waiting",
        @"audio\warning\step_1", @"audio\warning\step_2", @"audio\warning\step_3",
        @"audio\serious_warning\step_1", @"audio\serious_warning\step_2", @"audio\serious_warning\step_3",
        @"audio\blocked_1\enter", @"audio\blocked_1\1_min", @"audio\blocked_1\5_min",
        @"audio\blocked_2\enter", @"audio\blocked_2\1_min", @"audio\blocked_2\5_min",
        @"audio\paused\pause", @"audio\paused\resume", @"audio\disabled\disable"
    ];

    /// <summary>
    /// Devuelve los archivos de <paramref name="relativeDir"/> que cumplen el filtro: los del usuario si
    /// hay alguno; si no, los de serie. Nunca mezcla ambos, para que personalizar un estado sustituya
    /// por completo a las imágenes o voces originales.
    /// </summary>
    public static string[] FindFiles(string relativeDir, string searchPattern, Func<string, bool> filter)
    {
        foreach (var root in new[] { UserRoot, BundledRoot })
        {
            var dir = Path.Combine(root, relativeDir);
            if (!Directory.Exists(dir)) continue;

            try
            {
                var files = Directory.GetFiles(dir, searchPattern).Where(filter).ToArray();
                if (files.Length > 0) return files;
            }
            catch (Exception ex)
            {
                LogService.Log($"[AssetPaths] ERROR al leer '{dir}': {ex.Message}");
            }
        }

        return [];
    }

    /// <summary>Crea la carpeta de personalización con todas sus subcarpetas (vacías) si no existen.</summary>
    public static void EnsureUserFolders()
    {
        foreach (var folder in CustomizableFolders)
            Directory.CreateDirectory(Path.Combine(UserRoot, folder));
    }
}
