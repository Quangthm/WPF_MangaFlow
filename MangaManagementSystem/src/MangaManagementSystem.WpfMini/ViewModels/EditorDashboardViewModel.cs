using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaManagementSystem.WpfMini.Models;
using MangaManagementSystem.WpfMini.Services;

namespace MangaManagementSystem.WpfMini.ViewModels;

/// <summary>
/// ViewModel for the Editor Dashboard (KPIs + proposal queue + series activity).
/// Uses real API via EditorApiClient — no mock data fallback.
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
    private int _pendingAnnotationCount;

    [ObservableProperty]
    private int _serializedSeriesCount;

    // ── Proposal Queue (from dashboard DTO) ──

    [ObservableProperty]
    private ObservableCollection<EditorDashboardProposalDto> _proposalQueue = [];

    [ObservableProperty]
    private ObservableCollection<EditorDashboardSeriesActivityDto> _recentSeriesActivity = [];

    // ── Claimable/Claimed proposals (from proposals endpoint) ──

    [ObservableProperty]
    private ObservableCollection<ProposalQueueItem> _unclaimedProposals = [];

    [ObservableProperty]
    private ProposalQueueItem? _selectedUnclaimedProposal;

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

    // ── Computed Properties ──

    public bool HasUnclaimedProposals => UnclaimedProposals.Count > 0;
    public bool HasNoUnclaimedProposals => UnclaimedProposals.Count == 0;
    public bool HasMyClaimedProposals => MyClaimedProposals.Count > 0;
    public bool HasNoMyClaimedProposals => MyClaimedProposals.Count == 0;

    public bool HasProposalQueue => ProposalQueue.Count > 0;
    public bool HasNoProposalQueue => ProposalQueue.Count == 0;
    public bool HasSeriesActivity => RecentSeriesActivity.Count > 0;
    public bool HasNoSeriesActivity => RecentSeriesActivity.Count == 0;

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
            // 1. Load dashboard KPIs + proposal queue + series activity
            var dashboard = await _editorApi.GetDashboardAsync();
            if (dashboard is not null)
            {
                PendingProposalCount = dashboard.PendingProposalCount;
                CompletedProposalCount = dashboard.CompletedProposalCount;
                ChaptersUnderReviewCount = dashboard.ChaptersUnderReviewCount;
                PendingAnnotationCount = dashboard.PendingAnnotationCount;
                SerializedSeriesCount = dashboard.SerializedSeriesCount;

                ProposalQueue.Clear();
                foreach (var item in dashboard.ProposalReviewQueue)
                {
                    ProposalQueue.Add(item);
                }

                RecentSeriesActivity.Clear();
                foreach (var item in dashboard.RecentSeriesActivity)
                {
                    RecentSeriesActivity.Add(item);
                }
            }

            // 2. Load proposal queue for claim/unclaim UI
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
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load dashboard: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasUnclaimedProposals));
            OnPropertyChanged(nameof(HasNoUnclaimedProposals));
            OnPropertyChanged(nameof(HasMyClaimedProposals));
            OnPropertyChanged(nameof(HasNoMyClaimedProposals));
            OnPropertyChanged(nameof(HasProposalQueue));
            OnPropertyChanged(nameof(HasNoProposalQueue));
            OnPropertyChanged(nameof(HasSeriesActivity));
            OnPropertyChanged(nameof(HasNoSeriesActivity));
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
}