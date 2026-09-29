using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Breaksy.ViewModels;

namespace Breaksy.Views;

/// <summary>Editor de imágenes y sonidos del personaje, dentro de la pestaña Personalización.</summary>
public partial class CharacterEditorView : UserControl
{
    public CharacterEditorView()
    {
        InitializeComponent();
    }

    // Clic en un hueco vacío: abre el explorador. En uno lleno no hace nada (se quita con su X)
    private void OnSlotClick(object sender, MouseButtonEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is AssetSlotViewModel { IsEmpty: true } slot)
            slot.BrowseCommand.Execute(null);
    }

    // Mientras se arrastra un archivo encima, el borde se resalta (el IsMouseOver no se activa durante un arrastre)
    private void OnSlotDragEnter(object sender, DragEventArgs e) => Highlight(sender, e.Data.GetDataPresent(DataFormats.FileDrop));

    private void OnSlotDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnSlotDragLeave(object sender, DragEventArgs e) => Highlight(sender, false);

    private static void Highlight(object sender, bool on)
    {
        if (sender is not Border border) return;

        if (on)
            border.BorderBrush = (Brush)Application.Current.FindResource("AccentBrush");
        else
            border.ClearValue(Border.BorderBrushProperty);
    }

    private void OnSlotDrop(object sender, DragEventArgs e)
    {
        Highlight(sender, false);

        if (((FrameworkElement)sender).DataContext is not AssetSlotViewModel slot ||
            e.Data.GetData(DataFormats.FileDrop) is not string[] files)
            return;

        if (slot.AddFiles(files) > 0)
            MessageBox.Show($"Algún archivo no se ha añadido: solo se admiten {slot.FormatsText} y hasta {AssetListViewModel.MaxSlots} por lista.",
                "Breaksy", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
