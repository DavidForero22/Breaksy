using System.IO;
using System.Text.Json;
using Breaksy.Models;

namespace Breaksy.Services;

public record LanguageInfo(string Code, string Name);

/// <summary>
/// Textos traducidos, leídos de assets\lang\{idioma}.json (primero la carpeta del usuario, luego la de serie).
/// El inglés es el idioma por defecto y también el de reserva: si a otro idioma le falta una clave, se usa la inglesa.
/// </summary>
public static class LocalizationService
{
    public const string DefaultLanguage = "en";

    /// <summary>Idiomas disponibles; el nombre se muestra siempre en su propio idioma.</summary>
    public static IReadOnlyList<LanguageInfo> Languages { get; } =
    [
        new("en", "English"),
        new("es", "Español")
    ];

    private static readonly List<(WeakReference Owner, Action<object> Handler)> Subscribers = [];

    private static Dictionary<string, string>? _fallback;
    private static Dictionary<string, string>? _texts;

    public static string Language { get; private set; } = DefaultLanguage;

    /// <summary>Se dispara en el hilo de la interfaz al cambiar de idioma.</summary>
    public static event Action? LanguageChanged;

    /// <summary>Cambia el idioma de la interfaz. Un código desconocido se sustituye por el idioma por defecto.</summary>
    public static void SetLanguage(string code)
    {
        if (!Languages.Any(l => l.Code == code)) code = DefaultLanguage;
        if (_texts != null && code == Language) return;

        Language = code;
        Load();

        LanguageChanged?.Invoke();

        foreach (var (owner, handler) in Subscribers.ToArray())
        {
            if (owner.Target is { } target) handler(target);
            else Subscribers.RemoveAll(s => s.Owner == owner);
        }
    }

    /// <summary>
    /// Avisa a <paramref name="owner"/> al cambiar de idioma sin mantenerlo vivo. El manejador no debe capturar
    /// el propietario: recíbelo como parámetro (<c>o => o.Refrescar()</c>).
    /// </summary>
    public static void Subscribe<T>(T owner, Action<T> handler) where T : class =>
        Subscribers.Add((new WeakReference(owner), o => handler((T)o)));

    private static void Load()
    {
        _fallback = Read(DefaultLanguage);
        _texts = Language == DefaultLanguage ? _fallback : Read(Language);
    }

    private static Dictionary<string, string> Read(string language)
    {
        try
        {
            var file = AssetPaths.FindFiles("lang", $"{language}.json", _ => true).FirstOrDefault();
            if (file == null)
            {
                LogService.Log($"[LocalizationService] No se encontró 'lang/{language}.json'.");
                return [];
            }

            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file)) ?? [];
        }
        catch (Exception ex)
        {
            LogService.Log($"[LocalizationService] ERROR al leer las traducciones '{language}': {ex.Message}");
            return [];
        }
    }

    /// <summary>Devuelve el texto de la clave; si falta en el idioma actual, el inglés; y si no, <paramref name="fallback"/> o la propia clave.</summary>
    public static string Get(string key, string? fallback = null)
    {
        if (_texts == null) Load();
        if (_texts!.TryGetValue(key, out var text)) return text;
        return _fallback!.TryGetValue(key, out text) ? text : fallback ?? key;
    }

    /// <summary>Como <see cref="Get"/>, aplicando <see cref="string.Format(string, object[])"/> al texto.</summary>
    public static string Format(string key, params object[] args) => string.Format(Get(key), args);

    public static string StateName(BreaksyState state) => Get($"state.{state}", state.ToString());
}
