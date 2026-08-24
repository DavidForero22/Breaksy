using System.ComponentModel;
using System.Windows;
using Breaksy.Services;

namespace Breaksy.Views;

public partial class MainWindow : Window
{
    // Bloqueo de cierre normal
    // true  -> Alt+F4 y "cerrar" desde la barra de tareas quedan bloqueados, solo el botón "Cerrar (prueba)" puede cerrar la ventana.
    // false -> comportamiento normal de una ventana WPF.
    // TODO: cuando se integre con la máquina de estados, esta constante debería sustituirse por una condición real (p. ej. "estado actual es Bloqueado1 o Bloqueado2").
    private const bool BlockNormalClosing = true;

    // Se pone a true justo antes de cerrar desde el botón, para distinguir ese cierre "legítimo" de uno disparado por Windows (Alt+F4, barra de tareas...).
    private bool _allowClosingButton = false;

    private readonly KeyboardHookService _keyboardHook = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
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

    // único botón habilitado para cerrar
    private void OnBtnCloseClick(object sender, RoutedEventArgs e)
    {
        _allowClosingButton = true;
        Close();
    }

    // cancela cualquier cierre que no venga del botón
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (BlockNormalClosing && !_allowClosingButton)
        {
            e.Cancel = true;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _keyboardHook.KeyPressed -= OnKeyPressed;
        _keyboardHook.Dispose();
    }
}