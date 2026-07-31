using System.Windows;
using System.Windows.Controls;
using MangaManagementSystem.WpfMini.ViewModels.Workspaces;

namespace MangaManagementSystem.WpfMini.Views.Workspaces;

public partial class BoardWorkspaceView : UserControl
{
    private bool _initialized;

    public BoardWorkspaceView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        if (DataContext is BoardWorkspaceViewModel viewModel)
        {
            await viewModel.InitializeCommand.ExecuteAsync(null);
        }
    }
}
