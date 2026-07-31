using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    /// <summary>
    /// EF Core read-only implementation of the Tantou Editor dashboard repository. Every query
    /// uses <c>AsNoTracking</c>. No writes, no stored procedures. Counts are computed server-side
    /// in SQL; only the small preview lists are materialised.
    ///
    /// Each query is wrapped in a try-catch so a single failing table or transient error never
    /// crashes the entire dashboard. Failed KPIs return 0; failed preview lists return empty.
    /// </summary>
    public class EditorDashboardRepository : IEditorDashboardRepository
    {
        private const string ProposalStatusUnderEditorialReview = "UNDER_EDITORIAL_REVIEW";
        private const string ChapterStatusUnderReview = "UNDER_REVIEW";
        private const string SeriesStatusSerialized = "SERIALIZED";

        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<EditorDashboardRepository> _logger;

        public EditorDashboardRepository(
            ApplicationDbContext dbContext,
            ILogger<EditorDashboardRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<EditorDashboardData> GetDashboardDataAsync(
            Guid actorUserId, int proposalQueueTake, int recentSeriesTake, CancellationToken ct = default)
        {
            // ── KPI counts (computed in SQL) ──

            int pendingProposalCount = await TryCountAsync(
                () => _dbContext.SeriesProposals
                    .AsNoTracking()
                    .CountAsync(sp => sp.StatusCode == ProposalStatusUnderEditorialReview, ct),
                nameof(pendingProposalCount), ct);

            int chaptersUnderReviewCount = await TryCountAsync(
                () => _dbContext.Chapters
                    .AsNoTracking()
                    .CountAsync(c => c.StatusCode == ChapterStatusUnderReview, ct),
                nameof(chaptersUnderReviewCount), ct);

            int pendingAnnotationCount = await TryCountAsync(
                () => _dbContext.ChapterPageAnnotations
                    .AsNoTracking()
                    .CountAsync(a => a.ResolvedAtUtc == null, ct),
                nameof(pendingAnnotationCount), ct);

            int serializedSeriesCount = await TryCountAsync(
                () => _dbContext.Series
                    .AsNoTracking()
                    .Where(s => s.StatusCode == SeriesStatusSerialized)
                    .Where(s => _dbContext.ActiveSeriesContributors
                        .Any(asc => asc.SeriesId == s.SeriesId && asc.UserId == actorUserId))
                    .CountAsync(ct),
                nameof(serializedSeriesCount), ct);

            int completedProposalCount = await TryCountAsync(
                () => _dbContext.SeriesProposals
                    .AsNoTracking()
                    .CountAsync(sp => sp.StatusCode != ProposalStatusUnderEditorialReview, ct),
                nameof(completedProposalCount), ct);

            // ── Preview lists ──

            // Proposal review queue preview: newest UNDER_EDITORIAL_REVIEW proposals first.
            List<SeriesProposal> proposalQueue = await TryQueryAsync(
                () => _dbContext.SeriesProposals
                    .AsNoTracking()
                    .Include(sp => sp.Series)
                    .Include(sp => sp.SubmittedByUser)
                    .Where(sp => sp.StatusCode == ProposalStatusUnderEditorialReview)
                    .OrderByDescending(sp => sp.SubmittedAtUtc)
                    .Take(proposalQueueTake)
                    .ToListAsync(ct),
                nameof(proposalQueue), ct);

            // Recent series activity: most recently updated/created series first,
            // across all series so editors can see system-wide activity.
            // Series contributed to by the current editor are still included.
            List<Series> recentSeries = await TryQueryAsync(
                () => _dbContext.Series
                    .AsNoTracking()
                    .Include(s => s.Chapters)
                    .OrderByDescending(s => s.UpdatedAtUtc ?? s.CreatedAtUtc)
                    .Take(recentSeriesTake)
                    .ToListAsync(ct),
                nameof(recentSeries), ct);

            return new EditorDashboardData(
                pendingProposalCount,
                chaptersUnderReviewCount,
                pendingAnnotationCount,
                serializedSeriesCount,
                completedProposalCount,
                proposalQueue,
                recentSeries);
        }

        /// <summary>
        /// Runs a KPI count query, returning 0 on failure so a single missing table or transient
        /// error never crashes the entire dashboard.
        /// </summary>
        private async Task<int> TryCountAsync(Func<Task<int>> query, string label, CancellationToken ct)
        {
            try
            {
                return await query();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dashboard KPI query failed for {Label}, returning 0.", label);
                return 0;
            }
        }

        /// <summary>
        /// Runs a preview-list query, returning an empty list on failure so the dashboard shows
        /// empty sections instead of crashing.
        /// </summary>
        private async Task<List<T>> TryQueryAsync<T>(Func<Task<List<T>>> query, string label, CancellationToken ct)
        {
            try
            {
                return await query();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dashboard preview-list query failed for {Label}, returning empty list.", label);
                return new List<T>();
            }
        }
    }
}
