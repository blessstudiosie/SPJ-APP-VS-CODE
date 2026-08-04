using SQLite;

namespace SPJ_APP.Model
{
    [Table("sales_persons")]
    public class LocalSalesPerson
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public decimal TargetOmset { get; set; }
        public string? Role { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsSynced { get; set; } = true;
        public DateTime? LastSyncedUpdatedAt { get; set; }
    }
}