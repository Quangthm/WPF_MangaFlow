using System;
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

    public MainWindowViewModel(ApiClientBase api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public void SetSession(CurrentUserSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!Guid.TryParse(session.UserId, out _))
        {
            throw new InvalidOperationException(
                "The logged-in user ID is invalid.");
        }

        if (string.IsNullOrWhiteSpace(session.AccessToken))
        {
            throw new InvalidOperationException(
                "The login response does not contain a JWT access token.");
        }

        if (string.IsNullOrWhiteSpace(session.RoleCode))
        {
            throw new InvalidOperationException(
                "The login response does not contain a user role.");
        }

        // Xóa thông tin phiên cũ trước khi thiết lập phiên mới.
        _api.ClearActorUserId();
        _api.ClearBearerToken();

        // Một số API cũ sử dụng X-Actor-User-Id.
        _api.SetActorUserId(session.UserId);

        // Editorial Board API sử dụng JWT Bearer.
        _api.SetBearerToken(session.AccessToken);

        CurrentSession = session;
        IsLoggedIn = true;

        Title =
            $"Manga Management - {session.Username} ({session.RoleCode})";

        // Chỉ tạo Shell sau khi session và các HTTP header đã được thiết lập.
        CurrentViewModel =
            App.ServiceProvider.GetRequiredService<ShellViewModel>();
    }

    [RelayCommand]
    private void Logout()
    {
        // Xóa toàn bộ authentication header.
        _api.ClearActorUserId();
        _api.ClearBearerToken();

        CurrentSession = null;
        IsLoggedIn = false;
        Title = "Manga Management System";

        CurrentViewModel =
            App.ServiceProvider.GetRequiredService<LoginViewModel>();
    }
}