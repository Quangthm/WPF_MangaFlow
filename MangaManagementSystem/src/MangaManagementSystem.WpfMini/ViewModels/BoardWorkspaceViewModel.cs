using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.WpfMini.Services;
using MangaManagementSystem.WpfMini.ViewModels;

namespace MangaManagementSystem.WpfMini.ViewModels.Workspaces;

public partial class BoardWorkspaceViewModel : ObservableObject
{
    private readonly BoardApiClient _boardApi;
    private readonly MainWindowViewModel _mainWindow;

    public BoardWorkspaceViewModel(
        BoardApiClient boardApi,
        MainWindowViewModel mainWindow)
    {
        _boardApi = boardApi;
        _mainWindow = mainWindow;
    }

    public bool IsChief => _mainWindow.CurrentSession?.IsBoardChief == true;

    public ObservableCollection<string> PublicationFrequencies { get; } =
        ["WEEKLY", "MONTHLY", "IRREGULAR"];

    [ObservableProperty]
    private ObservableCollection<EditorialProposalReviewRowDto> _readyProposals = [];

    [ObservableProperty]
    private ObservableCollection<EditorialBoardPollDto> _openPolls = [];

    [ObservableProperty]
    private ObservableCollection<EditorialBoardPollDto> _pollHistory = [];

    [ObservableProperty]
    private EditorialProposalReviewRowDto? _selectedProposal;

    [ObservableProperty]
    private EditorialBoardPollDto? _selectedPoll;

    [ObservableProperty]
    private int _proposalReviewCount;

    [ObservableProperty]
    private int _openPollCount;

    [ObservableProperty]
    private int _awaitingDecisionCount;

    [ObservableProperty]
    private string _pollReason = "Vote to approve this series for serialization.";

    [ObservableProperty]
    private string _selectedPublicationFrequency = "WEEKLY";

    [ObservableProperty]
    private string _voteReason = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ClearMessages();

        try
        {
            var dashboardTask = _boardApi.GetDashboardAsync();
            var openPollsTask = _boardApi.GetOpenPollsAsync();
            var historyTask = _boardApi.GetHistoryAsync();

            await Task.WhenAll(dashboardTask, openPollsTask, historyTask);

            var dashboard = await dashboardTask;
            var openPolls = await openPollsTask ?? [];
            var history = await historyTask ?? [];

            ProposalReviewCount = dashboard?.ProposalReviewCount ?? 0;
            OpenPollCount = dashboard?.OpenPollCount ?? openPolls.Count;
            AwaitingDecisionCount = dashboard?.AwaitingDecisionCount ?? 0;

            // Only proposals passed by the editor should be opened for board voting.
            ReadyProposals = new ObservableCollection<EditorialProposalReviewRowDto>(
                dashboard?.RecentProposals
                    .Where(x => string.Equals(
                        x.Status,
                        "Board Review",
                        StringComparison.OrdinalIgnoreCase))
                ?? []);

            OpenPolls = new ObservableCollection<EditorialBoardPollDto>(openPolls);
            PollHistory = new ObservableCollection<EditorialBoardPollDto>(history);

            SelectedProposal = ReadyProposals.FirstOrDefault();
            SelectedPoll = OpenPolls.FirstOrDefault();
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
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenPollAsync()
    {
        ClearMessages();

        if (!IsChief)
        {
            ErrorMessage = "Only the Editorial Board Chief can open a poll.";
            return;
        }

        if (SelectedProposal is null)
        {
            ErrorMessage = "Select a proposal that is ready for board review.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PollReason))
        {
            ErrorMessage = "Poll reason is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedPublicationFrequency))
        {
            ErrorMessage = "Publication frequency is required.";
            return;
        }

        await RunActionAsync(async () =>
        {
            var result = await _boardApi.OpenPollAsync(
                SelectedProposal.ProposalId,
                PollReason.Trim(),
                SelectedPublicationFrequency);

            SuccessMessage = result is null
                ? "Poll opened."
                : $"Poll opened for series {SelectedProposal.Title}.";
        });
    }

    [RelayCommand]
    private async Task VoteAsync(string? choiceCode)
    {
        ClearMessages();

        if (SelectedPoll is null)
        {
            ErrorMessage = "Select an open poll first.";
            return;
        }

        if (choiceCode is not "APPROVE" and not "REJECT" and not "ABSTAIN")
        {
            ErrorMessage = "Invalid vote choice.";
            return;
        }

        if (choiceCode == "REJECT" && string.IsNullOrWhiteSpace(VoteReason))
        {
            ErrorMessage = "A reason is required when voting REJECT.";
            return;
        }

        await RunActionAsync(async () =>
        {
            await _boardApi.CastVoteAsync(
                SelectedPoll.PollId,
                choiceCode,
                string.IsNullOrWhiteSpace(VoteReason) ? null : VoteReason.Trim());

            SuccessMessage = $"Your {choiceCode} vote was saved.";
            VoteReason = string.Empty;
        });
    }

    [RelayCommand]
    private async Task FinalizePollAsync()
    {
        ClearMessages();

        if (!IsChief)
        {
            ErrorMessage = "Only the Editorial Board Chief can close a poll.";
            return;
        }

        if (SelectedPoll is null)
        {
            ErrorMessage = "Select an open poll first.";
            return;
        }

        await RunActionAsync(async () =>
        {
            var result = await _boardApi.FinalizePollAsync(SelectedPoll.PollId);
            SuccessMessage = result is null
                ? "Poll closed."
                : $"Poll closed. Series status: {result.SeriesStatusCode}.";
        });
    }

    [RelayCommand]
    private async Task CancelPollAsync()
    {
        ClearMessages();

        if (!IsChief)
        {
            ErrorMessage = "Only the Editorial Board Chief can cancel a poll.";
            return;
        }

        if (SelectedPoll is null)
        {
            ErrorMessage = "Select an open poll first.";
            return;
        }

        await RunActionAsync(async () =>
        {
            await _boardApi.CancelPollAsync(SelectedPoll.PollId);
            SuccessMessage = "Poll cancelled.";
        });
    }

    private async Task RunActionAsync(Func<Task> action)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;

        try
        {
            await action();
            await ReloadDataAfterActionAsync();
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ReloadDataAfterActionAsync()
    {
        var dashboardTask = _boardApi.GetDashboardAsync();
        var openPollsTask = _boardApi.GetOpenPollsAsync();
        var historyTask = _boardApi.GetHistoryAsync();

        await Task.WhenAll(dashboardTask, openPollsTask, historyTask);

        var dashboard = await dashboardTask;
        var openPolls = await openPollsTask ?? [];
        var history = await historyTask ?? [];

        ProposalReviewCount = dashboard?.ProposalReviewCount ?? 0;
        OpenPollCount = dashboard?.OpenPollCount ?? openPolls.Count;
        AwaitingDecisionCount = dashboard?.AwaitingDecisionCount ?? 0;

        ReadyProposals = new ObservableCollection<EditorialProposalReviewRowDto>(
            dashboard?.RecentProposals
                .Where(x => string.Equals(
                    x.Status,
                    "Board Review",
                    StringComparison.OrdinalIgnoreCase))
            ?? []);

        OpenPolls = new ObservableCollection<EditorialBoardPollDto>(openPolls);
        PollHistory = new ObservableCollection<EditorialBoardPollDto>(history);

        SelectedProposal = ReadyProposals.FirstOrDefault();
        SelectedPoll = OpenPolls.FirstOrDefault();
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
