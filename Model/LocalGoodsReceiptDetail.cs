using SQLite;

namespace SPJ_APP.Model
{
    [Table("goods_receipt_details")]
    public class LocalGoodsReceiptDetail
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string ReceiptId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public decimal QtyReady { get; set; }
        public decimal QtyFisik { get; set; }
        public string? Notes { get; set; }
    }
}
