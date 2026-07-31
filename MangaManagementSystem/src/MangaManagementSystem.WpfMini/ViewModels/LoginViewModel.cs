using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.WpfMini.Models;
using MangaManagementSystem.WpfMini.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MangaManagementSystem.WpfMini.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private const string TestPassword = "Password123!";

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
        if (IsLoading)
        {
            return;
        }

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
            var request = new LoginRequest
            {
                Username = Username.Trim(),
                Password = Password
            };

            var response = await _authApi.LoginAsync(request);

            if (response is null)
            {
                ErrorMessage = "Login failed. The server returned no response.";
                return;
            }

            if (response.User is null ||
                response.User.UserId == Guid.Empty)
            {
                ErrorMessage = "Login failed. Invalid user information.";
                return;
            }

            if (string.IsNullOrWhiteSpace(response.AccessToken))
            {
                ErrorMessage =
                    "Login succeeded, but the server did not return a JWT token.";
                return;
            }

            // Prefer the top-level RoleName.
            // Fall back to User.RoleName when necessary.
            var roleName = !string.IsNullOrWhiteSpace(response.RoleName)
                ? response.RoleName
                : response.User.RoleName ?? string.Empty;

            if (string.IsNullOrWhiteSpace(roleName))
            {
                ErrorMessage =
                    "Login succeeded, but the server did not return a role.";
                return;
            }

            var roleCode = MapRoleNameToCode(roleName);

            var session = new CurrentUserSession
            {
                UserId = response.User.UserId.ToString(),
                Username = response.User.Username,
                RoleCode = roleCode,
                AccessToken = response.AccessToken
            };

            MainVm.SetSession(session);
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = string.IsNullOrWhiteSpace(ex.Message)
                ? "Cannot connect to the API."
                : ex.Message;
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
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var users = await _authApi.GetTestUsersAsync();

            TestUsers = new ObservableCollection<TestUserDto>(
                users ?? []);
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage =
                $"Failed to connect to the API: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Failed to load test users: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task QuickLoginAsync(TestUserDto? user)
    {
        if (user is null || IsLoading)
        {
            return;
        }

        Username = user.Username;
        Password = TestPassword;

        await LoginAsync();
    }

    private static string MapRoleNameToCode(string roleName)
    {
        var normalizedRole = roleName.Trim();

        return normalizedRole switch
        {
            "Tantou Editor" => "EDITOR",
            "Editorial Board Chief" => "BOARD_CHIEF",
            "Editorial Board Member" => "BOARD_MEMBER",
            "Mangaka" => "MANGAKA",
            "Assistant" => "ASSISTANT",
            "Admin" => "ADMIN",

            // Already-normalized role codes
            "EDITOR" => "EDITOR",
            "BOARD_CHIEF" => "BOARD_CHIEF",
            "BOARD_MEMBER" => "BOARD_MEMBER",
            "MANGAKA" => "MANGAKA",
            "ASSISTANT" => "ASSISTANT",
            "ADMIN" => "ADMIN",

            _ => normalizedRole
                .Replace(" ", "_")
                .ToUpperInvariant()
        };
    }
}