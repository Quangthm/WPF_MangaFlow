using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.WpfMini.Interfaces;

namespace MangaManagementSystem.WpfMini.Services.Mangaka;

public sealed class MangakaChapterApiClient : IMangakaChapterApiClient
{
    private readonly ApiClientBase _apiClient;

    public MangakaChapterApiClient(ApiClientBase apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<MangakaChapterListItemDto>> GetMyChaptersAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.GetAsync<List<MangakaChapterListItemDto>>(
            "/api/mangaka/chapters",
            cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<MangakaChapterListItemDto>> GetSeriesChaptersAsync(
        Guid seriesId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(seriesId, nameof(seriesId), "series");
        var result = await _apiClient.GetAsync<List<MangakaChapterListItemDto>>(
            $"/api/mangaka/series/{seriesId}/chapters",
            cancellationToken);
        return result ?? [];
    }

    public async Task<MangakaChapterListItemDto> CreateChapterDraftAsync(
        CreateChapterDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(request.SeriesId, nameof(request.SeriesId), "series");
        var normalized = new CreateChapterDraftRequest(
            request.SeriesId,
            NormalizeLabel(request.ChapterNumberLabel),
            NormalizeTitle(request.ChapterTitle));
        var result = await _apiClient.PostAsync<
            CreateChapterDraftRequest,
            MangakaChapterListItemDto>(
            "/api/mangaka/chapters",
            normalized,
            cancellationToken);
        return RequireBody(result, "create-chapter");
    }

    public async Task<MangakaChapterListItemDto> UpdateChapterDraftAsync(
        Guid chapterId,
        UpdateChapterDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateId(chapterId, nameof(chapterId), "chapter");
        ArgumentNullException.ThrowIfNull(request);
        var normalized = new UpdateChapterDraftRequest(
            NormalizeLabel(request.ChapterNumberLabel),
            NormalizeTitle(request.ChapterTitle));
        var result = await _apiClient.PutAsync<
            UpdateChapterDraftRequest,
            MangakaChapterListItemDto>(
            $"/api/mangaka/chapters/{chapterId}",
            normalized,
            cancellationToken);
        return RequireBody(result, "update-chapter");
    }

    public async Task<MangakaChapterListItemDto> SubmitChapterForReviewAsync(
        Guid chapterId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(chapterId, nameof(chapterId), "chapter");
        var result = await _apiClient.PostAsync<MangakaChapterListItemDto>(
            $"/api/mangaka/chapters/{chapterId}/submit-review",
            cancellationToken);
        return RequireBody(result, "submit-review");
    }

    public async Task<MangakaChapterListItemDto> ScheduleApprovedChapterAsync(
        Guid chapterId,
        ScheduleApprovedChapterRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateId(chapterId, nameof(chapterId), "chapter");
        ArgumentNullException.ThrowIfNull(request);
        var result = await _apiClient.PostAsync<
            ScheduleApprovedChapterRequest,
            MangakaChapterListItemDto>(
            $"/api/mangaka/chapters/{chapterId}/schedule",
            request,
            cancellationToken);
        return RequireBody(result, "schedule-chapter");
    }

    private static void ValidateId(Guid id, string parameterName, string entityName)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                $"A valid {entityName} must be selected.",
                parameterName);
    }

    private static string NormalizeLabel(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new ArgumentException("Chapter number label is required.", nameof(value));
        if (normalized.Length > 20)
            throw new ArgumentException(
                "Chapter number label must be 20 characters or fewer.",
                nameof(value));
        return normalized;
    }

    private static string? NormalizeTitle(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > 200)
            throw new ArgumentException(
                "Chapter title must be 200 characters or fewer.",
                nameof(value));
        return normalized;
    }

    private static MangakaChapterListItemDto RequireBody(
        MangakaChapterListItemDto? result,
        string operation) =>
        result ?? throw new InvalidOperationException(
            $"The API returned an empty {operation} response.");
}
