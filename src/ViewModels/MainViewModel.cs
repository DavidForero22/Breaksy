using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Breaksy.Models;
using Breaksy.Services;

namespace Breaksy.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly BreaksyStateMachine _stateMachine;
    private readonly CharacterImageService _imageService;
    private readonly SettingsService _settings;
    private readonly Action _closeAction;

    private string _imagePath = string.Empty;
    public string ImagePath { get => _imagePath; set => SetProperty(ref _imagePath, value); }

    private string _stateText = "Estado: Idle";
    public string StateText { get => _stateText; set => SetProperty(ref _stateText, value); }

    private string _timeText = "--:--";
    public string TimeText { get => _timeText; set => SetProperty(ref _timeText, value); }

    private string _pauseMenuHeader = "Pausar";
    public string PauseMenuHeader { get => _pauseMenuHeader; set => SetProperty(ref _pauseMenuHeader, value); }

    private bool _canStart = true;
    public bool CanStart { get => _canStart; set => SetProperty(ref _canStart, value); }

    private bool _canPause = false;
    public bool CanPause { get => _canPause; set => SetProperty(ref _canPause, value); }

    private bool _canDisable = false;
    public bool CanDisable { get => _canDisable; set => SetProperty(ref _canDisable, value); }

    public ICommand StartCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand DisableCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand CloseCommand { get; }

    public bool ShowState => _settings.ShowState;
    public bool ShowTimer => _settings.ShowTimer;

    public MainViewModel(BreaksyStateMachine stateMachine, SettingsService settings, Action openSettingsAction, Action closeAction)
    {
        _stateMachine = stateMachine;
        _settings = settings;
        _closeAction = closeAction;

        _settings.PropertyChanged += OnSettingsChanged;

        _imageService = new CharacterImageService();

        StartCommand = new RelayCommand(_ => { if (CanStart) _stateMachine.Start(); });
        PauseCommand = new RelayCommand(_ => TogglePause());
        DisableCommand = new RelayCommand(_ => _stateMachine.Disable());
        OpenSettingsCommand = new RelayCommand(_ => openSettingsAction());
        CloseCommand = new RelayCommand(_ => _closeAction());

        _stateMachine.StateChanged += OnStateChanged;
        _stateMachine.Tick += OnTick;

        UpdateVisuals(_stateMachine.CurrentState);
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsService.ShowState))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowState)));

        if (e.PropertyName == nameof(SettingsService.ShowTimer))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowTimer)));
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
        LogService.Log($"[Status changed] {e.OldState} -> {e.NewState}");

        UpdateVisuals(e.NewState);
        OnTick(null, _stateMachine.RemainingTime);
    }

    private void UpdateVisuals(BreaksyState state)
    {
        CanStart = state is BreaksyState.Idle or BreaksyState.Disabled or BreaksyState.Waiting;
        CanPause = state is BreaksyState.Awaken or BreaksyState.Warning or BreaksyState.SeriousWarning or BreaksyState.Paused;
        PauseMenuHeader = state == BreaksyState.Paused ? "Reanudar" : "Pausar";
        CanDisable = state is not (BreaksyState.Blocked1 or BreaksyState.Blocked2 or BreaksyState.Sleeping or BreaksyState.Disabled);

        StateText = $"Estado: {state}";
        ImagePath = _imageService.GetImagePathForState(state);
    }

    public void Dispose()
    {
        _stateMachine.StateChanged -= OnStateChanged;
        _stateMachine.Tick -= OnTick;
        _settings.PropertyChanged -= OnSettingsChanged;
    }

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
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public RelayCommand(Action<object?> execute) => _execute = execute;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute(parameter);
}