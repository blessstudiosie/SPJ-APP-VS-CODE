using SQLite;

namespace SPJ_APP.Model
{
    [Table("delivery_details")]
    public class LocalDeliveryDetail
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string DeliveryId { get; set; } = string.Empty;
        public string SaleId { get; set; } = string.Empty;
    }
}