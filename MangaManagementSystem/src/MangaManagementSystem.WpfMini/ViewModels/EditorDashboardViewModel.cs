using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.WpfMini.Models;
using MangaManagementSystem.WpfMini.Services;

namespace MangaManagementSystem.WpfMini.ViewModels;

/// <summary>
/// ViewModel for the Editor Dashboard (KPIs + unclaimed proposals + my claimed proposals).
/// </summary>
public partial class EditorDashboardViewModel : ObservableObject
{
    private readonly EditorApiClient _editorApi;

    // ── KPI Counts ──

    [ObservableProperty]
    private int _pendingProposalCount;

    [ObservableProperty]
    private int _completedProposalCount;

    [ObservableProperty]
    private int _chaptersUnderReviewCount;

    [ObservableProperty]
    private int _serializedSeriesCount;

    // ── Section A: Unclaimed Proposals ──

    [ObservableProperty]
    private ObservableCollection<ProposalQueueItem> _unclaimedProposals = [];

    [ObservableProperty]
    private ProposalQueueItem? _selectedUnclaimedProposal;

    // ── Section B: My Claimed Proposals ──

    [ObservableProperty]
    private ObservableCollection<ProposalQueueItem> _myClaimedProposals = [];

    [ObservableProperty]
    private ProposalQueueItem? _selectedClaimedProposal;

    // ── Loading / Error ──

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _claimErrorMessage = string.Empty;

    [ObservableProperty]
    private string _claimSuccessMessage = string.Empty;

    [ObservableProperty]
    private bool _isClaiming;

    // ── Navigation (event to ShellViewModel) ──

    // ── Computed Properties ──

    public bool HasUnclaimedProposals => UnclaimedProposals.Count > 0;
    public bool HasNoUnclaimedProposals => UnclaimedProposals.Count == 0;
    public bool HasMyClaimedProposals => MyClaimedProposals.Count > 0;
    public bool HasNoMyClaimedProposals => MyClaimedProposals.Count == 0;

    /// <summary>
    /// Raised when the user wants to navigate to the Proposal Review tab for a specific proposal.
    /// </summary>
    public event Action<ProposalQueueItem>? NavigateToProposalReview;

    // ── Constructor ──

    public EditorDashboardViewModel(EditorApiClient editorApi)
    {
        _editorApi = editorApi;
    }

    // ── Commands ──

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var dashboard = await _editorApi.GetDashboardAsync();
            if (dashboard is not null)
            {
                PendingProposalCount = dashboard.PendingProposalCount;
                CompletedProposalCount = dashboard.CompletedProposalCount;
                ChaptersUnderReviewCount = dashboard.ChaptersUnderReviewCount;
                SerializedSeriesCount = dashboard.SerializedSeriesCount;
            }

            // Load unclaimed proposals (all queue items, client-filter to CanClaim=true)
            var allProposals = await _editorApi.GetProposalQueueAsync();
            if (allProposals is not null)
            {
                UnclaimedProposals.Clear();
                MyClaimedProposals.Clear();

                foreach (var item in allProposals)
                {
                    if (item.IsClaimedByCurrentEditor)
                    {
                        MyClaimedProposals.Add(item);
                    }
                    else if (item.CanClaim)
                    {
                        UnclaimedProposals.Add(item);
                    }
                }

                OnPropertyChanged(nameof(HasUnclaimedProposals));
                OnPropertyChanged(nameof(HasNoUnclaimedProposals));
                OnPropertyChanged(nameof(HasMyClaimedProposals));
                OnPropertyChanged(nameof(HasNoMyClaimedProposals));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load dashboard: {ex.Message}";
            LoadMockData();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ClaimProposalAsync(ProposalQueueItem? item)
    {
        if (item is null) return;

        IsClaiming = true;
        ClaimErrorMessage = string.Empty;
        ClaimSuccessMessage = string.Empty;

        try
        {
            var result = await _editorApi.ClaimProposalAsync(item.SeriesProposalId);
            if (result is not null)
            {
                ClaimSuccessMessage = $"Claimed \"{item.SeriesTitle}\" successfully.";

                // Move item from unclaimed to claimed
                UnclaimedProposals.Remove(item);
                item.IsClaimedByCurrentEditor = true;
                item.CanClaim = false;
                MyClaimedProposals.Insert(0, item);

                OnPropertyChanged(nameof(HasUnclaimedProposals));
                OnPropertyChanged(nameof(HasNoUnclaimedProposals));
                OnPropertyChanged(nameof(HasMyClaimedProposals));
                OnPropertyChanged(nameof(HasNoMyClaimedProposals));
            }
        }
        catch (Exception ex)
        {
            ClaimErrorMessage = $"Failed to claim: {ex.Message}";
        }
        finally
        {
            IsClaiming = false;
        }
    }

    [RelayCommand]
    private void OpenClaimedProposal(ProposalQueueItem? item)
    {
        if (item is null) return;
        NavigateToProposalReview?.Invoke(item);
    }

    // ── Mock Data ──

    private void LoadMockData()
    {
        PendingProposalCount = 12;
        CompletedProposalCount = 8;
        ChaptersUnderReviewCount = 5;
        SerializedSeriesCount = 3;

        UnclaimedProposals.Clear();
        MyClaimedProposals.Clear();

        UnclaimedProposals.Add(new ProposalQueueItem
        {
            SeriesProposalId = Guid.NewGuid(),
            SeriesId = Guid.NewGuid(),
            SeriesTitle = "Solo Leveling",
            SeriesSlug = "solo-leveling",
            ProposalVersionNo = 2,
            ProposalTitle = "Season 2 Proposal",
            SynopsisSnapshot = "The story follows Sung Jin-Woo...",
            StatusCode = "UNDER_EDITORIAL_REVIEW",
            SubmitterDisplayName = "TestMangaka1",
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-2),
            CanClaim = true,
            IsClaimedByCurrentEditor = false
        });

        UnclaimedProposals.Add(new ProposalQueueItem
        {
            SeriesProposalId = Guid.NewGuid(),
            SeriesId = Guid.NewGuid(),
            SeriesTitle = "Tower of God",
            SeriesSlug = "tower-of-god",
            ProposalVersionNo = 1,
            ProposalTitle = "Initial Proposal",
            SynopsisSnapshot = "A boy named Bam climbs a mysterious tower...",
            StatusCode = "UNDER_EDITORIAL_REVIEW",
            SubmitterDisplayName = "TestMangaka2",
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-5),
            CanClaim = true,
            IsClaimedByCurrentEditor = false
        });

        MyClaimedProposals.Add(new ProposalQueueItem
        {
            SeriesProposalId = Guid.NewGuid(),
            SeriesId = Guid.NewGuid(),
            SeriesTitle = "The Beginning After The End",
            SeriesSlug = "tmate",
            ProposalVersionNo = 1,
            ProposalTitle = "Season 1 Proposal",
            SynopsisSnapshot = "King Grey dies and is reincarnated...",
            StatusCode = "UNDER_EDITORIAL_REVIEW",
            SubmitterDisplayName = "TestMangaka3",
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-7),
            CanClaim = false,
            IsClaimedByCurrentEditor = true
        });

        OnPropertyChanged(nameof(HasUnclaimedProposals));
        OnPropertyChanged(nameof(HasNoUnclaimedProposals));
        OnPropertyChanged(nameof(HasMyClaimedProposals));
        OnPropertyChanged(nameof(HasNoMyClaimedProposals));
    }
}
