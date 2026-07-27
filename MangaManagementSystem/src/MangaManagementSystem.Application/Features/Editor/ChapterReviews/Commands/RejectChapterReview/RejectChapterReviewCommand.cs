using System;
using MangaManagementSystem.Application.DTOs.Editor;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.RejectChapterReview
{
    public sealed record RejectChapterReviewCommand(
        Guid ChapterId,
        Guid ActorUserId,
        string Feedback) : IRequest<EditorChapterReviewActionResultDto>;
}
