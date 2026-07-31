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

    public async Task<IReadOnlyList<ProposalQueueItemDto>> GetBoardReadyProposalsAsync(
        CancellationToken cancellationToken = default)
    {
        // Reuse the existing proposal queue endpoint instead of changing repository SQL.
        var result = await _api.GetAsync<List<ProposalQueueItemDto>>(
            "/api/editor/proposals?status=UNDER_BOARD_REVIEW",
            cancellationToken);

        return result ?? [];
    }

    public async Task<IReadOnlyList<EditorialBoardPollDto>> GetOpenPollsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _api.GetAsync<List<EditorialBoardPollDto>>(
            "/api/editorial-board/polls/open",
            cancellationToken);

        return result ?? [];
    }

    public async Task<IReadOnlyList<EditorialBoardPollDto>> GetHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _api.GetAsync<List<EditorialBoardPollDto>>(
            "/api/editorial-board/polls/history",
            cancellationToken);

        return result ?? [];
    }

    public Task<OpenSeriesBoardPollResultDto?> OpenPollAsync(
        Guid proposalId,
        string pollReason,
        string publicationFrequencyCode,
        CancellationToken cancellationToken = default)
    {
        var request = new OpenPollApiRequest(
            PollTypeCode: "START_SERIALIZATION",
            PollReason: pollReason,
            PublicationFrequencyCode: publicationFrequencyCode,
            EndsAtUtc: null);

        return _api.PostAsync<OpenPollApiRequest, OpenSeriesBoardPollResultDto>(
            $"/api/editorial-board/proposals/{proposalId}/polls",
            request,
            cancellationToken);
    }

    public Task<CastSeriesBoardVoteResultDto?> CastVoteAsync(
        Guid pollId,
        string choiceCode,
        string? voteReason,
        CancellationToken cancellationToken = default)
    {
        var request = new CastVoteApiRequest(choiceCode, voteReason);

        return _api.PostAsync<CastVoteApiRequest, CastSeriesBoardVoteResultDto>(
            $"/api/editorial-board/polls/{pollId}/votes",
            request,
            cancellationToken);
    }

    public Task<FinalizeBoardPollResultDto?> FinalizeAsync(
        Guid pollId,
        CancellationToken cancellationToken = default)
    {
        return _api.PostAsync<FinalizeBoardPollResultDto>(
            $"/api/editorial-board/polls/{pollId}/final-approval",
            cancellationToken);
    }

    public Task<FinalizeBoardPollResultDto?> CancelAsync(
        Guid pollId,
        CancellationToken cancellationToken = default)
    {
        return _api.PostAsync<FinalizeBoardPollResultDto>(
            $"/api/editorial-board/polls/{pollId}/cancel",
            cancellationToken);
    }

    private sealed record OpenPollApiRequest(
        string PollTypeCode,
        string PollReason,
        string? PublicationFrequencyCode,
        DateTime? EndsAtUtc);

    private sealed record CastVoteApiRequest(
        string ChoiceCode,
        string? VoteReason);
}
