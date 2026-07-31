using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http;

namespace MangaManagementSystem.WpfMini.Services;

public class ApiClientBase
{
    private readonly HttpClient _httpClient;

    public ApiClientBase(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetActorUserId(string userId)
    {
        if (!Guid.TryParse(userId, out _))
            return;

        _httpClient.DefaultRequestHeaders.Remove("X-Actor-User-Id");
        _httpClient.DefaultRequestHeaders.Add("X-Actor-User-Id", userId);
    }

    public void ClearActorUserId()
    {
        _httpClient.DefaultRequestHeaders.Remove("X-Actor-User-Id");
    }

    public async Task<T?> GetAsync<T>(
        string url,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
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
        var response = await _httpClient.PostAsJsonAsync(
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
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
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
        var response = await _httpClient.PutAsJsonAsync(
            url,
            body,
            JsonOptions,
            cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>(
            JsonOptions,
            cancellationToken);
    }

    public async Task<TResponse?> PostFormAsync<TResponse>(string url, MultipartFormDataContent form)
    {
        var response = await _httpClient.PostAsync(url, form);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    public async Task<TResponse?> PostFormAsync<TResponse>(
        string url,
        MultipartFormDataContent form,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(url, form, cancellationToken);
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
        var response = await _httpClient.PutAsJsonAsync(
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
        var response = await _httpClient.DeleteAsync(url, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    public async Task<TResponse?> PutFormAsync<TResponse>(
    string url,
    MultipartFormDataContent form)
    {
        var response = await _httpClient.PutAsync(url, form);

        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    /// <summary>
    /// Kiểm tra response status code. Nếu lỗi, đọc body để lấy chi tiết.
    /// </summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            var detail = string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var errProp))
                {
                    detail = errProp.GetString();
                }
                else if (doc.RootElement.TryGetProperty("message", out var msgProp))
                {
                    detail = msgProp.GetString();
                }
                else if (doc.RootElement.TryGetProperty("detail", out var detailProp))
                {
                    detail = detailProp.GetString();
                }
            }
            catch
            {
                // not JSON
            }

            var msg = !string.IsNullOrWhiteSpace(detail)
                ? detail
                : $"The request failed with HTTP {(int)response.StatusCode}.";

            throw new HttpRequestException(msg, null, response.StatusCode);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
