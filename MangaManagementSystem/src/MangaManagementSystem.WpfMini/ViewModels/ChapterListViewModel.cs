using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.WpfMini.Interfaces;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MangaManagementSystem.WpfMini.ViewModels;

public partial class ChapterListViewModel : ObservableObject
{
    private readonly IMangakaChapterApiClient _chapterApiClient;
    private readonly List<MangakaChapterListItemDto> _allChapters = [];

    public event Action? BackRequested;
    public event Action? CreateChapterRequested;
    public event Action<MangakaChapterListItemDto>? OpenChapterRequested;

    public IReadOnlyList<string> StatusFilters { get; } =
    [
        "ALL",
        "DRAFT",
        "REVISION_REQUESTED",
        "UNDER_REVIEW",
        "APPROVED",
        "SCHEDULED",
        "CANCELLED"
    ];

    [ObservableProperty]
    private Guid _seriesId;

    [ObservableProperty]
    private string _seriesTitle = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedStatusCode = "ALL";

    [ObservableProperty]
    private ObservableCollection<MangakaChapterListItemDto> _filteredChapters = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public int ResultCount => FilteredChapters.Count;
    public bool HasNoChapters =>
        !IsBusy &&
        string.IsNullOrEmpty(ErrorMessage) &&
        _allChapters.Count == 0;
    public bool HasNoFilteredResults =>
        !IsBusy &&
        string.IsNullOrEmpty(ErrorMessage) &&
        _allChapters.Count > 0 &&
        FilteredChapters.Count == 0;

    public ChapterListViewModel(IMangakaChapterApiClient chapterApiClient)
    {
        _chapterApiClient = chapterApiClient;
    }

    public async Task InitializeAsync(Guid seriesId, string seriesTitle)
    {
        Debug.WriteLine(
            $"ChapterList Initialize: SeriesId={seriesId}, Title={seriesTitle}");
        if (seriesId == Guid.Empty)
            throw new ArgumentException("A valid series is required.", nameof(seriesId));
        if (string.IsNullOrWhiteSpace(seriesTitle))
            throw new ArgumentException("Series title is required.", nameof(seriesTitle));

        SeriesId = seriesId;
        SeriesTitle = seriesTitle.Trim();
        SearchText = string.Empty;
        SelectedStatusCode = "ALL";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy || SeriesId == Guid.Empty)
            return;

        await LoadAsync();
    }

    [RelayCommand]
    private void Back()
    {
        if (!IsBusy && SeriesId != Guid.Empty)
            BackRequested?.Invoke();
    }

    [RelayCommand]
    private void CreateChapter()
    {
        if (!IsBusy && SeriesId != Guid.Empty)
        {
            Debug.WriteLine($"ChapterList CreateChapterRequested: SeriesId={SeriesId}");
            CreateChapterRequested?.Invoke();
        }
    }

    [RelayCommand]
    private void OpenChapter(MangakaChapterListItemDto? chapter)
    {
        if (!IsBusy && chapter is not null)
        {
            Debug.WriteLine($"ChapterList OpenChapterRequested: ChapterId={chapter.ChapterId}");
            OpenChapterRequested?.Invoke(chapter);
        }
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        NotifyEmptyState();

        try
        {
            var chapters = await _chapterApiClient.GetSeriesChaptersAsync(SeriesId);
            _allChapters.Clear();
            _allChapters.AddRange(chapters);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _allChapters.Clear();
            ApplyFilter();
            ErrorMessage = $"Failed to load chapters: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            NotifyEmptyState();
        }
    }

    private void ApplyFilter()
    {
        var query = _allChapters.AsEnumerable();
        var search = SearchText.Trim();

        if (search.Length > 0)
        {
            query = query.Where(chapter =>
                chapter.ChapterNumberLabel.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ||
                (chapter.ChapterTitle?.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.Equals(SelectedStatusCode, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(chapter =>
                string.Equals(
                    chapter.StatusCode,
                    SelectedStatusCode,
                    StringComparison.OrdinalIgnoreCase));
        }

        FilteredChapters = new ObservableCollection<MangakaChapterListItemDto>(
            query.OrderByDescending(chapter => chapter.UpdatedAtUtc ?? chapter.CreatedAtUtc));
        OnPropertyChanged(nameof(ResultCount));
        NotifyEmptyState();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedStatusCodeChanged(string value) => ApplyFilter();
    partial void OnErrorMessageChanged(string value) => NotifyEmptyState();

    private void NotifyEmptyState()
    {
        OnPropertyChanged(nameof(HasNoChapters));
        OnPropertyChanged(nameof(HasNoFilteredResults));
    }
}
