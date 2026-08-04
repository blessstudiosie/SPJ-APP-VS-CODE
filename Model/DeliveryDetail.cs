using Postgrest.Attributes;
using Postgrest.Models;

namespace SPJ_APP.Model
{
    [Table("delivery_details")]
    public class DeliveryDetail : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("delivery_id")]
        public Guid DeliveryId { get; set; }

        [Column("sale_id")]
        public Guid SaleId { get; set; }

        [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? CreatedAt { get; set; }
    }
}