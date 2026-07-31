using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;

namespace MangaManagementSystem.WpfMini.Services;

public sealed class BoardApiClient
{
    private readonly ApiClientBase _api;

    public BoardApiClient(ApiClientBase api)
    {
        _api = api;
    }

    public Task<List<ProposalQueueItemDto>?> GetReadyProposalsAsync(
        CancellationToken cancellationToken = default)
    {
        return _api.GetAsync<List<ProposalQueueItemDto>>(
            "/api/editor/proposals?status=UNDER_BOARD_REVIEW",
            cancellationToken);
    }

    public Task<List<EditorialBoardPollDto>?> GetOpenPollsAsync(
        CancellationToken cancellationToken = default)
    {
        return _api.GetAsync<List<EditorialBoardPollDto>>(
            "/api/editorial-board/polls/open",
            cancellationToken);
    }

    public Task<List<EditorialBoardPollDto>?> GetHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        return _api.GetAsync<List<EditorialBoardPollDto>>(
            "/api/editorial-board/polls/history",
            cancellationToken);
    }

    public Task<OpenSeriesBoardPollResultDto?> OpenPollAsync(
        Guid proposalId,
        string pollReason,
        string publicationFrequencyCode,
        CancellationToken cancellationToken = default)
    {
        var body = new OpenPollRequest(
            PollTypeCode: "START_SERIALIZATION",
            PollReason: pollReason,
            PublicationFrequencyCode: publicationFrequencyCode,
            EndsAtUtc: null);

        return _api.PostAsync<OpenPollRequest, OpenSeriesBoardPollResultDto>(
            $"/api/editorial-board/proposals/{proposalId}/polls",
            body,
            cancellationToken);
    }

    public Task<CastSeriesBoardVoteResultDto?> CastVoteAsync(
        Guid pollId,
        string choiceCode,
        string? voteReason,
        CancellationToken cancellationToken = default)
    {
        var body = new CastVoteRequest(choiceCode, voteReason);

        return _api.PostAsync<CastVoteRequest, CastSeriesBoardVoteResultDto>(
            $"/api/editorial-board/polls/{pollId}/votes",
            body,
            cancellationToken);
    }

    public Task<FinalizeBoardPollResultDto?> FinalizePollAsync(
        Guid pollId,
        CancellationToken cancellationToken = default)
    {
        return _api.PostAsync<FinalizeBoardPollResultDto>(
            $"/api/editorial-board/polls/{pollId}/final-approval",
            cancellationToken);
    }

    public Task<FinalizeBoardPollResultDto?> CancelPollAsync(
        Guid pollId,
        CancellationToken cancellationToken = default)
    {
        return _api.PostAsync<FinalizeBoardPollResultDto>(
            $"/api/editorial-board/polls/{pollId}/cancel",
            cancellationToken);
    }

    private sealed record OpenPollRequest(
        string PollTypeCode,
        string PollReason,
        string? PublicationFrequencyCode,
        DateTime? EndsAtUtc);

    private sealed record CastVoteRequest(
        string ChoiceCode,
        string? VoteReason);
}
