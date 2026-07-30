using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.WpfMini.Interfaces;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MangaManagementSystem.WpfMini.Services.Mangaka;

public sealed class MangakaSeriesApiClient : IMangakaSeriesApiClient
{
    private readonly ApiClientBase _apiClient;

    public MangakaSeriesApiClient(ApiClientBase apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<SeriesDraftCancelledDto> CancelDraftAsync(Guid seriesId, string? reason)
    {
        if (seriesId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid series must be selected to cancel.",
                nameof(seriesId));
        }

        var normalizedReason =
            string.IsNullOrWhiteSpace(reason)
                ? null
                : reason.Trim();

        if (normalizedReason?.Length > 500)
        {
            throw new ArgumentException(
                "The cancellation reason must be 500 characters or fewer.",
                nameof(reason));
        }

        var request = new
        {
            Reason = normalizedReason
        };

        var result =
            await _apiClient
                .PostAsync<object, SeriesDraftCancelledDto>(
                    $"/api/mangaka/series/{seriesId}/draft-cancellations",
                    request);

        return result
            ?? throw new InvalidOperationException(
                "The API returned an empty cancel-series response.");
    }

    public async Task<SeriesDraftCreatedDto> CreateDraftAsync(string title, string synopsis, IReadOnlyCollection<Guid> genreIds, IReadOnlyCollection<Guid> tagIds, string contentLanguageCode, string? publicationFrequencyCode, string? coverFilePath)
    {
        using var form = CreateSeriesForm(
           title,
           synopsis,
           genreIds,
           tagIds,
           contentLanguageCode,
           publicationFrequencyCode,
           coverFilePath);

        var result =
            await _apiClient.PostFormAsync<SeriesDraftCreatedDto>(
                "/api/mangaka/series/drafts",
                form);

        return result
            ?? throw new InvalidOperationException(
                "The API returned an empty create-series response.");
    }

    public async Task<IReadOnlyList<SeriesDto>> GetMySeriesAsync()
    {
        var result = await _apiClient.GetAsync<List<SeriesDto>>(
           "/api/mangaka/series/my-series");

        return result ?? new List<SeriesDto>();
    }

    public async Task<SeriesDto> GetSeriesByIdAsync(Guid seriesId)
    {
        var result = await _apiClient.GetAsync<SeriesDto>(
            $"/api/mangaka/series/{seriesId}/card");

        return result
            ?? throw new InvalidOperationException(
                "The API returned an empty series response.");
    }

    public async Task<SeriesProposalSubmittedDto> SubmitProposalAsync(Guid seriesId, string proposalFilePath)
    {
        using var form = new MultipartFormDataContent();

        AddFile(
            form,
            "ProposalFile",
            proposalFilePath);

        var result =
            await _apiClient.PostFormAsync<SeriesProposalSubmittedDto>(
                $"/api/mangaka/series/{seriesId}/proposal-submissions",
                form);

        return result
            ?? throw new InvalidOperationException(
                "The API returned an empty proposal-submission response.");
    }

    public async Task<SeriesDraftUpdatedDto> UpdateDraftAsync(Guid seriesId, string title, string synopsis, IReadOnlyCollection<Guid> genreIds, IReadOnlyCollection<Guid> tagIds, string contentLanguageCode, string? publicationFrequencyCode, string? coverFilePath)
    {
        using var form = CreateSeriesForm(
           title,
           synopsis,
           genreIds,
           tagIds,
           contentLanguageCode,
           publicationFrequencyCode,
           coverFilePath);

        var result =
            await _apiClient.PutFormAsync<SeriesDraftUpdatedDto>(
                $"/api/mangaka/series/{seriesId}/draft-profile",
                form);

        return result
            ?? throw new InvalidOperationException(
                "The API returned an empty update-series response.");
    }
    private static MultipartFormDataContent CreateSeriesForm(
       string title,
       string synopsis,
       IReadOnlyCollection<Guid> genreIds,
       IReadOnlyCollection<Guid> tagIds,
       string contentLanguageCode,
       string? publicationFrequencyCode,
       string? coverFilePath)
    {
        var form = new MultipartFormDataContent();

        form.Add(new StringContent(title), "Title");
        form.Add(new StringContent(synopsis), "Synopsis");
        form.Add(new StringContent(contentLanguageCode), "ContentLanguageCode");


        if (!string.IsNullOrWhiteSpace(publicationFrequencyCode))
        {
            form.Add(
                new StringContent(publicationFrequencyCode.Trim()),
                "PublicationFrequencyCode");
        }

        foreach (var genreId in genreIds)
        {
            form.Add(
                new StringContent(genreId.ToString()),
                "GenreIds");
        }

        foreach (var tagId in tagIds)
        {
            form.Add(
                new StringContent(tagId.ToString()),
                "TagIds");
        }

        // SourceSeriesId intentionally omitted.
        // Normal WPF create/edit leaves it null.

        if (!string.IsNullOrWhiteSpace(coverFilePath))
        {
            AddFile(
                form,
                "CoverFile",
                coverFilePath);
        }

        return form;
    }

    private static void AddFile(
        MultipartFormDataContent form,
        string fieldName,
        string filePath)
    {
        var stream = File.OpenRead(filePath);

        var content = new StreamContent(stream);

        content.Headers.ContentType =
            new MediaTypeHeaderValue(
                GetContentType(filePath));

        form.Add(
            content,
            fieldName,
            Path.GetFileName(filePath));
    }

    private static string GetContentType(string filePath)
    {
        return Path.GetExtension(filePath)
            .ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",

            ".pdf" => "application/pdf",

            ".doc" =>
                "application/msword",

            ".docx" =>
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

            _ => "application/octet-stream"
        };
    }
}