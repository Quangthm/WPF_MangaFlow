using MangaManagementSystem.WpfMini.Interfaces;
using MangaManagementSystem.WpfMini.Services;
using MangaManagementSystem.WpfMini.Services.Mangaka;
using MangaManagementSystem.WpfMini.Services.Series;
using MangaManagementSystem.WpfMini.ViewModels;
using MangaManagementSystem.WpfMini.ViewModels.Workspaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Windows;

namespace MangaManagementSystem.WpfMini;

public partial class App : System.Windows.Application
{
    public static ServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
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
        services.AddSingleton<EditorApiClient>();
        services.AddSingleton<FileUploadApiClient>();

        services.AddSingleton<IMangakaSeriesApiClient, MangakaSeriesApiClient>();
        services.AddSingleton<IMangakaChapterApiClient, MangakaChapterApiClient>();
        services.AddSingleton<IReferenceDataApiClient, ReferenceDataApiClient>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ShellViewModel>();
        services.AddTransient<MangakaWorkspaceViewModel>();
        services.AddTransient<EditorWorkspaceViewModel>();
        services.AddTransient<BoardWorkspaceViewModel>();
        services.AddTransient<EditorDashboardViewModel>();
        services.AddTransient<EditorProposalReviewViewModel>();
        services.AddTransient<EditorChapterReviewViewModel>();
        services.AddTransient<MangakaSeriesListViewModel>();
        services.AddTransient<SeriesEditorViewModel>();
        services.AddTransient<ChapterListViewModel>();
        services.AddTransient<ChapterEditorViewModel>();

        ServiceProvider = services.BuildServiceProvider();

        var mainWindow = new MainWindow();
        var mainVm = ServiceProvider.GetRequiredService<MainWindowViewModel>();
        mainVm.CurrentViewModel = ServiceProvider.GetRequiredService<LoginViewModel>();
        mainWindow.DataContext = mainVm;
        mainWindow.Show();
    }
}
