using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Breaksy.Models;
using Breaksy.Services;

namespace Breaksy.ViewModels;

public class BlockViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly BreaksyStateMachine _stateMachine;

    private bool _canExtend;
    public bool CanExtend { get => _canExtend; set => SetProperty(ref _canExtend, value); }

    public ICommand RestCommand { get; }
    public ICommand ExtendCommand { get; }

    public BlockViewModel(BreaksyStateMachine stateMachine)
    {
        _stateMachine = stateMachine;

        // Comandos de los botones de bloqueo
        RestCommand = new RelayCommand(_ => _stateMachine.RequestRest());
        ExtendCommand = new RelayCommand(_ => _stateMachine.RequestExtension());

        // El botón "+5 min" solo es visible en Blocked1
        CanExtend = _stateMachine.CurrentState == BreaksyState.Blocked1;

        _stateMachine.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, BreaksyStateChangedEventArgs e)
    {
        CanExtend = e.NewState == BreaksyState.Blocked1;
    }

    public void Dispose()
    {
        _stateMachine.StateChanged -= OnStateChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return;
        backingStore = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}