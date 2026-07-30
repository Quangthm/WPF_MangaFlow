using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.WpfMini.Interfaces;

namespace MangaManagementSystem.WpfMini.Services.Series;

public sealed class ReferenceDataApiClient : IReferenceDataApiClient
{
    private readonly ApiClientBase _apiClient;

    public ReferenceDataApiClient(
        ApiClientBase apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<GenreDto>> GetGenresAsync()
    {
        var result =
            await _apiClient.GetAsync<List<GenreDto>>(
                "/api/reference/genres");

        return result ?? [];
    }

    public async Task<IReadOnlyList<TagDto>> GetTagsAsync()
    {
        var result =
            await _apiClient.GetAsync<List<TagDto>>(
                "/api/reference/tags");

        return result ?? [];
    }
}