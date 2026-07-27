using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.WpfMini.ViewModels;

public abstract partial class SeriesListViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _pageTitle = "Series";

    [ObservableProperty]
    private string _pageSubtitle = "Browse series records.";

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedStatusCode = "ALL";

    [ObservableProperty]
    private ObservableCollection<string> _statusFilters =
    [
        "ALL",
        "PROPOSAL_DRAFT",
        "UNDER_EDITORIAL_REVIEW",
        "UNDER_BOARD_REVIEW",
        "SERIALIZED",
        "HIATUS",
        "COMPLETED",
        "CANCELLED"
    ];

    [ObservableProperty]
    private ObservableCollection<SeriesDto> _allSeries = [];

    [ObservableProperty]
    private ObservableCollection<SeriesDto> _filteredSeries = [];

    [ObservableProperty]
    private SeriesDto? _selectedSeries;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _showCreateButton;

    [ObservableProperty]
    private string _createButtonText = "Create Series";

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedStatusCodeChanged(string value)
    {
        ApplyFilter();
    }

    protected void ApplyFilter()
    {
        var search = SearchText.Trim().ToLowerInvariant();
        var status = SelectedStatusCode;

        var filtered = AllSeries.Where(series =>
        {
            var genreText = string.Join(" ", series.Genres.Select(g => g.GenreName));
            var tagText = string.Join(" ", series.Tags.Select(t => t.TagName));

            var matchesSearch =
                string.IsNullOrWhiteSpace(search)
                || series.Title.ToLowerInvariant().Contains(search)
                || series.Slug.ToLowerInvariant().Contains(search)
                || genreText.ToLowerInvariant().Contains(search)
                || tagText.ToLowerInvariant().Contains(search);

            var matchesStatus =
                status == "ALL"
                || string.IsNullOrWhiteSpace(status)
                || series.StatusCode == status;

            return matchesSearch && matchesStatus;
        });

        FilteredSeries = new ObservableCollection<SeriesDto>(filtered);
    }

    [RelayCommand]
    protected async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await LoadSeriesAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load series: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    protected abstract void CreateSeries();

    [RelayCommand]
    protected abstract void OpenSeries(SeriesDto? series);

    protected abstract Task LoadSeriesAsync();
}