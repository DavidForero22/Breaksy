using System.Windows;
using Breaksy.Models;
using Breaksy.Services;
using Breaksy.ViewModels;

namespace Breaksy.Views;

public partial class MainWindow : Window
{
    private readonly BreaksyStateMachine _stateMachine = new();
    private readonly KeyboardHookService _keyboardHook = new();
    private readonly VoiceService _voiceService;

    private MainViewModel? _viewModel;
    private BlockWindow? _blockWindow;

    public MainWindow()
    {
        InitializeComponent();

        _voiceService = new VoiceService(_stateMachine);

        _viewModel = new MainViewModel(_stateMachine, Close);
        DataContext = _viewModel;

        _stateMachine.StateChanged += OnStateChanged;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 20;
    }

    private void OnStateChanged(object? sender, BreaksyStateChangedEventArgs e)
    {
        // Gestión del Bloqueo de Teclado
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

    private void OnClosed(object? sender, EventArgs e)
    {
        _voiceService.Dispose();

        _stateMachine.StateChanged -= OnStateChanged;

        _viewModel?.Dispose();
        _stateMachine.Dispose();
        _keyboardHook.Dispose();

        _blockWindow?.Close();
    }
}