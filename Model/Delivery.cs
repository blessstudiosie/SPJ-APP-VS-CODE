using Postgrest.Attributes;
using Postgrest.Models;

namespace SPJ_APP.Model
{
    [Table("deliveries")]
    public class Delivery : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("delivery_number")]
        public string DeliveryNumber { get; set; } = string.Empty;

        [Column("driver_id")]
        public Guid? DriverId { get; set; }

        [Column("helper_id")]
        public Guid? HelperId { get; set; }

        [Column("checker_id")]
        public Guid? CheckerId { get; set; }

        [Column("status")]
        public string Status { get; set; } = "OPEN";

        [Column("closed_at")]
        public DateTime? ClosedAt { get; set; }

        [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? UpdatedAt { get; set; }
    }
}