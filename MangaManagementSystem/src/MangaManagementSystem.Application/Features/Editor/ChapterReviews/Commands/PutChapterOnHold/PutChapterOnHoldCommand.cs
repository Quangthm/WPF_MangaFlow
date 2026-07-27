using System;
using MangaManagementSystem.Application.DTOs.Editor;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.PutChapterOnHold
{
    public sealed record PutChapterOnHoldCommand(
        Guid ChapterId,
        Guid ActorUserId,
        string? Reason) : IRequest<EditorChapterReviewActionResultDto>;
}
