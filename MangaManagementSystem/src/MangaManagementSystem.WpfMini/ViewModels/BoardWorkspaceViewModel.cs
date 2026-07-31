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

    public bool IsChief { get; }
    public bool IsMember { get; }
    public string RoleDisplayName => IsChief
        ? "Editorial Board Chief"
        : "Editorial Board Member";

    public ObservableCollection<ProposalQueueItemDto> ReadyProposals { get; } = [];
    public ObservableCollection<EditorialBoardPollDto> OpenPolls { get; } = [];
    public ObservableCollection<EditorialBoardPollDto> PollHistory { get; } = [];

    public IReadOnlyList<string> PublicationFrequencies { get; } =
        ["WEEKLY", "MONTHLY", "IRREGULAR"];

    [ObservableProperty]
    private ProposalQueueItemDto? _selectedProposal;

    [ObservableProperty]
    private EditorialBoardPollDto? _selectedPoll;

    [ObservableProperty]
    private EditorialBoardPollDto? _selectedDecisionPoll;

    [ObservableProperty]
    private EditorialBoardPollDto? _selectedHistoryPoll;

    [ObservableProperty]
    private string _pollReason = string.Empty;

    [ObservableProperty]
    private string _publicationFrequencyCode = "WEEKLY";

    [ObservableProperty]
    private string _voteReason = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isOnProposalReview;

    [ObservableProperty]
    private bool _isOnBoardPolls;

    [ObservableProperty]
    private bool _isOnDecisionCenter;

    [ObservableProperty]
    private bool _isOnPollHistory;

    [ObservableProperty]
    private string _currentSectionTitle = "Board Polls";

    [ObservableProperty]
    private int _readyProposalCount;

    [ObservableProperty]
    private int _openPollCount;

    [ObservableProperty]
    private int _historyCount;

    public BoardWorkspaceViewModel(
        BoardApiClient boardApi,
        MainWindowViewModel mainWindowViewModel)
    {
        _boardApi = boardApi;

        var session = mainWindowViewModel.CurrentSession;
        IsChief = session?.IsBoardChief == true;
        IsMember = session?.IsBoardMember == true;

        if (IsChief)
        {
            SetSection(BoardSection.ProposalReview);
        }
        else
        {
            SetSection(BoardSection.BoardPolls);
        }

        _ = RefreshAsync();
    }

    [RelayCommand]
    private void NavigateToProposalReview()
    {
        if (IsChief)
        {
            SetSection(BoardSection.ProposalReview);
        }
    }

    [RelayCommand]
    private void NavigateToBoardPolls()
    {
        SetSection(BoardSection.BoardPolls);
    }

    [RelayCommand]
    private void NavigateToDecisionCenter()
    {
        if (IsChief)
        {
            SetSection(BoardSection.DecisionCenter);
        }
    }

    [RelayCommand]
    private void NavigateToPollHistory()
    {
        SetSection(BoardSection.PollHistory);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();

        try
        {
            await ReloadDataAsync();
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not load Editorial Board data: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenPollAsync()
    {
        if (!IsChief)
        {
            ErrorMessage = "Only the Editorial Board Chief can open a poll.";
            return;
        }

        if (SelectedProposal is null)
        {
            ErrorMessage = "Select a proposal first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PollReason))
        {
            ErrorMessage = "Poll reason is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PublicationFrequencyCode))
        {
            ErrorMessage = "Publication frequency is required.";
            return;
        }

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();

        try
        {
            var proposalTitle = SelectedProposal.ProposalTitle;

            var result = await _boardApi.OpenPollAsync(
                SelectedProposal.SeriesProposalId,
                PollReason.Trim(),
                PublicationFrequencyCode);

            if (result is null)
            {
                ErrorMessage = "The API did not return the created poll.";
                return;
            }

            PollReason = string.Empty;
            StatusMessage = $"Poll opened for '{proposalTitle}'.";

            await ReloadDataAsync();
            SetSection(BoardSection.BoardPolls);
            SelectedPoll = OpenPolls.FirstOrDefault(p => p.PollId == result.PollId);
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not open poll: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task VoteApproveAsync()
    {
        return CastVoteAsync("APPROVE");
    }

    [RelayCommand]
    private Task VoteRejectAsync()
    {
        return CastVoteAsync("REJECT");
    }

    [RelayCommand]
    private Task VoteAbstainAsync()
    {
        return CastVoteAsync("ABSTAIN");
    }

    [RelayCommand]
    private async Task FinalizePollAsync()
    {
        if (!IsChief)
        {
            ErrorMessage = "Only the Editorial Board Chief can close a poll.";
            return;
        }

        if (SelectedDecisionPoll is null)
        {
            ErrorMessage = "Select an open poll in Decision Center first.";
            return;
        }

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();

        try
        {
            var pollName = SelectedDecisionPoll.PollName;
            var result = await _boardApi.FinalizePollAsync(SelectedDecisionPoll.PollId);

            if (result is null)
            {
                ErrorMessage = "The API did not return the poll result.";
                return;
            }

            StatusMessage =
                $"'{pollName}' finished as {result.PollStatusCode}. " +
                $"Series status: {result.SeriesStatusCode}.";

            await ReloadDataAsync();
            SetSection(BoardSection.PollHistory);
            SelectedHistoryPoll = PollHistory.FirstOrDefault(p => p.PollId == result.PollId);
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not close poll: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelPollAsync()
    {
        if (!IsChief)
        {
            ErrorMessage = "Only the Editorial Board Chief can cancel a poll.";
            return;
        }

        if (SelectedDecisionPoll is null)
        {
            ErrorMessage = "Select an open poll in Decision Center first.";
            return;
        }

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();

        try
        {
            var pollName = SelectedDecisionPoll.PollName;
            var result = await _boardApi.CancelPollAsync(SelectedDecisionPoll.PollId);

            if (result is null)
            {
                ErrorMessage = "The API did not return the cancelled poll.";
                return;
            }

            StatusMessage = $"Poll '{pollName}' was cancelled.";

            await ReloadDataAsync();
            SetSection(BoardSection.PollHistory);
            SelectedHistoryPoll = PollHistory.FirstOrDefault(p => p.PollId == result.PollId);
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not cancel poll: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CastVoteAsync(string choiceCode)
    {
        if (SelectedPoll is null)
        {
            ErrorMessage = "Select an open poll first.";
            return;
        }

        if (choiceCode == "REJECT" && string.IsNullOrWhiteSpace(VoteReason))
        {
            ErrorMessage = "A reject vote requires a reason.";
            return;
        }

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();

        try
        {
            var selectedPollId = SelectedPoll.PollId;
            var reason = choiceCode == "REJECT"
                ? VoteReason.Trim()
                : null;

            var result = await _boardApi.CastVoteAsync(
                selectedPollId,
                choiceCode,
                reason);

            if (result is null)
            {
                ErrorMessage = "The API did not return the saved vote.";
                return;
            }

            VoteReason = string.Empty;
            StatusMessage = $"Your {result.ChoiceCode} vote was saved.";

            await ReloadDataAsync();
            SelectedPoll = OpenPolls.FirstOrDefault(p => p.PollId == selectedPollId);
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not save vote: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadDataAsync()
    {
        var openPolls = await _boardApi.GetOpenPollsAsync() ?? [];
        var history = await _boardApi.GetHistoryAsync() ?? [];

        ReplaceItems(OpenPolls, openPolls);
        ReplaceItems(PollHistory, history);

        if (IsChief)
        {
            var proposals = await _boardApi.GetReadyProposalsAsync() ?? [];
            var openSeriesIds = openPolls
                .Select(p => p.SeriesId)
                .ToHashSet();

            ReplaceItems(
                ReadyProposals,
                proposals.Where(p => !openSeriesIds.Contains(p.SeriesId)));
        }
        else
        {
            ReadyProposals.Clear();
        }

        ReadyProposalCount = ReadyProposals.Count;
        OpenPollCount = OpenPolls.Count;
        HistoryCount = PollHistory.Count;
    }

    private static void ReplaceItems<T>(
        ObservableCollection<T> target,
        IEnumerable<T> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private void SetSection(BoardSection section)
    {
        IsOnProposalReview = section == BoardSection.ProposalReview;
        IsOnBoardPolls = section == BoardSection.BoardPolls;
        IsOnDecisionCenter = section == BoardSection.DecisionCenter;
        IsOnPollHistory = section == BoardSection.PollHistory;

        CurrentSectionTitle = section switch
        {
            BoardSection.ProposalReview => "Proposal Review",
            BoardSection.BoardPolls => "Board Polls",
            BoardSection.DecisionCenter => "Decision Center",
            BoardSection.PollHistory => "Poll History",
            _ => "Editorial Board"
        };

        ClearMessages();
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
    }

    private enum BoardSection
    {
        ProposalReview,
        BoardPolls,
        DecisionCenter,
        PollHistory
    }
}
