using SQLite;
using System;

namespace SPJ_APP.Model
{
    public class LocalActivityLog
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsSynced { get; set; }
    }
}
