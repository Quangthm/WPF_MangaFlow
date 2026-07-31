using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.WpfMini.Interfaces;
using MangaManagementSystem.WpfMini.Models;
using Microsoft.Win32;
using System.Windows;
using System.IO;

namespace MangaManagementSystem.WpfMini.ViewModels;

public partial class ChapterPageWorkspaceViewModel : ObservableObject
{
    private readonly IMangakaPageApiClient _pageApiClient;

    [ObservableProperty] private Guid _chapterId;
    [ObservableProperty] private string _chapterStatusCode = string.Empty;
    [ObservableProperty] private string _chapterDisplayName = string.Empty;
    [ObservableProperty] private ChapterPageItemViewModel? _selectedPage;
    [ObservableProperty] private ChapterPageVersionItemViewModel? _selectedVersion;
    [ObservableProperty] private string _selectedPageNotes = string.Empty;
    [ObservableProperty] private string _newVersionNote = string.Empty;
    [ObservableProperty] private bool _setNewVersionAsCurrent = true;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _busyMessage = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _successMessage = string.Empty;
    [ObservableProperty] private bool _isInitialized;

    public ObservableCollection<ChapterPageItemViewModel> Pages { get; } = [];

    public bool HasPages => Pages.Count > 0;
    public bool HasNoPages => IsInitialized && Pages.Count == 0;
    public bool HasSelectedPage => SelectedPage != null;
    public bool HasSelectedVersion => SelectedVersion != null;
    public bool IsChapterSaved => ChapterId != Guid.Empty;
    public bool CanMutatePages =>
        IsInitialized &&
        !IsBusy &&
        IsChapterSaved &&
        (IsStatus("DRAFT") || IsStatus("REVISION_REQUESTED"));
    public bool CanAddPages => CanMutatePages;
    public bool CanUploadVersion => CanMutatePages && HasSelectedPage;
    public bool CanSetCurrentVersion =>
        CanMutatePages &&
        HasSelectedPage &&
        HasSelectedVersion &&
        !SelectedVersion!.IsCurrentVersion;
    public bool CanSavePageNotes => CanMutatePages && HasSelectedPage;
    public bool CanDeletePage => CanMutatePages && HasSelectedPage;
    public string ContentLockMessage
    {
        get
        {
            if (!IsChapterSaved)
                return "Save the chapter before adding pages.";
            if (!IsInitialized)
                return string.Empty;
            if (!IsStatus("DRAFT") && !IsStatus("REVISION_REQUESTED"))
                return "Pages are read-only while this chapter is in its current status.";
            return string.Empty;
        }
    }
    public bool HasContentLockMessage => !string.IsNullOrWhiteSpace(ContentLockMessage);

    public ChapterPageWorkspaceViewModel(IMangakaPageApiClient pageApiClient)
    {
        _pageApiClient = pageApiClient;
    }

    public async Task InitializeAsync(
        Guid chapterId,
        string chapterStatusCode,
        string chapterDisplayName)
    {
        if (chapterId == Guid.Empty)
            throw new ArgumentException("A saved chapter is required.", nameof(chapterId));

        ChapterId = chapterId;
        ChapterStatusCode = chapterStatusCode ?? string.Empty;
        ChapterDisplayName = chapterDisplayName ?? string.Empty;
        IsInitialized = true;
        await LoadAsync(null);
    }

    public void ResetForUnsavedChapter()
    {
        ChapterId = Guid.Empty;
        ChapterStatusCode = "DRAFT";
        ChapterDisplayName = "Unsaved chapter";
        Pages.Clear();
        SelectedPage = null;
        SelectedVersion = null;
        SelectedPageNotes = string.Empty;
        NewVersionNote = string.Empty;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        BusyMessage = string.Empty;
        IsInitialized = true;
        NotifyState();
    }

    public void UpdateChapterStatus(string chapterStatusCode)
    {
        ChapterStatusCode = chapterStatusCode ?? string.Empty;
        NotifyState();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!IsChapterSaved || IsBusy) return;
        await LoadAsync(SelectedPage?.ChapterPageId);
    }

    [RelayCommand]
    private async Task AddPagesAsync()
    {
        if (!CanAddPages) return;

        var dialog = new OpenFileDialog
        {
            Title = "Select chapter page images",
            Multiselect = true,
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.webp|JPEG files|*.jpg;*.jpeg|PNG files|*.png|WebP files|*.webp"
        };
        if (dialog.ShowDialog() != true) return;

        BeginBusy("Uploading chapter pages...");
        var createdIds = new List<Guid>();
        var failures = new List<string>();
        var nextPageNo = Pages.Select(page => page.PageNo).DefaultIfEmpty(0).Max() + 1;
        try
        {
            foreach (var filePath in dialog.FileNames)
            {
                try
                {
                    BusyMessage = $"Uploading {Path.GetFileName(filePath)} as Page {nextPageNo}...";
                    var result = await _pageApiClient.CreatePageWithFileAsync(
                        ChapterId,
                        nextPageNo,
                        null,
                        "Original upload",
                        filePath);
                    createdIds.Add(result.Page.ChapterPageId);
                    nextPageNo++;
                }
                catch (Exception ex)
                {
                    failures.Add($"{Path.GetFileName(filePath)}: {ex.Message}");
                }
            }

            await LoadCoreAsync(createdIds.FirstOrDefault());
            if (failures.Count == 0)
                SuccessMessage = $"{createdIds.Count} page(s) uploaded successfully.";
            else
                ErrorMessage =
                    $"{createdIds.Count} page(s) uploaded; {failures.Count} failed. " +
                    string.Join(" | ", failures);
        }
        catch (Exception ex)
        {
            ErrorMessage = Friendly(ex);
        }
        finally
        {
            EndBusy();
        }
    }

    [RelayCommand]
    private async Task UploadNewVersionAsync()
    {
        if (!CanUploadVersion || SelectedPage == null) return;

        var dialog = CreateSingleImageDialog("Select a new page version");
        if (dialog.ShowDialog() != true) return;

        var pageId = SelectedPage.ChapterPageId;
        BeginBusy("Uploading the new version...");
        try
        {
            var created = await _pageApiClient.CreateVersionWithFileAsync(
                pageId,
                Normalize(NewVersionNote),
                SetNewVersionAsCurrent,
                dialog.FileName);
            NewVersionNote = string.Empty;
            await LoadCoreAsync(pageId, created.ChapterPageVersionId);
            SuccessMessage = $"Version {created.VersionNo} uploaded successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = Friendly(ex);
        }
        finally
        {
            EndBusy();
        }
    }

    [RelayCommand]
    private async Task SetCurrentVersionAsync()
    {
        if (!CanSetCurrentVersion || SelectedPage == null || SelectedVersion == null) return;
        if (MessageBox.Show(
                $"Make Version {SelectedVersion.VersionNo} the current version?",
                "Set Current Version",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        BeginBusy("Updating the current version...");
        try
        {
            await _pageApiClient.SetCurrentVersionAsync(
                SelectedPage.ChapterPageId,
                SelectedVersion.ChapterPageVersionId);
            foreach (var version in SelectedPage.Versions)
                version.IsCurrentVersion = version == SelectedVersion;
            SelectedPage.CurrentVersion = SelectedVersion;
            SelectedPage.RefreshVersionSummary();
            SuccessMessage = $"Version {SelectedVersion.VersionNo} is now current.";
        }
        catch (Exception ex)
        {
            ErrorMessage = Friendly(ex);
        }
        finally
        {
            EndBusy();
            NotifyState();
        }
    }

    [RelayCommand]
    private async Task SavePageNotesAsync()
    {
        if (!CanSavePageNotes || SelectedPage == null) return;
        var normalized = Normalize(SelectedPageNotes);

        BeginBusy("Saving page notes...");
        try
        {
            var updated = await _pageApiClient.UpdatePageNotesAsync(
                SelectedPage.ChapterPageId,
                normalized);
            SelectedPage.PageNotes = updated.PageNotes;
            SelectedPageNotes = updated.PageNotes ?? string.Empty;
            SuccessMessage = "Page notes saved.";
        }
        catch (Exception ex)
        {
            ErrorMessage = Friendly(ex);
        }
        finally
        {
            EndBusy();
        }
    }

    [RelayCommand]
    private async Task DeletePageAsync()
    {
        if (!CanDeletePage || SelectedPage == null) return;
        var page = SelectedPage;
        if (MessageBox.Show(
                $"Delete Page {page.PageNo}?\n\nThe page will disappear from the active chapter pages. Its version history is preserved.",
                "Delete Page",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var oldIndex = Pages.IndexOf(page);
        BeginBusy($"Deleting Page {page.PageNo}...");
        try
        {
            await _pageApiClient.DeletePageAsync(page.ChapterPageId);
            Pages.Remove(page);
            SelectedPage = Pages.Count == 0
                ? null
                : Pages[Math.Min(oldIndex, Pages.Count - 1)];
            SuccessMessage = $"Page {page.PageNo} deleted.";
        }
        catch (Exception ex)
        {
            ErrorMessage = Friendly(ex);
        }
        finally
        {
            EndBusy();
            NotifyState();
        }
    }

    private async Task LoadAsync(Guid? preferredPageId)
    {
        BeginBusy("Loading pages and versions...");
        try
        {
            await LoadCoreAsync(preferredPageId);
        }
        catch (Exception ex)
        {
            ErrorMessage = Friendly(ex);
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task LoadCoreAsync(
        Guid? preferredPageId,
        Guid? preferredVersionId = null)
    {
        var pageDtos = (await _pageApiClient.GetPagesByChapterAsync(ChapterId))
            .OrderBy(page => page.PageNo)
            .ToList();
        var pageIds = pageDtos.Select(page => page.ChapterPageId).ToList();
        var versionDtos = await _pageApiClient.GetVersionsByPageIdsAsync(pageIds);
        var fileIds = versionDtos.Select(version => version.PageFileId).Distinct().ToList();
        var fileDtos = await _pageApiClient.GetFileResourcesByIdsAsync(fileIds);
        var filesById = fileDtos.ToDictionary(file => file.FileResourceId);
        var versionsByPage = versionDtos.GroupBy(version => version.ChapterPageId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(v => v.VersionNo));

        Pages.Clear();
        foreach (var pageDto in pageDtos)
        {
            var page = new ChapterPageItemViewModel
            {
                ChapterPageId = pageDto.ChapterPageId,
                ChapterId = pageDto.ChapterId,
                PageNo = pageDto.PageNo,
                PageNotes = pageDto.PageNotes
            };
            if (versionsByPage.TryGetValue(page.ChapterPageId, out var versions))
            {
                foreach (var dto in versions)
                {
                    filesById.TryGetValue(dto.PageFileId, out var file);
                    page.Versions.Add(new ChapterPageVersionItemViewModel
                    {
                        ChapterPageVersionId = dto.ChapterPageVersionId,
                        ChapterPageId = dto.ChapterPageId,
                        VersionNo = dto.VersionNo,
                        PageFileId = dto.PageFileId,
                        VersionNote = dto.VersionNote,
                        IsCurrentVersion = dto.IsCurrentVersion,
                        FileResource = file
                    });
                }
            }
            page.CurrentVersion =
                page.Versions.FirstOrDefault(version => version.IsCurrentVersion) ??
                page.Versions.FirstOrDefault();
            page.RefreshVersionSummary();
            Pages.Add(page);
        }

        SelectedPage =
            Pages.FirstOrDefault(page => page.ChapterPageId == preferredPageId) ??
            Pages.FirstOrDefault();
        if (SelectedPage != null && preferredVersionId.HasValue)
            SelectedVersion = SelectedPage.Versions.FirstOrDefault(
                version => version.ChapterPageVersionId == preferredVersionId) ??
                SelectedPage.CurrentVersion;
        NotifyState();
    }

    partial void OnSelectedPageChanged(ChapterPageItemViewModel? value)
    {
        SelectedPageNotes = value?.PageNotes ?? string.Empty;
        var version = value?.CurrentVersion ?? value?.Versions.FirstOrDefault();
        if (value != null) value.SelectedVersion = version;
        SelectedVersion = version;
        NewVersionNote = string.Empty;
        NotifyState();
    }

    partial void OnSelectedVersionChanged(ChapterPageVersionItemViewModel? value)
    {
        if (SelectedPage != null) SelectedPage.SelectedVersion = value;
        NotifyState();
    }

    partial void OnIsBusyChanged(bool value) => NotifyState();
    partial void OnChapterStatusCodeChanged(string value) => NotifyState();
    partial void OnChapterIdChanged(Guid value) => NotifyState();
    partial void OnIsInitializedChanged(bool value) => NotifyState();

    private void BeginBusy(string message)
    {
        IsBusy = true;
        BusyMessage = message;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private void EndBusy()
    {
        IsBusy = false;
        BusyMessage = string.Empty;
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasPages));
        OnPropertyChanged(nameof(HasNoPages));
        OnPropertyChanged(nameof(HasSelectedPage));
        OnPropertyChanged(nameof(HasSelectedVersion));
        OnPropertyChanged(nameof(IsChapterSaved));
        OnPropertyChanged(nameof(CanMutatePages));
        OnPropertyChanged(nameof(CanAddPages));
        OnPropertyChanged(nameof(CanUploadVersion));
        OnPropertyChanged(nameof(CanSetCurrentVersion));
        OnPropertyChanged(nameof(CanSavePageNotes));
        OnPropertyChanged(nameof(CanDeletePage));
        OnPropertyChanged(nameof(ContentLockMessage));
        OnPropertyChanged(nameof(HasContentLockMessage));
    }

    private bool IsStatus(string status) =>
        string.Equals(ChapterStatusCode, status, StringComparison.OrdinalIgnoreCase);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Friendly(Exception exception) =>
        string.IsNullOrWhiteSpace(exception.Message)
            ? "The operation could not be completed."
            : exception.Message;

    private static OpenFileDialog CreateSingleImageDialog(string title) =>
        new()
        {
            Title = title,
            Multiselect = false,
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.webp|JPEG files|*.jpg;*.jpeg|PNG files|*.png|WebP files|*.webp"
        };
}
