using System.Windows;
using Breaksy.ViewModels;

namespace Breaksy.Views;

public partial class BlockWindow : Window
{
    public BlockWindow(BlockViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;

        Left = workArea.Right - ActualWidth - 20;
        Top = workArea.Bottom - 180 - 20 - ActualHeight;
    }
}