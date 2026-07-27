using MangaManagementSystem.Application.DTOs.Manga;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaManagementSystem.WpfMini.Services.Mangaka.Contracts
{
    public interface IMangakaSeriesApiClient
    {
        Task<IReadOnlyList<SeriesDto>> GetMySeriesAsync();
    }
}
