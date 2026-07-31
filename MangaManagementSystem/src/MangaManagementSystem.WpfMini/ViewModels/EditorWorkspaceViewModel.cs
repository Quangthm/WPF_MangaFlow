using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

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
        CurrentContentViewModel = _serviceProvider
            .GetRequiredService<EditorDashboardViewModel>();
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