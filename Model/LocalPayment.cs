using SQLite;

namespace SPJ_APP.Model
{
    [Table("payments")]
    public class LocalPayment
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string SaleId { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string Status { get; set; } = "APPROVED";
        public string? Notes { get; set; }

        public bool IsSynced { get; set; } = false;
    }
}