using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Breaksy.Models;
using Breaksy.Services;

namespace Breaksy.ViewModels;

public record InterruptionLevelOption(InterruptionLevel Level, string Name, string Description);

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settings;

    // Se vuelve a crear al cambiar de idioma para que los nombres y descripciones se traduzcan
    public IReadOnlyList<InterruptionLevelOption> InterruptionLevels { get; private set; } = BuildInterruptionLevels();

    private static InterruptionLevelOption[] BuildInterruptionLevels() =>
    [
        new(InterruptionLevel.Light, LocalizationService.Get("level.light"), LocalizationService.Get("level.light_desc")),
        new(InterruptionLevel.Intermediate, LocalizationService.Get("level.intermediate"), LocalizationService.Get("level.intermediate_desc")),
        new(InterruptionLevel.Strict, LocalizationService.Get("level.strict"), LocalizationService.Get("level.strict_desc"))
    ];

    public IReadOnlyList<LanguageInfo> Languages => LocalizationService.Languages;

    public LanguageInfo SelectedLanguage
    {
        get => Languages.First(l => l.Code == _settings.Language);
        set { if (value != null) _settings.Language = value.Code; }
    }

    private void RefreshLanguageTexts()
    {
        InterruptionLevels = BuildInterruptionLevels();
        OnPropertyChanged(nameof(InterruptionLevels));
        OnPropertyChanged(nameof(SelectedInterruptionLevel));
    }

    public InterruptionLevelOption SelectedInterruptionLevel
    {
        get => InterruptionLevels.First(o => o.Level == _settings.InterruptionLevel);
        set => _settings.InterruptionLevel = value.Level;
    }

    // La interfaz expresa "reproducir"; SettingsService guarda lo contrario (silenciado)
    public bool PlayVoices
    {
        get => !_settings.IsMuted;
        set => _settings.IsMuted = !value;
    }

    public bool PlaySounds
    {
        get => !_settings.IsSoundMuted;
        set => _settings.IsSoundMuted = !value;
    }

    // Se guarda en el registro de Windows, no en SettingsService, para que el instalador pueda activarlo
    public bool StartWithWindows
    {
        get => StartupService.IsEnabled;
        set
        {
            StartupService.SetEnabled(value);
            // Relee el valor real por si no se pudo cambiar
            OnPropertyChanged();
        }
    }

    public bool IsKeyboardBlockUnavailable => !_settings.IsKeyboardBlockAvailable;

    public bool ShowState
    {
        get => _settings.ShowState;
        set => _settings.ShowState = value;
    }

    public bool ShowTimer
    {
        get => _settings.ShowTimer;
        set => _settings.ShowTimer = value;
    }

    public bool IsDebugConsoleEnabled
    {
        get => _settings.IsDebugConsoleEnabled;
        set => _settings.IsDebugConsoleEnabled = value;
    }

    public TestMenuViewModel TestMenu { get; }
    public CharacterEditorViewModel CharacterEditor { get; }

    public UpdateService Updates { get; }
    public ICommand CheckUpdatesCommand { get; }
    public ICommand InstallUpdateCommand { get; }
    public ICommand OpenCustomizationGuideCommand { get; }
    public ICommand OpenCustomizationFolderCommand { get; }
    public ICommand OpenReleaseHistoryCommand { get; }
    public string CustomizationFolder => AssetPaths.UserRoot;

    public SettingsViewModel(SettingsService settings, BreaksyStateMachine stateMachine, UpdateService updates)
    {
        _settings = settings;
        TestMenu = new TestMenuViewModel(stateMachine, _settings);
        CharacterEditor = new CharacterEditorViewModel(_settings);
        Updates = updates;
        CheckUpdatesCommand = new RelayCommand(async _ => await Updates.CheckAsync());
        InstallUpdateCommand = new RelayCommand(async _ => await Updates.DownloadAndRestartAsync());
        OpenCustomizationGuideCommand = new RelayCommand(_ => OpenCustomizationGuide());
        OpenCustomizationFolderCommand = new RelayCommand(_ => OpenCustomizationFolder());
        OpenReleaseHistoryCommand = new RelayCommand(_ => OpenReleaseHistory());

        _settings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SettingsService.IsSoundMuted))
                OnPropertyChanged(nameof(PlaySounds));

            if (e.PropertyName == nameof(SettingsService.IsMuted))
                OnPropertyChanged(nameof(PlayVoices));

            if (e.PropertyName == nameof(SettingsService.IsKeyboardBlockAvailable))
                OnPropertyChanged(nameof(IsKeyboardBlockUnavailable));

            if (e.PropertyName == nameof(SettingsService.ShowState))
                OnPropertyChanged(nameof(ShowState));

            if (e.PropertyName == nameof(SettingsService.ShowTimer))
                OnPropertyChanged(nameof(ShowTimer));

            if (e.PropertyName == nameof(SettingsService.IsDebugConsoleEnabled))
                OnPropertyChanged(nameof(IsDebugConsoleEnabled));

            if (e.PropertyName == nameof(SettingsService.InterruptionLevel))
                OnPropertyChanged(nameof(SelectedInterruptionLevel));
        };
    }

    private const string CustomizationGuideFileName = "Guía de Personalización Breaksy.pdf";

    private static void OpenReleaseHistory()
    {
        try
        {
            // Abre la página de releases de GitHub en el navegador predeterminado
            Process.Start(new ProcessStartInfo(UpdateService.ReleasesUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LogService.Log($"[Settings] ERROR al abrir el historial de versiones: {ex.Message}");
            System.Windows.MessageBox.Show($"No se pudo abrir el navegador:\n\n{ex.Message}\n\n{UpdateService.ReleasesUrl}",
                "Breaksy", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private static void OpenCustomizationFolder()
    {
        try
        {
            // Se crea con todas las subcarpetas para que el usuario vea dónde va cada archivo
            AssetPaths.EnsureUserFolders();
            Process.Start(new ProcessStartInfo(AssetPaths.UserRoot) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LogService.Log($"[Settings] ERROR al abrir la carpeta de personalización: {ex.Message}");
            System.Windows.MessageBox.Show($"No se pudo abrir la carpeta de personalización:\n\n{ex.Message}",
                "Breaksy", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private static void OpenCustomizationGuide()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CustomizationGuideFileName);
        if (!File.Exists(path))
        {
            LogService.Log($"[Settings] No se encontró la guía de personalización en '{path}'.");
            System.Windows.MessageBox.Show(LocalizationService.Get("msg.guide_missing"),
                "Breaksy", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Abre el PDF con el visor predeterminado del sistema
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LogService.Log($"[Settings] ERROR al abrir la guía de personalización: {ex.Message}");
            System.Windows.MessageBox.Show($"No se pudo abrir la guía de personalización:\n\n{ex.Message}",
                "Breaksy", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
