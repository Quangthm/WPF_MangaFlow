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

    public async Task<T?> GetAsync<T>(string url)
    {
        var response = await _httpClient.GetAsync(url);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var response = await _httpClient.PostAsJsonAsync(url, body, JsonOptions);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    public async Task<TResponse?> PostFormAsync<TResponse>(string url, MultipartFormDataContent form)
    {
        var response = await _httpClient.PostAsync(url, form);
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
            }
            catch
            {
                // not JSON
            }

            var msg = $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})";
            if (!string.IsNullOrEmpty(detail))
                msg += $": {detail}";

            throw new HttpRequestException(msg, null, response.StatusCode);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
