namespace SPJ_APP.Model
{
    public class SaleDisplayItem
    {
        public string Id { get; set; } = "";
        public string Nota { get; set; } = "";
        public string CustomerName { get; set; } = "-";
        public string SalesPersonName { get; set; } = "-";
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string Status { get; set; } = "SO";
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
        public LocalSale Original { get; set; } = null!;
    }
}