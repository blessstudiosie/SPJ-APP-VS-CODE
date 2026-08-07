using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace SPJ_APP.Model
{
    [Table("sales_details")]
    public class SaleDetail : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("sale_id")]
        public string? SaleId { get; set; }

        [Column("product_id")]
        public string? ProductId { get; set; }

        [Column("nota")]
        public string? Nota { get; set; }

        [Column("item_name")]
        public string? ItemName { get; set; }

        [Column("qty")]
        public decimal Qty { get; set; } = 1;

        [Column("price")]
        public decimal Price { get; set; }

        [Column("price_category")]
        public string PriceCategory { get; set; } = "R";

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? CreatedAt { get; set; }
    }
}