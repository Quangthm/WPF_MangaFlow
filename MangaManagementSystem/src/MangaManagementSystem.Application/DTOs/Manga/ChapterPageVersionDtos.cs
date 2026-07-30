namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageVersionDto(
        Guid ChapterPageVersionId,
        Guid ChapterPageId,
        short VersionNo,
        Guid PageFileId,
        string? VersionNote,
        bool IsCurrentVersion
    );
}
