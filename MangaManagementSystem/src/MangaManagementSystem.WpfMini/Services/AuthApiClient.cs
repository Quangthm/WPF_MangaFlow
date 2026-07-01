using MangaManagementSystem.WpfMini.Models;

namespace MangaManagementSystem.WpfMini.Services;

public class AuthApiClient
{
    private readonly ApiClientBase _api;

    public AuthApiClient(ApiClientBase api)
    {
        _api = api;
    }

    public Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        return _api.PostAsync<LoginRequest, LoginResponse>("/api/wpf/auth/login", request);
    }

    public Task<List<TestUserDto>?> GetTestUsersAsync()
    {
        return _api.GetAsync<List<TestUserDto>>("/api/wpf/auth/test-users");
    }
}
