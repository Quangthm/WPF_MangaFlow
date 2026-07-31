using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MangaManagementSystem.API.Contracts
{
    public sealed class CreateChapterPageWithFileForm
    {
        [Required]
        public Guid ChapterId { get; init; }

        [Range(1, int.MaxValue)]
        public int PageNo { get; init; }

        [MaxLength(2000)]
        public string? PageNotes { get; init; }

        [MaxLength(1000)]
        public string? VersionNote { get; init; }

        [Required]
        public IFormFile? PageFile { get; init; }
    }

    public sealed class CreateChapterPageVersionWithFileForm
    {
        [Required]
        public Guid ChapterPageId { get; init; }

        [MaxLength(1000)]
        public string? VersionNote { get; init; }

        public bool SetAsCurrent { get; init; }

        [Required]
        public IFormFile? PageFile { get; init; }
    }
}
