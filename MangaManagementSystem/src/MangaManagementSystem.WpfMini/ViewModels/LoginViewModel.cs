using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MangaManagementSystem.WpfMini.Models;
using MangaManagementSystem.WpfMini.Services;

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

            if (response is null || response.User.UserId == Guid.Empty)
            {
                ErrorMessage = "Login failed. No valid response from server.";
                return;
            }

            var session = new CurrentUserSession
            {
                UserId = response.User.UserId.ToString(),
                Username = response.User.Username,
                RoleCode = MapRoleNameToCode(response.RoleName),
                AccessToken = response.AccessToken
            };

            MainVm.SetSession(session);
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

    private static string MapRoleNameToCode(string roleName)
    {
        return roleName.Trim() switch
        {
            "Tantou Editor" => "EDITOR",
            "Editorial Board Chief" => "BOARD_CHIEF",
            "Editorial Board Member" => "BOARD_MEMBER",
            "Mangaka" => "MANGAKA",
            "Assistant" => "ASSISTANT",
            "Admin" => "ADMIN",
            _ => roleName.Trim().Replace(" ", "_").ToUpperInvariant()
        };
    }
}
