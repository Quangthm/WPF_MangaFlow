using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.WpfMini.Models;

public partial class ChapterPageItemViewModel : ObservableObject
{
    public Guid ChapterPageId { get; init; }
    public Guid ChapterId { get; init; }
    public int PageNo { get; init; }

    [ObservableProperty] private string? _pageNotes;
    [ObservableProperty] private ChapterPageVersionItemViewModel? _selectedVersion;
    [ObservableProperty] private ChapterPageVersionItemViewModel? _currentVersion;

    public ObservableCollection<ChapterPageVersionItemViewModel> Versions { get; } = [];
    public int VersionCount => Versions.Count;
    public bool HasVersions => Versions.Count > 0;
    public string DisplayName => $"Page {PageNo}";
    public string CurrentVersionText =>
        CurrentVersion == null ? "No current version" : $"Current: v{CurrentVersion.VersionNo}";

    public void RefreshVersionSummary()
    {
        OnPropertyChanged(nameof(VersionCount));
        OnPropertyChanged(nameof(HasVersions));
        OnPropertyChanged(nameof(CurrentVersionText));
    }
}

public partial class ChapterPageVersionItemViewModel : ObservableObject
{
    public Guid ChapterPageVersionId { get; init; }
    public Guid ChapterPageId { get; init; }
    public short VersionNo { get; init; }
    public Guid PageFileId { get; init; }
    public string? VersionNote { get; init; }

    [ObservableProperty] private bool _isCurrentVersion;
    [ObservableProperty] private FileResourceDto? _fileResource;

    public string? ImageUrl =>
        IsImageAvailable ? FileResource!.CloudinarySecureUrl : null;
    public string? OriginalFileName => FileResource?.OriginalFileName;
    public bool IsImageAvailable =>
        FileResource is { DeletedAtUtc: null } &&
        !string.IsNullOrWhiteSpace(FileResource.CloudinarySecureUrl);
    public bool IsImageUnavailable => !IsImageAvailable;
    public string VersionDisplayText => $"Version {VersionNo}";

    partial void OnFileResourceChanged(FileResourceDto? value)
    {
        OnPropertyChanged(nameof(ImageUrl));
        OnPropertyChanged(nameof(OriginalFileName));
        OnPropertyChanged(nameof(IsImageAvailable));
        OnPropertyChanged(nameof(IsImageUnavailable));
    }
}
