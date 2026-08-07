using System;
using SQLite;

namespace SPJ_APP.Model
{
    [Table("sales_orders_queue")]
    public class LocalSalesOrderQueue
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string? SalesPersonId { get; set; }
        public string SalesPersonName { get; set; } = string.Empty;
        public string? CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string ItemsJson { get; set; } = "[]";
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public bool IsSynced { get; set; } = false;
        public DateTime? LastSyncedUpdatedAt { get; set; }
    }

    // Helper item class for deserialization of items_json
    public class SalesOrderItemDTO
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal => Qty * UnitPrice;
    }
}
