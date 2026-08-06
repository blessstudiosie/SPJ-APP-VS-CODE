using Postgrest.Models;
using Postgrest.Attributes;

namespace SPJ_APP.Model
{
    [Table("activity_logs")]
    public class ActivityLog : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("user_name")]
        public string UserName { get; set; }

        [Column("action")]
        public string Action { get; set; }

        [Column("details")]
        public string? Details { get; set; }

        [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime CreatedAt { get; set; }
    }
}
