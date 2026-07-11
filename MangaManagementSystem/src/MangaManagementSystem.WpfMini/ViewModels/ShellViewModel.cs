using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MangaManagementSystem.WpfMini.Models;

namespace MangaManagementSystem.WpfMini.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainVm;

    [ObservableProperty]
    private CurrentUserSession? _session;

    [ObservableProperty]
    private ObservableObject? _currentEditorViewModel;

    // Navigation state
    [ObservableProperty]
    private bool _isOnProposalReview;

    [ObservableProperty]
    private bool _isOnBoardPolls;

    // Role visibility flags
    [ObservableProperty]
    private bool _isEditor;

    [ObservableProperty]
    private bool _isBoardRole;

    public ShellViewModel(MainWindowViewModel mainVm)
    {
        _mainVm = mainVm;
        Session = mainVm.CurrentSession;

        // Set role flags
        if (Session is not null)
        {
            IsEditor = Session.IsEditor;
            IsBoardRole = Session.IsBoardRole;
        }

        // Default to Proposal Review for editor
        if (IsEditor)
        {
            NavigateToProposalReviewCommand.Execute(null);
        }
    }

    [RelayCommand]
    private void NavigateToProposalReview()
    {
        IsOnProposalReview = true;
        IsOnBoardPolls = false;
        CurrentEditorViewModel = App.ServiceProvider.GetRequiredService<EditorProposalReviewViewModel>();
    }

    [RelayCommand]
    private void NavigateToBoardPolls()
    {
        IsOnProposalReview = false;
        IsOnBoardPolls = true;
        // BoardPollListViewModel sẽ được tạo sau
        CurrentEditorViewModel = null;
    }

    [RelayCommand]
    private void Logout()
    {
        _mainVm.LogoutCommand.Execute(null);
    }
}
