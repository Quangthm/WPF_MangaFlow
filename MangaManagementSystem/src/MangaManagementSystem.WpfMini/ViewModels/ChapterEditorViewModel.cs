using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.WpfMini.Interfaces;
using System.Diagnostics;
using System.Windows;

namespace MangaManagementSystem.WpfMini.ViewModels;

public partial class ChapterEditorViewModel : ObservableObject
{
    private readonly IMangakaChapterApiClient _chapterApiClient;

    public event Action? BackRequested;

    [ObservableProperty] private bool _isCreateMode;
    [ObservableProperty] private Guid? _chapterId;
    [ObservableProperty] private Guid _seriesId;
    [ObservableProperty] private string _seriesTitle = string.Empty;
    [ObservableProperty] private string _chapterNumberLabel = string.Empty;
    [ObservableProperty] private string _chapterTitle = string.Empty;
    [ObservableProperty] private string _statusCode = "DRAFT";
    [ObservableProperty] private DateTime? _plannedReleaseDate;
    [ObservableProperty] private DateTime? _releasedAtUtc;
    [ObservableProperty] private DateTime? _createdAtUtc;
    [ObservableProperty] private DateTime? _updatedAtUtc;
    [ObservableProperty] private ChapterEditorialReviewSummaryDto? _latestReview;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _successMessage = string.Empty;

    public string PageTitle => IsCreateMode ? "Create Chapter" : "Chapter Details";
    public string SaveButtonText => IsCreateMode ? "Create Chapter" : "Save Changes";
    public bool CanEditMetadata =>
        !IsBusy &&
        (IsCreateMode ||
         IsStatus("DRAFT") ||
         IsStatus("REVISION_REQUESTED"));
    public bool CanSave => CanEditMetadata;
    public bool CanSubmit =>
        !IsBusy &&
        !IsCreateMode &&
        ChapterId.HasValue &&
        (IsStatus("DRAFT") || IsStatus("REVISION_REQUESTED"));
    public bool CanSchedule =>
        !IsBusy &&
        !IsCreateMode &&
        ChapterId.HasValue &&
        IsStatus("APPROVED");
    public bool HasLatestReview => LatestReview is not null;
    public bool HasNoLatestReview => LatestReview is null;
    public bool CanOpenMarkupUrl => TryGetMarkupUri(out _);

    public ChapterEditorViewModel(IMangakaChapterApiClient chapterApiClient)
    {
        _chapterApiClient = chapterApiClient;
    }

    public void InitializeCreate(Guid seriesId, string seriesTitle)
    {
        if (seriesId == Guid.Empty)
            throw new ArgumentException("A valid series is required.", nameof(seriesId));
        if (string.IsNullOrWhiteSpace(seriesTitle))
            throw new ArgumentException("Series title is required.", nameof(seriesTitle));

        IsCreateMode = true;
        ChapterId = null;
        SeriesId = seriesId;
        SeriesTitle = seriesTitle.Trim();
        ChapterNumberLabel = string.Empty;
        ChapterTitle = string.Empty;
        StatusCode = "DRAFT";
        PlannedReleaseDate = null;
        ReleasedAtUtc = null;
        CreatedAtUtc = null;
        UpdatedAtUtc = null;
        LatestReview = null;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        NotifyStateProperties();
    }

    public void InitializeExisting(MangakaChapterListItemDto chapter)
    {
        ArgumentNullException.ThrowIfNull(chapter);
        ApplyChapter(chapter);
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanSave)
            return;

        if (!TryNormalizeMetadata(out var label, out var title))
            return;

        IsBusy = true;
        ClearMessages();
        try
        {
            MangakaChapterListItemDto result;
            if (IsCreateMode)
            {
                result = await _chapterApiClient.CreateChapterDraftAsync(
                    new CreateChapterDraftRequest(SeriesId, label, title));
                ApplyChapter(result);
                SuccessMessage = "Chapter draft created successfully.";
            }
            else
            {
                if (!ChapterId.HasValue)
                {
                    ErrorMessage = "No chapter is currently loaded.";
                    return;
                }

                result = await _chapterApiClient.UpdateChapterDraftAsync(
                    ChapterId.Value,
                    new UpdateChapterDraftRequest(label, title));
                ApplyChapter(result);
                SuccessMessage = "Chapter metadata updated successfully.";
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
    private async Task SubmitAsync()
    {
        if (!CanSubmit || !ChapterId.HasValue)
            return;

        var confirmation = MessageBox.Show(
            "Submit this chapter for editorial review?",
            "Submit Chapter",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes)
            return;

        IsBusy = true;
        ClearMessages();
        try
        {
            var result = await _chapterApiClient.SubmitChapterForReviewAsync(
                ChapterId.Value);
            ApplyChapter(result);
            SuccessMessage = "Chapter submitted for editorial review.";
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
    private async Task ScheduleAsync()
    {
        if (!CanSchedule || !ChapterId.HasValue)
            return;

        if (!PlannedReleaseDate.HasValue)
        {
            ErrorMessage = "Select a planned release date.";
            return;
        }

        if (PlannedReleaseDate.Value.Date < DateTime.Today)
        {
            ErrorMessage = "Planned release date cannot be earlier than today.";
            return;
        }

        var confirmation = MessageBox.Show(
            $"Schedule this chapter for {PlannedReleaseDate.Value:dd/MM/yyyy}?",
            "Schedule Chapter",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes)
            return;

        IsBusy = true;
        ClearMessages();
        try
        {
            var result = await _chapterApiClient.ScheduleApprovedChapterAsync(
                ChapterId.Value,
                new ScheduleApprovedChapterRequest(PlannedReleaseDate.Value));
            ApplyChapter(result);
            SuccessMessage = "Chapter scheduled successfully.";
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
    private void OpenMarkupUrl()
    {
        if (!CanOpenMarkupUrl || !TryGetMarkupUri(out var uri))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not open the markup URL: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Back()
    {
        if (!IsBusy)
        {
            Debug.WriteLine(
                $"ChapterEditor BackRequested: ChapterId={ChapterId}, SeriesId={SeriesId}");
            BackRequested?.Invoke();
        }
    }

    private void ApplyChapter(MangakaChapterListItemDto chapter)
    {
        IsCreateMode = false;
        ChapterId = chapter.ChapterId;
        SeriesId = chapter.SeriesId;
        SeriesTitle = chapter.SeriesTitle;
        ChapterNumberLabel = chapter.ChapterNumberLabel;
        ChapterTitle = chapter.ChapterTitle ?? string.Empty;
        StatusCode = chapter.StatusCode;
        PlannedReleaseDate = chapter.PlannedReleaseDate;
        ReleasedAtUtc = chapter.ReleasedAtUtc;
        CreatedAtUtc = chapter.CreatedAtUtc;
        UpdatedAtUtc = chapter.UpdatedAtUtc;
        LatestReview = chapter.LatestReview;
        NotifyStateProperties();
    }

    private bool TryNormalizeMetadata(out string label, out string? title)
    {
        label = ChapterNumberLabel.Trim();
        title = string.IsNullOrWhiteSpace(ChapterTitle) ? null : ChapterTitle.Trim();

        if (label.Length == 0)
        {
            ErrorMessage = "Chapter number label is required.";
            return false;
        }
        if (label.Length > 20)
        {
            ErrorMessage = "Chapter number label must be 20 characters or fewer.";
            return false;
        }
        if (title?.Length > 200)
        {
            ErrorMessage = "Chapter title must be 200 characters or fewer.";
            return false;
        }
        return true;
    }

    private bool IsStatus(string status) =>
        string.Equals(StatusCode, status, StringComparison.OrdinalIgnoreCase);

    private bool TryGetMarkupUri(out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(
                LatestReview?.MarkupFileUrl,
                UriKind.Absolute,
                out var parsedUri) ||
            (parsedUri.Scheme != Uri.UriSchemeHttp &&
             parsedUri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        uri = parsedUri;
        return true;
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    partial void OnIsCreateModeChanged(bool value) => NotifyStateProperties();
    partial void OnStatusCodeChanged(string value) => NotifyStateProperties();
    partial void OnIsBusyChanged(bool value) => NotifyStateProperties();
    partial void OnLatestReviewChanged(ChapterEditorialReviewSummaryDto? value) =>
        NotifyStateProperties();

    private void NotifyStateProperties()
    {
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(CanEditMetadata));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanSubmit));
        OnPropertyChanged(nameof(CanSchedule));
        OnPropertyChanged(nameof(HasLatestReview));
        OnPropertyChanged(nameof(HasNoLatestReview));
        OnPropertyChanged(nameof(CanOpenMarkupUrl));
    }
}
