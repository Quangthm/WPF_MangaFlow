using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace MangaManagementSystem.WpfMini.ViewModels.Workspaces;

public partial class MangakaWorkspaceViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableObject? _currentContentViewModel;

    [ObservableProperty]
    private bool _isOnSeries;

    public MangakaWorkspaceViewModel(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // Default Mangaka landing page.
        NavigateToSeriesCommand.Execute(null);
    }

    [RelayCommand]
    private async Task NavigateToSeriesAsync()
    {
        IsOnSeries = true;

        var viewModel = _serviceProvider
            .GetRequiredService<MangakaSeriesListViewModel>();

        viewModel.CreateSeriesRequested +=
            OpenCreateSeriesEditor;

        viewModel.OpenSeriesRequested +=
            OpenEditSeriesEditor;

        CurrentContentViewModel = viewModel;

        await viewModel.RefreshCommand
            .ExecuteAsync(null);
    }

    private async void OpenCreateSeriesEditor()
    {
        var viewModel = _serviceProvider
            .GetRequiredService<SeriesEditorViewModel>();

        viewModel.BackRequested +=
            ReturnToSeriesList;

        CurrentContentViewModel = viewModel;

        await viewModel.InitializeCreateAsync();
    }

    private async void OpenEditSeriesEditor(
        Guid seriesId)
    {
        var viewModel = _serviceProvider
            .GetRequiredService<SeriesEditorViewModel>();

        viewModel.BackRequested +=
            ReturnToSeriesList;

        CurrentContentViewModel = viewModel;

        await viewModel.InitializeEditAsync(seriesId);
    }

    private async void ReturnToSeriesList()
    {
        await NavigateToSeriesAsync();
    }
}