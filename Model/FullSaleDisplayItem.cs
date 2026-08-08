using System;
using System.Collections.Generic;
using SPJ_APP.Model;

namespace SPJ_APP.Model
{
    public class FullSaleDisplayItem
    {
        public string Id { get; set; } = string.Empty;
        public string Nota { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = "Pelanggan Umum";
        public string SalesPersonId { get; set; } = string.Empty;
        public string SalesPersonName { get; set; } = "Sales Umum";
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string Status { get; set; } = "SO";
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
        public string? Description { get; set; }

        public List<FullSaleDetailDisplayItem> Details { get; set; } = new();
        public LocalSale HeaderOriginal { get; set; } = null!;
    }

    public class FullSaleDetailDisplayItem
    {
        public string Id { get; set; } = string.Empty;
        public string SaleId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = "(produk dihapus)";
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public string PriceCategory { get; set; } = "R";
        public decimal Subtotal { get; set; }
        public LocalSalesDetail DetailOriginal { get; set; } = null!;
    }
}
