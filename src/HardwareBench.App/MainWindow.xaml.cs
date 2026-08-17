using System.Windows;
using HardwareBench.App.ViewModels;

namespace HardwareBench.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}