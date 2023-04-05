using System.Windows;
using WPFUI.ViewModels;

namespace WPFUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}