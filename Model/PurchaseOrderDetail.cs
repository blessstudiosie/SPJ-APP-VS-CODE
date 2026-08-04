using Postgrest.Models;
using Postgrest.Attributes;

namespace SPJ_APP.Model
{
    [Table("purchase_order_details")]
    public class PurchaseOrderDetail : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("purchase_order_id")]
        public Guid PurchaseOrderId { get; set; }

        [Column("product_id")]
        public Guid ProductId { get; set; }

        [Column("qty_calculated")]
        public decimal QtyCalculated { get; set; }

        [Column("qty_ratio")]
        public decimal QtyRatio { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }
    }
}
