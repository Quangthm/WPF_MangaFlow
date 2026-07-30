using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace MangaManagementSystem.WpfMini.ViewModels.Workspaces;

public partial class MangakaWorkspaceViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    // The Chapter List belongs to one specific Series.
    private Guid _chapterSeriesId;
    private string _chapterSeriesTitle = string.Empty;

    [ObservableProperty]
    private ObservableObject? _currentContentViewModel;

    [ObservableProperty]
    private bool _isOnSeries;

    [ObservableProperty]
    private bool _isOnChapters;

    public MangakaWorkspaceViewModel(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        NavigateToSeriesCommand.Execute(null);
    }

    // =========================================================
    // SIDEBAR NAVIGATION
    // =========================================================

    [RelayCommand]
    private async Task NavigateToSeriesAsync()
    {
        SetActiveSection(isSeries: true);

        var viewModel = _serviceProvider
            .GetRequiredService<MangakaSeriesListViewModel>();

        viewModel.CreateSeriesRequested +=
            OpenCreateSeriesEditor;

        viewModel.OpenSeriesRequested +=
            OpenEditSeriesEditor;

        CurrentContentViewModel = viewModel;

        await viewModel.RefreshCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task NavigateToChaptersAsync()
    {
        // A newly created Series may now have an ID even though the
        // workspace originally opened the editor in Create mode.
        CaptureSeriesContextFromCurrentContent();

        if (_chapterSeriesId == Guid.Empty)
        {
            System.Windows.MessageBox.Show(
                "Select or create a series first.\n\n" +
                "Open a series from My Series, then select Chapters.",
                "Series Required",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            await NavigateToSeriesAsync();
            return;
        }

        await ShowChapterListAsync();
    }

    private void SetActiveSection(bool isSeries)
    {
        IsOnSeries = isSeries;
        IsOnChapters = !isSeries;
    }

    // =========================================================
    // SERIES NAVIGATION
    // =========================================================

    private async void OpenCreateSeriesEditor()
    {
        await ShowSeriesEditorAsync(null);
    }

    private async void OpenEditSeriesEditor(Guid seriesId)
    {
        await ShowSeriesEditorAsync(seriesId);
    }

    private async Task ShowSeriesEditorAsync(Guid? seriesId)
    {
        SetActiveSection(isSeries: true);

        var viewModel = _serviceProvider
            .GetRequiredService<SeriesEditorViewModel>();

        viewModel.BackRequested += ReturnToSeriesList;
        viewModel.ManageChaptersRequested += OpenChapterList;

        CurrentContentViewModel = viewModel;

        Debug.WriteLine(
            $"Mangaka workspace content: " +
            $"{nameof(SeriesEditorViewModel)}, " +
            $"SeriesId={seriesId}");

        if (seriesId.HasValue)
        {
            await viewModel.InitializeEditAsync(seriesId.Value);

            // Remember the selected series so the sidebar Chapters
            // button knows which chapter list to open.
            _chapterSeriesId =
                viewModel.SeriesId ?? seriesId.Value;

            _chapterSeriesTitle =
                viewModel.Title;
        }
        else
        {
            // Create mode has no Series ID until the draft is saved.
            _chapterSeriesId = Guid.Empty;
            _chapterSeriesTitle = string.Empty;

            await viewModel.InitializeCreateAsync();
        }
    }

    private void CaptureSeriesContextFromCurrentContent()
    {
        if (CurrentContentViewModel
                is not SeriesEditorViewModel seriesEditor ||
            !seriesEditor.SeriesId.HasValue)
        {
            return;
        }

        _chapterSeriesId =
            seriesEditor.SeriesId.Value;

        _chapterSeriesTitle =
            seriesEditor.Title;
    }

    // =========================================================
    // CHAPTER LIST NAVIGATION
    // =========================================================

    private async void OpenChapterList(
        Guid seriesId,
        string seriesTitle)
    {
        Debug.WriteLine(
            $"Workspace ManageChaptersRequested: " +
            $"SeriesId={seriesId}, Title={seriesTitle}");

        _chapterSeriesId = seriesId;
        _chapterSeriesTitle = seriesTitle;

        await ShowChapterListAsync();
    }

    private async Task ShowChapterListAsync()
    {
        if (_chapterSeriesId == Guid.Empty)
        {
            await NavigateToSeriesAsync();
            return;
        }

        SetActiveSection(isSeries: false);

        var viewModel = _serviceProvider
            .GetRequiredService<ChapterListViewModel>();

        viewModel.BackRequested +=
            ReturnToOriginatingSeries;

        viewModel.CreateChapterRequested +=
            OpenCreateChapterEditor;

        viewModel.OpenChapterRequested +=
            OpenExistingChapterEditor;

        CurrentContentViewModel = viewModel;

        Debug.WriteLine(
            $"Mangaka workspace content: " +
            $"{nameof(ChapterListViewModel)}, " +
            $"SeriesId={_chapterSeriesId}");

        await viewModel.InitializeAsync(
            _chapterSeriesId,
            _chapterSeriesTitle);
    }

    // =========================================================
    // CHAPTER EDITOR NAVIGATION
    // =========================================================

    private void OpenCreateChapterEditor()
    {
        SetActiveSection(isSeries: false);

        Debug.WriteLine(
            $"Workspace CreateChapterRequested: " +
            $"SeriesId={_chapterSeriesId}");

        var viewModel = _serviceProvider
            .GetRequiredService<ChapterEditorViewModel>();

        viewModel.BackRequested +=
            ReturnToChapterList;

        viewModel.InitializeCreate(
            _chapterSeriesId,
            _chapterSeriesTitle);

        CurrentContentViewModel = viewModel;

        Debug.WriteLine(
            $"Mangaka workspace content: " +
            $"{nameof(ChapterEditorViewModel)} (create)");
    }

    private async void OpenExistingChapterEditor(
        MangakaChapterListItemDto chapter)
    {
        SetActiveSection(isSeries: false);

        Debug.WriteLine(
            $"Workspace OpenChapterRequested: " +
            $"ChapterId={chapter.ChapterId}");

        var viewModel = _serviceProvider
            .GetRequiredService<ChapterEditorViewModel>();

        viewModel.BackRequested +=
            ReturnToChapterList;

        CurrentContentViewModel = viewModel;

        try
        {
            await viewModel.InitializeExistingAsync(chapter);
            Debug.WriteLine(
                $"Mangaka workspace content: " +
                $"{nameof(ChapterEditorViewModel)} (existing)");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"The chapter editor could not be opened: {ex.Message}",
                "Open Chapter",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            await ShowChapterListAsync();
        }
    }

    // =========================================================
    // BACK NAVIGATION
    // =========================================================

    private async void ReturnToChapterList()
    {
        Debug.WriteLine(
            "Chapter Editor BackRequested: " +
            "returning to refreshed Chapter List.");

        await ShowChapterListAsync();
    }

    private async void ReturnToOriginatingSeries()
    {
        Debug.WriteLine(
            $"Chapter List BackRequested: " +
            $"returning to SeriesId={_chapterSeriesId}");

        await ShowSeriesEditorAsync(
            _chapterSeriesId);
    }

    private async void ReturnToSeriesList()
    {
        await NavigateToSeriesAsync();
    }
}
