using System;
using MangaManagementSystem.Application.DTOs.Editor;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.ApproveChapterReview
{
    public sealed record ApproveChapterReviewCommand(
        Guid ChapterId,
        Guid ActorUserId,
        string? Feedback) : IRequest<EditorChapterReviewActionResultDto>;
}
