using Postgrest.Attributes;
using Postgrest.Models;

namespace SPJ_APP.Model
{
    [Table("sales")]
    public class Sale : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("nota")]
        public string Nota { get; set; } = string.Empty;

        [Column("customer_id")]
        public Guid? CustomerId { get; set; }

        [Column("order_date")]
        public DateTime OrderDate { get; set; }

        [Column("delivery_date")]
        public DateTime? DeliveryDate { get; set; }

        [Column("status")]
        public string Status { get; set; } = "SO";

        [Column("sales_person_id")]
        public Guid? SalesPersonId { get; set; }

        [Column("total")]
        public decimal Total { get; set; }

        [Column("paid")]
        public decimal Paid { get; set; }

        [Column("remaining")]
        public decimal Remaining { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? UpdatedAt { get; set; }
    }
}