using Microsoft.AspNetCore.Http;
using System;

namespace Mineplex.FinPlanner.Api.Models
{
    public class UploadDealDocumentDto
    {
        public IFormFile File { get; set; } = null!;
        public string? Tags { get; set; } // Comma separated
    }

    public class DealDocumentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = "";
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = "application/octet-stream";
        public DateTime UploadedAt { get; set; }
        public string? Tags { get; set; }
        public string? UploadedByName { get; set; }
    }
}
