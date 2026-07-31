using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.WpfMini.Interfaces;

namespace MangaManagementSystem.WpfMini.Services.Mangaka;

public sealed class MangakaPageApiClient : IMangakaPageApiClient
{
    private readonly ApiClientBase _apiClient;

    public MangakaPageApiClient(ApiClientBase apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<ChapterPageDto>> GetPagesByChapterAsync(
        Guid chapterId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(chapterId, nameof(chapterId));
        return await _apiClient.GetAsync<List<ChapterPageDto>>(
            $"/api/mangaka/pages/by-chapter/{chapterId}",
            cancellationToken) ?? [];
    }

    public async Task<CreatePageWithVersionResponseDto> CreatePageWithFileAsync(
        Guid chapterId,
        int pageNo,
        string? pageNotes,
        string? versionNote,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ValidateId(chapterId, nameof(chapterId));
        if (pageNo <= 0) throw new ArgumentOutOfRangeException(nameof(pageNo));

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(chapterId.ToString()), "ChapterId");
        form.Add(new StringContent(pageNo.ToString(CultureInfo.InvariantCulture)), "PageNo");
        AddOptional(form, "PageNotes", pageNotes);
        AddOptional(form, "VersionNote", versionNote);
        AddFile(form, filePath, "PageFile");

        var result = await _apiClient.PostFormAsync<CreatePageWithVersionResponseDto>(
            "/api/mangaka/pages/create-with-file",
            form,
            cancellationToken);
        return result ?? throw new InvalidOperationException(
            "The API returned an empty create-page response.");
    }

    public async Task<ChapterPageDto> UpdatePageNotesAsync(
        Guid pageId,
        string? pageNotes,
        CancellationToken cancellationToken = default)
    {
        ValidateId(pageId, nameof(pageId));
        var result = await _apiClient.PutAsync<UpdatePageNotesRequest, ChapterPageDto>(
            $"/api/mangaka/pages/{pageId}/notes",
            new UpdatePageNotesRequest(pageNotes),
            cancellationToken);
        return result ?? throw new InvalidOperationException(
            "The API returned an empty update-page response.");
    }

    public Task DeletePageAsync(
        Guid pageId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(pageId, nameof(pageId));
        return _apiClient.DeleteAsync(
            $"/api/mangaka/pages/{pageId}",
            cancellationToken);
    }

    public async Task<IReadOnlyList<ChapterPageVersionDto>> GetVersionsByPageIdsAsync(
        IReadOnlyList<Guid> pageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pageIds);
        if (pageIds.Any(id => id == Guid.Empty))
            throw new ArgumentException("Page IDs must be valid.", nameof(pageIds));
        if (pageIds.Count == 0) return [];

        return await _apiClient.PostAsync<
            GetVersionsByPageIdsRequest,
            List<ChapterPageVersionDto>>(
            "/api/mangaka/pages/versions/by-page-ids",
            new GetVersionsByPageIdsRequest(pageIds),
            cancellationToken) ?? [];
    }

    public async Task<ChapterPageVersionDto> CreateVersionWithFileAsync(
        Guid pageId,
        string? versionNote,
        bool setAsCurrent,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ValidateId(pageId, nameof(pageId));

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(pageId.ToString()), "ChapterPageId");
        AddOptional(form, "VersionNote", versionNote);
        form.Add(new StringContent(
            setAsCurrent ? "true" : "false"), "SetAsCurrent");
        AddFile(form, filePath, "PageFile");

        var result = await _apiClient.PostFormAsync<ChapterPageVersionDto>(
            $"/api/mangaka/pages/{pageId}/versions/create-with-file",
            form,
            cancellationToken);
        return result ?? throw new InvalidOperationException(
            "The API returned an empty create-version response.");
    }

    public Task SetCurrentVersionAsync(
        Guid pageId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(pageId, nameof(pageId));
        ValidateId(versionId, nameof(versionId));
        return _apiClient.PutAsync(
            $"/api/mangaka/pages/{pageId}/versions/set-current",
            new SetCurrentVersionRequest(versionId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<FileResourceDto>> GetFileResourcesByIdsAsync(
        IReadOnlyList<Guid> fileIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileIds);
        if (fileIds.Any(id => id == Guid.Empty))
            throw new ArgumentException("File IDs must be valid.", nameof(fileIds));
        if (fileIds.Count == 0) return [];

        return await _apiClient.PostAsync<
            GetFileResourcesByIdsRequest,
            List<FileResourceDto>>(
            "/api/mangaka/pages/files/by-ids",
            new GetFileResourcesByIdsRequest(fileIds),
            cancellationToken) ?? [];
    }

    private static void AddOptional(
        MultipartFormDataContent form,
        string name,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            form.Add(new StringContent(value.Trim()), name);
    }

    private static void AddFile(
        MultipartFormDataContent form,
        string filePath,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new FileNotFoundException("Select an existing image file.", filePath);

        var stream = File.OpenRead(filePath);
        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(GetMediaType(filePath));
        form.Add(content, fieldName, Path.GetFileName(filePath));
    }

    private static string GetMediaType(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => throw new InvalidOperationException(
                "Only JPG, JPEG, PNG, and WebP images are supported.")
        };

    private static void ValidateId(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("A valid identifier is required.", parameterName);
    }
}
