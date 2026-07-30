using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;

namespace MangaManagementSystem.WpfMini.Services;

/// <summary>
/// Thin WPF client for the existing Editorial Board API.
/// No business logic or database access is performed here.
/// </summary>
public sealed class BoardApiClient
{
    private readonly ApiClientBase _api;

    public BoardApiClient(ApiClientBase api)
    {
        _api = api;
    }

    public Task<EditorialDashboardDto?> GetDashboardAsync()
        => _api.GetAsync<EditorialDashboardDto>("/api/editorial-board/dashboard");

    public Task<List<EditorialBoardPollDto>?> GetOpenPollsAsync()
        => _api.GetAsync<List<EditorialBoardPollDto>>("/api/editorial-board/polls/open");

    public Task<List<EditorialBoardPollDto>?> GetHistoryAsync()
        => _api.GetAsync<List<EditorialBoardPollDto>>("/api/editorial-board/polls/history");

    public Task<OpenSeriesBoardPollResultDto?> OpenPollAsync(
        Guid proposalId,
        string pollReason,
        string publicationFrequencyCode,
        DateTime? endsAtUtc = null)
    {
        var request = new
        {
            PollTypeCode = "START_SERIALIZATION",
            PollReason = pollReason,
            PublicationFrequencyCode = publicationFrequencyCode,
            EndsAtUtc = endsAtUtc
        };

        return _api.PostAsync<object, OpenSeriesBoardPollResultDto>(
            $"/api/editorial-board/proposals/{proposalId}/polls",
            request);
    }

    public Task<CastSeriesBoardVoteResultDto?> CastVoteAsync(
        Guid pollId,
        string choiceCode,
        string? voteReason)
    {
        var request = new
        {
            ChoiceCode = choiceCode,
            VoteReason = voteReason
        };

        return _api.PostAsync<object, CastSeriesBoardVoteResultDto>(
            $"/api/editorial-board/polls/{pollId}/votes",
            request);
    }

    public Task<FinalizeBoardPollResultDto?> FinalizePollAsync(Guid pollId)
        => _api.PostAsync<object, FinalizeBoardPollResultDto>(
            $"/api/editorial-board/polls/{pollId}/final-approval",
            new { });

    public Task<FinalizeBoardPollResultDto?> CancelPollAsync(Guid pollId)
        => _api.PostAsync<object, FinalizeBoardPollResultDto>(
            $"/api/editorial-board/polls/{pollId}/cancel",
            new { });
}
