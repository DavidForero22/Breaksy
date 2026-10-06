using System.Windows;
using Breaksy.Models;
using Breaksy.Services;
using Breaksy.ViewModels;

namespace Breaksy.Views;

public partial class MainWindow : Window
{
    private readonly BreaksyStateMachine _stateMachine = new();
    private readonly KeyboardHookService _keyboardHook = new();
    private readonly SettingsService _settingsService = new();
    private readonly VoiceService _voiceService;
    private readonly TrayIconService _trayIcon;
    private readonly UpdateService _updateService = new();

    private MainViewModel? _viewModel;
    private BlockWindow? _blockWindow;
    private TimeUpWindow? _timeUpWindow;
    private SettingsWindow? _settingsWindow;
    private DebugConsoleWindow? _debugConsoleWindow;

    public MainWindow()
    {
        InitializeComponent();

        // Duraciones guardadas de la sesión anterior
        _stateMachine.NormalDuration = TimeSpan.FromMinutes(_settingsService.AwakenMinutes);
        _stateMachine.WarningDuration = TimeSpan.FromMinutes(_settingsService.WarningMinutes);
        _stateMachine.SleepDuration = TimeSpan.FromSeconds(_settingsService.SleepSeconds);

        _voiceService = new VoiceService(_stateMachine, _settingsService);

        _viewModel = new MainViewModel(_stateMachine, _settingsService, OpenSettingsWindow, Close);
        DataContext = _viewModel;

        _trayIcon = new TrayIconService(_viewModel, _updateService, ShowCharacter);

        _stateMachine.StateChanged += OnStateChanged;

        _settingsService.PropertyChanged += OnSettingsChanged;
        UpdateDebugConsole();

        Loaded += OnLoaded;
        ContentRendered += (_, _) =>
        {
            CheckKeyboardHook();
            _updateService.StartPeriodicChecks();
        };
        Closed += OnClosed;
    }

    // Comprueba al arrancar que el hook de teclado se puede registrar, para avisar antes del primer bloqueo
    private void CheckKeyboardHook()
    {
        if (_keyboardHook.CheckAvailability()) return;

        _settingsService.IsKeyboardBlockAvailable = false;
        MessageBox.Show(
            this,
            "Windows no ha permitido registrar el bloqueo de teclado.\n\n" +
            $"Detalle: {_keyboardHook.LastError}\n\n" +
            "Breaksy seguirá funcionando (avisos, voces y ventanas de bloqueo), " +
            "pero el teclado no se bloqueará durante los descansos.\n\n" +
            "Prueba a reiniciar la aplicación o a ejecutarla con los mismos permisos que tus otras aplicaciones.",
            "Breaksy - Bloqueo de teclado no disponible",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }


    private void ShowCharacter()
    {
        Show();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Activate();
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
            UpdateDebugConsole();
        }
    }

    private void OnStateChanged(object? sender, BreaksyStateChangedEventArgs e)
    {
        var level = _settingsService.InterruptionLevel;
        bool isBlocked = e.NewState is BreaksyState.Blocked1 or BreaksyState.Blocked2;

        // Bloqueo de teclado: solo en nivel Intermedio o superior
        bool requiresKeyboardBlock = level >= InterruptionLevel.Intermediate
            && (isBlocked || e.NewState == BreaksyState.Sleeping);
        if (requiresKeyboardBlock && _settingsService.IsKeyboardBlockAvailable)
        {
            _keyboardHook.SuppressKeys = true;
            if (!_keyboardHook.TryStart())
            {
                // El resto del ciclo (voces, ventanas de bloqueo) sigue funcionando sin teclado bloqueado
                _keyboardHook.SuppressKeys = false;
                _settingsService.IsKeyboardBlockAvailable = false;
                _trayIcon.ShowWarning("Breaksy: bloqueo de teclado no disponible",
                    "No se pudo bloquear el teclado. El descanso continúa sin bloqueo.");
            }
        }
        else
        {
            _keyboardHook.SuppressKeys = false;
            _keyboardHook.Stop();
        }

        // Gestión de la Ventana Modal (Pantalla de Bloqueo)
        // Ventana de advertencia centrada: solo en nivel Estricto.
        // Se muestra antes que la de opciones para que esta quede por encima.
        bool requiresTimeUpWindow = level == InterruptionLevel.Strict && isBlocked;

        if (requiresTimeUpWindow && _timeUpWindow == null)
        {
            _timeUpWindow = new TimeUpWindow(canExtend: e.NewState == BreaksyState.Blocked1);
            _timeUpWindow.Show();
        }
        else if (!requiresTimeUpWindow && _timeUpWindow != null)
        {
            _timeUpWindow.Close();
            _timeUpWindow = null;
        }

        // La ventana de opciones también aparece en los avisos, para poder descansar antes del bloqueo
        bool requiresBlockWindow = isBlocked || e.NewState is BreaksyState.Warning or BreaksyState.SeriousWarning;

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
        // No modal y de instancia única, para poder usar la consola de depuración a la vez
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }

        var settingsViewModel = new SettingsViewModel(_settingsService, _stateMachine, _updateService);
        _settingsWindow = new SettingsWindow(settingsViewModel)
        {
            Owner = this
        };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void UpdateDebugConsole()
    {
        if (_settingsService.IsDebugConsoleEnabled && _debugConsoleWindow == null)
        {
            _debugConsoleWindow = new DebugConsoleWindow();
            // Cerrar la consola desde su propia X desactiva la opción en lugar de cerrar la app
            _debugConsoleWindow.Closed += (_, _) =>
            {
                _debugConsoleWindow = null;
                _settingsService.IsDebugConsoleEnabled = false;
            };
            _debugConsoleWindow.Show();
        }
        else if (!_settingsService.IsDebugConsoleEnabled && _debugConsoleWindow != null)
        {
            _debugConsoleWindow.Close();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _stateMachine.StateChanged -= OnStateChanged;
        _settingsService.PropertyChanged -= OnSettingsChanged;

        _viewModel?.Dispose();
        _stateMachine.Dispose();
        _keyboardHook.Dispose();
        _voiceService.Dispose();
        _trayIcon.Dispose();
        _updateService.Dispose();

        _blockWindow?.Close();
        _timeUpWindow?.Close();
        _settingsWindow?.Close();
        _debugConsoleWindow?.Close();
    }

}