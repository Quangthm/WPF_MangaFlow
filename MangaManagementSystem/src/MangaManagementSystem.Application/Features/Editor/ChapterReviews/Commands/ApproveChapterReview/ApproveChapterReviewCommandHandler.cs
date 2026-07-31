using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.ApproveChapterReview
{
    public sealed class ApproveChapterReviewCommandHandler
        : IRequestHandler<ApproveChapterReviewCommand, EditorChapterReviewActionResultDto>
    {
        private readonly IEditorChapterRepository _repository;
        public ApproveChapterReviewCommandHandler(IEditorChapterRepository repository)
        {
            _repository = repository;
        }
        public async Task<EditorChapterReviewActionResultDto> Handle(
            ApproveChapterReviewCommand request, CancellationToken cancellationToken)
        {
            return await _repository.ApproveChapterAsync(
                request.ChapterId, request.ActorUserId, request.Feedback, cancellationToken);
        }
    }
}
