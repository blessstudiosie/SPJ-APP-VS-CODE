using System;
using SQLite;

namespace SPJ_APP.Model
{
    [Table("visit_logs_queue")]
    public class LocalVisitLogQueue
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string? SalesPersonId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public bool IsNewCustomer { get; set; } = false;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public bool IsSynced { get; set; } = false;
        public DateTime? LastSyncedUpdatedAt { get; set; }
    }
}
