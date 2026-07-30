using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.WpfMini.Interfaces;
using MangaManagementSystem.WpfMini.Services;
using MangaManagementSystem.WpfMini.Services.Series;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace MangaManagementSystem.WpfMini.ViewModels;

public partial class SeriesEditorViewModel : ObservableObject
{
    private readonly IMangakaSeriesApiClient _seriesApiClient;
    private readonly IReferenceDataApiClient _referenceDataApiClient;

    public event Action? BackRequested;

    [ObservableProperty]
    private Guid? _seriesId;

    [ObservableProperty]
    private bool _isCreateMode = true;

    [ObservableProperty]
    private string _pageTitle = "Create Series";

    [ObservableProperty]
    private string _statusCode = "PROPOSAL_DRAFT";

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _slugPreview = string.Empty;

    [ObservableProperty]
    private string _synopsis = string.Empty;

    [ObservableProperty]
    private string _contentLanguageCode = "ja";

    [ObservableProperty]
    private string? _publicationFrequencyCode;

    [ObservableProperty]
    private ObservableCollection<LookupSelectionItem> _genres = [];

    [ObservableProperty]
    private ObservableCollection<LookupSelectionItem> _tags = [];

    [ObservableProperty]
    private string? _selectedCoverFilePath;

    [ObservableProperty]
    private string? _coverPreviewSource;

    [ObservableProperty]
    private string? _proposalFilePath;

    [ObservableProperty]
    private string _cancellationReason = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    public bool CanEditDraft =>
        IsCreateMode ||
        string.Equals(
            StatusCode,
            "PROPOSAL_DRAFT",
            StringComparison.OrdinalIgnoreCase);

    public bool CanUseDraftActions =>
        !IsCreateMode &&
        SeriesId.HasValue &&
        string.Equals(
            StatusCode,
            "PROPOSAL_DRAFT",
            StringComparison.OrdinalIgnoreCase);

    public string SaveButtonText =>
        IsCreateMode
            ? "Create Draft"
            : "Save Changes";

    public string SelectedCoverFileName =>
        string.IsNullOrWhiteSpace(SelectedCoverFilePath)
            ? "No new cover selected"
            : Path.GetFileName(SelectedCoverFilePath);

    public string ProposalFileName =>
        string.IsNullOrWhiteSpace(ProposalFilePath)
            ? "No proposal document selected"
            : Path.GetFileName(ProposalFilePath);

    public SeriesEditorViewModel(
        IMangakaSeriesApiClient seriesApiClient,
        IReferenceDataApiClient referenceDataApiClient)
    {
        _seriesApiClient = seriesApiClient;
        _referenceDataApiClient = referenceDataApiClient;
    }

    public async Task InitializeCreateAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            SeriesId = null;
            IsCreateMode = true;
            PageTitle = "Create Series";
            StatusCode = "PROPOSAL_DRAFT";

            Title = string.Empty;
            SlugPreview = string.Empty;
            Synopsis = string.Empty;

            ContentLanguageCode = "ja";
            PublicationFrequencyCode = null;

            SelectedCoverFilePath = null;
            CoverPreviewSource = null;
            ProposalFilePath = null;
            CancellationReason = string.Empty;

            await LoadReferenceDataAsync(
                selectedGenreIds: [],
                selectedTagIds: []);

            NotifyModeProperties();
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Failed to prepare the series editor: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task InitializeEditAsync(Guid seriesId)
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            await LoadSeriesAsync(seriesId);
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Failed to load the series: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSeriesAsync(Guid seriesId)
    {
        var series =
            await _seriesApiClient.GetSeriesByIdAsync(seriesId);

        SeriesId = series.SeriesId;

        IsCreateMode = false;
        PageTitle = "Edit Series";

        StatusCode = series.StatusCode;

        Title = series.Title;
        SlugPreview = series.Slug;
        Synopsis = series.Synopsis;

        ContentLanguageCode =
            series.ContentLanguageCode;

        PublicationFrequencyCode =
            series.PublicationFrequencyCode;

        CoverPreviewSource =
            series.CoverUrl;

        SelectedCoverFilePath = null;
        ProposalFilePath = null;
        CancellationReason = string.Empty;

        await LoadReferenceDataAsync(
            series.Genres.Select(x => x.GenreId),
            series.Tags.Select(x => x.TagId));

        NotifyModeProperties();
    }

    private async Task LoadReferenceDataAsync(
        IEnumerable<Guid> selectedGenreIds,
        IEnumerable<Guid> selectedTagIds)
    {
        var genreTask =
            _referenceDataApiClient.GetGenresAsync();

        var tagTask =
            _referenceDataApiClient.GetTagsAsync();

        await Task.WhenAll(
            genreTask,
            tagTask);

        var selectedGenres =
            selectedGenreIds.ToHashSet();

        var selectedTags =
            selectedTagIds.ToHashSet();

        Genres =
            new ObservableCollection<LookupSelectionItem>(
                genreTask.Result.Select(
                    genre => new LookupSelectionItem(
                        genre.GenreId,
                        genre.GenreName,
                        selectedGenres.Contains(
                            genre.GenreId))));

        Tags =
            new ObservableCollection<LookupSelectionItem>(
                tagTask.Result.Select(
                    tag => new LookupSelectionItem(
                        tag.TagId,
                        tag.TagName,
                        selectedTags.Contains(
                            tag.TagId))));
    }

    [RelayCommand]
    private void BrowseCover()
    {
        if (!CanEditDraft || IsBusy)
            return;

        var dialog = new OpenFileDialog
        {
            Title = "Select Series Cover",
            Filter =
                "Image Files (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        SelectedCoverFilePath =
            dialog.FileName;

        CoverPreviewSource =
            dialog.FileName;

        OnPropertyChanged(
            nameof(SelectedCoverFileName));
    }

    [RelayCommand]
    private void BrowseProposal()
    {
        if (!CanUseDraftActions || IsBusy)
            return;

        var dialog = new OpenFileDialog
        {
            Title = "Select Proposal Document",
            Filter =
                "Proposal Documents (*.pdf;*.doc;*.docx)|*.pdf;*.doc;*.docx",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        ProposalFilePath =
            dialog.FileName;

        OnPropertyChanged(
            nameof(ProposalFileName));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy || !CanEditDraft)
            return;

        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        var genreIds =
            Genres
                .Where(x => x.IsSelected)
                .Select(x => x.Id)
                .ToArray();

        var tagIds =
            Tags
                .Where(x => x.IsSelected)
                .Select(x => x.Id)
                .ToArray();

        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage =
                "Title is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Synopsis))
        {
            ErrorMessage =
                "Synopsis is required.";
            return;
        }

        if (genreIds.Length == 0)
        {
            ErrorMessage =
                "Select at least one genre.";
            return;
        }

        IsBusy = true;

        try
        {
            if (IsCreateMode)
            {
                var result =
                    await _seriesApiClient.CreateDraftAsync(
                        title: Title.Trim(),
                        synopsis: Synopsis.Trim(),
                        genreIds: genreIds,
                        tagIds: tagIds,
                        contentLanguageCode:
                            ContentLanguageCode,
                        publicationFrequencyCode:
                            PublicationFrequencyCode,
                        coverFilePath:
                            SelectedCoverFilePath);

                await LoadSeriesAsync(
                    result.SeriesId);

                SuccessMessage =
                    "Series draft created successfully.";
            }
            else
            {
                if (!SeriesId.HasValue)
                {
                    ErrorMessage =
                        "No series is currently loaded.";
                    return;
                }

                await _seriesApiClient.UpdateDraftAsync(
                    seriesId: SeriesId.Value,
                    title: Title.Trim(),
                    synopsis: Synopsis.Trim(),
                    genreIds: genreIds,
                    tagIds: tagIds,
                    contentLanguageCode:
                        ContentLanguageCode,
                    publicationFrequencyCode:
                        PublicationFrequencyCode,
                    coverFilePath:
                        SelectedCoverFilePath);

                await LoadSeriesAsync(
                    SeriesId.Value);

                SuccessMessage =
                    "Series changes saved successfully.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelDraftAsync()
    {
        if (!CanUseDraftActions ||
            !SeriesId.HasValue ||
            IsBusy)
        {
            return;
        }

        var confirmation =
            System.Windows.MessageBox.Show(
                "Cancel this series draft? This will change its status to CANCELLED.",
                "Cancel Series Draft",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

        if (confirmation !=
            System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsBusy = true;

        try
        {
            var result =
                await _seriesApiClient.CancelDraftAsync(
                    SeriesId.Value,
                    NullIfWhiteSpace(
                        CancellationReason));

            StatusCode =
                result.StatusCode;

            SuccessMessage =
                "Series draft cancelled.";

            NotifyModeProperties();

            BackRequested?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SubmitProposalAsync()
    {
        if (!CanUseDraftActions ||
            !SeriesId.HasValue ||
            IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                ProposalFilePath))
        {
            ErrorMessage =
                "Select a proposal document first.";
            return;
        }

        var confirmation =
            System.Windows.MessageBox.Show(
                "Submit this proposal for editorial review?",
                "Submit Proposal",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

        if (confirmation !=
            System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsBusy = true;

        try
        {
            var result =
                await _seriesApiClient.SubmitProposalAsync(
                    SeriesId.Value,
                    ProposalFilePath);

            StatusCode =
                result.SeriesStatusCode;

            SuccessMessage =
                "Proposal submitted for editorial review.";

            NotifyModeProperties();

            BackRequested?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Back()
    {
        if (IsBusy)
            return;

        BackRequested?.Invoke();
    }

    partial void OnIsCreateModeChanged(
        bool value)
    {
        NotifyModeProperties();
    }

    partial void OnStatusCodeChanged(
        string value)
    {
        NotifyModeProperties();
    }

    partial void OnSelectedCoverFilePathChanged(
        string? value)
    {
        OnPropertyChanged(
            nameof(SelectedCoverFileName));
    }

    partial void OnProposalFilePathChanged(
        string? value)
    {
        OnPropertyChanged(
            nameof(ProposalFileName));
    }

    private void NotifyModeProperties()
    {
        OnPropertyChanged(
            nameof(CanEditDraft));

        OnPropertyChanged(
            nameof(CanUseDraftActions));

        OnPropertyChanged(
            nameof(SaveButtonText));
    }

    private static string? NullIfWhiteSpace(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
    partial void OnTitleChanged(string value)
    {
        SlugPreview = GenerateSlugPreview(value);
    }
    private static string GenerateSlugPreview(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        return SlugGenerator.FromTitle(title);
    }
}


/// <summary>
/// UI-only state used by the Genre/Tag checkboxes.
/// It does not replace GenreDto or TagDto.
/// </summary>
public partial class LookupSelectionItem
    : ObservableObject
{
    public Guid Id { get; }

    public string Name { get; }

    [ObservableProperty]
    private bool _isSelected;

    public LookupSelectionItem(
        Guid id,
        string name,
        bool isSelected)
    {
        Id = id;
        Name = name;
        IsSelected = isSelected;
    }
}