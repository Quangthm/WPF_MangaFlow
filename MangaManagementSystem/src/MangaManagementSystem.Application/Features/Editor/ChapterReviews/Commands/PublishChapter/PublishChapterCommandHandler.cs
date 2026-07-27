using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.PublishChapter
{
    public sealed class PublishChapterCommandHandler
        : IRequestHandler<PublishChapterCommand, EditorChapterReviewActionResultDto>
    {
        private readonly IEditorChapterRepository _repository;
        public PublishChapterCommandHandler(IEditorChapterRepository repository)
        {
            _repository = repository;
        }
        public async Task<EditorChapterReviewActionResultDto> Handle(
            PublishChapterCommand request, CancellationToken cancellationToken)
        {
            return await _repository.PublishChapterAsync(
                request.ChapterId, request.ActorUserId, cancellationToken);
        }
    }
}
