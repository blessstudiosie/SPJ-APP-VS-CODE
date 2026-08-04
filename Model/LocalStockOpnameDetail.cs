using SQLite;

namespace SPJ_APP.Model
{
    [Table("stock_opname_details")]
    public class LocalStockOpnameDetail
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string OpnameId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public decimal SystemQty { get; set; }
        public decimal ActualQty { get; set; }
        public decimal DifferenceQty { get; set; }
        public string? Notes { get; set; }
    }
}
