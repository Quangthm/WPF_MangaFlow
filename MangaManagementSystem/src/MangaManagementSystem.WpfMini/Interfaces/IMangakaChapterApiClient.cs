using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.WpfMini.Interfaces;

public interface IMangakaChapterApiClient
{
    Task<IReadOnlyList<MangakaChapterListItemDto>> GetMyChaptersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MangakaChapterListItemDto>> GetSeriesChaptersAsync(
        Guid seriesId,
        CancellationToken cancellationToken = default);

    Task<MangakaChapterListItemDto> CreateChapterDraftAsync(
        CreateChapterDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<MangakaChapterListItemDto> UpdateChapterDraftAsync(
        Guid chapterId,
        UpdateChapterDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<MangakaChapterListItemDto> SubmitChapterForReviewAsync(
        Guid chapterId,
        CancellationToken cancellationToken = default);

    Task<MangakaChapterListItemDto> ScheduleApprovedChapterAsync(
        Guid chapterId,
        ScheduleApprovedChapterRequest request,
        CancellationToken cancellationToken = default);
}
