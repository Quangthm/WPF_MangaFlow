using System.Net.Http.Headers;
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

    public void SetAuthToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearAuthToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public Task<T?> GetAsync<T>(string url)
    {
        return _httpClient.GetFromJsonAsync<T>(url, JsonOptions);
    }

    public Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body)
    {
        return _httpClient.PostAsJsonAsync(url, body, JsonOptions)
            .ContinueWith(t =>
            {
                t.Result.EnsureSuccessStatusCode();
                return t.Result.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
            }).Unwrap();
    }

    public async Task<TResponse?> PostFormAsync<TResponse>(string url, MultipartFormDataContent form)
    {
        var response = await _httpClient.PostAsync(url, form);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
