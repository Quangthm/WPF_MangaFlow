using MangaManagementSystem.Application.DTOs.Manga;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaManagementSystem.WpfMini.Interfaces
{
    public interface IMangakaSeriesApiClient
    {
        Task<IReadOnlyList<SeriesDto>> GetMySeriesAsync();
        Task<SeriesDto> GetSeriesByIdAsync(Guid seriesId);

        Task<SeriesDraftCreatedDto> CreateDraftAsync(
            string title,
            string synopsis,
            IReadOnlyCollection<Guid> genreIds,
            IReadOnlyCollection<Guid> tagIds,
            string contentLanguageCode,
            string? publicationFrequencyCode,
            string? coverFilePath);

        Task<SeriesDraftUpdatedDto> UpdateDraftAsync(
            Guid seriesId,
            string title,
            string synopsis,
            IReadOnlyCollection<Guid> genreIds,
            IReadOnlyCollection<Guid> tagIds,
            string contentLanguageCode,
            string? publicationFrequencyCode,
            string? coverFilePath);

        Task<SeriesDraftCancelledDto> CancelDraftAsync(
            Guid seriesId,
            string? reason);

        Task<SeriesProposalSubmittedDto> SubmitProposalAsync(
            Guid seriesId,
            string proposalFilePath);
    }
}
