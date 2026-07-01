using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.WpfMini.Models;

namespace MangaManagementSystem.WpfMini.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainVm;

    [ObservableProperty]
    private CurrentUserSession? _session;

    public ShellViewModel(MainWindowViewModel mainVm)
    {
        _mainVm = mainVm;
        Session = mainVm.CurrentSession;
    }

    [RelayCommand]
    private void Logout()
    {
        _mainVm.LogoutCommand.Execute(null);
    }
}
