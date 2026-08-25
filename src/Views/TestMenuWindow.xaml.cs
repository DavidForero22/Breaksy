using System.Windows;
using Breaksy.ViewModels;

namespace Breaksy.Views;

public partial class TestMenuWindow : Window
{
    public TestMenuWindow(TestMenuViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}