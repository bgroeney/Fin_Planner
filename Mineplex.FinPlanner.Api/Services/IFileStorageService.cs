using System;
using System.Threading.Tasks;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves a file to persistent storage
        /// </summary>
        /// <param name="portfolioId">Portfolio ID for organization</param>
        /// <param name="fileName">Original filename</param>
        /// <param name="content">File content bytes</param>
        /// <returns>Relative storage path string</returns>
        Task<string> SaveFileAsync(Guid portfolioId, string fileName, byte[] content);

        /// <summary>
        /// Retrieves file content from storage
        /// </summary>
        /// <param name="storagePath">Relative storage path returned from SaveFileAsync</param>
        /// <returns>File content bytes or null if not found</returns>
        Task<byte[]?> GetFileAsync(string storagePath);

        /// <summary>
        /// Deletes a file from storage
        /// </summary>
        /// <param name="storagePath">Relative storage path</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteFileAsync(string storagePath);

        /// <summary>
        /// Gets a full system path (for internal use) or download URL
        /// </summary>
        string GetDownloadUrl(string storagePath);
    }
}
