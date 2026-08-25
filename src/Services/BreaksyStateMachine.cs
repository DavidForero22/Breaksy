using System.Windows.Threading;
using Breaksy.Models;

namespace Breaksy.Services;

/// <summary>
/// Máquina de estados. No toca la UI: expone el estado actual, el tiempo restante, y eventos para que la ventana reaccione.
/// El temporizador usa DispatcherTimer por simplicidad; toda la lógica de negocio vive aquí, no en MainWindow.
/// </summary>
public class BreaksyStateMachine : IDisposable
{
    public static readonly TimeSpan NormalDuration = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan ExtensionDuration = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan WarningDuration = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan SleepDuration = TimeSpan.FromSeconds(10);

    private readonly DispatcherTimer _timer;
    private BreaksyState _stateBeforePause;

    public BreaksyState CurrentState { get; private set; } = BreaksyState.Idle;
    public TimeSpan RemainingTime { get; private set; } = TimeSpan.Zero;
    public bool ExtensionUsed { get; private set; } = false;

    public event EventHandler<BreaksyStateChangedEventArgs>? StateChanged;
    public event EventHandler<TimeSpan>? Tick;

    public BreaksyStateMachine()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
    }

    // === Acciones disparadas por el usuario ===

    /// <summary>Arranca un nuevo ciclo. Válido desde Idle, Disabled o Waiting.</summary>
    public void Start()
    {
        if (CurrentState is not (BreaksyState.Idle or BreaksyState.Disabled or BreaksyState.Waiting)) return;
        ExtensionUsed = false;
        EnterCountdown(BreaksyState.Awaken, NormalDuration);
    }

    /// <summary>Pausa el ciclo actual. Válido solo durante Awaken, Warning o SeriousWarning.</summary>
    public void Pause()
    {
        if (CurrentState is not (BreaksyState.Awaken or BreaksyState.Warning or BreaksyState.SeriousWarning)) return;
        _timer.Stop();
        _stateBeforePause = CurrentState;
        ChangeState(BreaksyState.Paused);
    }

    /// <summary>Reanuda tras una pausa, retomando el tiempo restante exacto.</summary>
    public void Resume()
    {
        if (CurrentState != BreaksyState.Paused) return;
        ChangeState(_stateBeforePause);
        _timer.Start();
    }

    /// <summary>Desactiva la app. No permitido durante Blocked1, Blocked2 o Sleeping.</summary>
    public void Disable()
    {
        if (CurrentState is BreaksyState.Blocked1 or BreaksyState.Blocked2 or BreaksyState.Sleeping) return;
        _timer.Stop();
        RemainingTime = TimeSpan.Zero;
        ExtensionUsed = false;
        ChangeState(BreaksyState.Disabled);
    }

    /// <summary>Botón "¡5 minutos más!". Solo disponible en Blocked1.</summary>
    public void RequestExtension()
    {
        if (CurrentState != BreaksyState.Blocked1) return;
        ExtensionUsed = true;
        EnterCountdown(BreaksyState.Awaken, ExtensionDuration);
    }

    /// <summary>Botón "Descansar". Disponible en Blocked1 y Blocked2.</summary>
    public void RequestRest()
    {
        if (CurrentState is not (BreaksyState.Blocked1 or BreaksyState.Blocked2)) return;
        EnterCountdown(BreaksyState.Sleeping, SleepDuration);
    }

    // === ACCIONES DE DEPURACIÓN ===

    /// <summary>Fuerza un cambio de estado ignorando las reglas normales.</summary>
    public void ForceState(BreaksyState newState)
    {
        _timer.Stop();
        var duration = newState switch
        {
            BreaksyState.Awaken => NormalDuration,
            BreaksyState.Warning or BreaksyState.SeriousWarning => WarningDuration,
            BreaksyState.Sleeping => SleepDuration,
            _ => TimeSpan.Zero
        };

        RemainingTime = duration;
        ChangeState(newState);

        if (duration > TimeSpan.Zero) _timer.Start();
    }

    /// <summary>Forzar un tiempo restante específico sin cambiar el estado actual.</summary>
    public void ForceTime(TimeSpan time)
    {
        RemainingTime = time;
        Tick?.Invoke(this, RemainingTime);
    }

    // === Temporizador interno ===

    private void OnTimerTick(object? sender, EventArgs e)
    {
        RemainingTime -= _timer.Interval;
        if (RemainingTime < TimeSpan.Zero) RemainingTime = TimeSpan.Zero;

        Tick?.Invoke(this, RemainingTime);

        if (RemainingTime <= TimeSpan.Zero) OnCountdownExpired();
    }

    private void OnCountdownExpired()
    {
        _timer.Stop();

        switch (CurrentState)
        {
            case BreaksyState.Awaken when !ExtensionUsed: EnterCountdown(BreaksyState.Warning, WarningDuration); break;
            case BreaksyState.Awaken when ExtensionUsed: EnterCountdown(BreaksyState.SeriousWarning, WarningDuration); break;
            case BreaksyState.Warning: ChangeState(BreaksyState.Blocked1); break;
            case BreaksyState.SeriousWarning: ChangeState(BreaksyState.Blocked2); break;
            case BreaksyState.Sleeping:
                ExtensionUsed = false;
                ChangeState(BreaksyState.Waiting);
                break;
        }
    }

    private void EnterCountdown(BreaksyState state, TimeSpan duration)
    {
        RemainingTime = duration;
        ChangeState(state);
        _timer.Start();
    }

    private void ChangeState(BreaksyState newState)
    {
        var oldState = CurrentState;
        CurrentState = newState;
        StateChanged?.Invoke(this, new BreaksyStateChangedEventArgs(oldState, newState));
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }
}