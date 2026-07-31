using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterPageVersionService
    {
        Task<ChapterPageVersionDto?> GetChapterPageVersionByIdAsync(Guid id);
        Task<IEnumerable<ChapterPageVersionDto>> GetChapterPageVersionsByPageIdsAsync(
            IEnumerable<Guid> chapterPageIds);
        Task<bool> SetCurrentVersionAsync(Guid chapterPageId, Guid chapterPageVersionId);

        Task<ChapterPageVersionDto> CreateVersionWithFileAsync(
            Guid chapterPageId,
            CreateFileResourceDto fileDto,
            string? versionNote,
            bool setAsCurrent,
            Guid actorUserId,
            string? actorRoleName,
            CancellationToken cancellationToken = default);

        Task<CreatePageWithVersionResponseDto> CreatePageWithVersionAndFileAsync(
            CreatePageWithVersionRequestDto request,
            Guid actorUserId,
            string? actorRoleName,
            CancellationToken cancellationToken = default);
    }
}
