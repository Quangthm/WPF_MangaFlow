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
    private ObservableObject? _currentContentViewModel;

    // Navigation state
    [ObservableProperty]
    private bool _isOnMangakaSeries;

    [ObservableProperty]
    private bool _isOnProposalReview;

    [ObservableProperty]
    private bool _isOnBoardPolls;

    // Role visibility flags
    [ObservableProperty]
    private bool _isMangaka;

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
            IsMangaka = Session.IsMangaka;
            IsEditor = Session.IsEditor;
            IsBoardRole = Session.IsBoardRole;
        }

        // Default landing page after login by role.
        if (IsMangaka)
        {
            NavigateToMangakaSeriesCommand.Execute(null);
        }
        else if (IsEditor)
        {
            NavigateToProposalReviewCommand.Execute(null);
        }
        else if (IsBoardRole)
        {
            NavigateToBoardPollsCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task NavigateToMangakaSeries()
    {
        ClearNavigationState();
        IsOnMangakaSeries = true;
        var viewModel =
        App.ServiceProvider
           .GetRequiredService<MangakaSeriesListViewModel>();

        CurrentContentViewModel = viewModel;
        await viewModel.RefreshCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void NavigateToProposalReview()
    {
        ClearNavigationState();
        IsOnProposalReview = true;

        CurrentContentViewModel =
            App.ServiceProvider.GetRequiredService<EditorProposalReviewViewModel>();
    }

    [RelayCommand]
    private void NavigateToBoardPolls()
    {
        ClearNavigationState();
        IsOnBoardPolls = true;

        // Replace this with BoardPollListViewModel later when you implement board UI.
        CurrentContentViewModel = null;
    }

    [RelayCommand]
    private void Logout()
    {
        _mainVm.LogoutCommand.Execute(null);
    }
    private void ClearNavigationState()
    {
        IsOnMangakaSeries = false;
        IsOnProposalReview = false;
        IsOnBoardPolls = false;
    }
}
