using SQLite;

namespace SPJ_APP.Model
{
    [Table("stock_opnames")]
    public class LocalStockOpname
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string OpnameNumber { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime OpnameDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsSynced { get; set; } = false;
    }
}
