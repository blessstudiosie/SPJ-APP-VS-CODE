using SQLite;

namespace SPJ_APP.Model
{
    [Table("purchase_orders")]
    public class LocalPurchaseOrder
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string PurchaseOrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string? Notes { get; set; }
        public decimal TotalQty { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsSynced { get; set; } = false;
    }
}
