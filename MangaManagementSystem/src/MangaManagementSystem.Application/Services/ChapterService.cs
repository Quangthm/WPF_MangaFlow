using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class ChapterService : IChapterService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ChapterService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto)
        {
            var entity = new Chapter
            {
                SeriesId = dto.SeriesId,
                ChapterNumberLabel = dto.ChapterNumberLabel,
                ChapterTitle = dto.ChapterTitle,
                StatusCode = dto.StatusCode,
                PlannedReleaseDate = dto.PlannedReleaseDate,
                CreatedAtUtc = System.DateTime.UtcNow
            };
            await _unitOfWork.Chapters.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<ChapterDto?> GetChapterByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.Chapters.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }
        public async Task EnsureChapterAllowsContentMutationsAsync(Guid chapterId)
        {
            var entity = await _unitOfWork.Chapters.GetByIdAsync(chapterId);
            if (entity == null)
                throw new InvalidOperationException("Chapter does not exist.");

            var blockedStatuses = new[]
            {
                "UNDER_REVIEW", "APPROVED", "SCHEDULED",
                "ON_HOLD", "RELEASED", "CANCELLED"
            };

            if (blockedStatuses.Contains(entity.StatusCode))
                throw new InvalidOperationException(
                    "This chapter is locked for content changes. " +
                    "Content editing is blocked while the chapter is UNDER_REVIEW, APPROVED, SCHEDULED, " +
                    "ON_HOLD, RELEASED, or CANCELLED.");
        }
        public async Task<IEnumerable<ChapterDto>> GetChaptersBySeriesIdAsync(Guid seriesId)
        {
            var all = await _unitOfWork.Chapters.GetAllAsync();
            return all.Where(c => c.SeriesId == seriesId).Select(MapToDto);
        }

        public async Task DeleteChapterAsync(Guid id)
        {
            var entity = await _unitOfWork.Chapters.GetByIdAsync(id);
            if (entity != null)
            {
                await _unitOfWork.Chapters.DeleteWithDependenciesAsync(id);
            }
        }

        public async Task UpdateChapterStatusAsync(Guid id, string statusCode)
        {
            var entity = await _unitOfWork.Chapters.GetByIdAsync(id);
            if (entity != null)
            {
                entity.StatusCode = statusCode;
                _unitOfWork.Chapters.Update(entity);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task UpdateChapterTitleAsync(Guid id, string newTitle)
        {
            var entity = await _unitOfWork.Chapters.GetByIdAsync(id);
            if (entity != null)
            {
                entity.ChapterTitle = newTitle;
                _unitOfWork.Chapters.Update(entity);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private static ChapterDto MapToDto(Chapter c) => new(
            c.ChapterId,
            c.SeriesId,
            c.ChapterNumberLabel,
            c.ChapterTitle,
            c.StatusCode,
            c.PlannedReleaseDate,
            c.ReleasedAtUtc,
            c.CreatedAtUtc,
            c.CreatedByUserId
        );
    }
}
