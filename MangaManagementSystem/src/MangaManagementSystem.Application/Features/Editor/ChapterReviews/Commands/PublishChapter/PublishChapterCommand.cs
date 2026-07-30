using System;
using MangaManagementSystem.Application.DTOs.Editor;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.PublishChapter
{
    public sealed record PublishChapterCommand(
        Guid ChapterId,
        Guid ActorUserId) : IRequest<EditorChapterReviewActionResultDto>;
}
