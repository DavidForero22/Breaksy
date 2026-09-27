using System;
using System.Collections.Generic;

namespace Breaksy.Services;

/// <summary>
/// Servicio centralizado de registro con marca de tiempo.
/// Guarda un historial en memoria para que la consola de depuración lo muestre al abrirse.
/// </summary>
public static class LogService
{
    private const int MaxEntries = 1000;
    private static readonly Queue<string> _entries = new();
    private static readonly object _lock = new();

    /// <summary>Se dispara con cada nueva línea (puede llegar desde cualquier hilo).</summary>
    public static event Action<string>? EntryAdded;

    public static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var line = $"[{timestamp}] {message}";

        lock (_lock)
        {
            _entries.Enqueue(line);
            if (_entries.Count > MaxEntries) _entries.Dequeue();
        }

        System.Diagnostics.Debug.WriteLine(line);
        EntryAdded?.Invoke(line);
    }

    public static IReadOnlyList<string> GetHistory()
    {
        lock (_lock) return _entries.ToArray();
    }
}
