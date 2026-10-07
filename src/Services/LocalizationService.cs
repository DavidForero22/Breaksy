using System.IO;
using System.Text.Json;
using Breaksy.Models;

namespace Breaksy.Services;

/// <summary>
/// Textos traducidos, leídos de assets\lang\{idioma}.json (primero la carpeta del usuario, luego la de serie).
/// De momento solo está el español, que es el idioma por defecto.
/// </summary>
public static class LocalizationService
{
    public const string DefaultLanguage = "es";

    private static Dictionary<string, string>? _texts;

    public static string Language { get; private set; } = DefaultLanguage;

    /// <summary>Carga las traducciones del idioma indicado. Si no existe, no se traduce nada.</summary>
    public static void Load(string language = DefaultLanguage)
    {
        Language = language;
        _texts = [];

        try
        {
            var file = AssetPaths.FindFiles("lang", $"{language}.json", _ => true).FirstOrDefault();
            if (file == null)
            {
                LogService.Log($"[LocalizationService] No se encontró 'lang/{language}.json'.");
                return;
            }

            _texts = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file)) ?? [];
        }
        catch (Exception ex)
        {
            LogService.Log($"[LocalizationService] ERROR al leer las traducciones: {ex.Message}");
        }
    }

    /// <summary>Devuelve el texto de la clave, o <paramref name="fallback"/> (o la propia clave) si falta.</summary>
    public static string Get(string key, string? fallback = null)
    {
        if (_texts == null) Load();
        return _texts!.TryGetValue(key, out var text) ? text : fallback ?? key;
    }

    public static string StateName(BreaksyState state) => Get($"state.{state}", state.ToString());
}
