using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;

namespace MangaManagementSystem.WpfMini.Services;

public sealed class BoardApiClient
{
    private const string BoardBaseUrl = "/api/editorial-board";
    private const string EditorProposalUrl = "/api/editor/proposals";

    private readonly ApiClientBase _api;

    public BoardApiClient(ApiClientBase api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <summary>
    /// Lấy các proposal đã được Editor chuyển sang Editorial Board.
    ///
    /// Không dùng /api/editorial-board/dashboard vì dashboard hiện đang
    /// phụ thuộc truy vấn SQL cũ. Endpoint Editor này dùng X-Actor-User-Id,
    /// header đã được thiết lập trong MainWindowViewModel.SetSession().
    /// </summary>
    public async Task<IReadOnlyList<ProposalQueueItemDto>>
        GetBoardReadyProposalsAsync(
            CancellationToken cancellationToken = default)
    {
        const string status = "UNDER_BOARD_REVIEW";

        var result =
            await _api.GetAsync<List<ProposalQueueItemDto>>(
                $"{EditorProposalUrl}?status={Uri.EscapeDataString(status)}",
                cancellationToken);

        return result ?? [];
    }

    /// <summary>
    /// Lấy tất cả poll đang OPEN.
    /// Endpoint này yêu cầu JWT Bearer token.
    /// </summary>
    public async Task<IReadOnlyList<EditorialBoardPollDto>>
        GetOpenPollsAsync(
            CancellationToken cancellationToken = default)
    {
        var result =
            await _api.GetAsync<List<EditorialBoardPollDto>>(
                $"{BoardBaseUrl}/polls/open",
                cancellationToken);

        return result ?? [];
    }

    /// <summary>
    /// Lấy lịch sử poll CLOSED hoặc CANCELLED.
    /// Endpoint này yêu cầu JWT Bearer token.
    /// </summary>
    public async Task<IReadOnlyList<EditorialBoardPollDto>>
        GetHistoryAsync(
            CancellationToken cancellationToken = default)
    {
        var result =
            await _api.GetAsync<List<EditorialBoardPollDto>>(
                $"{BoardBaseUrl}/polls/history",
                cancellationToken);

        return result ?? [];
    }

    /// <summary>
    /// Board Chief mở poll START_SERIALIZATION cho proposal.
    /// </summary>
    public Task<OpenSeriesBoardPollResultDto?> OpenPollAsync(
        Guid proposalId,
        string pollReason,
        string publicationFrequencyCode,
        CancellationToken cancellationToken = default)
    {
        if (proposalId == Guid.Empty)
        {
            throw new ArgumentException(
                "Proposal ID is required.",
                nameof(proposalId));
        }

        if (string.IsNullOrWhiteSpace(pollReason))
        {
            throw new ArgumentException(
                "Poll reason is required.",
                nameof(pollReason));
        }

        if (string.IsNullOrWhiteSpace(publicationFrequencyCode))
        {
            throw new ArgumentException(
                "Publication frequency is required.",
                nameof(publicationFrequencyCode));
        }

        var request = new OpenPollApiRequest(
            PollTypeCode: "START_SERIALIZATION",
            PollReason: pollReason.Trim(),
            PublicationFrequencyCode:
                publicationFrequencyCode.Trim().ToUpperInvariant(),
            EndsAtUtc: null);

        return _api.PostAsync<
            OpenPollApiRequest,
            OpenSeriesBoardPollResultDto>(
                $"{BoardBaseUrl}/proposals/{proposalId}/polls",
                request,
                cancellationToken);
    }

    /// <summary>
    /// Board Chief hoặc Board Member bỏ phiếu.
    /// </summary>
    public Task<CastSeriesBoardVoteResultDto?> CastVoteAsync(
        Guid pollId,
        string choiceCode,
        string? voteReason,
        CancellationToken cancellationToken = default)
    {
        if (pollId == Guid.Empty)
        {
            throw new ArgumentException(
                "Poll ID is required.",
                nameof(pollId));
        }

        var normalizedChoice =
            choiceCode?.Trim().ToUpperInvariant();

        if (normalizedChoice is not
            ("APPROVE" or "REJECT" or "ABSTAIN"))
        {
            throw new ArgumentException(
                "Vote choice must be APPROVE, REJECT, or ABSTAIN.",
                nameof(choiceCode));
        }

        if (normalizedChoice == "REJECT" &&
            string.IsNullOrWhiteSpace(voteReason))
        {
            throw new ArgumentException(
                "Vote reason is required for REJECT.",
                nameof(voteReason));
        }

        var request = new CastVoteApiRequest(
            ChoiceCode: normalizedChoice,
            VoteReason: string.IsNullOrWhiteSpace(voteReason)
                ? null
                : voteReason.Trim());

        return _api.PostAsync<
            CastVoteApiRequest,
            CastSeriesBoardVoteResultDto>(
                $"{BoardBaseUrl}/polls/{pollId}/votes",
                request,
                cancellationToken);
    }

    /// <summary>
    /// Board Chief đóng poll và tính kết quả.
    /// </summary>
    public Task<FinalizeBoardPollResultDto?> FinalizeAsync(
        Guid pollId,
        CancellationToken cancellationToken = default)
    {
        if (pollId == Guid.Empty)
        {
            throw new ArgumentException(
                "Poll ID is required.",
                nameof(pollId));
        }

        return _api.PostAsync<FinalizeBoardPollResultDto>(
            $"{BoardBaseUrl}/polls/{pollId}/final-approval",
            cancellationToken);
    }

    /// <summary>
    /// Board Chief hủy poll.
    /// </summary>
    public Task<FinalizeBoardPollResultDto?> CancelAsync(
        Guid pollId,
        CancellationToken cancellationToken = default)
    {
        if (pollId == Guid.Empty)
        {
            throw new ArgumentException(
                "Poll ID is required.",
                nameof(pollId));
        }

        return _api.PostAsync<FinalizeBoardPollResultDto>(
            $"{BoardBaseUrl}/polls/{pollId}/cancel",
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