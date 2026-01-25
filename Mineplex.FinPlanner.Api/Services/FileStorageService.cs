using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mineplex.FinPlanner.Api.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _basePath;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(IHostEnvironment env, ILogger<LocalFileStorageService> logger)
        {
            // Store uploads in a folder relative to content root
            _basePath = Path.Combine(env.ContentRootPath, "uploads");
            _logger = logger;

            // Ensure base processing directory exists
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task<string> SaveFileAsync(Guid portfolioId, string fileName, byte[] content)
        {
            try
            {
                // Organize by Portfolio -> Year -> Month
                var now = DateTime.UtcNow;
                var relativeFolder = Path.Combine("portfolios", portfolioId.ToString(), now.Year.ToString(), now.Month.ToString("00"));
                var fullFolder = Path.Combine(_basePath, relativeFolder);

                if (!Directory.Exists(fullFolder))
                {
                    Directory.CreateDirectory(fullFolder);
                }

                // Append timestamp to filename to prevent collisions
                var safeFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.UtcNow.Ticks}{Path.GetExtension(fileName)}";
                var relativePath = Path.Combine(relativeFolder, safeFileName);
                var fullPath = Path.Combine(_basePath, relativePath);

                await File.WriteAllBytesAsync(fullPath, content);

                _logger.LogInformation("Saved file to {Path}", fullPath);
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save file {FileName}", fileName);
                throw;
            }
        }

        public async Task<byte[]?> GetFileAsync(string storagePath)
        {
            var fullPath = Path.Combine(_basePath, storagePath);
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found at {Path}", fullPath);
                return null;
            }

            return await File.ReadAllBytesAsync(fullPath);
        }

        public Task<bool> DeleteFileAsync(string storagePath)
        {
            var fullPath = Path.Combine(_basePath, storagePath);
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Deleted file at {Path}", fullPath);
                    return Task.FromResult(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete file {Path}", fullPath);
                    return Task.FromResult(false);
                }
            }
            return Task.FromResult(false);
        }

        public string GetDownloadUrl(string storagePath)
        {
            // For local storage, this would typically return a controller endpoint URL that proxies the file
            // e.g., /api/import/download/{fileId} which uses the storage path
            return storagePath;
        }
    }
}
