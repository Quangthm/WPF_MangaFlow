using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;

namespace MangaManagementSystem.Application.Services
{
    public sealed class ChapterPageVersionService : IChapterPageVersionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChapterPageVersionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChapterPageVersionDto?> GetChapterPageVersionByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterPageVersions.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ChapterPageVersionDto>> GetChapterPageVersionsByPageIdsAsync(
            IEnumerable<Guid> chapterPageIds)
        {
            var idSet = chapterPageIds.ToHashSet();
            var versions = await _unitOfWork.ChapterPageVersions.FindAsync(
                version => idSet.Contains(version.ChapterPageId));

            return versions
                .OrderBy(version => version.ChapterPageId)
                .ThenBy(version => version.VersionNo)
                .Select(MapToDto);
        }

        public async Task<ChapterPageVersionDto> CreateVersionWithFileAsync(
            Guid chapterPageId,
            CreateFileResourceDto fileDto,
            string? versionNote,
            bool setAsCurrent,
            Guid actorUserId,
            string? actorRoleName,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var file = CreateFileResource(fileDto, actorUserId);
                await _unitOfWork.FileResources.AddAsync(file);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var existingVersions = await _unitOfWork.ChapterPageVersions.FindAsync(
                    version => version.ChapterPageId == chapterPageId);
                var nextVersionNumber = checked((short)(
                    existingVersions
                        .Select(version => (int)version.VersionNo)
                        .DefaultIfEmpty(0)
                        .Max() + 1));

                var version = new ChapterPageVersion
                {
                    ChapterPageId = chapterPageId,
                    VersionNo = nextVersionNumber,
                    PageFileId = file.FileResourceId,
                    VersionNote = versionNote,
                    IsCurrentVersion = false
                };
                await _unitOfWork.ChapterPageVersions.AddAsync(version);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (setAsCurrent)
                {
                    await UnsetCurrentVersionsAsync(
                        chapterPageId, version.ChapterPageVersionId, cancellationToken);

                    version.IsCurrentVersion = true;
                    _unitOfWork.ChapterPageVersions.Update(version);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                await AddAuditEventAsync(
                    actorUserId,
                    actorRoleName,
                    "VERSION_CREATED",
                    "ChapterPageVersion",
                    version.ChapterPageVersionId,
                    new
                    {
                        chapter_page_id = chapterPageId,
                        version_no = version.VersionNo,
                        file_resource_id = file.FileResourceId,
                        set_as_current = setAsCurrent
                    },
                    cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return MapToDto(version);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<bool> SetCurrentVersionAsync(
            Guid chapterPageId,
            Guid chapterPageVersionId)
        {
            var versions = (await _unitOfWork.ChapterPageVersions.FindAsync(
                version => version.ChapterPageId == chapterPageId)).ToList();
            var newCurrent = versions.FirstOrDefault(
                version => version.ChapterPageVersionId == chapterPageVersionId);
            if (newCurrent == null) return false;
            if (newCurrent.IsCurrentVersion) return true;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var current in versions.Where(version => version.IsCurrentVersion))
                {
                    current.IsCurrentVersion = false;
                    _unitOfWork.ChapterPageVersions.Update(current);
                }
                await _unitOfWork.SaveChangesAsync();

                newCurrent.IsCurrentVersion = true;
                _unitOfWork.ChapterPageVersions.Update(newCurrent);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<CreatePageWithVersionResponseDto> CreatePageWithVersionAndFileAsync(
            CreatePageWithVersionRequestDto request,
            Guid actorUserId,
            string? actorRoleName,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var file = CreateFileResource(request.FileDto, actorUserId);
                await _unitOfWork.FileResources.AddAsync(file);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var page = new ChapterPage
                {
                    ChapterId = request.ChapterId,
                    PageNo = request.PageNo,
                    PageNotes = request.PageNotes
                };
                await _unitOfWork.ChapterPages.AddAsync(page);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var version = new ChapterPageVersion
                {
                    ChapterPageId = page.ChapterPageId,
                    VersionNo = 1,
                    PageFileId = file.FileResourceId,
                    VersionNote = request.VersionNote,
                    IsCurrentVersion = true
                };
                await _unitOfWork.ChapterPageVersions.AddAsync(version);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await AddAuditEventAsync(
                    actorUserId,
                    actorRoleName,
                    "PAGE_CREATED",
                    "ChapterPage",
                    page.ChapterPageId,
                    new
                    {
                        chapter_id = page.ChapterId,
                        page_no = page.PageNo,
                        chapter_page_version_id = version.ChapterPageVersionId,
                        file_resource_id = file.FileResourceId
                    },
                    cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return new CreatePageWithVersionResponseDto(
                    new ChapterPageDto(
                        page.ChapterPageId,
                        page.ChapterId,
                        page.PageNo,
                        page.PageNotes,
                        null,
                        null),
                    MapToDto(version),
                    MapFileToDto(file));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        private async Task UnsetCurrentVersionsAsync(
            Guid chapterPageId,
            Guid exceptVersionId,
            CancellationToken cancellationToken)
        {
            var currentVersions = await _unitOfWork.ChapterPageVersions.FindAsync(
                version => version.ChapterPageId == chapterPageId &&
                           version.IsCurrentVersion &&
                           version.ChapterPageVersionId != exceptVersionId);

            foreach (var current in currentVersions)
            {
                current.IsCurrentVersion = false;
                _unitOfWork.ChapterPageVersions.Update(current);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task AddAuditEventAsync(
            Guid actorUserId,
            string? actorRoleName,
            string actionCode,
            string entityType,
            Guid entityId,
            object detail,
            CancellationToken cancellationToken)
        {
            await _unitOfWork.AuditEvents.AddAsync(new AuditEvent
            {
                OccurredAtUtc = DateTime.UtcNow,
                ActorUserId = actorUserId,
                ActorRoleName = actorRoleName,
                ActionCode = actionCode,
                EntityType = entityType,
                EntityId = entityId.ToString(),
                DetailJson = System.Text.Json.JsonSerializer.Serialize(detail)
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static FileResource CreateFileResource(
            CreateFileResourceDto dto,
            Guid actorUserId) =>
            new()
            {
                FilePurposeCode = dto.FilePurposeCode,
                OriginalFileName = dto.OriginalFileName,
                CloudinaryPublicId = dto.CloudinaryPublicId,
                CloudinarySecureUrl = dto.CloudinarySecureUrl,
                ContentType = dto.ContentType,
                FileSizeBytes = dto.FileSizeBytes,
                Sha256Hash = dto.Sha256Hash,
                UploadedByUserId = actorUserId,
                UploadedAtUtc = DateTime.UtcNow
            };

        private static ChapterPageVersionDto MapToDto(ChapterPageVersion version) =>
            new(
                version.ChapterPageVersionId,
                version.ChapterPageId,
                version.VersionNo,
                version.PageFileId,
                version.VersionNote,
                version.IsCurrentVersion);

        private static FileResourceDto MapFileToDto(FileResource file) =>
            new(
                file.FileResourceId,
                file.FilePurposeCode,
                file.OriginalFileName,
                file.CloudinaryPublicId,
                file.CloudinarySecureUrl,
                file.ContentType,
                file.FileSizeBytes,
                file.Sha256Hash,
                file.UploadedByUserId,
                file.UploadedAtUtc,
                file.DeletedAtUtc,
                file.DeletedByUserId);
    }
}
