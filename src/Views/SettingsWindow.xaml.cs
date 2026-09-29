using System.Windows;
using Breaksy.ViewModels;

namespace Breaksy.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        Native.WindowAnimationHelper.Enable(this);
        DataContext = viewModel;
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
