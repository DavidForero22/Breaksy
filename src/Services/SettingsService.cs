using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Breaksy.Models;

namespace Breaksy.Services;

/// <summary>
/// Contiene la configuración global de la aplicación y la guarda en %AppData%\Breaksy\settings.json.
/// El arranque con Windows no se guarda aquí: vive en el registro (ver <see cref="StartupService"/>).
/// </summary>
public class SettingsService : INotifyPropertyChanged
{
    private static readonly string SettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Breaksy", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // Lo que se guarda en disco; los nombres no deben cambiar para no perder la configuración
    private class PersistedSettings
    {
        public string Language { get; set; } = LocalizationService.DefaultLanguage;
        public bool IsMuted { get; set; }
        public bool IsSoundMuted { get; set; }
        public bool ShowState { get; set; } = true;
        public bool ShowTimer { get; set; } = true;
        public InterruptionLevel InterruptionLevel { get; set; } = InterruptionLevel.Intermediate;
        public bool IsDebugConsoleEnabled { get; set; }
        public int AwakenMinutes { get; set; } = 20;
        public int WarningMinutes { get; set; } = 1;
        public int SleepSeconds { get; set; } = 35;
    }

    private bool _loading;

    public SettingsService()
    {
        Load();
        PropertyChanged += (_, e) =>
        {
            // IsKeyboardBlockAvailable depende del equipo, no es una preferencia
            if (!_loading && e.PropertyName != nameof(IsKeyboardBlockAvailable)) Save();
        };
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;

            var data = JsonSerializer.Deserialize<PersistedSettings>(File.ReadAllText(SettingsPath));
            if (data == null) return;

            _loading = true;
            Language = data.Language;
            IsMuted = data.IsMuted;
            IsSoundMuted = data.IsSoundMuted;
            ShowState = data.ShowState;
            ShowTimer = data.ShowTimer;
            InterruptionLevel = Enum.IsDefined(data.InterruptionLevel) ? data.InterruptionLevel : InterruptionLevel.Intermediate;
            IsDebugConsoleEnabled = data.IsDebugConsoleEnabled;
            AwakenMinutes = Math.Max(1, data.AwakenMinutes);
            WarningMinutes = Math.Max(1, data.WarningMinutes);
            SleepSeconds = Math.Max(1, data.SleepSeconds);
        }
        catch (Exception ex)
        {
            // Archivo corrupto o ilegible: se usan los valores por defecto y se sobrescribirá al guardar
            LogService.Log($"[SettingsService] ERROR al leer la configuración: {ex.Message}");
        }
        finally
        {
            _loading = false;
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var data = new PersistedSettings
            {
                Language = Language,
                IsMuted = IsMuted,
                IsSoundMuted = IsSoundMuted,
                ShowState = ShowState,
                ShowTimer = ShowTimer,
                InterruptionLevel = InterruptionLevel,
                IsDebugConsoleEnabled = IsDebugConsoleEnabled,
                AwakenMinutes = AwakenMinutes,
                WarningMinutes = WarningMinutes,
                SleepSeconds = SleepSeconds
            };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(data, JsonOptions));
        }
        catch (Exception ex)
        {
            LogService.Log($"[SettingsService] ERROR al guardar la configuración: {ex.Message}");
        }
    }

    // Duraciones (se editan desde el menú de pruebas de la consola de depuración)
    private int _awakenMinutes = 20;
    public int AwakenMinutes
    {
        get => _awakenMinutes;
        set { if (_awakenMinutes != value) { _awakenMinutes = value; OnPropertyChanged(); } }
    }

    private int _warningMinutes = 1;
    public int WarningMinutes
    {
        get => _warningMinutes;
        set { if (_warningMinutes != value) { _warningMinutes = value; OnPropertyChanged(); } }
    }

    private int _sleepSeconds = 35;
    public int SleepSeconds
    {
        get => _sleepSeconds;
        set { if (_sleepSeconds != value) { _sleepSeconds = value; OnPropertyChanged(); } }
    }

    private string _language = LocalizationService.DefaultLanguage;
    /// <summary>Idioma de la interfaz (código, p. ej. "en" o "es").</summary>
    public string Language
    {
        get => _language;
        set
        {
            if (!LocalizationService.Languages.Any(l => l.Code == value)) value = LocalizationService.DefaultLanguage;
            if (_language == value) return;
            _language = value;
            LocalizationService.SetLanguage(value);
            OnPropertyChanged();
        }
    }

    private bool _isMuted = false;
    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (_isMuted != value)
            {
                _isMuted = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isSoundMuted = false;
    /// <summary>Silencia los sonidos que no son voces (por ejemplo, la alarma al terminar el descanso).</summary>
    public bool IsSoundMuted
    {
        get => _isSoundMuted;
        set
        {
            if (_isSoundMuted != value)
            {
                _isSoundMuted = value;
                OnPropertyChanged();
            }
        }
    }

    private InterruptionLevel _interruptionLevel = InterruptionLevel.Intermediate;
    public InterruptionLevel InterruptionLevel
    {
        get => _interruptionLevel;
        set
        {
            if (_interruptionLevel != value)
            {
                _interruptionLevel = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _showState = true;
    public bool ShowState
    {
        get => _showState;
        set
        {
            if (_showState != value)
            {
                _showState = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _showTimer = true;
    public bool ShowTimer
    {
        get => _showTimer;
        set
        {
            if (_showTimer != value)
            {
                _showTimer = value;
                OnPropertyChanged();
            }
        }
    }

    // No es una preferencia del usuario: indica si el bloqueo de teclado funciona en este equipo.
    // Se expone aquí para que la configuración pueda avisar cuando no está disponible.
    private bool _isKeyboardBlockAvailable = true;
    public bool IsKeyboardBlockAvailable
    {
        get => _isKeyboardBlockAvailable;
        set
        {
            if (_isKeyboardBlockAvailable != value)
            {
                _isKeyboardBlockAvailable = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isDebugConsoleEnabled = false;
    public bool IsDebugConsoleEnabled
    {
        get => _isDebugConsoleEnabled;
        set
        {
            if (_isDebugConsoleEnabled != value)
            {
                _isDebugConsoleEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}