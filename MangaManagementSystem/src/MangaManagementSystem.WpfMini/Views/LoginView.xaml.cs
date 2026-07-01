using System.Windows;
using System.Windows.Controls;

namespace MangaManagementSystem.WpfMini.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.LoginViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(vm.Password))
                {
                    PasswordBox.Password = vm.Password;
                }
            };

            PasswordBox.PasswordChanged += (_, _) =>
            {
                vm.Password = PasswordBox.Password;
            };
        }
    }
}
