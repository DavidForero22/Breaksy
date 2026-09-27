using System.IO;
using Microsoft.Win32;

namespace Breaksy.Services;

/// <summary>
/// Gestiona el arranque automático con Windows mediante la clave Run del usuario actual.
/// El instalador usa la misma clave y el mismo nombre de valor, así que ambos se mantienen sincronizados.
/// </summary>
public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Breaksy";

    public static bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
                return key?.GetValue(ValueName) is string;
            }
            catch (Exception ex)
            {
                LogService.Log($"[StartupService] ERROR al leer el arranque automático: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>Activa o desactiva el arranque automático. Devuelve false si no se pudo cambiar.</summary>
    public static bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (enabled)
                key.SetValue(ValueName, $"\"{GetLauncherPath()}\"");
            else
                key.DeleteValue(ValueName, throwOnMissingValue: false);

            LogService.Log($"[StartupService] Arranque automático {(enabled ? "activado" : "desactivado")}.");
            return true;
        }
        catch (Exception ex)
        {
            LogService.Log($"[StartupService] ERROR al cambiar el arranque automático: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// En la versión instalada, el ejecutable real vive en "current\" y junto a esa carpeta hay un
    /// lanzador (Breaksy.exe) que no cambia con las actualizaciones: es el que debe arrancar Windows.
    /// </summary>
    private static string GetLauncherPath()
    {
        var appDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var launcher = Path.Combine(Path.GetDirectoryName(appDir) ?? appDir, "Breaksy.exe");

        return Path.GetFileName(appDir).Equals("current", StringComparison.OrdinalIgnoreCase) && File.Exists(launcher)
            ? launcher
            : Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "Breaksy.exe");
    }
}
