using System.Text.Json.Serialization;

namespace MangaManagementSystem.WpfMini.Models;

/// <summary>
/// Item trong queue editor proposal review.
/// Map từ backend ProposalQueueItemDto.
/// </summary>
public class ProposalQueueItem
{
    [JsonPropertyName("seriesProposalId")]
    public Guid SeriesProposalId { get; set; }

    [JsonPropertyName("seriesId")]
    public Guid SeriesId { get; set; }

    [JsonPropertyName("seriesTitle")]
    public string SeriesTitle { get; set; } = string.Empty;

    [JsonPropertyName("seriesSlug")]
    public string SeriesSlug { get; set; } = string.Empty;

    [JsonPropertyName("proposalVersionNo")]
    public short ProposalVersionNo { get; set; }

    [JsonPropertyName("proposalTitle")]
    public string ProposalTitle { get; set; } = string.Empty;

    [JsonPropertyName("synopsisSnapshot")]
    public string SynopsisSnapshot { get; set; } = string.Empty;

    [JsonPropertyName("statusCode")]
    public string StatusCode { get; set; } = string.Empty;

    [JsonPropertyName("submittedByUserId")]
    public Guid SubmittedByUserId { get; set; }

    [JsonPropertyName("submitterDisplayName")]
    public string SubmitterDisplayName { get; set; } = string.Empty;

    [JsonPropertyName("submittedAtUtc")]
    public DateTime SubmittedAtUtc { get; set; }

    [JsonPropertyName("reviewedByUserId")]
    public Guid? ReviewedByUserId { get; set; }

    [JsonPropertyName("reviewerDisplayName")]
    public string? ReviewerDisplayName { get; set; }

    [JsonPropertyName("reviewedAtUtc")]
    public DateTime? ReviewedAtUtc { get; set; }

    [JsonPropertyName("comments")]
    public string? Comments { get; set; }

    [JsonPropertyName("proposalFileId")]
    public Guid ProposalFileId { get; set; }

    [JsonPropertyName("proposalFileUrl")]
    public string? ProposalFileUrl { get; set; }

    [JsonPropertyName("proposalFileName")]
    public string? ProposalFileName { get; set; }

    [JsonPropertyName("markupFileId")]
    public Guid? MarkupFileId { get; set; }

    [JsonPropertyName("markupFileUrl")]
    public string? MarkupFileUrl { get; set; }

    // Claim info (computed server-side)
    [JsonPropertyName("isClaimedByCurrentEditor")]
    public bool IsClaimedByCurrentEditor { get; set; }

    [JsonPropertyName("canClaim")]
    public bool CanClaim { get; set; }

    // Computed display helpers
    public string SubmittedAtDisplay => SubmittedAtUtc.ToString("MMM dd, yyyy");

    public string StatusDisplay => StatusCode switch
    {
        "UNDER_EDITORIAL_REVIEW" => "Under Review",
        "UNDER_BOARD_REVIEW" => "Board Review",
        "REVISION_REQUESTED" => "Revision Requested",
        "APPROVED" => "Approved",
        "CANCELLED" => "Cancelled",
        _ => StatusCode
    };
}

/// <summary>
/// Chi tiết proposal cho editor review.
/// Map từ backend EditorProposalDetailDto.
/// </summary>
public class ProposalDetail
{
    [JsonPropertyName("seriesProposalId")]
    public Guid SeriesProposalId { get; set; }

    [JsonPropertyName("seriesId")]
    public Guid SeriesId { get; set; }

    [JsonPropertyName("seriesTitle")]
    public string SeriesTitle { get; set; } = string.Empty;

    [JsonPropertyName("seriesSlug")]
    public string SeriesSlug { get; set; } = string.Empty;

    [JsonPropertyName("seriesCoverUrl")]
    public string? SeriesCoverUrl { get; set; }

    [JsonPropertyName("proposalVersionNo")]
    public short ProposalVersionNo { get; set; }

    [JsonPropertyName("proposalTitle")]
    public string ProposalTitle { get; set; } = string.Empty;

    [JsonPropertyName("genres")]
    public List<GenreDto> Genres { get; set; } = [];

    [JsonPropertyName("tags")]
    public List<TagDto> Tags { get; set; } = [];

    [JsonPropertyName("synopsisSnapshot")]
    public string SynopsisSnapshot { get; set; } = string.Empty;

    [JsonPropertyName("proposalStatusCode")]
    public string ProposalStatusCode { get; set; } = string.Empty;

    [JsonPropertyName("seriesStatusCode")]
    public string? SeriesStatusCode { get; set; }

    [JsonPropertyName("submittedByUserId")]
    public Guid SubmittedByUserId { get; set; }

    [JsonPropertyName("submitterDisplayName")]
    public string SubmitterDisplayName { get; set; } = string.Empty;

    [JsonPropertyName("submittedAtUtc")]
    public DateTime SubmittedAtUtc { get; set; }

    [JsonPropertyName("reviewedByUserId")]
    public Guid? ReviewedByUserId { get; set; }

    [JsonPropertyName("reviewerDisplayName")]
    public string? ReviewerDisplayName { get; set; }

    [JsonPropertyName("reviewedAtUtc")]
    public DateTime? ReviewedAtUtc { get; set; }

    [JsonPropertyName("comments")]
    public string? Comments { get; set; }

    [JsonPropertyName("proposalFileId")]
    public Guid ProposalFileId { get; set; }

    [JsonPropertyName("proposalFileName")]
    public string? ProposalFileName { get; set; }

    [JsonPropertyName("proposalFileUrl")]
    public string? ProposalFileUrl { get; set; }

    [JsonPropertyName("markupFileId")]
    public Guid? MarkupFileId { get; set; }

    [JsonPropertyName("markupFileName")]
    public string? MarkupFileName { get; set; }

    [JsonPropertyName("markupFileUrl")]
    public string? MarkupFileUrl { get; set; }

    // Permission flags (computed server-side)
    [JsonPropertyName("currentActorIsActiveTantouEditorContributor")]
    public bool CurrentActorIsActiveTantouEditorContributor { get; set; }

    [JsonPropertyName("currentActorHasClaimed")]
    public bool CurrentActorHasClaimed { get; set; }

    [JsonPropertyName("hasEditorialDecision")]
    public bool HasEditorialDecision { get; set; }

    [JsonPropertyName("canClaim")]
    public bool CanClaim { get; set; }

    [JsonPropertyName("canRequestRevision")]
    public bool CanRequestRevision { get; set; }

    [JsonPropertyName("canPassToBoard")]
    public bool CanPassToBoard { get; set; }

    [JsonPropertyName("canCancel")]
    public bool CanCancel { get; set; }

    // Display helpers
    public string StatusDisplay => ProposalStatusCode switch
    {
        "UNDER_EDITORIAL_REVIEW" => "Under Review",
        "UNDER_BOARD_REVIEW" => "Board Review",
        "REVISION_REQUESTED" => "Revision Requested",
        "APPROVED" => "Approved",
        "CANCELLED" => "Cancelled",
        _ => ProposalStatusCode
    };

    public string SubmittedAtDisplay => SubmittedAtUtc.ToString("MMM dd, yyyy");

    public bool HasMarkup => MarkupFileId.HasValue;

    public bool HasSeriesCover => !string.IsNullOrEmpty(SeriesCoverUrl);
}

/// <summary>
/// Result của editor review action.
/// Map từ backend EditorReviewActionResultDto.
/// </summary>
public class EditorReviewActionResult
{
    [JsonPropertyName("seriesProposalId")]
    public Guid SeriesProposalId { get; set; }

    [JsonPropertyName("proposalStatusCode")]
    public string ProposalStatusCode { get; set; } = string.Empty;
}

/// <summary>
/// Genre lookup model (dùng chung cho nhiều màn hình).
/// </summary>
public class GenreDto
{
    [JsonPropertyName("genreId")]
    public Guid GenreId { get; set; }

    [JsonPropertyName("genreName")]
    public string GenreName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Tag lookup model.
/// </summary>
public class TagDto
{
    [JsonPropertyName("tagId")]
    public Guid TagId { get; set; }

    [JsonPropertyName("tagName")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// Dashboard Models
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Editor dashboard read model (KPIs + proposal queue + series activity).
/// </summary>
public class EditorDashboardDto
{
    [JsonPropertyName("pendingProposalCount")]
    public int PendingProposalCount { get; set; }

    [JsonPropertyName("chaptersUnderReviewCount")]
    public int ChaptersUnderReviewCount { get; set; }

    [JsonPropertyName("pendingAnnotationCount")]
    public int PendingAnnotationCount { get; set; }

    [JsonPropertyName("serializedSeriesCount")]
    public int SerializedSeriesCount { get; set; }

    [JsonPropertyName("completedProposalCount")]
    public int CompletedProposalCount { get; set; }

    [JsonPropertyName("proposalReviewQueue")]
    public List<EditorDashboardProposalDto> ProposalReviewQueue { get; set; } = [];

    [JsonPropertyName("recentSeriesActivity")]
    public List<EditorDashboardSeriesActivityDto> RecentSeriesActivity { get; set; } = [];
}

/// <summary>
/// A single proposal row in the dashboard's Proposal Review Queue section.
/// </summary>
public class EditorDashboardProposalDto
{
    [JsonPropertyName("seriesProposalId")]
    public Guid SeriesProposalId { get; set; }

    [JsonPropertyName("seriesId")]
    public Guid SeriesId { get; set; }

    [JsonPropertyName("seriesTitle")]
    public string SeriesTitle { get; set; } = string.Empty;

    [JsonPropertyName("proposalTitle")]
    public string ProposalTitle { get; set; } = string.Empty;

    [JsonPropertyName("proposalVersionNo")]
    public short ProposalVersionNo { get; set; }

    [JsonPropertyName("submittedByDisplayName")]
    public string SubmittedByDisplayName { get; set; } = string.Empty;

    [JsonPropertyName("submittedAtUtc")]
    public DateTime SubmittedAtUtc { get; set; }

    [JsonPropertyName("statusCode")]
    public string StatusCode { get; set; } = string.Empty;

    public string SubmittedAtDisplay => SubmittedAtUtc.ToString("MMM dd, yyyy");

    public string StatusDisplay => StatusCode switch
    {
        "UNDER_EDITORIAL_REVIEW" => "Under Review",
        "UNDER_BOARD_REVIEW" => "Board Review",
        "REVISION_REQUESTED" => "Revision Requested",
        "APPROVED" => "Approved",
        "CANCELLED" => "Cancelled",
        _ => StatusCode
    };
}

/// <summary>
/// A single series activity row in the dashboard's Recent Series Activity section.
/// </summary>
public class EditorDashboardSeriesActivityDto
{
    [JsonPropertyName("seriesId")]
    public Guid SeriesId { get; set; }

    [JsonPropertyName("seriesTitle")]
    public string SeriesTitle { get; set; } = string.Empty;

    [JsonPropertyName("seriesSlug")]
    public string SeriesSlug { get; set; } = string.Empty;

    [JsonPropertyName("statusCode")]
    public string StatusCode { get; set; } = string.Empty;

    [JsonPropertyName("latestChapterLabel")]
    public string? LatestChapterLabel { get; set; }

    [JsonPropertyName("lastActivityAtUtc")]
    public DateTime? LastActivityAtUtc { get; set; }

    [JsonPropertyName("canOpenSeriesSlugPage")]
    public bool CanOpenSeriesSlugPage { get; set; }

    public string LastActivityDisplay =>
        LastActivityAtUtc?.ToString("MMM dd, yyyy") ?? "N/A";
}

// ═══════════════════════════════════════════════════════════════
// Chapter Review Models
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Chapter review queue read model (KPIs + filtered chapter list).
/// </summary>
public class EditorChapterReviewQueueDto
{
    [JsonPropertyName("underReviewCount")]
    public int UnderReviewCount { get; set; }

    [JsonPropertyName("approvedThisWeekCount")]
    public int ApprovedThisWeekCount { get; set; }

    [JsonPropertyName("revisionRequestedCount")]
    public int RevisionRequestedCount { get; set; }

    [JsonPropertyName("onHoldCount")]
    public int OnHoldCount { get; set; }

    [JsonPropertyName("chapters")]
    public List<EditorChapterReviewQueueItemDto> Chapters { get; set; } = [];
}

/// <summary>
/// A single chapter row in the review queue.
/// </summary>
public class EditorChapterReviewQueueItemDto
{
    [JsonPropertyName("chapterId")]
    public Guid ChapterId { get; set; }

    [JsonPropertyName("seriesId")]
    public Guid SeriesId { get; set; }

    [JsonPropertyName("seriesTitle")]
    public string SeriesTitle { get; set; } = string.Empty;

    [JsonPropertyName("seriesSlug")]
    public string? SeriesSlug { get; set; }

    [JsonPropertyName("chapterNumberLabel")]
    public string ChapterNumberLabel { get; set; } = string.Empty;

    [JsonPropertyName("chapterTitle")]
    public string? ChapterTitle { get; set; }

    [JsonPropertyName("statusCode")]
    public string StatusCode { get; set; } = string.Empty;

    [JsonPropertyName("pageCount")]
    public int PageCount { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("workspaceUrl")]
    public string? WorkspaceUrl { get; set; }

    // Display helpers
    public string DisplayTitle => !string.IsNullOrEmpty(ChapterTitle)
        ? $"{ChapterNumberLabel} - {ChapterTitle}"
        : ChapterNumberLabel;

    public string CreatedAtDisplay => CreatedAtUtc.ToString("MMM dd, yyyy");

    public string StatusDisplay => StatusCode switch
    {
        "UNDER_REVIEW" => "Under Review",
        "REVISION_REQUESTED" => "Revision Requested",
        "ON_HOLD" => "On Hold",
        "APPROVED" => "Approved",
        "SCHEDULED" => "Scheduled",
        "PUBLISHED" => "Published",
        _ => StatusCode
    };
}

/// <summary>
/// Full review detail for a single chapter (pages + annotations).
/// </summary>
public class EditorChapterReviewDetailDto
{
    [JsonPropertyName("chapterId")]
    public Guid ChapterId { get; set; }

    [JsonPropertyName("seriesId")]
    public Guid SeriesId { get; set; }

    [JsonPropertyName("seriesTitle")]
    public string SeriesTitle { get; set; } = string.Empty;

    [JsonPropertyName("seriesSlug")]
    public string? SeriesSlug { get; set; }

    [JsonPropertyName("chapterNumberLabel")]
    public string ChapterNumberLabel { get; set; } = string.Empty;

    [JsonPropertyName("chapterTitle")]
    public string? ChapterTitle { get; set; }

    [JsonPropertyName("statusCode")]
    public string StatusCode { get; set; } = string.Empty;

    [JsonPropertyName("pageCount")]
    public int PageCount { get; set; }

    [JsonPropertyName("currentVersionCount")]
    public int CurrentVersionCount { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("submittedByDisplayName")]
    public string? SubmittedByDisplayName { get; set; }

    [JsonPropertyName("pages")]
    public List<EditorChapterReviewPageDto> Pages { get; set; } = [];

    [JsonPropertyName("openAnnotations")]
    public List<EditorChapterReviewAnnotationDto> OpenAnnotations { get; set; } = [];

    [JsonPropertyName("workspaceUrl")]
    public string? WorkspaceUrl { get; set; }

    [JsonPropertyName("canOpenWorkspace")]
    public bool CanOpenWorkspace { get; set; }

    // Display helpers
    public string DisplayTitle => !string.IsNullOrEmpty(ChapterTitle)
        ? $"{ChapterNumberLabel} - {ChapterTitle}"
        : ChapterNumberLabel;

    public string StatusDisplay => StatusCode switch
    {
        "UNDER_REVIEW" => "Under Review",
        "REVISION_REQUESTED" => "Revision Requested",
        "ON_HOLD" => "On Hold",
        "APPROVED" => "Approved",
        "SCHEDULED" => "Scheduled",
        "PUBLISHED" => "Published",
        _ => StatusCode
    };

    public string CreatedAtDisplay => CreatedAtUtc.ToString("MMM dd, yyyy");
}

/// <summary>
/// A single chapter page with its current version file URL.
/// </summary>
public class EditorChapterReviewPageDto
{
    [JsonPropertyName("chapterPageId")]
    public Guid ChapterPageId { get; set; }

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    [JsonPropertyName("currentVersionId")]
    public Guid? CurrentVersionId { get; set; }

    [JsonPropertyName("currentVersionFileUrl")]
    public string? CurrentVersionFileUrl { get; set; }

    [JsonPropertyName("currentVersionNo")]
    public short? CurrentVersionNo { get; set; }
}

/// <summary>
/// An open (unresolved) annotation on a chapter page.
/// </summary>
public class EditorChapterReviewAnnotationDto
{
    [JsonPropertyName("annotationId")]
    public Guid AnnotationId { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("issueTypeCode")]
    public string IssueTypeCode { get; set; } = string.Empty;

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("createdByDisplayName")]
    public string? CreatedByDisplayName { get; set; }

    [JsonPropertyName("isResolved")]
    public bool IsResolved { get; set; }

    public string CreatedAtDisplay => CreatedAtUtc.ToString("MMM dd, yyyy HH:mm");
}

/// <summary>
/// Result of an editorial chapter review action.
/// </summary>
public class EditorChapterReviewActionResult
{
    [JsonPropertyName("chapterId")]
    public Guid ChapterId { get; set; }

    [JsonPropertyName("statusCode")]
    public string StatusCode { get; set; } = string.Empty;
}
