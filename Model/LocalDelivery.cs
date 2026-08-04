using SQLite;

namespace SPJ_APP.Model
{
    [Table("deliveries")]
    public class LocalDelivery
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string DeliveryNumber { get; set; } = string.Empty;
        public string? DriverId { get; set; }
        public string? HelperId { get; set; }
        public string? CheckerId { get; set; }
        public string Status { get; set; } = "OPEN";
        public DateTime? ClosedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsSynced { get; set; } = false;
        public DateTime? LastSyncedUpdatedAt { get; set; }
    }
}