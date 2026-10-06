using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Breaksy.Models;
using Breaksy.Services;

namespace Breaksy.ViewModels;

public class TestMenuViewModel : INotifyPropertyChanged
{
    private readonly BreaksyStateMachine _stateMachine;
    private readonly SettingsService _settings;

    public Array AvailableStates => Enum.GetValues(typeof(BreaksyState));

    private BreaksyState _selectedState;
    public BreaksyState SelectedState
    {
        get => _selectedState;
        set => SetProperty(ref _selectedState, value);
    }

    // --- Duraciones configurables ---
    private int _awakenMinutes;
    public int AwakenMinutes
    {
        get => _awakenMinutes;
        set
        {
            if (SetProperty(ref _awakenMinutes, value))
            {
                _stateMachine.NormalDuration = TimeSpan.FromMinutes(value);
                _settings.AwakenMinutes = value;
            }
        }
    }

    private int _warningMinutes;
    public int WarningMinutes
    {
        get => _warningMinutes;
        set
        {
            if (SetProperty(ref _warningMinutes, value))
            {
                _stateMachine.WarningDuration = TimeSpan.FromMinutes(value);
                _settings.WarningMinutes = value;
            }
        }
    }

    private int _sleepSeconds;
    public int SleepSeconds
    {
        get => _sleepSeconds;
        set
        {
            if (SetProperty(ref _sleepSeconds, value))
            {
                _stateMachine.SleepDuration = TimeSpan.FromSeconds(value);
                _settings.SleepSeconds = value;
            }
        }
    }

    public ICommand ForceStateCommand { get; }

    public TestMenuViewModel(BreaksyStateMachine stateMachine, SettingsService settings)
    {
        _stateMachine = stateMachine;
        _settings = settings;
        SelectedState = _stateMachine.CurrentState;

        _awakenMinutes = (int)_stateMachine.NormalDuration.TotalMinutes;
        _warningMinutes = (int)_stateMachine.WarningDuration.TotalMinutes;
        _sleepSeconds = (int)_stateMachine.SleepDuration.TotalSeconds;

        ForceStateCommand = new RelayCommand(_ => _stateMachine.ForceState(SelectedState));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
        backingStore = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}