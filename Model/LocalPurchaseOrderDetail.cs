using SQLite;

namespace SPJ_APP.Model
{
    [Table("purchase_order_details")]
    public class LocalPurchaseOrderDetail
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string PurchaseOrderId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public decimal QtyCalculated { get; set; }
        public decimal QtyRatio { get; set; }
        public string? Notes { get; set; }
    }
}
