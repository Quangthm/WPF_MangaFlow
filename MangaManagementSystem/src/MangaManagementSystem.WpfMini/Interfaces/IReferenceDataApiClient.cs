using MangaManagementSystem.Application.DTOs.Manga;


namespace MangaManagementSystem.WpfMini.Interfaces
{
    public interface IReferenceDataApiClient
    {
        Task<IReadOnlyList<GenreDto>> GetGenresAsync();
        Task<IReadOnlyList<TagDto>> GetTagsAsync();
    }
}
