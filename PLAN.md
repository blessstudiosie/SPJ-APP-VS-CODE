# Rencana Pengembangan SPJ App

Dokumen ini melacak semua tugas yang perlu diselesaikan untuk meningkatkan dan menstabilkan aplikasi SPJ App.

## Tahap 1: Stabilitas Inti dan Otomatisasi (Core Stability & Automation)
*Tujuan: Memastikan fondasi aplikasi kuat, data aman, dan proses manual berkurang.*

- [x] **Sinkronisasi Otomatis:** Mengubah proses sinkronisasi agar berjalan secara otomatis di latar belakang (background service) tanpa perlu intervensi manual.
- [x] **Stabilitas Sinkronisasi:** Menganalisis dan memperbaiki akar masalah error sinkronisasi untuk mencegah kerusakan data di masa depan.
  - **Catatan:** Untuk mengatasi inkonsistensi data antara tabel `Delivery` dan `DeliveryDetail`, sebuah fungsi RPC (Remote Procedure Call) Supabase bernama `upsert_delivery_with_details` telah dibuat. Fungsi ini menerima data header dan detail, lalu menyimpannya dalam satu transaksi atomik di sisi server. Ini menggantikan logika sinkronisasi manual yang kompleks di sisi klien dan memastikan integritas data.
- [x] **Fitur Backup Database:** Menambahkan fungsionalitas untuk melakukan backup database lokal (SQLite) dan mencadangkan data penting dari Supabase.
- [x] **Inisialisasi Awal:** Membuat alur untuk penyiapan awal aplikasi, memastikan semua konfigurasi dan data awal siap saat pertama kali dijalankan.

## Tahap 2: Pengembangan Fitur Utama (Major Feature Development)
*Tujuan: Menambah fitur-fitur baru yang paling krusial untuk operasional bisnis.*

- [x] **Purchase Order (PO) Otomatis:**
  - [x] Membuat antarmuka baru bagi user untuk memilih barang dan memasukkan total kuantitas yang diinginkan.
  - [x] Mengimplementasikan logika untuk menghitung dan membagi kuantitas secara proporsional ke setiap barang yang dipilih.
- [x] **Manajemen Produk:**
  - [x] Menambahkan fungsi pencarian di daftar barang.
  - [x] Menambahkan filter berdasarkan kategori barang.
  - [x] Membuat fitur untuk mengekspor daftar harga (price list) ke dalam format PDF.

## Tahap 3: Peningkatan Pengalaman Pengguna (User Experience Enhancement)
*Tujuan: Menyempurnakan interaksi pengguna dengan aplikasi agar lebih cepat dan efisien.*

- [x] **Filter Laporan:** Menambahkan filter berdasarkan rentang tanggal (date range) di semua halaman laporan.
- [x] **Shortcut Keyboard:** Mengimplementasikan shortcut keyboard untuk navigasi dan aksi umum di seluruh aplikasi (misalnya: `Ctrl+N` untuk data baru, `Ctrl+S` untuk menyimpan, `F5` untuk refresh).

## Tahap 4: Finalisasi dan Penjaminan Kualitas (Finalization & QA)
*Tujuan: Memastikan aplikasi benar-benar siap untuk produksi.*

- [x] **Pengujian Komprehensif:** Melakukan pengujian menyeluruh untuk semua fitur baru dan yang sudah ada.
- [x] **Pembersihan Kode:** Merapikan kode dan memastikan tidak ada bug yang tersisa.

## Tahap 5: Fitur Manajemen Data (Data Management Features)
*Tujuan: Memberikan pengguna kemampuan untuk mengimpor, merestore, dan membackup data mereka secara mandiri.*

- [ ] **Review Fitur Backup/Export yang Ada**
  - [ ] **Tugas:** Verifikasi bahwa `BackupLocalDatabaseAsync` dan `ExportLocalDataAsJsonAsync` di `BackupService.cs` berfungsi sesuai harapan.
  - [ ] **Status:** Sudah ada, perlu konfirmasi fungsionalitas.

- [ ] **Implementasi Fitur Restore from Backup**
  - [ ] **Tujuan:** Mengembalikan kondisi data aplikasi dari file backup JSON yang sebelumnya diekspor.
  - [ ] **Langkah-langkah Logika (Service Layer):**
    - [ ] Buat metode baru `RestoreDataFromJsonAsync(string jsonFilePath)` di dalam `Service/BackupService.cs`.
    - [ ] Metode harus membaca file JSON dan melakukan deserialisasi ke dalam model data backup.
    - [ ] Di dalam sebuah transaksi database:
      - [ ] Hapus semua data dari tabel-tabel lokal yang relevan (misalnya: `LocalProduct`, `LocalSale`, dll.).
      - [ ] Masukkan data dari objek hasil deserialisasi ke dalam tabel yang sesuai.
    - [ ] Jika terjadi error, transaksi harus di-rollback untuk mencegah data korup.
  - [ ] **Langkah-langkah Antarmuka (View Layer):**
    - [ ] Tambahkan tombol "Restore dari Backup (JSON)..." di `View/SettingsWindow.xaml`.
    - [ ] Buat event handler di `View/SettingsWindow.xaml.cs` yang:
      - [ ] Menggunakan `OpenFileDialog` untuk meminta pengguna memilih file `.json`.
      - [ ] Memanggil metode `RestoreDataFromJsonAsync`.
      - [ ] Menampilkan notifikasi sukses atau gagal menggunakan `DialogHelper`.
  - [ ] **Status:** Belum dimulai.

- [x] **Implementasi Fitur Impor dari CSV**
  - [x] **Tujuan:** Memungkinkan pengguna mengimpor data massal dari sumber eksternal (misalnya: data dari sistem lama) ke dalam aplikasi.
  - [x] **Pendekatan:** Impor berbasis template. Pengguna harus menyediakan file CSV dengan header kolom yang cocok dengan properti model data aplikasi.
  - [x] **Sub-tahap: Impor Pelanggan (Customers)**
    - [x] Buat `Service/ImportService.cs` dengan metode `ImportCustomersFromCsvAsync`.
    - [x] Buat jendela `View/ImportDataWindow.xaml` untuk antarmuka impor pelanggan.
    - [x] Tambahkan tombol pemicu di `SettingsWindow.xaml`.
  - [x] **Sub-tahap: Impor Produk (Products)**
    - [x] Tambahkan metode `ImportProductsFromCsvAsync` ke `ImportService.cs`.
    - [x] Tentukan format CSV untuk produk (contoh: Id,Name,Stock,Price,Category,IsActive).
    - [x] (Sementara) Adaptasi `ImportDataWindow` khusus untuk Produk, atau buat yang baru jika perlu.
  - [x] **Sub-tahap: Refactor Jendela Impor menjadi Dinamis**
    - [x] Modifikasi `ImportDataWindow.xaml` untuk memiliki dropdown (ComboBox) guna memilih tipe data (Pelanggan, Produk).
    - [x] Logika di `ImportDataWindow.xaml.cs` harus berubah secara dinamis berdasarkan pilihan (menampilkan format CSV yang benar, memanggil metode service yang benar).
  - [x] **Status:** Selesai.

## Tahap 6: Sistem Komunikasi & Otorisasi
*Tujuan: Membangun alur kerja untuk menerima permintaan dari klien mobile (Android) dan mengimplementasikan otorisasi untuk aksi-aksi penting.*

- [ ] **Desain & Implementasi Tabel `change_requests` di Supabase**
    - [ ] **Tugas:** Definisikan dan buat tabel `change_requests` di Supabase untuk bertindak sebagai antrian pesan.
    - [ ] **Kolom Penting:** `id`, `created_at`, `requested_by`, `request_type` (misal: 'CREATE_NOTA'), `payload` (JSON), `status` ('SO', 'APPROVED', 'REJECTED'), `processed_at`, `processed_by`.
    - [ ] **Status:** Belum dimulai.

- [ ] **Buat Halaman "Inbox Permintaan"**
    - [ ] **Tugas:** Buat halaman/view baru di aplikasi desktop untuk menampilkan daftar permintaan yang masuk dengan status `SO`.
    - [ ] **Fitur:** Harus bisa refresh secara periodik atau manual untuk mengambil data baru dari tabel `change_requests`.
    - [ ] **Status:** Belum dimulai.

- [ ] **Buat Halaman Detail & Konfirmasi Permintaan**
    - [ ] **Tugas:** Buat UI yang dapat menampilkan detail permintaan (misalnya, item-item dalam nota) saat satu permintaan di-klik dari Inbox.
    - [ ] **Fitur Konfirmasi Sebagian (Partial Approval):**
        - [ ] User desktop harus bisa mengedit kuantitas atau menghapus item dari permintaan nota sebelum menyetujui.
    - [ ] **Logika Persetujuan:** Tombol "Setujui" akan mengambil data yang sudah divalidasi/diedit dan menyimpannya ke database **lokal**. Status di `change_requests` diperbarui menjadi `APPROVED`.
    - [ ] **Status:** Belum dimulai.

- [ ] **Implementasi Otorisasi Perubahan Status**
    - [ ] **Tugas:** Buat mekanisme yang meminta password saat status Nota diubah menjadi `TEMPO` atau `DONE`.
    - [ ] **Alur:**
        - [ ] Tampilkan dialog popup yang meminta password.
        - [ ] Verifikasi password tersebut dengan password milik user yang memiliki role 'Manager' atau 'Owner' di tabel `sales_persons`.
    - [ ] **Status:** Belum dimulai.

## Tahap 7: Sistem Login & Audit Trail
*Tujuan: Mengetahui siapa yang menggunakan aplikasi, dan membuka jalan untuk otorisasi berbasis role (terkait Tahap 6).*

- [ ] **Kolom Password di `sales_persons`**
  - [ ] Tambah kolom `password` (TEXT) di Supabase (production & dev) dan di `Model/SalesPerson.cs` + `Model/LocalSalesPerson.cs`.
  - [ ] **Utang teknis - PENTING:** password saat ini direncanakan disimpan plain text untuk mempercepat rilis awal. WAJIB diganti ke hashing (BCrypt atau setara) sebelum aplikasi ini dipakai oleh banyak user sungguhan. Jangan tutup task ini sebagai "selesai" sampai hashing diterapkan.

- [ ] **Halaman Login**
  - [ ] Buat `View/LoginWindow.xaml` + `.xaml.cs`: pilih nama dari `sales_persons`, masukkan password, validasi terhadap data lokal.
  - [ ] **PERIKSA DULU** alur startup yang sudah ada di `AppInitializationService` dan `MainWindow.xaml` (LoadingOverlay) sebelum mengubah `App.xaml.cs` - pastikan Login terintegrasi dengan urutan yang benar (Login → Initialization/Sync awal → MainWindow), bukan saling menimpa.

- [ ] **Session User Aktif**
  - [ ] Buat `Service/CurrentUserService.cs` (singleton in-memory) untuk menyimpan siapa yang sedang login di sesi berjalan.

- [ ] **Audit Trail via `activity_logs`**
  - [ ] Buat `Model/ActivityLog.cs` (Supabase) + `Model/LocalActivityLog.cs` (lokal), daftarkan tabel lokal di `LocalDatabaseService.cs`.
  - [ ] Buat `Service/ActivityLogService.cs` dengan method `LogAsync(action, details)` yang otomatis mengambil nama user dari `CurrentUserService`.
  - [ ] Tambahkan `SyncActivityLogsAsync` di `SyncService.cs`, masukkan ke `SyncAllAsync`.
  - [ ] Panggil `ActivityLogService.LogAsync(...)` minimal di titik-titik penting: login, cetak nota, catat pembayaran, selesaikan pengiriman.

- [ ] **Sambungkan ke Tahap 6 - Otorisasi Perubahan Status**
  - [ ] Task "Implementasi Otorisasi Perubahan Status" di Tahap 6 sekarang **tidak lagi terblokir** - password per-user sudah tersedia. Gunakan `sales_persons.password` + `role` (cek role = 'MANAGER' atau 'OWNER') untuk validasi saat status nota diubah ke `TEMPO`/`DONE`.

## Tahap 8: Migrasi Data Production → Development
*Tujuan: Memindahkan data asli dari Supabase production ke Supabase development yang sudah diperbaiki strukturnya (UUID-based).*

- [ ] **Setup `postgres_fdw`** di project development untuk baca langsung dari production (read-only, aman).
- [ ] **Migrasi Master Data** (products, sales_persons, customers) - idempotent via `ON CONFLICT DO UPDATE`, aman dijalankan berkali-kali untuk testing.
- [ ] **Migrasi Transaksi** (sales, sales_details, payments) - idempotent per nota.
- [ ] **Migrasi Data Historis** (returns, stock_opname, barang_masuk, check_in_logs, activity_logs) - **catatan:** bagian `return_details`, `stock_opname_details`, `barang_masuk_details` butuh strategi mapping ID lama→baru yang lebih hati-hati (belum diselesaikan skripnya, lanjutkan kalau data historis ini dianggap penting untuk dibawa).
- [ ] Setelah migrasi final disepakati, `DROP SERVER production_server CASCADE;` untuk keamanan (tutup akses).
## Catatan Ruang Lingkup (Scope Decision) - 6 Agustus 2026
Tabel/fitur berikut **DIKELUARKAN dari ruang lingkup saat ini** - tidak perlu dimigrasikan, dikembangkan, atau dipelihara sampai ada keputusan untuk mengaktifkannya kembali:
- `returns` & `return_details`
- `barang_masuk` & `barang_masuk_details`
- `stock_opname_details` (detail per-item dari sesi stock opname)

## Tahap 8: Migrasi Data Production → Development (REVISI - scope dipersempit)
*Tujuan: Memindahkan data asli dari Supabase production ke Supabase development.*

- [ ] **Setup `postgres_fdw`** di project development untuk baca langsung dari production (read-only, aman).
- [ ] **Migrasi Master Data**: products, sales_persons, customers - idempotent via `ON CONFLICT DO UPDATE`.
- [ ] **Migrasi Transaksi**: sales, sales_details, payments - idempotent per nota.
- [ ] ~~Migrasi returns, barang_masuk, stock_opname_details~~ - **DI LUAR SCOPE saat ini**, lihat Catatan Ruang Lingkup di atas.
- [ ] Setelah migrasi final disepakati, `DROP SERVER production_server CASCADE;` untuk keamanan.

## Tahap 9: Developer Tools
- [ ] **Database Inspector Page**: alat admin untuk lihat/edit/hapus data lokal (SQLite) langsung lewat UI, tabel dipilih dari dropdown (Produk, Customer, Sales Person, Nota, Item Nota, Pembayaran, Pengiriman, Item Pengiriman, Purchase Order, Activity Log - TIDAK termasuk returns/barang_masuk/stock_opname_details sesuai scope saat ini). Wajib ada peringatan tegas bahwa alat ini melewati validasi bisnis aplikasi (stok, total nota, dll tidak otomatis disesuaikan). Akses lewat menu terpisah "Developer" dengan konfirmasi sebelum masuk.
- [ ] **Status:** Belum dimulai.