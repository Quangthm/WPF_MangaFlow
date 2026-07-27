using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    /// <summary>
    /// EF Core implementation of the Editor chapter review write operations.
    /// Uses EF transactions for status transitions and enforces that the actor is an
    /// active Tantou Editor contributor of the chapter's series.
    /// </summary>
    public sealed class EditorChapterRepository : IEditorChapterRepository
    {
        private const string TantouEditorRole = "Tantou Editor";
        private const string StatusUnderReview = "UNDER_REVIEW";
        private const string StatusApproved = "APPROVED";
        private const string StatusRevisionRequested = "REVISION_REQUESTED";
        private const string StatusOnHold = "ON_HOLD";
        private const string StatusScheduled = "SCHEDULED";
        private const string StatusPublished = "PUBLISHED";

        private readonly ApplicationDbContext _dbContext;

        public EditorChapterRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EditorChapterReviewActionResultDto> ApproveChapterAsync(
            Guid chapterId, Guid editorUserId, string? feedback, CancellationToken ct = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                var chapter = await _dbContext.Chapters
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId, ct);

                if (chapter == null)
                    throw new InvalidOperationException("Chapter does not exist.");

                await EnsureActiveTantouEditorContributorAsync(editorUserId, chapter.SeriesId, ct);

                if (chapter.StatusCode != StatusUnderReview)
                    throw new InvalidOperationException("Only chapters under review can be approved.");

                chapter.StatusCode = StatusApproved;
                chapter.UpdatedAtUtc = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new EditorChapterReviewActionResultDto(chapter.ChapterId, chapter.StatusCode);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<EditorChapterReviewActionResultDto> RejectChapterAsync(
            Guid chapterId, Guid editorUserId, string feedback, CancellationToken ct = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                var chapter = await _dbContext.Chapters
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId, ct);

                if (chapter == null)
                    throw new InvalidOperationException("Chapter does not exist.");

                await EnsureActiveTantouEditorContributorAsync(editorUserId, chapter.SeriesId, ct);

                if (chapter.StatusCode != StatusUnderReview)
                    throw new InvalidOperationException("Only chapters under review can be rejected.");

                chapter.StatusCode = StatusRevisionRequested;
                chapter.UpdatedAtUtc = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new EditorChapterReviewActionResultDto(chapter.ChapterId, chapter.StatusCode);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<EditorChapterReviewActionResultDto> PutChapterOnHoldAsync(
            Guid chapterId, Guid editorUserId, string? reason, CancellationToken ct = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                var chapter = await _dbContext.Chapters
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId, ct);

                if (chapter == null)
                    throw new InvalidOperationException("Chapter does not exist.");

                await EnsureActiveTantouEditorContributorAsync(editorUserId, chapter.SeriesId, ct);

                if (chapter.StatusCode != StatusUnderReview)
                    throw new InvalidOperationException("Only chapters under review can be put on hold.");

                chapter.StatusCode = StatusOnHold;
                chapter.UpdatedAtUtc = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new EditorChapterReviewActionResultDto(chapter.ChapterId, chapter.StatusCode);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<EditorChapterReviewActionResultDto> PublishChapterAsync(
            Guid chapterId, Guid editorUserId, CancellationToken ct = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                var chapter = await _dbContext.Chapters
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId, ct);

                if (chapter == null)
                    throw new InvalidOperationException("Chapter does not exist.");

                await EnsureActiveTantouEditorContributorAsync(editorUserId, chapter.SeriesId, ct);

                if (chapter.StatusCode != StatusApproved && chapter.StatusCode != StatusScheduled)
                    throw new InvalidOperationException("Only APPROVED or SCHEDULED chapters can be published.");

                chapter.StatusCode = StatusPublished;
                chapter.ReleasedAtUtc = DateTime.UtcNow;
                chapter.UpdatedAtUtc = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new EditorChapterReviewActionResultDto(chapter.ChapterId, chapter.StatusCode);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        private async Task EnsureActiveTantouEditorContributorAsync(
            Guid editorUserId, Guid seriesId, CancellationToken ct)
        {
            if (editorUserId == Guid.Empty)
                throw new InvalidOperationException("A valid signed-in user is required.");

            bool isActiveContributor = await _dbContext.ActiveSeriesContributors
                .AsNoTracking()
                .AnyAsync(asc =>
                    asc.SeriesId == seriesId &&
                    asc.UserId == editorUserId &&
                    asc.RoleName == TantouEditorRole,
                    ct);

            if (!isActiveContributor)
                throw new InvalidOperationException("Only active Tantou Editor contributors of this series can perform review actions.");
        }
    }
}
