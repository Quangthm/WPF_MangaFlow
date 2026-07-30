using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;

namespace MangaManagementSystem.Application.Interfaces
{
    /// <summary>
    /// Write repository for Editor chapter review actions. Handles status transitions
    /// (approve, reject, hold, publish) with access control (active Tantou Editor contributor check).
    /// </summary>
    public interface IEditorChapterRepository
    {
        Task<EditorChapterReviewActionResultDto> ApproveChapterAsync(
            Guid chapterId, Guid editorUserId, string? feedback, CancellationToken ct = default);

        Task<EditorChapterReviewActionResultDto> RejectChapterAsync(
            Guid chapterId, Guid editorUserId, string feedback, CancellationToken ct = default);

        Task<EditorChapterReviewActionResultDto> PutChapterOnHoldAsync(
            Guid chapterId, Guid editorUserId, string? reason, CancellationToken ct = default);

        Task<EditorChapterReviewActionResultDto> PublishChapterAsync(
            Guid chapterId, Guid editorUserId, CancellationToken ct = default);
    }
}
