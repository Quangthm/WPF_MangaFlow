using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.PutChapterOnHold
{
    public sealed class PutChapterOnHoldCommandHandler
        : IRequestHandler<PutChapterOnHoldCommand, EditorChapterReviewActionResultDto>
    {
        private readonly IEditorChapterRepository _repository;
        public PutChapterOnHoldCommandHandler(IEditorChapterRepository repository)
        {
            _repository = repository;
        }
        public async Task<EditorChapterReviewActionResultDto> Handle(
            PutChapterOnHoldCommand request, CancellationToken cancellationToken)
        {
            return await _repository.PutChapterOnHoldAsync(
                request.ChapterId, request.ActorUserId, request.Reason, cancellationToken);
        }
    }
}
