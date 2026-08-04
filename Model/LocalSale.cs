using SQLite;

namespace SPJ_APP.Model
{
    [Table("sales")]
    public class LocalSale
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string Nota { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string Status { get; set; } = "SO";
        public string? SalesPersonId { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsSynced { get; set; } = false;
        public DateTime? LastSyncedUpdatedAt { get; set; }
    }
}