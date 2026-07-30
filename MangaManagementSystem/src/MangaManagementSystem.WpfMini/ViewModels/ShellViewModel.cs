using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.WpfMini.Models;
using MangaManagementSystem.WpfMini.ViewModels.Workspaces;
using Microsoft.Extensions.DependencyInjection;

namespace MangaManagementSystem.WpfMini.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private CurrentUserSession? _session;

    [ObservableProperty]
    private ObservableObject? _currentWorkspaceViewModel;

    public ShellViewModel(
        MainWindowViewModel mainViewModel,
        IServiceProvider serviceProvider)
    {
        _mainViewModel = mainViewModel;
        _serviceProvider = serviceProvider;

        Session = mainViewModel.CurrentSession;
        CurrentWorkspaceViewModel = ResolveWorkspace(Session);
    }

    private ObservableObject? ResolveWorkspace(
        CurrentUserSession? session)
    {
        if (session is null)
        {
            return null;
        }

        if (session.IsMangaka)
        {
            return _serviceProvider
                .GetRequiredService<MangakaWorkspaceViewModel>();
        }

        if (session.IsEditor)
        {
            return _serviceProvider
                .GetRequiredService<EditorWorkspaceViewModel>();
        }

        if (session.IsBoardRole)
        {
            return _serviceProvider
                .GetRequiredService<BoardWorkspaceViewModel>();
        }

        return null;
    }

    [RelayCommand]
    private void Logout()
    {
        _mainViewModel.LogoutCommand.Execute(null);
    }
}