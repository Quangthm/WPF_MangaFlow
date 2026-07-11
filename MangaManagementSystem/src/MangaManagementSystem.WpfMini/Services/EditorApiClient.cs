using System.IO;
using System.Net.Http;
using MangaManagementSystem.WpfMini.Models;

namespace MangaManagementSystem.WpfMini.Services;

/// <summary>
/// API client cho Editor workflows.
/// Gọi các endpoint /api/wpf/editor/* (sẽ tạo sau).
/// </summary>
public class EditorApiClient
{
    private readonly ApiClientBase _api;

    public EditorApiClient(ApiClientBase api)
    {
        _api = api;
    }

    /// <summary>
    /// Lấy queue proposal cần review.
    /// GET /api/wpf/editor/proposals/queue?status={status}
    /// </summary>
    public Task<List<ProposalQueueItem>?> GetProposalQueueAsync(string? status = null)
    {
        var url = "/api/wpf/editor/proposals/queue";
        if (!string.IsNullOrEmpty(status))
            url += $"?status={status}";

        return _api.GetAsync<List<ProposalQueueItem>>(url);
    }

    /// <summary>
    /// Lấy chi tiết proposal.
    /// GET /api/wpf/editor/proposals/{proposalId}
    /// </summary>
    public Task<ProposalDetail?> GetProposalDetailAsync(Guid proposalId)
    {
        return _api.GetAsync<ProposalDetail>($"/api/wpf/editor/proposals/{proposalId}");
    }

    /// <summary>
    /// Request Revision — comments required, markup optional.
    /// POST /api/wpf/editor/proposals/{proposalId}/request-revision
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
            $"/api/wpf/editor/proposals/{proposalId}/request-revision", form);
    }

    /// <summary>
    /// Pass to Board — comments and markup optional.
    /// POST /api/wpf/editor/proposals/{proposalId}/pass-to-board
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
            $"/api/wpf/editor/proposals/{proposalId}/pass-to-board", form);
    }

    /// <summary>
    /// Cancel proposal — comments + markup required.
    /// POST /api/wpf/editor/proposals/{proposalId}/cancel
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
            $"/api/wpf/editor/proposals/{proposalId}/cancel", form);
    }

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
