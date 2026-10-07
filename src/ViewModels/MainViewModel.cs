using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Breaksy.Models;
using Breaksy.Services;

namespace Breaksy.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly BreaksyStateMachine _stateMachine;
    private readonly CharacterImageService _imageService;
    private readonly SettingsService _settings;
    private readonly Action _closeAction;
    private FileSystemWatcher? _watcher;
    private DispatcherTimer? _refreshTimer;

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
        WatchCharacterFolder();
    }

    // Si cambian los sprites del estado actual (desde el editor o a mano) se vuelve a elegir la imagen
    private void WatchCharacterFolder()
    {
        try
        {
            var folder = Path.Combine(AssetPaths.UserRoot, "character");
            Directory.CreateDirectory(folder);

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _refreshTimer.Tick += (_, _) =>
            {
                _refreshTimer.Stop();
                ImagePath = _imageService.GetImagePathForState(_stateMachine.CurrentState);
            };

            _watcher = new FileSystemWatcher(folder)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };
            _watcher.Created += OnCharacterFilesChanged;
            _watcher.Deleted += OnCharacterFilesChanged;
            _watcher.Renamed += OnCharacterFilesChanged;
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            LogService.Log($"[MainViewModel] ERROR al vigilar la carpeta de imágenes: {ex.Message}");
        }
    }

    private void OnCharacterFilesChanged(object sender, FileSystemEventArgs e)
    {
        var stateKey = CharacterImageService.GetStateKey(_stateMachine.CurrentState);
        var relative = Path.GetRelativePath(Path.Combine(AssetPaths.UserRoot, "character"), e.FullPath);
        if (!relative.StartsWith(stateKey + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return;

        // Los eventos llegan en otro hilo; el temporizador espera a que termine la copia y agrupa los avisos
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Start();
        });
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
        _refreshTimer?.Stop();
        _watcher?.Dispose();
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