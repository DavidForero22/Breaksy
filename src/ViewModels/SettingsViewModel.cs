using System.ComponentModel;
using System.Runtime.CompilerServices;
using Breaksy.Models;
using Breaksy.Services;

namespace Breaksy.ViewModels;

public record InterruptionLevelOption(InterruptionLevel Level, string Name, string Description);

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settings;

    public IReadOnlyList<InterruptionLevelOption> InterruptionLevels { get; } =
    [
        new(InterruptionLevel.Light, "Ligero",
            "El personaje cambia de estado y te avisa por voz."),
        new(InterruptionLevel.Intermediate, "Intermedio",
            "Además, se bloquea el teclado para que no puedas seguir escribiendo."),
        new(InterruptionLevel.Strict, "Estricto",
            "Además, aparece una ventana de advertencia en el centro de la pantalla.")
    ];

    public InterruptionLevelOption SelectedInterruptionLevel
    {
        get => InterruptionLevels.First(o => o.Level == _settings.InterruptionLevel);
        set => _settings.InterruptionLevel = value.Level;
    }

    public bool IsMuted
    {
        get => _settings.IsMuted;
        set => _settings.IsMuted = value;
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

    public SettingsViewModel(SettingsService settings, BreaksyStateMachine stateMachine)
    {
        _settings = settings;
        TestMenu = new TestMenuViewModel(stateMachine);

        _settings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SettingsService.IsMuted))
                OnPropertyChanged(nameof(IsMuted));

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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
