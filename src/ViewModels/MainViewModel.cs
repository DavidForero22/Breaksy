using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Breaksy.Models;
using Breaksy.Services;

namespace Breaksy.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly BreaksyStateMachine _stateMachine;
    private readonly KeyboardHookService _keyboardHook;
    private readonly Action _closeAction;

    // Propiedades enlazadas a la UI
    private string _stateText = "Estado: Idle";
    public string StateText { get => _stateText; set => SetProperty(ref _stateText, value); }

    private string _timeText = "--:--";
    public string TimeText { get => _timeText; set => SetProperty(ref _timeText, value); }

    private string _face = "🧍";
    public string Face { get => _face; set => SetProperty(ref _face, value); }

    private Brush _faceBackground = new SolidColorBrush(Color.FromRgb(100, 150, 200));
    public Brush FaceBackground { get => _faceBackground; set => SetProperty(ref _faceBackground, value); }

    private string _pauseMenuHeader = "Pausar";
    public string PauseMenuHeader { get => _pauseMenuHeader; set => SetProperty(ref _pauseMenuHeader, value); }

    private bool _canStart = true;
    public bool CanStart { get => _canStart; set => SetProperty(ref _canStart, value); }

    private bool _canPause = false;
    public bool CanPause { get => _canPause; set => SetProperty(ref _canPause, value); }

    private bool _canDisable = false;
    public bool CanDisable { get => _canDisable; set => SetProperty(ref _canDisable, value); }

    // Comandos
    public ICommand StartCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand DisableCommand { get; }
    public ICommand CloseCommand { get; }

    public MainViewModel(Action closeAction)
    {
        _closeAction = closeAction;
        _stateMachine = new BreaksyStateMachine();
        _keyboardHook = new KeyboardHookService();

        StartCommand = new RelayCommand(_ => _stateMachine.Start());
        PauseCommand = new RelayCommand(_ => TogglePause());
        DisableCommand = new RelayCommand(_ => _stateMachine.Disable());
        CloseCommand = new RelayCommand(_ => _closeAction());

        _stateMachine.StateChanged += OnStateChanged;
        _stateMachine.Tick += OnTick;
        _keyboardHook.KeyPressed += OnKeyPressed;

        UpdateVisuals(_stateMachine.CurrentState);
    }

    private void TogglePause()
    {
        if (_stateMachine.CurrentState == BreaksyState.Paused)
            _stateMachine.Resume();
        else
            _stateMachine.Pause();
    }

    private void OnTick(object? sender, TimeSpan remaining)
    {
        TimeText = remaining <= TimeSpan.Zero
            ? "--:--"
            : $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";
    }

    private void OnStateChanged(object? sender, BreaksyStateChangedEventArgs e)
    {
        Console.WriteLine($"[Breaksy] {e.OldState} -> {e.NewState}");
        UpdateVisuals(e.NewState);
        OnTick(null, _stateMachine.RemainingTime);

        bool requiresKeyboardBlock = e.NewState is BreaksyState.Blocked1 or BreaksyState.Blocked2 or BreaksyState.Sleeping;

        if (requiresKeyboardBlock)
        {
            _keyboardHook.SuppressKeys = true;
            _keyboardHook.Start();
        }
        else
        {
            _keyboardHook.SuppressKeys = false;
            _keyboardHook.Stop();
        }
    }

    private void UpdateVisuals(BreaksyState state)
    {
        StateText = $"Estado: {state}";

        CanStart = state is BreaksyState.Idle or BreaksyState.Disabled or BreaksyState.Waiting;
        CanPause = state is BreaksyState.Awaken or BreaksyState.Warning or BreaksyState.SeriousWarning or BreaksyState.Paused;
        PauseMenuHeader = state == BreaksyState.Paused ? "Reanudar" : "Pausar";
        CanDisable = state is not (BreaksyState.Blocked1 or BreaksyState.Blocked2 or BreaksyState.Sleeping or BreaksyState.Disabled);

        switch (state)
        {
            case BreaksyState.Disabled:
                Face = "🌑";
                FaceBackground = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                break;
            case BreaksyState.Idle:
            case BreaksyState.Waiting:
                Face = "🧍";
                FaceBackground = new SolidColorBrush(Color.FromRgb(100, 150, 200));
                break;
            case BreaksyState.Awaken:
                Face = "🧑‍💻";
                FaceBackground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                break;
            case BreaksyState.Paused:
                Face = "⏸️";
                FaceBackground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                break;
            case BreaksyState.Warning:
            case BreaksyState.SeriousWarning:
                Face = "😠";
                FaceBackground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                break;
            case BreaksyState.Blocked1:
            case BreaksyState.Blocked2:
                Face = "🛑";
                FaceBackground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                break;
            case BreaksyState.Sleeping:
                Face = "💤";
                FaceBackground = new SolidColorBrush(Color.FromRgb(103, 58, 183));
                break;
        }
    }

    private void OnKeyPressed(object? sender, KeyboardKeyEventArgs e)
    {
        var estado = e.IsKeyDown ? "DOWN" : "UP  ";
        Console.WriteLine($"[{estado}] {e.Key}");
    }

    public void Dispose()
    {
        _stateMachine.StateChanged -= OnStateChanged;
        _stateMachine.Tick -= OnTick;
        _stateMachine.Dispose();

        _keyboardHook.KeyPressed -= OnKeyPressed;
        _keyboardHook.Dispose();
    }

    // --- INotifyPropertyChanged ---
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return;
        backingStore = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    public event EventHandler? CanExecuteChanged { add { } remove { } } // No se usa activamente aquí
    public RelayCommand(Action<object?> execute) => _execute = execute;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute(parameter);
}