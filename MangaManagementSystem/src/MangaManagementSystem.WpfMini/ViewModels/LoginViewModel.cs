using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.WpfMini.Models;
using MangaManagementSystem.WpfMini.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MangaManagementSystem.WpfMini.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthApiClient _authApi;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<TestUserDto> _testUsers = [];

    private MainWindowViewModel MainVm =>
        App.ServiceProvider.GetRequiredService<MainWindowViewModel>();

    public LoginViewModel(AuthApiClient authApi)
    {
        _authApi = authApi;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Username is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Password is required.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var response = await _authApi.LoginAsync(new LoginRequest
            {
                Username = Username.Trim(),
                Password = Password
            });

            if (response is null
                || response.User.UserId == Guid.Empty
                || string.IsNullOrWhiteSpace(response.AccessToken))
            {
                ErrorMessage = "Login failed. The server returned an invalid response.";
                return;
            }

            MainVm.SetSession(new CurrentUserSession
            {
                UserId = response.User.UserId.ToString(),
                Username = response.User.Username,
                RoleCode = MapRoleNameToCode(response.RoleName),
                AccessToken = response.AccessToken,
                ExpiresAtUtc = response.ExpiresAtUtc
            });
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadTestUsersAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var users = await _authApi.GetTestUsersAsync();
            TestUsers = new ObservableCollection<TestUserDto>(users ?? []);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load test users: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task QuickLoginAsync(TestUserDto? user)
    {
        if (user is null)
        {
            return;
        }

        Username = user.Username;
        Password = "Password123!";
        await LoginAsync();
    }

    private static string MapRoleNameToCode(string? roleName)
    {
        return roleName?.Trim() switch
        {
            "Tantou Editor" => "EDITOR",
            "Editorial Board Chief" => "BOARD_CHIEF",
            "Editorial Board Member" => "BOARD_MEMBER",
            "Mangaka" => "MANGAKA",
            "Assistant" => "ASSISTANT",
            "Admin" => "ADMIN",
            _ => (roleName ?? string.Empty)
                .Replace(" ", "_", StringComparison.Ordinal)
                .ToUpperInvariant()
        };
    }
}
