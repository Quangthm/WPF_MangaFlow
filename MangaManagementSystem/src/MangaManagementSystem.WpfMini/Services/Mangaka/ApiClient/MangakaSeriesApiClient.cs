using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.WpfMini.Services.Mangaka.Contracts;

namespace MangaManagementSystem.WpfMini.Services.Mangaka.ApiClient;

public sealed class MangakaSeriesApiClient : IMangakaSeriesApiClient
{
    private readonly ApiClientBase _apiClient;

    public MangakaSeriesApiClient(ApiClientBase apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<SeriesDto>> GetMySeriesAsync()
    {
        var result = await _apiClient.GetAsync<List<SeriesDto>>(
           "/api/mangaka/series/my-series");

        return result ?? new List<SeriesDto>();
    }
}