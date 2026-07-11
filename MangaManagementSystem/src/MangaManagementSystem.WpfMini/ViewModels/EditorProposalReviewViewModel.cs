using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.WpfMini.Models;
using MangaManagementSystem.WpfMini.Services;

namespace MangaManagementSystem.WpfMini.ViewModels;

/// <summary>
/// Master-Detail ViewModel cho màn hình Editor Proposal Review.
/// Left: Queue (ListBox). Right: Detail + Actions Panel.
/// </summary>
public partial class EditorProposalReviewViewModel : ObservableObject
{
    private readonly EditorApiClient _editorApi;
    private readonly ApiClientBase _api;

    // ── Queue (Master) ──

    [ObservableProperty]
    private ObservableCollection<ProposalQueueItem> _proposalQueue = [];

    [ObservableProperty]
    private ProposalQueueItem? _selectedProposal;

    [ObservableProperty]
    private bool _isQueueLoading;

    [ObservableProperty]
    private string _queueErrorMessage = string.Empty;

    [ObservableProperty]
    private string _statusFilter = string.Empty; // Empty = all

    // ── Detail (Detail) ──

    [ObservableProperty]
    private ProposalDetail? _selectedDetail;

    [ObservableProperty]
    private bool _isDetailLoading;

    [ObservableProperty]
    private string _detailErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasDetail;

    // ── Action form ──

    [ObservableProperty]
    private string _comments = string.Empty;

    [ObservableProperty]
    private string _markupFilePath = string.Empty;

    [ObservableProperty]
    private string _markupFileName = string.Empty;

    [ObservableProperty]
    private bool _isSubmittingAction;

    [ObservableProperty]
    private string _actionErrorMessage = string.Empty;

    [ObservableProperty]
    private string _actionSuccessMessage = string.Empty;

    // ── Action mode ──
    // Xác định action nào đang active trong action panel

    [ObservableProperty]
    private bool _isRevisionMode;

    [ObservableProperty]
    private bool _isPassToBoardMode;

    [ObservableProperty]
    private bool _isCancelMode;

    // ── Constructor ──

    public EditorProposalReviewViewModel(EditorApiClient editorApi, ApiClientBase api)
    {
        _editorApi = editorApi;
        _api = api;
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
            var result = await _editorApi.GetProposalQueueAsync(filter);

            ProposalQueue.Clear();
            if (result is not null)
            {
                foreach (var item in result)
                {
                    ProposalQueue.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            QueueErrorMessage = $"Failed to load queue: {ex.Message}";

            // Fallback: mock data cho UI development
            LoadMockQueue();
        }
        finally
        {
            IsQueueLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectProposalAsync(ProposalQueueItem? item)
    {
        if (item is null) return;

        SelectedProposal = item;
        IsDetailLoading = true;
        DetailErrorMessage = string.Empty;
        HasDetail = false;
        ClearActionForm();

        try
        {
            var detail = await _editorApi.GetProposalDetailAsync(item.SeriesProposalId);
            if (detail is not null)
            {
                SelectedDetail = detail;
                HasDetail = true;

                // Auto-select mode: mặc định là Request Revision
                SetRevisionMode();
            }
        }
        catch (Exception ex)
        {
            DetailErrorMessage = $"Failed to load detail: {ex.Message}";

            // Fallback: mock detail
            LoadMockDetail(item);
        }
        finally
        {
            IsDetailLoading = false;
        }
    }

    // ── Action Mode Selectors ──

    [RelayCommand]
    private void SetRevisionMode()
    {
        IsRevisionMode = true;
        IsPassToBoardMode = false;
        IsCancelMode = false;
        ActionErrorMessage = string.Empty;
        ActionSuccessMessage = string.Empty;
    }

    [RelayCommand]
    private void SetPassToBoardMode()
    {
        IsRevisionMode = false;
        IsPassToBoardMode = true;
        IsCancelMode = false;
        ActionErrorMessage = string.Empty;
        ActionSuccessMessage = string.Empty;
    }

    [RelayCommand]
    private void SetCancelMode()
    {
        IsRevisionMode = false;
        IsPassToBoardMode = false;
        IsCancelMode = true;
        ActionErrorMessage = string.Empty;
        ActionSuccessMessage = string.Empty;
    }

    // ── Execute Actions ──

    [RelayCommand]
    private async Task SubmitRevisionAsync()
    {
        if (string.IsNullOrWhiteSpace(Comments))
        {
            ActionErrorMessage = "Comments are required to request a revision.";
            return;
        }

        if (SelectedDetail is null) return;

        IsSubmittingAction = true;
        ActionErrorMessage = string.Empty;
        ActionSuccessMessage = string.Empty;

        try
        {
            var result = await _editorApi.RequestRevisionAsync(
                SelectedDetail.SeriesProposalId, Comments,
                string.IsNullOrEmpty(MarkupFilePath) ? null : MarkupFilePath);

            if (result is not null)
            {
                ActionSuccessMessage = $"Revision requested. Status: {result.ProposalStatusCode}";
                ClearActionForm();

                // Refresh queue + detail
                await LoadQueueAsync();
                SelectedDetail = null;
                HasDetail = false;
            }
        }
        catch (Exception ex)
        {
            ActionErrorMessage = $"Failed to request revision: {ex.Message}";
        }
        finally
        {
            IsSubmittingAction = false;
        }
    }

    [RelayCommand]
    private async Task SubmitPassToBoardAsync()
    {
        if (SelectedDetail is null) return;

        IsSubmittingAction = true;
        ActionErrorMessage = string.Empty;
        ActionSuccessMessage = string.Empty;

        try
        {
            var result = await _editorApi.PassToBoardAsync(
                SelectedDetail.SeriesProposalId, Comments,
                string.IsNullOrEmpty(MarkupFilePath) ? null : MarkupFilePath);

            if (result is not null)
            {
                ActionSuccessMessage = $"Passed to board. Status: {result.ProposalStatusCode}";
                ClearActionForm();

                await LoadQueueAsync();
                SelectedDetail = null;
                HasDetail = false;
            }
        }
        catch (Exception ex)
        {
            ActionErrorMessage = $"Failed to pass to board: {ex.Message}";
        }
        finally
        {
            IsSubmittingAction = false;
        }
    }

    [RelayCommand]
    private async Task SubmitCancelAsync()
    {
        if (string.IsNullOrWhiteSpace(Comments))
        {
            ActionErrorMessage = "Comments are required to cancel.";
            return;
        }

        if (string.IsNullOrEmpty(MarkupFilePath))
        {
            ActionErrorMessage = "A markup file is required to cancel.";
            return;
        }

        if (SelectedDetail is null) return;

        IsSubmittingAction = true;
        ActionErrorMessage = string.Empty;
        ActionSuccessMessage = string.Empty;

        try
        {
            var result = await _editorApi.CancelProposalAsync(
                SelectedDetail.SeriesProposalId, Comments, MarkupFilePath);

            if (result is not null)
            {
                ActionSuccessMessage = $"Proposal cancelled. Status: {result.ProposalStatusCode}";
                ClearActionForm();

                await LoadQueueAsync();
                SelectedDetail = null;
                HasDetail = false;
            }
        }
        catch (Exception ex)
        {
            ActionErrorMessage = $"Failed to cancel: {ex.Message}";
        }
        finally
        {
            IsSubmittingAction = false;
        }
    }

    // ── File picker ──

    [RelayCommand]
    private void BrowseMarkupFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Markup File",
            Filter = "PDF files (*.pdf)|*.pdf|Word documents (*.doc;*.docx)|*.doc;*.docx|Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            MarkupFilePath = dialog.FileName;
            MarkupFileName = Path.GetFileName(dialog.FileName);
        }
    }

    [RelayCommand]
    private void ClearMarkupFile()
    {
        MarkupFilePath = string.Empty;
        MarkupFileName = string.Empty;
    }

    // ── Helpers ──

    private void ClearActionForm()
    {
        Comments = string.Empty;
        MarkupFilePath = string.Empty;
        MarkupFileName = string.Empty;
        ActionErrorMessage = string.Empty;
        ActionSuccessMessage = string.Empty;
    }

    /// <summary>
    /// Mock data cho UI development khi backend chưa sẵn sàng.
    /// </summary>
    private void LoadMockQueue()
    {
        ProposalQueue.Clear();
        ProposalQueue.Add(new ProposalQueueItem
        {
            SeriesProposalId = Guid.NewGuid(),
            SeriesId = Guid.NewGuid(),
            SeriesTitle = "Solo Leveling",
            SeriesSlug = "solo-leveling",
            ProposalVersionNo = 2,
            ProposalTitle = "Solo Leveling - Season 2 Proposal",
            SynopsisSnapshot = "The story follows Sung Jin-Woo...",
            StatusCode = "UNDER_EDITORIAL_REVIEW",
            SubmitterDisplayName = "TestMangaka1",
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-2),
            Comments = null,
            ProposalFileName = "solo-leveling-v2.pdf"
        });
        ProposalQueue.Add(new ProposalQueueItem
        {
            SeriesProposalId = Guid.NewGuid(),
            SeriesId = Guid.NewGuid(),
            SeriesTitle = "Tower of God",
            SeriesSlug = "tower-of-god",
            ProposalVersionNo = 1,
            ProposalTitle = "Tower of God - Initial Proposal",
            SynopsisSnapshot = "A boy named Bam climbs a mysterious tower...",
            StatusCode = "UNDER_EDITORIAL_REVIEW",
            SubmitterDisplayName = "TestMangaka2",
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-5),
            Comments = null,
            ProposalFileName = "tower-of-god-v1.pdf"
        });
        ProposalQueue.Add(new ProposalQueueItem
        {
            SeriesProposalId = Guid.NewGuid(),
            SeriesId = Guid.NewGuid(),
            SeriesTitle = "The Beginning After The End",
            SeriesSlug = "tmate",
            ProposalVersionNo = 1,
            ProposalTitle = "TBATE - Season 1 Proposal",
            SynopsisSnapshot = "King Grey dies and is reincarnated...",
            StatusCode = "REVISION_REQUESTED",
            SubmitterDisplayName = "TestMangaka3",
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-7),
            Comments = "Please expand the synopsis.",
            ProposalFileName = "tbate-v1.pdf"
        });
    }

    private void LoadMockDetail(ProposalQueueItem item)
    {
        var rng = new Random();
        SelectedDetail = new ProposalDetail
        {
            SeriesProposalId = item.SeriesProposalId,
            SeriesId = item.SeriesId,
            SeriesTitle = item.SeriesTitle,
            SeriesSlug = item.SeriesSlug,
            SeriesCoverUrl = null,
            ProposalVersionNo = item.ProposalVersionNo,
            ProposalTitle = item.ProposalTitle,
            Genres = [new GenreDto { GenreId = 1, GenreName = "Action" }, new GenreDto { GenreId = 5, GenreName = "Fantasy" }],
            Tags = [new TagDto { TagId = 23, TagName = "Magic" }, new TagDto { TagId = 31, TagName = "Overpowered Protagonist" }],
            SynopsisSnapshot = item.SynopsisSnapshot,
            ProposalStatusCode = item.StatusCode,
            SubmitterDisplayName = item.SubmitterDisplayName,
            SubmittedAtUtc = item.SubmittedAtUtc,
            ProposalFileName = item.ProposalFileName,
            CanClaim = true,
            CanRequestRevision = item.StatusCode == "UNDER_EDITORIAL_REVIEW",
            CanPassToBoard = item.StatusCode == "UNDER_EDITORIAL_REVIEW",
            CanCancel = item.StatusCode is "UNDER_EDITORIAL_REVIEW" or "UNDER_BOARD_REVIEW",
            CurrentActorIsActiveTantouEditorContributor = true,
            CurrentActorHasClaimed = false,
            HasEditorialDecision = false
        };
        HasDetail = true;
    }
}
