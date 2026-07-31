using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers.Mangaka
{
    [ApiController]
    [Route("api/mangaka/pages")]
    public sealed class MangakaPageController : ControllerBase
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";
        private const string ActorRoleName = "Mangaka";
        private const string ChapterPagePurpose = "CHAPTER_PAGE_VERSION";
        private const long MaxChapterPageImageBytes = 10 * 1024 * 1024;

        private static readonly HashSet<string> AllowedImageContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

        private readonly IChapterPageService _pageService;
        private readonly IChapterPageVersionService _versionService;
        private readonly IFileResourceService _fileResourceService;
        private readonly IChapterService _chapterService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<MangakaPageController> _logger;

        public MangakaPageController(
            IChapterPageService pageService,
            IChapterPageVersionService versionService,
            IFileResourceService fileResourceService,
            IChapterService chapterService,
            IFileStorageService fileStorageService,
            ILogger<MangakaPageController> logger)
        {
            _pageService = pageService;
            _versionService = versionService;
            _fileResourceService = fileResourceService;
            _chapterService = chapterService;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        [HttpGet("by-chapter/{chapterId:guid}")]
        public async Task<IActionResult> GetByChapterAsync(Guid chapterId)
        {
            if (!TryResolveActorUserId(out _)) return ActorRequired();
            if (chapterId == Guid.Empty) return Invalid("Invalid chapter ID.");

            try
            {
                return Ok(await _pageService.GetChapterPagesByChapterIdAsync(chapterId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pages for chapter {ChapterId}.", chapterId);
                return Problem("Could not load pages right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{pageId:guid}")]
        public async Task<IActionResult> GetByIdAsync(Guid pageId)
        {
            if (!TryResolveActorUserId(out _)) return ActorRequired();
            if (pageId == Guid.Empty) return Invalid("Invalid page ID.");

            try
            {
                var page = await _pageService.GetChapterPageByIdAsync(pageId);
                return page == null ? NotFound() : Ok(page);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading page {PageId}.", pageId);
                return Problem("Could not load the page right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("counts")]
        public async Task<IActionResult> GetCountsAsync([FromBody] PageCountsRequest? request)
        {
            if (!TryResolveActorUserId(out _)) return ActorRequired();
            if (request?.ChapterIds == null) return Invalid("Chapter IDs are required.");
            if (request.ChapterIds.Any(id => id == Guid.Empty))
                return Invalid("Chapter IDs must be valid.");

            try
            {
                return Ok(await _pageService.GetPageCountsByChapterIdsAsync(request.ChapterIds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading page counts.");
                return Problem("Could not load page counts right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("{pageId:guid}/notes")]
        public async Task<IActionResult> UpdateNotesAsync(
            Guid pageId,
            [FromBody] UpdatePageNotesRequest? request)
        {
            if (!TryResolveActorUserId(out _)) return ActorRequired();
            if (pageId == Guid.Empty) return Invalid("Invalid page ID.");
            if (request == null) return Invalid("Request body is required.");

            try
            {
                var page = await _pageService.GetChapterPageByIdAsync(pageId);
                if (page == null) return NotFound();
                await _chapterService.EnsureChapterAllowsContentMutationsAsync(page.ChapterId);

                var updated = await _pageService.UpdateChapterPageAsync(new UpdateChapterPageDto(
                    page.ChapterPageId,
                    page.ChapterId,
                    page.PageNo,
                    string.IsNullOrWhiteSpace(request.PageNotes) ? null : request.PageNotes.Trim()));
                return updated == null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Invalid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notes for page {PageId}.", pageId);
                return Problem("Could not update the page note right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpDelete("{pageId:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid pageId)
        {
            if (!TryResolveActorUserId(out var actorUserId)) return ActorRequired();
            if (pageId == Guid.Empty) return Invalid("Invalid page ID.");

            try
            {
                var page = await _pageService.GetChapterPageByIdAsync(pageId);
                if (page == null) return NotFound();
                await _chapterService.EnsureChapterAllowsContentMutationsAsync(page.ChapterId);

                return await _pageService.DeleteChapterPageAsync(pageId, actorUserId)
                    ? Ok(new { pageId })
                    : Invalid("The page could not be deleted.");
            }
            catch (InvalidOperationException ex)
            {
                return Invalid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting page {PageId}.", pageId);
                return Problem("Could not delete the page right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("create-with-file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePageWithFileAsync(
            [FromForm] CreateChapterPageWithFileForm? form,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out var actorUserId)) return ActorRequired();
            if (form == null) return Invalid("Multipart form data is required.");
            if (form.ChapterId == Guid.Empty) return Invalid("Invalid chapter ID.");
            if (form.PageNo <= 0) return Invalid("Page number must be greater than zero.");
            var fileFailure = ValidatePageFile(form.PageFile);
            if (fileFailure != null) return fileFailure;

            FileUploadResultDto? upload = null;
            try
            {
                await _chapterService.EnsureChapterAllowsContentMutationsAsync(form.ChapterId);
                upload = await UploadPageFileAsync(form.PageFile!, cancellationToken);
                var request = new CreatePageWithVersionRequestDto(
                    form.ChapterId,
                    form.PageNo,
                    Normalize(form.PageNotes),
                    ToFileDto(upload),
                    Normalize(form.VersionNote));

                var result = await _versionService.CreatePageWithVersionAndFileAsync(
                    request, actorUserId, ActorRoleName, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                await CleanupUploadAsync(upload);
                return Invalid(ex.Message);
            }
            catch (Exception ex)
            {
                await CleanupUploadAsync(upload);
                _logger.LogError(ex, "Error creating a page for chapter {ChapterId}.", form.ChapterId);
                return Problem("Could not create the page right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("versions/by-page-ids")]
        public async Task<IActionResult> GetVersionsByPageIdsAsync(
            [FromBody] GetVersionsByPageIdsRequest? request)
        {
            if (!TryResolveActorUserId(out _)) return ActorRequired();
            if (request?.PageIds == null) return Invalid("Page IDs are required.");
            if (request.PageIds.Any(id => id == Guid.Empty)) return Invalid("Page IDs must be valid.");

            try
            {
                return Ok(await _versionService.GetChapterPageVersionsByPageIdsAsync(request.PageIds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading page versions.");
                return Problem("Could not load versions right now.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("versions/{versionId:guid}")]
        public async Task<IActionResult> GetVersionByIdAsync(Guid versionId)
        {
            if (!TryResolveActorUserId(out _)) return ActorRequired();
            if (versionId == Guid.Empty) return Invalid("Invalid version ID.");

            try
            {
                var version = await _versionService.GetChapterPageVersionByIdAsync(versionId);
                return version == null ? NotFound() : Ok(version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading version {VersionId}.", versionId);
                return Problem("Could not load the version right now.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("{pageId:guid}/versions/create-with-file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateVersionWithFileAsync(
            Guid pageId,
            [FromForm] CreateChapterPageVersionWithFileForm? form,
            CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out var actorUserId)) return ActorRequired();
            if (pageId == Guid.Empty) return Invalid("Invalid page ID.");
            if (form == null) return Invalid("Multipart form data is required.");
            if (form.ChapterPageId == Guid.Empty || form.ChapterPageId != pageId)
                return Invalid("The form page ID must match the route page ID.");
            var fileFailure = ValidatePageFile(form.PageFile);
            if (fileFailure != null) return fileFailure;

            FileUploadResultDto? upload = null;
            try
            {
                var page = await _pageService.GetChapterPageByIdAsync(pageId);
                if (page == null) return NotFound();
                await _chapterService.EnsureChapterAllowsContentMutationsAsync(page.ChapterId);

                upload = await UploadPageFileAsync(form.PageFile!, cancellationToken);
                var result = await _versionService.CreateVersionWithFileAsync(
                    pageId,
                    ToFileDto(upload),
                    Normalize(form.VersionNote),
                    form.SetAsCurrent,
                    actorUserId,
                    ActorRoleName,
                    cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                await CleanupUploadAsync(upload);
                return Invalid(ex.Message);
            }
            catch (Exception ex)
            {
                await CleanupUploadAsync(upload);
                _logger.LogError(ex, "Error creating a version for page {PageId}.", pageId);
                return Problem("Could not create the page version right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("{pageId:guid}/versions/set-current")]
        public async Task<IActionResult> SetCurrentVersionAsync(
            Guid pageId,
            [FromBody] SetCurrentVersionRequest? request)
        {
            if (!TryResolveActorUserId(out _)) return ActorRequired();
            if (pageId == Guid.Empty || request?.ChapterPageVersionId == Guid.Empty)
                return Invalid("Invalid current-version request.");

            try
            {
                var page = await _pageService.GetChapterPageByIdAsync(pageId);
                if (page == null) return NotFound();
                await _chapterService.EnsureChapterAllowsContentMutationsAsync(page.ChapterId);
                return await _versionService.SetCurrentVersionAsync(
                    pageId, request!.ChapterPageVersionId)
                    ? Ok()
                    : Invalid("The version does not belong to this page.");
            }
            catch (InvalidOperationException ex)
            {
                return Invalid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting current version on page {PageId}.", pageId);
                return Problem("Could not set the current version right now.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("files/by-ids")]
        public async Task<IActionResult> GetFileResourcesByIdsAsync(
            [FromBody] GetFileResourcesByIdsRequest? request)
        {
            if (!TryResolveActorUserId(out _)) return ActorRequired();
            if (request?.FileIds == null) return Invalid("File IDs are required.");
            if (request.FileIds.Any(id => id == Guid.Empty)) return Invalid("File IDs must be valid.");

            try
            {
                return Ok(await _fileResourceService.GetFileResourcesByIdsAsync(request.FileIds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading file resources.");
                return Problem("Could not load files right now.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private bool TryResolveActorUserId(out Guid actorUserId)
        {
            actorUserId = Guid.Empty;
            if (!Request.Headers.TryGetValue(ActorUserIdHeader, out var values)) return false;
            return Guid.TryParse(values.ToString(), out actorUserId) && actorUserId != Guid.Empty;
        }

        private BadRequestObjectResult ActorRequired() =>
            Invalid("Could not identify the requesting user. Please sign in again.");

        private BadRequestObjectResult Invalid(string message) =>
            BadRequest(new ApiErrorResponse(message));

        private IActionResult? ValidatePageFile(IFormFile? file)
        {
            if (file == null) return Invalid("A chapter page image is required.");
            if (file.Length <= 0) return Invalid("The chapter page image is empty.");
            if (file.Length > MaxChapterPageImageBytes)
                return Invalid("The chapter page image must not exceed 10 MB.");
            if (!AllowedImageContentTypes.Contains(file.ContentType ?? string.Empty))
                return Invalid("Only JPEG, PNG, and WebP chapter page images are supported.");
            return null;
        }

        private async Task<FileUploadResultDto> UploadPageFileAsync(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            return await _fileStorageService.UploadFileAsync(
                stream.ToArray(),
                Path.GetFileName(file.FileName),
                file.ContentType,
                ChapterPagePurpose);
        }

        private static CreateFileResourceDto ToFileDto(FileUploadResultDto upload) =>
            new(
                ChapterPagePurpose,
                upload.OriginalFileName,
                upload.PublicId,
                upload.SecureUrl,
                upload.ContentType,
                upload.FileSizeBytes,
                upload.Sha256Hash,
                null);

        private async Task CleanupUploadAsync(FileUploadResultDto? upload)
        {
            if (upload != null) await CleanupPublicIdAsync(upload.PublicId);
        }

        private async Task CleanupPublicIdAsync(string? publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId)) return;
            try
            {
                await _fileStorageService.DeleteFileAsync(publicId, "image");
            }
            catch (Exception cleanupException)
            {
                _logger.LogWarning(cleanupException,
                    "Best-effort cleanup failed for uploaded file {PublicId}.", publicId);
            }
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
