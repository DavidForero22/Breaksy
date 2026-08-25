using System.Windows;
using Breaksy.Models;
using Breaksy.Services;
using Breaksy.ViewModels;
using Breaksy.Native;

namespace Breaksy.Views;

public partial class MainWindow : Window
{
    private readonly BreaksyStateMachine _stateMachine = new();
    private readonly KeyboardHookService _keyboardHook = new();
    private readonly SettingsService _settingsService = new();
    private readonly VoiceService _voiceService;

    private MainViewModel? _viewModel;
    private BlockWindow? _blockWindow;

    public MainWindow()
    {
        InitializeComponent();

        _voiceService = new VoiceService(_stateMachine, _settingsService);

        _viewModel = new MainViewModel(_stateMachine, OpenSettingsWindow, Close);
        DataContext = _viewModel;

        _stateMachine.StateChanged += OnStateChanged;

        _settingsService.PropertyChanged += OnSettingsChanged;
        ConsoleInterop.SetConsoleVisibility(_settingsService.IsDebugConsoleEnabled);

        Loaded += OnLoaded;
        Closed += OnClosed;
    }


    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 20;
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsService.IsDebugConsoleEnabled))
        {
            ConsoleInterop.SetConsoleVisibility(_settingsService.IsDebugConsoleEnabled);
        }
    }

    private void OnStateChanged(object? sender, BreaksyStateChangedEventArgs e)
    {
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

        // Gestión de la Ventana Modal (Pantalla de Bloqueo)
        bool requiresBlockWindow = e.NewState is BreaksyState.Blocked1 or BreaksyState.Blocked2;

        if (requiresBlockWindow && _blockWindow == null)
        {
            var blockViewModel = new BlockViewModel(_stateMachine);
            _blockWindow = new BlockWindow(blockViewModel);
            _blockWindow.Show();
        }
        else if (!requiresBlockWindow && _blockWindow != null)
        {
            _blockWindow.Close();
            _blockWindow = null;
        }
    }

    private void OpenSettingsWindow()
    {
        Action openTestMenu = () =>
        {
            var testViewModel = new TestMenuViewModel(_stateMachine);
            var testWindow = new TestMenuWindow(testViewModel) { Owner = this };
            testWindow.ShowDialog();
        };

        var settingsViewModel = new SettingsViewModel(_settingsService, openTestMenu);
        var settingsWindow = new SettingsWindow(settingsViewModel)
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _stateMachine.StateChanged -= OnStateChanged;
        _settingsService.PropertyChanged -= OnSettingsChanged;

        _viewModel?.Dispose();
        _stateMachine.Dispose();
        _keyboardHook.Dispose();
        _voiceService.Dispose();

        _blockWindow?.Close();
    }

}