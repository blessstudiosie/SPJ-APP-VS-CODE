using SQLite;

namespace SPJ_APP.Model
{
    [Table("products")]
    public class LocalProduct
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public decimal StokReady { get; set; }
        public decimal StokFisik { get; set; }
        public decimal HargaR { get; set; }
        public decimal HargaSg { get; set; }
        public decimal HargaG { get; set; }
        public decimal HargaP { get; set; }
        public string? Kategori { get; set; }
        public string? Satuan { get; set; }
        public string? SatuanBesar { get; set; }
        public decimal QtyRatio { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsSynced { get; set; } = true;

        // Menyimpan updated_at dari server saat terakhir kali produk ini di-pull.
        // Dipakai untuk deteksi konflik: kalau server berubah setelah ini, jangan ditimpa.
        public DateTime? LastSyncedUpdatedAt { get; set; }
    }
}