using Postgrest.Models;
using Postgrest.Attributes;

namespace SPJ_APP.Model
{
    [Table("purchase_orders")]
    public class PurchaseOrder : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("purchase_order_number")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [Column("order_date")]
        public DateTime OrderDate { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("total_qty")]
        public decimal TotalQty { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
