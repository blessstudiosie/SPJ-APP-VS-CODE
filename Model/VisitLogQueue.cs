using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SPJ_APP.Model
{
    [Table("visit_logs_queue")]
    public class VisitLogQueue : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("sales_person_id")]
        public Guid? SalesPersonId { get; set; }

        [Column("sales_person_name")]
        public string SalesPersonName { get; set; } = string.Empty;

        [Column("customer_id")]
        public Guid? CustomerId { get; set; }

        [Column("customer_name")]
        public string CustomerName { get; set; } = string.Empty;

        [Column("is_new_customer")]
        public bool IsNewCustomer { get; set; } = false;

        [Column("latitude")]
        public double Latitude { get; set; }

        [Column("longitude")]
        public double Longitude { get; set; }

        [Column("photo_url")]
        public string? PhotoUrl { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("status")]
        public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED

        [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? UpdatedAt { get; set; }
    }
}
