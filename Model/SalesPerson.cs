using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace SPJ_APP.Model
{
    [Table("sales_persons")]
    public class SalesPerson : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("target_omset")]
        public decimal TargetOmset { get; set; }

        [Column("role")]
        public string? Role { get; set; }

        [Column("password")]
        public string? Password { get; set; }

        [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? UpdatedAt { get; set; }
    }
}