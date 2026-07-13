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
    public int GenreId { get; set; }

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
    public int TagId { get; set; }

    [JsonPropertyName("tagName")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
