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
        // Use the main login endpoint because it returns the JWT required by EditorialBoardController.
        return _api.PostAsync<LoginRequest, LoginResponse>(
            "/api/auth/login",
            request);
    }

    public Task<List<TestUserDto>?> GetTestUsersAsync()
    {
        return _api.GetAsync<List<TestUserDto>>(
            "/api/wpf/auth/test-users");
    }
}
