using MangaManagementSystem.WpfMini.Models;

namespace MangaManagementSystem.WpfMini.Services;

public sealed class AuthApiClient
{
    private readonly ApiClientBase _api;

    public AuthApiClient(ApiClientBase api)
    {
        _api = api;
    }

    public Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        return _api.PostAsync<LoginRequest, LoginResponse>(
            "/api/auth/login",
            request,
            cancellationToken);
    }

    public Task<List<TestUserDto>?> GetTestUsersAsync(
        CancellationToken cancellationToken = default)
    {
        return _api.GetAsync<List<TestUserDto>>(
            "/api/wpf/auth/test-users",
            cancellationToken);
    }
}
