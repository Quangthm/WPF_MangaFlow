using System.IO;
using System.Net.Http;
using MangaManagementSystem.WpfMini.Models;

namespace MangaManagementSystem.WpfMini.Services;

/// <summary>
/// API client for Editor workflows. Calls /api/editor/* endpoints directly.
/// Automatically includes the X-Actor-User-Id header for every request.
/// </summary>
public class EditorApiClient
{
    private readonly ApiClientBase _api;

    public EditorApiClient(ApiClientBase api)
    {
        _api = api;
    }

    // ── Dashboard ───────────────────────────────────────────────

    /// <summary>
    /// Gets the editor dashboard read model (KPIs + proposal queue + series activity).
    /// GET /api/editor/dashboard
    /// </summary>
    public Task<EditorDashboardDto?> GetDashboardAsync()
    {
        return _api.GetAsync<EditorDashboardDto>("/api/editor/dashboard");
    }

    // ── Proposals (Queue, Detail, Claim) ────────────────────────

    /// <summary>
    /// Gets the editorial proposal queue, optionally filtered by status or claimed-by-me.
    /// GET /api/editor/proposals?status={status}&claimedByMe={claimedByMe}
    /// </summary>
    public Task<List<ProposalQueueItem>?> GetProposalQueueAsync(
        string? status = null, bool? claimedByMe = null)
    {
        var url = "/api/editor/proposals";
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(status))
            queryParams.Add($"status={status}");

        if (claimedByMe == true)
            queryParams.Add("claimedByMe=true");

        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams);

        return _api.GetAsync<List<ProposalQueueItem>>(url);
    }

    /// <summary>
    /// Gets a single proposal's detail with permission flags.
    /// GET /api/editor/proposals/{proposalId}
    /// </summary>
    public Task<ProposalDetail?> GetProposalDetailAsync(Guid proposalId)
    {
        return _api.GetAsync<ProposalDetail>($"/api/editor/proposals/{proposalId}");
    }

    /// <summary>
    /// Claims a proposal for editorial review.
    /// POST /api/editor/proposals/{proposalId}/claims
    /// </summary>
    public Task<EditorReviewActionResult?> ClaimProposalAsync(Guid proposalId, string? notes = null)
    {
        var body = new { notes };
        return _api.PostAsync<object, EditorReviewActionResult>(
            $"/api/editor/proposals/{proposalId}/claims", body);
    }

    /// <summary>
    /// Request Revision — comments required, markup optional.
    /// POST /api/editor/proposals/{proposalId}/revision-requests
    /// </summary>
    public async Task<EditorReviewActionResult?> RequestRevisionAsync(
        Guid proposalId, string comments, string? markupFilePath = null)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(comments), "comments" }
        };

        if (!string.IsNullOrEmpty(markupFilePath) && File.Exists(markupFilePath))
        {
            var fileBytes = await File.ReadAllBytesAsync(markupFilePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(GetMimeType(markupFilePath));
            form.Add(fileContent, "markupFile", Path.GetFileName(markupFilePath));
        }

        return await _api.PostFormAsync<EditorReviewActionResult>(
            $"/api/editor/proposals/{proposalId}/revision-requests", form);
    }

    /// <summary>
    /// Pass to Board — comments and markup optional.
    /// POST /api/editor/proposals/{proposalId}/board-submissions
    /// </summary>
    public async Task<EditorReviewActionResult?> PassToBoardAsync(
        Guid proposalId, string? comments = null, string? markupFilePath = null)
    {
        var form = new MultipartFormDataContent();

        if (!string.IsNullOrEmpty(comments))
            form.Add(new StringContent(comments), "comments");

        if (!string.IsNullOrEmpty(markupFilePath) && File.Exists(markupFilePath))
        {
            var fileBytes = await File.ReadAllBytesAsync(markupFilePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(GetMimeType(markupFilePath));
            form.Add(fileContent, "markupFile", Path.GetFileName(markupFilePath));
        }

        return await _api.PostFormAsync<EditorReviewActionResult>(
            $"/api/editor/proposals/{proposalId}/board-submissions", form);
    }

    /// <summary>
    /// Cancel proposal — comments + markup required.
    /// POST /api/editor/proposals/{proposalId}/cancellations
    /// </summary>
    public async Task<EditorReviewActionResult?> CancelProposalAsync(
        Guid proposalId, string comments, string markupFilePath)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(comments), "comments" }
        };

        if (!string.IsNullOrEmpty(markupFilePath) && File.Exists(markupFilePath))
        {
            var fileBytes = await File.ReadAllBytesAsync(markupFilePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(GetMimeType(markupFilePath));
            form.Add(fileContent, "markupFile", Path.GetFileName(markupFilePath));
        }

        return await _api.PostFormAsync<EditorReviewActionResult>(
            $"/api/editor/proposals/{proposalId}/cancellations", form);
    }

    // ── Chapter Review ──────────────────────────────────────────

    /// <summary>
    /// Gets the chapter review queue (KPIs + chapter list), scoped to the editor's series.
    /// GET /api/editor/chapters/review-queue?status={status}
    /// </summary>
    public Task<EditorChapterReviewQueueDto?> GetChapterReviewQueueAsync(string? status = null)
    {
        var url = "/api/editor/chapters/review-queue";
        if (!string.IsNullOrEmpty(status))
            url += $"?status={status}";

        return _api.GetAsync<EditorChapterReviewQueueDto>(url);
    }

    /// <summary>
    /// Gets the scoped review detail for one chapter (pages + annotations).
    /// GET /api/editor/chapters/{chapterId}/review-detail
    /// </summary>
    public Task<EditorChapterReviewDetailDto?> GetChapterReviewDetailAsync(Guid chapterId)
    {
        return _api.GetAsync<EditorChapterReviewDetailDto>(
            $"/api/editor/chapters/{chapterId}/review-detail");
    }

    /// <summary>
    /// Approves a chapter under review.
    /// POST /api/editor/chapters/{chapterId}/approve
    /// </summary>
    public Task<EditorChapterReviewActionResult?> ApproveChapterAsync(
        Guid chapterId, string? feedback = null)
    {
        var body = new { feedback };
        return _api.PostAsync<object, EditorChapterReviewActionResult>(
            $"/api/editor/chapters/{chapterId}/approve", body);
    }

    /// <summary>
    /// Rejects / requests revision for a chapter.
    /// POST /api/editor/chapters/{chapterId}/reject
    /// </summary>
    public Task<EditorChapterReviewActionResult?> RejectChapterAsync(
        Guid chapterId, string feedback)
    {
        var body = new { feedback };
        return _api.PostAsync<object, EditorChapterReviewActionResult>(
            $"/api/editor/chapters/{chapterId}/reject", body);
    }

    /// <summary>
    /// Puts a chapter on hold.
    /// POST /api/editor/chapters/{chapterId}/hold
    /// </summary>
    public Task<EditorChapterReviewActionResult?> PutChapterOnHoldAsync(
        Guid chapterId, string? reason = null)
    {
        var body = new { feedback = reason };
        return _api.PostAsync<object, EditorChapterReviewActionResult>(
            $"/api/editor/chapters/{chapterId}/hold", body);
    }

    /// <summary>
    /// Publishes a scheduled/approved chapter.
    /// POST /api/editor/chapters/{chapterId}/publish
    /// </summary>
    public Task<EditorChapterReviewActionResult?> PublishChapterAsync(Guid chapterId)
    {
        return _api.PostAsync<object, EditorChapterReviewActionResult>(
            $"/api/editor/chapters/{chapterId}/publish", new { });
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static string GetMimeType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}
