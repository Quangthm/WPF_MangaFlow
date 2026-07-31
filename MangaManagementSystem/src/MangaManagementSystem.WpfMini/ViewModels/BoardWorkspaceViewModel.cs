using System.Collections.ObjectModel;
using System.Net.Http;
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
        _boardApi = boardApi;
        _mainWindowViewModel = mainWindowViewModel;

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

    [RelayCommand]
    private async Task NavigateToProposalReviewAsync()
    {
        if (!IsChief)
        {
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

    [RelayCommand]
    private async Task OpenPollAsync()
    {
        if (!IsChief)
        {
            StatusMessage = "Only the Editorial Board Chief can open a poll.";
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

        await RunBusyAsync(async () =>
        {
            var result = await _boardApi.OpenPollAsync(
                SelectedProposal.SeriesProposalId,
                PollReason.Trim(),
                SelectedPublicationFrequency);

            StatusMessage = result is null
                ? "The poll could not be opened."
                : $"Poll opened for {SelectedProposal.SeriesTitle}.";

            PollReason = string.Empty;
            SelectedProposal = null;

            await LoadOpenPollsCoreAsync();
            await LoadProposalsCoreAsync();
        });
    }

    [RelayCommand]
    private async Task CastVoteAsync(string? choiceCode)
    {
        if (SelectedOpenPoll is null)
        {
            StatusMessage = "Select an open poll first.";
            return;
        }

        var normalizedChoice = choiceCode?.Trim().ToUpperInvariant();
        if (normalizedChoice is not ("APPROVE" or "REJECT" or "ABSTAIN"))
        {
            StatusMessage = "Invalid vote choice.";
            return;
        }

        if (normalizedChoice == "REJECT" && string.IsNullOrWhiteSpace(VoteReason))
        {
            StatusMessage = "A reason is required when voting REJECT.";
            return;
        }

        var pollId = SelectedOpenPoll.PollId;

        await RunBusyAsync(async () =>
        {
            await _boardApi.CastVoteAsync(
                pollId,
                normalizedChoice,
                string.IsNullOrWhiteSpace(VoteReason) ? null : VoteReason.Trim());

            StatusMessage = $"Your vote was saved as {normalizedChoice}.";
            VoteReason = string.Empty;

            await LoadOpenPollsCoreAsync(pollId);
        });
    }

    [RelayCommand]
    private async Task FinalizePollAsync()
    {
        if (!IsChief)
        {
            StatusMessage = "Only the Editorial Board Chief can close a poll.";
            return;
        }

        if (SelectedDecisionPoll is null)
        {
            StatusMessage = "Select an open poll in Decision Center first.";
            return;
        }

        var pollId = SelectedDecisionPoll.PollId;

        await RunBusyAsync(async () =>
        {
            var result = await _boardApi.FinalizeAsync(pollId);

            StatusMessage = result is null
                ? "The poll could not be closed."
                : $"Poll {result.PollStatusCode}. Series status: {result.SeriesStatusCode}.";

            SelectedDecisionPoll = null;
            await LoadOpenPollsCoreAsync();
            await LoadHistoryCoreAsync();
        });
    }

    [RelayCommand]
    private async Task CancelPollAsync()
    {
        if (!IsChief)
        {
            StatusMessage = "Only the Editorial Board Chief can cancel a poll.";
            return;
        }

        if (SelectedDecisionPoll is null)
        {
            StatusMessage = "Select an open poll in Decision Center first.";
            return;
        }

        var pollId = SelectedDecisionPoll.PollId;

        await RunBusyAsync(async () =>
        {
            var result = await _boardApi.CancelAsync(pollId);

            StatusMessage = result is null
                ? "The poll could not be cancelled."
                : "Poll cancelled.";

            SelectedDecisionPoll = null;
            await LoadOpenPollsCoreAsync();
            await LoadHistoryCoreAsync();
        });
    }

    private async Task RefreshAllAsync()
    {
        await RunBusyAsync(async () =>
        {
            await LoadOpenPollsCoreAsync();
            await LoadHistoryCoreAsync();

            if (IsChief)
            {
                await LoadProposalsCoreAsync();
            }

            StatusMessage = "Editorial Board data loaded.";
        });
    }

    private async Task LoadProposalsAsync()
    {
        await RunBusyAsync(LoadProposalsCoreAsync);
    }

    private async Task LoadOpenPollsAsync()
    {
        await RunBusyAsync(() => LoadOpenPollsCoreAsync());
    }

    private async Task LoadHistoryAsync()
    {
        await RunBusyAsync(LoadHistoryCoreAsync);
    }

    private async Task LoadProposalsCoreAsync()
    {
        if (!IsChief)
        {
            Proposals = [];
            return;
        }

        var proposals = await _boardApi.GetBoardReadyProposalsAsync();
        var openSeriesIds = OpenPolls.Select(poll => poll.SeriesId).ToHashSet();

        Proposals = new ObservableCollection<ProposalQueueItemDto>(
            proposals
                .Where(proposal =>
                    proposal.StatusCode == "UNDER_BOARD_REVIEW" &&
                    !openSeriesIds.Contains(proposal.SeriesId))
                .OrderByDescending(proposal => proposal.SubmittedAtUtc));
    }

    private async Task LoadOpenPollsCoreAsync(Guid? preserveSelectedPollId = null)
    {
        var polls = await _boardApi.GetOpenPollsAsync();
        OpenPolls = new ObservableCollection<EditorialBoardPollDto>(polls);

        if (preserveSelectedPollId is Guid pollId)
        {
            SelectedOpenPoll = OpenPolls.FirstOrDefault(poll => poll.PollId == pollId);
        }
    }

    private async Task LoadHistoryCoreAsync()
    {
        var history = await _boardApi.GetHistoryAsync();
        PollHistory = new ObservableCollection<EditorialBoardPollDto>(history);
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
            StatusMessage = ex.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Unauthorized. Log out, log in again, then reopen Board Polls."
                : ex.Message;
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
