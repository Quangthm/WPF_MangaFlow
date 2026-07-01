using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MangaManagementSystem.WpfMini.Services;
using MangaManagementSystem.WpfMini.ViewModels;
using MangaManagementSystem.WpfMini.Views;

namespace MangaManagementSystem.WpfMini;

public partial class App : Application
{
    public static ServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(System.IO.Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        var configuration = builder.Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddSingleton(sp =>
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(configuration["ApiBaseUrl"] ?? "https://localhost:5001"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            return client;
        });

        services.AddSingleton<ApiClientBase>();

        services.AddSingleton<AuthApiClient>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ShellViewModel>();

        ServiceProvider = services.BuildServiceProvider();

        var mainWindow = new MainWindow();
        mainWindow.DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.Show();
    }
}
