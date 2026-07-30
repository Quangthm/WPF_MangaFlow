using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.WpfMini.Interfaces;

public interface IMangakaPageApiClient
{
    Task<IReadOnlyList<ChapterPageDto>> GetPagesByChapterAsync(
        Guid chapterId,
        CancellationToken cancellationToken = default);

    Task<CreatePageWithVersionResponseDto> CreatePageWithFileAsync(
        Guid chapterId,
        int pageNo,
        string? pageNotes,
        string? versionNote,
        string filePath,
        CancellationToken cancellationToken = default);

    Task<ChapterPageDto> UpdatePageNotesAsync(
        Guid pageId,
        string? pageNotes,
        CancellationToken cancellationToken = default);

    Task DeletePageAsync(
        Guid pageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChapterPageVersionDto>> GetVersionsByPageIdsAsync(
        IReadOnlyList<Guid> pageIds,
        CancellationToken cancellationToken = default);

    Task<ChapterPageVersionDto> CreateVersionWithFileAsync(
        Guid pageId,
        string? versionNote,
        bool setAsCurrent,
        string filePath,
        CancellationToken cancellationToken = default);

    Task SetCurrentVersionAsync(
        Guid pageId,
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileResourceDto>> GetFileResourcesByIdsAsync(
        IReadOnlyList<Guid> fileIds,
        CancellationToken cancellationToken = default);
}
