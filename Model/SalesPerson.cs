using Postgrest.Attributes;
using Postgrest.Models;

namespace SPJ_APP.Model
{
    [Table("sales_persons")]
    public class SalesPerson : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

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

        [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
public DateTime? CreatedAt { get; set; }

[Column("updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
public DateTime? UpdatedAt { get; set; }

// TODO: Hash password before production use. Currently plain text for initial release.
[Column("password")]
public string? Password { get; set; }

    }
}