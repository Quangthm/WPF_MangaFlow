using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MangaManagementSystem.WpfMini.Models;

namespace MangaManagementSystem.WpfMini.ViewModels.Workspaces;

public partial class EditorWorkspaceViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableObject? _currentContentViewModel;

    [ObservableProperty]
    private bool _isOnDashboard;

    [ObservableProperty]
    private bool _isOnProposalReview;

    [ObservableProperty]
    private bool _isOnChapterReview;

    public EditorWorkspaceViewModel(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // Default Editor landing page.
        NavigateToDashboardCommand.Execute(null);
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        ClearNavigationState();
        IsOnDashboard = true;

        var dashboardVm = _serviceProvider
            .GetRequiredService<EditorDashboardViewModel>();

        // Wire the "→ Review" click on claimed proposals to navigate
        // directly to the Proposal Review tab with the item pre-selected.
        dashboardVm.NavigateToProposalReview += OnDashboardNavigateToProposalReview;

        CurrentContentViewModel = dashboardVm;
    }

    private async void OnDashboardNavigateToProposalReview(ProposalQueueItem item)
    {
        ClearNavigationState();
        IsOnProposalReview = true;

        var reviewVm = _serviceProvider
            .GetRequiredService<EditorProposalReviewViewModel>();

        CurrentContentViewModel = reviewVm;

        // Load the queue and auto-select the specific proposal (awaited so
        // the ListBox binding finds the correct item instance in the queue).
        await reviewVm.InitializeWithProposalAsync(item.SeriesProposalId);
    }

    [RelayCommand]
    private void NavigateToProposalReview()
    {
        ClearNavigationState();
        IsOnProposalReview = true;
        CurrentContentViewModel = _serviceProvider
            .GetRequiredService<EditorProposalReviewViewModel>();
    }

    [RelayCommand]
    private void NavigateToChapterReview()
    {
        ClearNavigationState();
        IsOnChapterReview = true;
        CurrentContentViewModel = _serviceProvider
            .GetRequiredService<EditorChapterReviewViewModel>();
    }

    private void ClearNavigationState()
    {
        IsOnDashboard = false;
        IsOnProposalReview = false;
        IsOnChapterReview = false;
    }
}