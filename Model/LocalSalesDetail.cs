using SQLite;

namespace SPJ_APP.Model
{
    [Table("sales_details")]
    public class LocalSalesDetail
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string SaleId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public string PriceCategory { get; set; } = "R";
        public decimal Subtotal { get; set; }
    }
}