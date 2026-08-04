using Postgrest.Attributes;
using Postgrest.Models;

namespace SPJ_APP.Model
{
    [Table("sales_details")]
    public class SaleDetail : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("sale_id")]
        public Guid SaleId { get; set; }

        [Column("product_id")]
        public Guid ProductId { get; set; }

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