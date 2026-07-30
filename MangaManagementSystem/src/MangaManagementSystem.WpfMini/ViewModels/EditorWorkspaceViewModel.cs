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
    private bool _isOnProposalReview;

    public EditorWorkspaceViewModel(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // Default Editor landing page.
        NavigateToProposalReviewCommand.Execute(null);
    }

    [RelayCommand]
    private void NavigateToProposalReview()
    {
        IsOnProposalReview = true;

        CurrentContentViewModel = _serviceProvider
            .GetRequiredService<EditorProposalReviewViewModel>();
    }
}