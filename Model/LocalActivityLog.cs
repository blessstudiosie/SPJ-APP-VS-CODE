using SQLite;

namespace SPJ_APP.Model
{
    [Table("activity_logs")]
    public class LocalActivityLog
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsSynced { get; set; } = false;
    }
}