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

        MessageText.Text = Services.LocalizationService.Get(canExtend ? "timeup.can_extend" : "timeup.no_extend");
    }
}
