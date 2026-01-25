using System.ComponentModel.DataAnnotations;

namespace Mineplex.FinPlanner.Api.Models.Portfolios
{
    public class CreatePortfolioDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public List<CategoryTargetDto> TargetAllocation { get; set; } = new();
    }

    public class CategoryTargetDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public decimal TargetPercentage { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class PortfolioDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<CategoryTargetDto> TargetAllocation { get; set; } = new();
        public decimal TotalValue { get; set; }
        public Guid? BenchmarkAssetId { get; set; }
        public string BenchmarkSymbol { get; set; } = string.Empty;
    }

    public class UpdatePortfolioDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid? BenchmarkAssetId { get; set; }
    }
}
