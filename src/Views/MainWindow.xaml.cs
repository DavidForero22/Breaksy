using System.ComponentModel;
using System.Windows;
using Breaksy.Models;
using Breaksy.Services;

namespace Breaksy.Views;

public partial class MainWindow : Window
{
    private readonly KeyboardHookService _keyboardHook = new();
    private readonly BreaksyStateMachine _stateMachine = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _keyboardHook.KeyPressed += OnKeyPressed;
        _keyboardHook.Start();
        Console.WriteLine("[Breaksy] Hook de teclado activo.\n");

        _stateMachine.StateChanged += OnStateChanged;
        _stateMachine.Tick += OnTick;

        UpdateStateLabel(_stateMachine.CurrentState);
        UpdateTimeLabel(_stateMachine.RemainingTime);
        UpdateButtons(_stateMachine.CurrentState);
    }

    private void OnKeyPressed(object? sender, KeyboardKeyEventArgs e)
    {
        var estado = e.IsKeyDown ? "DOWN" : "UP  ";
        Console.WriteLine($"[{estado}] {e.Key}");
    }

    private void OnStateChanged(object? sender, BreaksyStateChangedEventArgs e)
    {
        Console.WriteLine($"[Breaksy] {e.OldState} -> {e.NewState}");
        UpdateStateLabel(e.NewState);
        UpdateTimeLabel(_stateMachine.RemainingTime);
        UpdateButtons(e.NewState);

        // Bloqueo de teclas dependiendo del estado
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

    private void OnTick(object? sender, TimeSpan remaining) => UpdateTimeLabel(remaining);

    private void UpdateStateLabel(BreaksyState state) => TxTStatus.Text = $"Estado: {state}";

    private void UpdateTimeLabel(TimeSpan remaining)
    {
        TxtTime.Text = remaining <= TimeSpan.Zero
            ? "--:--"
            : $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";
    }

    // Habilita/deshabilita botones según el estado, solo para que las pruebas manuales sean más claras. 
    // Los guardas reales viven en BreaksyStateMachine; esto es puramente cosmético.
    private void UpdateButtons(BreaksyState state)
    {
        BtnStart.IsEnabled = state is BreaksyState.Idle or BreaksyState.Disabled or BreaksyState.Waiting;

        BtnPause.IsEnabled = state is BreaksyState.Awaken or BreaksyState.Warning
            or BreaksyState.SeriousWarning or BreaksyState.Paused;
        BtnPause.Content = state == BreaksyState.Paused ? "Reanudar" : "Pausar";

        BtnExtend.IsEnabled = state == BreaksyState.Blocked1;
        BtnRest.IsEnabled = state is BreaksyState.Blocked1 or BreaksyState.Blocked2;
        BtnDisable.IsEnabled = state is not (BreaksyState.Blocked1 or BreaksyState.Blocked2 or BreaksyState.Sleeping);
    }

    private void OnBtnStartClick(object sender, RoutedEventArgs e) => _stateMachine.Start();

    private void OnBtnPauseClick(object sender, RoutedEventArgs e)
    {
        if (_stateMachine.CurrentState == BreaksyState.Paused)
        {
            _stateMachine.Resume();
        }
        else
        {
            _stateMachine.Pause();
        }
    }

    private void OnBtnExtendClick(object sender, RoutedEventArgs e) => _stateMachine.RequestExtension();

    private void OnBtnRestClick(object sender, RoutedEventArgs e) => _stateMachine.RequestRest();

    private void OnBtnDisableClick(object sender, RoutedEventArgs e) => _stateMachine.Disable();

    // único botón habilitado para cerrar
    private void OnBtnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _keyboardHook.KeyPressed -= OnKeyPressed;
        _keyboardHook.Dispose();

        _stateMachine.StateChanged -= OnStateChanged;
        _stateMachine.Tick -= OnTick;
        _stateMachine.Dispose();
    }
}