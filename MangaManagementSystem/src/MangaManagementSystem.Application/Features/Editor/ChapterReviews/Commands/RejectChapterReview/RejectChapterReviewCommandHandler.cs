using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.RejectChapterReview
{
    public sealed class RejectChapterReviewCommandHandler
        : IRequestHandler<RejectChapterReviewCommand, EditorChapterReviewActionResultDto>
    {
        private readonly IEditorChapterRepository _repository;
        public RejectChapterReviewCommandHandler(IEditorChapterRepository repository)
        {
            _repository = repository;
        }
        public async Task<EditorChapterReviewActionResultDto> Handle(
            RejectChapterReviewCommand request, CancellationToken cancellationToken)
        {
            return await _repository.RejectChapterAsync(
                request.ChapterId, request.ActorUserId, request.Feedback, cancellationToken);
        }
    }
}
