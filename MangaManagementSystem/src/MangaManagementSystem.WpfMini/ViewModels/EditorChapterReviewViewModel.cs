using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.WpfMini.Models;
using MangaManagementSystem.WpfMini.Services;

namespace MangaManagementSystem.WpfMini.ViewModels;

/// <summary>
/// Master-Detail ViewModel for the Editor Chapter Review screen.
/// Left: Queue (list of chapters). Right: Detail + action buttons.
/// </summary>
public partial class EditorChapterReviewViewModel : ObservableObject
{
    private readonly EditorApiClient _editorApi;

    // ── Queue (Master) ──

    [ObservableProperty]
    private ObservableCollection<EditorChapterReviewQueueItemDto> _chapterQueue = [];

    [ObservableProperty]
    private EditorChapterReviewQueueItemDto? _selectedChapter;

    [ObservableProperty]
    private bool _isQueueLoading;

    [ObservableProperty]
    private string _queueErrorMessage = string.Empty;

    [ObservableProperty]
    private string _statusFilter = string.Empty;

    // ── KPIs ──

    [ObservableProperty]
    private int _underReviewCount;

    [ObservableProperty]
    private int _approvedThisWeekCount;

    [ObservableProperty]
    private int _revisionRequestedCount;

    [ObservableProperty]
    private int _onHoldCount;

    // ── Detail ──

    [ObservableProperty]
    private EditorChapterReviewDetailDto? _selectedDetail;

    [ObservableProperty]
    private bool _isDetailLoading;

    [ObservableProperty]
    private string _detailErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasDetail;

    [ObservableProperty]
    private bool _hasAnnotations;

    // ── Action State ──

    [ObservableProperty]
    private string _feedback = string.Empty;

    [ObservableProperty]
    private bool _isSubmittingAction;

    [ObservableProperty]
    private string _actionErrorMessage = string.Empty;

    [ObservableProperty]
    private string _actionSuccessMessage = string.Empty;

    [ObservableProperty]
    private string _selectedPageImageUrl = string.Empty;

    // ── Constructor ──

    public EditorChapterReviewViewModel(EditorApiClient editorApi)
    {
        _editorApi = editorApi;
    }

    // ── Commands ──

    [RelayCommand]
    private async Task LoadQueueAsync()
    {
        IsQueueLoading = true;
        QueueErrorMessage = string.Empty;

        try
        {
            var filter = string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter;
            var result = await _editorApi.GetChapterReviewQueueAsync(filter);

            ChapterQueue.Clear();
            if (result is not null)
            {
                UnderReviewCount = result.UnderReviewCount;
                ApprovedThisWeekCount = result.ApprovedThisWeekCount;
                RevisionRequestedCount = result.RevisionRequestedCount;
                OnHoldCount = result.OnHoldCount;

                foreach (var item in result.Chapters)
                {
                    ChapterQueue.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            QueueErrorMessage = $"Failed to load queue: {ex.Message}";
            LoadMockQueue();
        }
        finally
        {
            IsQueueLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectChapterAsync(EditorChapterReviewQueueItemDto? item)
    {
        if (item is null) return;

        SelectedChapter = item;
        IsDetailLoading = true;
        DetailErrorMessage = string.Empty;
        HasDetail = false;
        ClearAction();

        try
        {
            var detail = await _editorApi.GetChapterReviewDetailAsync(item.ChapterId);
            if (detail is not null)
            {
                SelectedDetail = detail;
                HasDetail = true;
                HasAnnotations = detail.OpenAnnotations.Count > 0;
            }
        }
        catch (Exception ex)
        {
            DetailErrorMessage = $"Failed to load detail: {ex.Message}";
            LoadMockDetail(item);
        }
        finally
        {
            IsDetailLoading = false;
        }
    }

    [RelayCommand]
    private void SelectPage(EditorChapterReviewPageDto? page)
    {
        if (page?.CurrentVersionFileUrl is not null)
        {
            SelectedPageImageUrl = page.CurrentVersionFileUrl;
        }
    }

    // ── Action Commands ──

    [RelayCommand]
    private async Task ApproveChapterAsync()
    {
        if (SelectedDetail is null) return;
        await ExecuteActionAsync(
            () => _editorApi.ApproveChapterAsync(SelectedDetail.ChapterId, Feedback),
            "approved");
    }

    [RelayCommand]
    private async Task RejectChapterAsync()
    {
        if (SelectedDetail is null) return;

        if (string.IsNullOrWhiteSpace(Feedback))
        {
            ActionErrorMessage = "Feedback is required to request a revision.";
            return;
        }

        await ExecuteActionAsync(
            () => _editorApi.RejectChapterAsync(SelectedDetail.ChapterId, Feedback),
            "rejected");
    }

    [RelayCommand]
    private async Task PutChapterOnHoldAsync()
    {
        if (SelectedDetail is null) return;
        await ExecuteActionAsync(
            () => _editorApi.PutChapterOnHoldAsync(SelectedDetail.ChapterId, Feedback),
            "put on hold");
    }

    [RelayCommand]
    private async Task PublishChapterAsync()
    {
        if (SelectedDetail is null) return;
        await ExecuteActionAsync(
            () => _editorApi.PublishChapterAsync(SelectedDetail.ChapterId),
            "published");
    }

    // ── Helpers ──

    private async Task ExecuteActionAsync(
        Func<Task<EditorChapterReviewActionResult?>> action,
        string actionLabel)
    {
        IsSubmittingAction = true;
        ActionErrorMessage = string.Empty;
        ActionSuccessMessage = string.Empty;

        try
        {
            var result = await action();
            if (result is not null)
            {
                ActionSuccessMessage = $"Chapter {actionLabel}. Status: {result.StatusCode}";
                ClearAction();

                // Refresh queue + detail
                await LoadQueueAsync();
                SelectedDetail = null;
                HasDetail = false;
            }
        }
        catch (Exception ex)
        {
            ActionErrorMessage = $"Failed to {actionLabel}: {ex.Message}";
        }
        finally
        {
            IsSubmittingAction = false;
        }
    }

    private void ClearAction()
    {
        Feedback = string.Empty;
        ActionErrorMessage = string.Empty;
        ActionSuccessMessage = string.Empty;
        SelectedPageImageUrl = string.Empty;
    }

    // ── Mock Data ──

    private void LoadMockQueue()
    {
        UnderReviewCount = 5;
        ApprovedThisWeekCount = 2;
        RevisionRequestedCount = 1;
        OnHoldCount = 0;

        ChapterQueue.Clear();
        ChapterQueue.Add(new EditorChapterReviewQueueItemDto
        {
            ChapterId = Guid.NewGuid(),
            SeriesId = Guid.NewGuid(),
            SeriesTitle = "Solo Leveling",
            SeriesSlug = "solo-leveling",
            ChapterNumberLabel = "Ch. 5",
            ChapterTitle = "The Awakening",
            StatusCode = "UNDER_REVIEW",
            PageCount = 24,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        ChapterQueue.Add(new EditorChapterReviewQueueItemDto
        {
            ChapterId = Guid.NewGuid(),
            SeriesId = Guid.NewGuid(),
            SeriesTitle = "Tower of God",
            SeriesSlug = "tower-of-god",
            ChapterNumberLabel = "Ch. 12",
            ChapterTitle = "The Test",
            StatusCode = "UNDER_REVIEW",
            PageCount = 18,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
        });
        ChapterQueue.Add(new EditorChapterReviewQueueItemDto
        {
            ChapterId = Guid.NewGuid(),
            SeriesId = Guid.NewGuid(),
            SeriesTitle = "TBATE",
            SeriesSlug = "tmate",
            ChapterNumberLabel = "Ch. 8",
            ChapterTitle = "New Beginnings",
            StatusCode = "REVISION_REQUESTED",
            PageCount = 20,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-7)
        });
    }

    private void LoadMockDetail(EditorChapterReviewQueueItemDto item)
    {
        var pages = new List<EditorChapterReviewPageDto>();
        for (int i = 1; i <= item.PageCount; i++)
        {
            pages.Add(new EditorChapterReviewPageDto
            {
                ChapterPageId = Guid.NewGuid(),
                PageNumber = i,
                CurrentVersionId = Guid.NewGuid(),
                CurrentVersionFileUrl = null,
                CurrentVersionNo = 1
            });
        }

        SelectedDetail = new EditorChapterReviewDetailDto
        {
            ChapterId = item.ChapterId,
            SeriesId = item.SeriesId,
            SeriesTitle = item.SeriesTitle,
            SeriesSlug = item.SeriesSlug,
            ChapterNumberLabel = item.ChapterNumberLabel,
            ChapterTitle = item.ChapterTitle,
            StatusCode = item.StatusCode,
            PageCount = item.PageCount,
            CurrentVersionCount = item.PageCount,
            CreatedAtUtc = item.CreatedAtUtc,
            SubmittedByDisplayName = "TestMangaka",
            Pages = pages,
            OpenAnnotations = []
        };
        HasDetail = true;
        HasAnnotations = false;
    }
}
