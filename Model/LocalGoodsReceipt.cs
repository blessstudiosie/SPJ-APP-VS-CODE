using SQLite;

namespace SPJ_APP.Model
{
    [Table("goods_receipts")]
    public class LocalGoodsReceipt
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string ReceiptNumber { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime ReceiptDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsSynced { get; set; } = false;
    }
}
