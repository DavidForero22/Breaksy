using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Breaksy.Models;
using Breaksy.Services;

namespace Breaksy.ViewModels;

public class TestMenuViewModel : INotifyPropertyChanged
{
    private readonly BreaksyStateMachine _stateMachine;

    public Array AvailableStates => Enum.GetValues(typeof(BreaksyState));

    private BreaksyState _selectedState;
    public BreaksyState SelectedState
    {
        get => _selectedState;
        set => SetProperty(ref _selectedState, value);
    }

    private int _minutes;
    public int Minutes
    {
        get => _minutes;
        set => SetProperty(ref _minutes, value);
    }

    private int _seconds;
    public int Seconds
    {
        get => _seconds;
        set => SetProperty(ref _seconds, value);
    }

    public ICommand ForceStateCommand { get; }
    public ICommand ForceTimeCommand { get; }

    public TestMenuViewModel(BreaksyStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        SelectedState = _stateMachine.CurrentState;

        Minutes = _stateMachine.RemainingTime.Minutes;
        Seconds = _stateMachine.RemainingTime.Seconds;

        ForceStateCommand = new RelayCommand(_ => _stateMachine.ForceState(SelectedState));
        ForceTimeCommand = new RelayCommand(_ =>
        {
            var newTime = new TimeSpan(0, Minutes, Seconds);
            _stateMachine.ForceTime(newTime);
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return;
        backingStore = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}