using Mineplex.FinPlanner.Api.Models.Import;

namespace Mineplex.FinPlanner.Api.Services.Import
{
    public interface INetwealthCsvParser
    {
        ImportPreviewDto Parse(string csvContent);
    }
}
