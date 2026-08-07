using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SPJ_APP.Model
{
    [Table("sales_orders_queue")]
    public class SalesOrderQueue : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("sales_person_id")]
        public Guid? SalesPersonId { get; set; }

        [Column("customer_id")]
        public Guid? CustomerId { get; set; }

        [Column("customer_name")]
        public string CustomerName { get; set; } = string.Empty;

        [Column("items_json")]
        public string ItemsJson { get; set; } = "[]";

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("status")]
        public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? UpdatedAt { get; set; }
    }
}
