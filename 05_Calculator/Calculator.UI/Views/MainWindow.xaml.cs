using System.Windows;
using System.Windows.Input;
using Calculator.UI.ViewModels;

namespace Calculator.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.HandleKeyboardInput(e.Key, Keyboard.Modifiers);
        }
    }

    private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsNavDrawerOpen = false;
            vm.IsHistoryOpen = false;
            vm.IsMemoryDrawerOpen = false;
            vm.IsTrigFlyoutOpen = false;
            vm.IsFuncFlyoutOpen = false;
            vm.IsTreeModalOpen = false;
        }
    }
}
