using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.WpfMini.Models;
using MangaManagementSystem.WpfMini.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MangaManagementSystem.WpfMini.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ApiClientBase _api;

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
    }

    public void SetSession(CurrentUserSession session)
    {
        _api.SetActorUserId(session.UserId);
        _api.SetBearerToken(session.AccessToken);

        CurrentSession = session;
        IsLoggedIn = true;
        Title = $"Manga Management - {session.Username} ({session.RoleCode})";
        CurrentViewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();
    }

    [RelayCommand]
    private void Logout()
    {
        _api.ClearActorUserId();
        _api.ClearBearerToken();

        CurrentSession = null;
        IsLoggedIn = false;
        Title = "Manga Management System";
        CurrentViewModel = App.ServiceProvider.GetRequiredService<LoginViewModel>();
    }
}
