using CommunityToolkit.Mvvm.ComponentModel;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.WpfMini.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MangaManagementSystem.WpfMini.ViewModels
{
    public partial class MangakaSeriesListViewModel
    : SeriesListViewModelBase
    {
        private readonly IMangakaSeriesApiClient _apiClient;
        public event Action? CreateSeriesRequested;

        public event Action<Guid>? OpenSeriesRequested;
        public MangakaSeriesListViewModel(IMangakaSeriesApiClient apiClient)
        {
            _apiClient = apiClient;
            PageTitle = "My Series";
            PageSubtitle = "Manage your manga series.";
            ShowCreateButton = true;
        }


        protected async override Task LoadSeriesAsync()
        {
            var result = await _apiClient.GetMySeriesAsync();

            AllSeries.Clear();

            foreach (var series in result)
            {
                AllSeries.Add(series);
            }
        }

        protected override void CreateSeries()
        {
            CreateSeriesRequested?.Invoke();
        }

        protected override void OpenSeries(SeriesDto? series)
        {
            if (series is null)
                return;

            SelectedSeries = series;

            OpenSeriesRequested?.Invoke(
                series.SeriesId);
        }
    }
}
