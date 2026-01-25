using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mineplex.FinPlanner.Api.Models.Import;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly INetwealthImportService _importService;
        private readonly IFileStorageService _fileStorageService;

        public ImportController(INetwealthImportService importService, IFileStorageService fileStorageService)
        {
            _importService = importService;
            _fileStorageService = fileStorageService;
        }

        private Guid GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        [HttpPost("preview")]
        public async Task<ActionResult<ImportPreviewDto>> PreviewImport([FromBody] ConfirmImportDto dto)
        {
            try
            {
                var result = await _importService.PreviewImportAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                // Return 400 for bad file
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmImport([FromBody] ConfirmImportDto dto)
        {
            try
            {
                await _importService.ConfirmImportAsync(GetUserId(), dto);
                return Ok(new { message = "Import successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("history/{portfolioId}")]
        public async Task<ActionResult<List<FileUploadDto>>> GetImportHistory(Guid portfolioId)
        {
            try
            {
                var history = await _importService.GetUploadHistoryAsync(portfolioId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("download/{fileId}")]
        public async Task<IActionResult> DownloadFile(Guid fileId)
        {
            try
            {
                var path = await _importService.GetFileDownloadUrlAsync(fileId, GetUserId());
                if (string.IsNullOrEmpty(path)) return NotFound();

                var fileContent = await _fileStorageService.GetFileAsync(path);
                if (fileContent == null) return NotFound();

                // Determine content type (simplified)
                var contentType = "text/csv";
                if (path.EndsWith(".xlsx")) contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(fileContent, contentType, Path.GetFileName(path));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{fileId}")]
        public async Task<IActionResult> DeleteImport(Guid fileId)
        {
            try
            {
                await _importService.DeleteImportAsync(fileId, GetUserId());
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{fileId}/unload")]
        public async Task<IActionResult> UnloadImport(Guid fileId)
        {
            try
            {
                await _importService.ToggleImportActiveAsync(fileId, GetUserId(), false);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{fileId}/reload")]
        public async Task<IActionResult> ReloadImport(Guid fileId)
        {
            try
            {
                await _importService.ToggleImportActiveAsync(fileId, GetUserId(), true);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
