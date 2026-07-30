using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MangaManagementSystem.WpfMini.ViewModels.Workspaces;

public partial class BoardWorkspaceViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isOnBoardPolls = true;

    [RelayCommand]
    private void NavigateToBoardPolls()
    {
        IsOnBoardPolls = true;

        // TODO:
        // Resolve and display BoardPollListViewModel
        // when the Board UI is implemented.
    }
}