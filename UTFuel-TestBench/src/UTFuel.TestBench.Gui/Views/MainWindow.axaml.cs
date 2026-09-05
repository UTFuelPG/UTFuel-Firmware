using Avalonia.Controls;

using UTFuel.TestBench.Gui.ViewModels;


namespace UTFuel.TestBench.Gui.Views;


public partial class MainWindow :
    Window
{
    public MainWindow()
    {
        InitializeComponent();


        Closed +=
            async (
                _,
                _
            ) =>
            {
                if (
                    DataContext
                    is
                    MainWindowViewModel
                    viewModel
                )
                {
                    await viewModel
                        .ShutdownAsync();
                }
            };
    }
}