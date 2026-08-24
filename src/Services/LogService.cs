using System;

namespace Breaksy.Services;

/// <summary>
/// Servicio centralizado para imprimir mensajes en la consola con marca de tiempo.
/// </summary>
public static class LogService
{
    public static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"[{timestamp}] {message}");
    }
}