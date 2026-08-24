using Breaksy.Models;

namespace Breaksy.Services;

/// <summary>
/// Datos de una transición de estado emitida por BreaksyStateMachine.
/// </summary>
public class BreaksyStateChangedEventArgs : EventArgs
{
    public BreaksyState OldState { get; }
    public BreaksyState NewState { get; }

    public BreaksyStateChangedEventArgs(BreaksyState oldState, BreaksyState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}