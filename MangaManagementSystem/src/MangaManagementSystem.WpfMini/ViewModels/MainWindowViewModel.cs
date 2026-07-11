using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MangaManagementSystem.WpfMini.Models;
using MangaManagementSystem.WpfMini.Services;

namespace MangaManagementSystem.WpfMini.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ApiClientBase _api;
    private readonly AuthApiClient _authApi;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private CurrentUserSession? _currentSession;

    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    private string _title = "Manga Management System";

    public MainWindowViewModel(ApiClientBase api, AuthApiClient authApi)
    {
        _api = api;
        _authApi = authApi;
    }

    public void SetSession(CurrentUserSession session)
    {
        _api.SetAuthToken(session.Token);
        CurrentSession = session;
        IsLoggedIn = true;
        Title = $"Manga Management - {session.DisplayName} ({session.RoleCode})";
        CurrentViewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();
    }

    [RelayCommand]
    private void Logout()
    {
        _api.ClearAuthToken();
        CurrentSession = null;
        IsLoggedIn = false;
        Title = "Manga Management System";
        CurrentViewModel = App.ServiceProvider.GetRequiredService<LoginViewModel>();
    }
}
