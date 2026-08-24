using System.Windows;
using Breaksy.Services;

namespace Breaksy.Views;

public partial class MainWindow : Window
{
    private readonly KeyboardHookService _keyboardHook = new();

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
        Console.WriteLine("[Breaksy] Hook de teclado activo. Pulsa teclas para verlas aquí.\n");
    }

    private void OnKeyPressed(object? sender, KeyboardKeyEventArgs e)
    {
        var estado = e.IsKeyDown ? "DOWN" : "UP  ";
        Console.WriteLine($"[{estado}] {e.Key}");
    }

    // Liberar el hook al cerrar. Si no, queda colgado hasta que el proceso muere.
    private void OnClosed(object? sender, EventArgs e)
    {
        _keyboardHook.KeyPressed -= OnKeyPressed;
        _keyboardHook.Dispose();
    }
}
