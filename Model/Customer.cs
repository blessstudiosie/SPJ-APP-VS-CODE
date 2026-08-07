using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace SPJ_APP.Model
{
    [Table("customers")]
    public class Customer : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("owner_name")]
        public string? OwnerName { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("jalur_pengiriman")]
        public string? JalurPengiriman { get; set; }

        [Column("latitude")]
        public double? Latitude { get; set; }

        [Column("longitude")]
        public double? Longitude { get; set; }

        [Column("sales_person_id")]
        public string? SalesPersonId { get; set; }

        [Column("limit_piutang")]
        public decimal LimitPiutang { get; set; }

        [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? UpdatedAt { get; set; }
    }
}