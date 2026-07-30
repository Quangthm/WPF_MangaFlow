using System.Windows;
using System.Windows.Controls;
using MangaManagementSystem.WpfMini.ViewModels.Workspaces;

namespace MangaManagementSystem.WpfMini.Views.Workspaces;

public partial class BoardWorkspaceView : UserControl
{
    private bool _hasLoaded;

    public BoardWorkspaceView()
    {
        InitializeComponent();
    }

    private void BoardWorkspaceView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;

        if (DataContext is BoardWorkspaceViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
