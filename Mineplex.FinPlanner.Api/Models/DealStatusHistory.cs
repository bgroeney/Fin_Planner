using System.Text.Json.Serialization;

namespace Mineplex.FinPlanner.Api.Models
{
    public class DealStatusHistory
    {
        public Guid Id { get; set; }
        public Guid DealId { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string? Comment { get; set; }

        public Guid ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Point-in-time snapshots
        public string? InputsSnapshotJson { get; set; }
        public string? SpreadsheetSnapshotJson { get; set; }
        public Guid? SimulationSnapshotId { get; set; }

        [JsonIgnore]
        public PropertyDeal? Deal { get; set; }

        [JsonIgnore]
        public DealSimulationResult? SimulationSnapshot { get; set; }
    }
}
