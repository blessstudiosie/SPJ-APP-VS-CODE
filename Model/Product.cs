using Postgrest.Attributes;
using Postgrest.Models;

namespace SPJ_APP.Model
{
    [Table("products")]
    public class Product : BaseModel
    {
        [PrimaryKey("name", false)]
        public string Name { get; set; } = string.Empty;

        [Column("stok_ready")]
        public decimal StokReady { get; set; }

        [Column("stok_fisik")]
        public decimal StokFisik { get; set; }

        [Column("harga_r")]
        public decimal HargaR { get; set; }

        [Column("harga_sg")]
        public decimal HargaSg { get; set; }

        [Column("harga_g")]
        public decimal HargaG { get; set; }

        [Column("harga_p")]
        public decimal HargaP { get; set; }

        [Column("kategori")]
        public string? Kategori { get; set; }

        [Column("satuan")]
        public string? Satuan { get; set; }

        [Column("satuan_besar")]
        public string? SatuanBesar { get; set; }

        [Column("qty_ratio")]
        public decimal QtyRatio { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime? UpdatedAt { get; set; }
    }
}