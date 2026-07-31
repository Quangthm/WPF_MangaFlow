using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.WpfMini.Services;

namespace MangaManagementSystem.WpfMini.ViewModels.Workspaces;

public partial class BoardWorkspaceViewModel : ObservableObject
{
    private readonly BoardApiClient _boardApi;
    private readonly MainWindowViewModel _mainWindowViewModel;

    public BoardWorkspaceViewModel(
        BoardApiClient boardApi,
        MainWindowViewModel mainWindowViewModel)
    {
        _boardApi = boardApi ??
            throw new ArgumentNullException(nameof(boardApi));

        _mainWindowViewModel = mainWindowViewModel ??
            throw new ArgumentNullException(nameof(mainWindowViewModel));

        // Chief mở mặc định Proposal Review.
        // Member mở mặc định Board Polls.
        IsOnProposalReview = IsChief;
        IsOnBoardPolls = !IsChief;
    }

    public bool IsChief =>
        _mainWindowViewModel.CurrentSession?.IsBoardChief == true;

    public IReadOnlyList<string> PublicationFrequencies { get; } =
        ["WEEKLY", "MONTHLY", "IRREGULAR"];

    [ObservableProperty]
    private ObservableCollection<ProposalQueueItemDto> _proposals = [];

    [ObservableProperty]
    private ObservableCollection<EditorialBoardPollDto> _openPolls = [];

    [ObservableProperty]
    private ObservableCollection<EditorialBoardPollDto> _pollHistory = [];

    [ObservableProperty]
    private ProposalQueueItemDto? _selectedProposal;

    [ObservableProperty]
    private EditorialBoardPollDto? _selectedOpenPoll;

    [ObservableProperty]
    private EditorialBoardPollDto? _selectedDecisionPoll;

    [ObservableProperty]
    private EditorialBoardPollDto? _selectedHistoryPoll;

    [ObservableProperty]
    private string _pollReason = string.Empty;

    [ObservableProperty]
    private string _selectedPublicationFrequency = "WEEKLY";

    [ObservableProperty]
    private string _voteReason = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isOnProposalReview;

    [ObservableProperty]
    private bool _isOnBoardPolls;

    [ObservableProperty]
    private bool _isOnDecisionCenter;

    [ObservableProperty]
    private bool _isOnHistory;

    // =========================================================
    // INITIALIZE / REFRESH
    // =========================================================

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await RefreshAllAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await RefreshAllAsync();
    }

    // =========================================================
    // NAVIGATION
    // =========================================================

    [RelayCommand]
    private async Task NavigateToProposalReviewAsync()
    {
        if (!IsChief)
        {
            StatusMessage =
                "Only the Editorial Board Chief can open Proposal Review.";
            return;
        }

        ShowOnly(proposals: true);
        await LoadProposalsAsync();
    }

    [RelayCommand]
    private async Task NavigateToBoardPollsAsync()
    {
        ShowOnly(polls: true);
        await LoadOpenPollsAsync();
    }

    [RelayCommand]
    private async Task NavigateToDecisionCenterAsync()
    {
        if (!IsChief)
        {
            StatusMessage =
                "Only the Editorial Board Chief can open Decision Center.";
            return;
        }

        ShowOnly(decisions: true);
        await LoadOpenPollsAsync();
    }

    [RelayCommand]
    private async Task NavigateToHistoryAsync()
    {
        ShowOnly(history: true);
        await LoadHistoryAsync();
    }

    // =========================================================
    // OPEN POLL
    // =========================================================

    [RelayCommand]
    private async Task OpenPollAsync()
    {
        if (!IsChief)
        {
            StatusMessage =
                "Only the Editorial Board Chief can open a poll.";
            return;
        }

        if (SelectedProposal is null)
        {
            StatusMessage = "Select a proposal first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PollReason))
        {
            StatusMessage = "Poll reason is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedPublicationFrequency))
        {
            StatusMessage = "Official publication frequency is required.";
            return;
        }

        var proposalId = SelectedProposal.SeriesProposalId;
        var seriesTitle = SelectedProposal.SeriesTitle;

        await RunBusyAsync(async () =>
        {
            var result = await _boardApi.OpenPollAsync(
                proposalId,
                PollReason.Trim(),
                SelectedPublicationFrequency.Trim().ToUpperInvariant());

            if (result is null)
            {
                StatusMessage = "The poll could not be opened.";
                return;
            }

            StatusMessage = $"Poll opened for {seriesTitle}.";

            PollReason = string.Empty;
            SelectedProposal = null;

            await LoadOpenPollsCoreAsync();
            await LoadProposalsCoreAsync();
        });
    }

    // =========================================================
    // CAST VOTE
    // =========================================================

    [RelayCommand]
    private async Task CastVoteAsync(string? choiceCode)
    {
        if (SelectedOpenPoll is null)
        {
            StatusMessage = "Select an open poll first.";
            return;
        }

        var normalizedChoice =
            choiceCode?.Trim().ToUpperInvariant();

        if (normalizedChoice is not
            ("APPROVE" or "REJECT" or "ABSTAIN"))
        {
            StatusMessage = "Invalid vote choice.";
            return;
        }

        if (normalizedChoice == "REJECT" &&
            string.IsNullOrWhiteSpace(VoteReason))
        {
            StatusMessage =
                "A reason is required when voting REJECT.";
            return;
        }

        var pollId = SelectedOpenPoll.PollId;

        await RunBusyAsync(async () =>
        {
            await _boardApi.CastVoteAsync(
                pollId,
                normalizedChoice,
                string.IsNullOrWhiteSpace(VoteReason)
                    ? null
                    : VoteReason.Trim());

            StatusMessage =
                $"Your vote was saved as {normalizedChoice}.";

            VoteReason = string.Empty;

            await LoadOpenPollsCoreAsync(pollId);
        });
    }

    // =========================================================
    // FINALIZE POLL
    // =========================================================

    [RelayCommand]
    private async Task FinalizePollAsync()
    {
        if (!IsChief)
        {
            StatusMessage =
                "Only the Editorial Board Chief can close a poll.";
            return;
        }

        if (SelectedDecisionPoll is null)
        {
            StatusMessage =
                "Select an open poll in Decision Center first.";
            return;
        }

        var pollId = SelectedDecisionPoll.PollId;

        await RunBusyAsync(async () =>
        {
            var result = await _boardApi.FinalizeAsync(pollId);

            if (result is null)
            {
                StatusMessage = "The poll could not be closed.";
                return;
            }

            StatusMessage =
                $"Poll {result.PollStatusCode}. " +
                $"Series status: {result.SeriesStatusCode}.";

            SelectedDecisionPoll = null;

            await LoadOpenPollsCoreAsync();
            await LoadHistoryCoreAsync();

            if (IsChief)
            {
                await LoadProposalsCoreAsync();
            }
        });
    }

    // =========================================================
    // CANCEL POLL
    // =========================================================

    [RelayCommand]
    private async Task CancelPollAsync()
    {
        if (!IsChief)
        {
            StatusMessage =
                "Only the Editorial Board Chief can cancel a poll.";
            return;
        }

        if (SelectedDecisionPoll is null)
        {
            StatusMessage =
                "Select an open poll in Decision Center first.";
            return;
        }

        var pollId = SelectedDecisionPoll.PollId;

        await RunBusyAsync(async () =>
        {
            var result = await _boardApi.CancelAsync(pollId);

            if (result is null)
            {
                StatusMessage = "The poll could not be cancelled.";
                return;
            }

            StatusMessage = "Poll cancelled.";
            SelectedDecisionPoll = null;

            await LoadOpenPollsCoreAsync();
            await LoadHistoryCoreAsync();

            if (IsChief)
            {
                await LoadProposalsCoreAsync();
            }
        });
    }

    // =========================================================
    // REFRESH ALL
    // =========================================================

    private async Task RefreshAllAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        var errors = new List<string>();

        try
        {
            /*
             * Quan trọng:
             * Proposal được tải TRƯỚC.
             *
             * Nếu Open Poll hoặc History bị Unauthorized,
             * proposal vẫn xuất hiện thay vì toàn bộ màn hình trống.
             */
            if (IsChief)
            {
                await TryLoadSectionAsync(
                    "Proposals",
                    LoadProposalsCoreAsync,
                    errors);
            }

            var openPollsLoaded = await TryLoadSectionAsync(
                "Open polls",
                () => LoadOpenPollsCoreAsync(),
                errors);

            /*
             * Sau khi lấy được poll, tải lại proposal để loại các
             * series đã có poll đang OPEN.
             */
            if (IsChief && openPollsLoaded)
            {
                await TryLoadSectionAsync(
                    "Proposals",
                    LoadProposalsCoreAsync,
                    errors,
                    avoidDuplicateError: true);
            }

            await TryLoadSectionAsync(
                "History",
                LoadHistoryCoreAsync,
                errors);

            StatusMessage = errors.Count == 0
                ? "Editorial Board data loaded."
                : string.Join(" | ", errors);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // =========================================================
    // PUBLIC LOAD WRAPPERS
    // =========================================================

    private async Task LoadProposalsAsync()
    {
        await RunBusyAsync(LoadProposalsCoreAsync);
    }

    private async Task LoadOpenPollsAsync()
    {
        await RunBusyAsync(
            () => LoadOpenPollsCoreAsync());
    }

    private async Task LoadHistoryAsync()
    {
        await RunBusyAsync(LoadHistoryCoreAsync);
    }

    // =========================================================
    // CORE LOAD METHODS
    // =========================================================

    private async Task LoadProposalsCoreAsync()
    {
        if (!IsChief)
        {
            Proposals = [];
            SelectedProposal = null;
            return;
        }

        var proposals =
            await _boardApi.GetBoardReadyProposalsAsync();

        var openSeriesIds = OpenPolls
            .Select(poll => poll.SeriesId)
            .ToHashSet();

        var filteredProposals = proposals
            .Where(proposal =>
                string.Equals(
                    proposal.StatusCode,
                    "UNDER_BOARD_REVIEW",
                    StringComparison.OrdinalIgnoreCase) &&
                !openSeriesIds.Contains(proposal.SeriesId))
            .OrderByDescending(proposal =>
                proposal.SubmittedAtUtc)
            .ToList();

        Proposals =
            new ObservableCollection<ProposalQueueItemDto>(
                filteredProposals);

        if (SelectedProposal is not null)
        {
            var selectedId =
                SelectedProposal.SeriesProposalId;

            SelectedProposal = Proposals.FirstOrDefault(
                proposal =>
                    proposal.SeriesProposalId == selectedId);
        }
    }

    private async Task LoadOpenPollsCoreAsync(
        Guid? preserveSelectedPollId = null)
    {
        var selectedOpenPollId =
            preserveSelectedPollId ??
            SelectedOpenPoll?.PollId;

        var selectedDecisionPollId =
            SelectedDecisionPoll?.PollId;

        var polls =
            await _boardApi.GetOpenPollsAsync();

        OpenPolls =
            new ObservableCollection<EditorialBoardPollDto>(
                polls.OrderByDescending(
                    poll => poll.StartAtUtc));

        SelectedOpenPoll = selectedOpenPollId.HasValue
            ? OpenPolls.FirstOrDefault(
                poll => poll.PollId == selectedOpenPollId.Value)
            : null;

        SelectedDecisionPoll = selectedDecisionPollId.HasValue
            ? OpenPolls.FirstOrDefault(
                poll => poll.PollId == selectedDecisionPollId.Value)
            : null;
    }

    private async Task LoadHistoryCoreAsync()
    {
        var selectedHistoryPollId =
            SelectedHistoryPoll?.PollId;

        var history =
            await _boardApi.GetHistoryAsync();

        PollHistory =
            new ObservableCollection<EditorialBoardPollDto>(
                history.OrderByDescending(
                    poll => poll.EndAtUtc ?? poll.StartAtUtc));

        SelectedHistoryPoll = selectedHistoryPollId.HasValue
            ? PollHistory.FirstOrDefault(
                poll =>
                    poll.PollId == selectedHistoryPollId.Value)
            : null;
    }

    // =========================================================
    // ERROR HANDLING
    // =========================================================

    private static async Task<bool> TryLoadSectionAsync(
        string sectionName,
        Func<Task> action,
        ICollection<string> errors,
        bool avoidDuplicateError = false)
    {
        try
        {
            await action();
            return true;
        }
        catch (HttpRequestException ex)
        {
            var message = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    $"{sectionName}: Unauthorized. Log out and log in again.",

                HttpStatusCode.Forbidden =>
                    $"{sectionName}: Your role is not allowed to access this API.",

                _ => $"{sectionName}: {ex.Message}"
            };

            if (!avoidDuplicateError ||
                !errors.Contains(message))
            {
                errors.Add(message);
            }

            return false;
        }
        catch (Exception ex)
        {
            var message =
                $"{sectionName}: {ex.Message}";

            if (!avoidDuplicateError ||
                !errors.Contains(message))
            {
                errors.Add(message);
            }

            return false;
        }
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            await action();
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    "Unauthorized. Log out and log in again.",

                HttpStatusCode.Forbidden =>
                    "Your account does not have permission for this action.",

                _ => ex.Message
            };
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // =========================================================
    // VIEW SWITCHING
    // =========================================================

    private void ShowOnly(
        bool proposals = false,
        bool polls = false,
        bool decisions = false,
        bool history = false)
    {
        IsOnProposalReview = proposals;
        IsOnBoardPolls = polls;
        IsOnDecisionCenter = decisions;
        IsOnHistory = history;
        StatusMessage = string.Empty;
    }
}