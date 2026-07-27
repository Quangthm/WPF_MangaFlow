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

    private MainWindowViewModel MainVm => App.ServiceProvider.GetRequiredService<MainWindowViewModel>();

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

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var request = new LoginRequest
            {
                Username = Username,
                Password = Password
            };

            var response = await _authApi.LoginAsync(request);

            if (response is null)
            {
                ErrorMessage = "Login failed. No response from server.";
                return;
            }

            var session = new CurrentUserSession
            {
                UserId = response.UserId,
                Username = response.Username,
                RoleCode = response.RoleCode
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
            if (users is not null)
            {
                TestUsers = new ObservableCollection<TestUserDto>(users);
            }
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
        if (user is null) return;

        Username = user.Username;
        Password = "Password123!";
        await LoginAsync();
    }
}
