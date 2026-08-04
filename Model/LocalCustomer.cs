using SQLite;

namespace SPJ_APP.Model
{
    [Table("customers")]
    public class LocalCustomer
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? JalurPengiriman { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? SalesPersonId { get; set; }
        public decimal LimitPiutang { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsSynced { get; set; } = true;
        public DateTime? LastSyncedUpdatedAt { get; set; }
    }
}