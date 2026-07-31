using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http;

namespace MangaManagementSystem.WpfMini.Services;

public sealed class ApiClientBase
{
    private readonly HttpClient _httpClient;

    public ApiClientBase(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetActorUserId(string userId)
    {
        _httpClient.DefaultRequestHeaders.Remove("X-Actor-User-Id");

        if (Guid.TryParse(userId, out _))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Actor-User-Id", userId);
        }
    }

    public void ClearActorUserId()
    {
        _httpClient.DefaultRequestHeaders.Remove("X-Actor-User-Id");
    }

    public void SetBearerToken(string? accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(accessToken)
                ? null
                : new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public void ClearBearerToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<T?> GetAsync<T>(
        string url,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<T>(
            JsonOptions,
            cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string url,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            url,
            body,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<TResponse>(
            JsonOptions,
            cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TResponse>(
        string url,
        CancellationToken cancellationToken = default)
    {
        using var content = new ByteArrayContent([]);
        using var response = await _httpClient.PostAsync(
            url,
            content,
            cancellationToken);

        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<TResponse>(
            JsonOptions,
            cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string url,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            url,
            body,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<TResponse>(
            JsonOptions,
            cancellationToken);
    }

    public async Task<TResponse?> PostFormAsync<TResponse>(
        string url,
        MultipartFormDataContent form)
    {
        using var response = await _httpClient.PostAsync(url, form);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    public async Task<TResponse?> PostFormAsync<TResponse>(
        string url,
        MultipartFormDataContent form,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync(url, form, cancellationToken);
        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<TResponse>(
            JsonOptions,
            cancellationToken);
    }

    public async Task PutAsync<TRequest>(
        string url,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            url,
            body,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response);
    }

    public async Task DeleteAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(url, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    public async Task<TResponse?> PutFormAsync<TResponse>(
        string url,
        MultipartFormDataContent form)
    {
        using var response = await _httpClient.PutAsync(url, form);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        string? detail = null;

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.TryGetProperty("error", out var errorProperty))
            {
                detail = errorProperty.ValueKind == JsonValueKind.String
                    ? errorProperty.GetString()
                    : errorProperty.TryGetProperty("message", out var nestedMessage)
                        ? nestedMessage.GetString()
                        : null;
            }

            if (string.IsNullOrWhiteSpace(detail)
                && root.TryGetProperty("message", out var messageProperty))
            {
                detail = messageProperty.GetString();
            }

            if (string.IsNullOrWhiteSpace(detail)
                && root.TryGetProperty("detail", out var detailProperty))
            {
                detail = detailProperty.GetString();
            }

            if (string.IsNullOrWhiteSpace(detail)
                && root.TryGetProperty("title", out var titleProperty))
            {
                detail = titleProperty.GetString();
            }
        }
        catch (JsonException)
        {
            // The response was not JSON. The status-code fallback below is enough.
        }

        var message = !string.IsNullOrWhiteSpace(detail)
            ? detail
            : $"The request failed with HTTP {(int)response.StatusCode}.";

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
