using Postgrest.Attributes;
using Postgrest.Models;

namespace SPJ_APP.Model
{
    [Table("payments")]
    public class Payment : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("sale_id")]
        public Guid SaleId { get; set; }

        [Column("payment_date")]
        public DateTime PaymentDate { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("payment_method")]
        public string? PaymentMethod { get; set; }

        [Column("status")]
        public string Status { get; set; } = "APPROVED";

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}