using System.Windows;

namespace Breaksy.Views;

/// <summary>
/// Ventana de advertencia centrada en pantalla (nivel de interrupción Estricto).
/// Se cierra automáticamente al salir del estado de bloqueo.
/// </summary>
public partial class TimeUpWindow : Window
{
    public TimeUpWindow(bool canExtend)
    {
        InitializeComponent();

        MessageText.Text = canExtend
            ? "Llevas un buen rato sin parar. Descansa la vista o pide 5 minutos más desde las opciones junto al personaje."
            : "Ya usaste la prórroga. Es hora de descansar: pulsa \"Descansar\" junto al personaje.";
    }
}
