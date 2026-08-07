using Postgrest.Attributes;
using Postgrest.Models;

namespace SPJ_APP.Model
{
    [Table("sales_details")]
    public class SaleDetail : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("nota")]
        public string Nota { get; set; } = string.Empty;

        [Column("item_name")]
        public string ItemName { get; set; } = string.Empty;

        [Column("qty")]
        public decimal Qty { get; set; }

        [Column("price")]
        public decimal Price { get; set; }

        [Column("price_category")]
        public string PriceCategory { get; set; } = "R";

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}