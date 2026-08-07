# 📖 GEMINI.md - Panduan Fundamental & Arsitektur Proyek SPJ APP

Dokumen ini berisi standar fundamental, aturan bisnis, serta panduan arsitektur untuk pengembangan aplikasi **CV. SARANA PRIMA JAYA (SPJ APP)** pada platform **Desktop (C# WPF)** dan **Android (Kotlin Jetpack Compose)**.

---

## 🌐 1. ATURAN ARSITEKTUR KUNCI & DATA MODEL (GLOBAL)

### A. Rumus Pengakuan Omset Penjualan (Business Revenue Rule)
1. **Acuan Tanggal**: Tanggal pengakuan omset **WAJIB menggunakan Tanggal Kirim (`DeliveryDate`)**, bukan tanggal input order (`OrderDate`). Hal ini karena omset baru diakui secara sah saat barang benar-benar dikirim/diterima pelanggan.
2. **Kualifikasi Status Omset**: HANYA transaksi berstatus **`TEMPO`** (dikirim dengan sisa tagihan) atau **`DONE`** (dikirim & lunas) yang dihitung sebagai Omset Sah.
3. **Status Non-Omset**: Status **`SO`** (Draft Order), **`ON PROSES`** (Gudang/Cetak Nota), dan **`DALAM PENGIRIMAN`** (Di Jalan) adalah pipa alur kerja (*pipeline*) dan **TIDAK BOLEH** dihitung sebagai omset.

### B. Resolusi Nama vs ID pada Tampilan (UI Display Resolution Rule)
1. **Penyimpanan Database**: Di database SQLite lokal dan PostgreSQL Supabase remote, referensi entitas menggunakan ID/GUID (`CustomerId`, `SalesPersonId`).
2. **Aturan Tampilan UI**: Di seluruh tabel DataGrid, Form, List, Card, Dialog Desktop, dan Layar Android, **ID/GUID TIDAK BOLEH ditampilkan mentah ke user**. Sistem HARUS menyilangkan ID tersebut dengan tabel master (`customers` dan `sales_persons`) untuk menampilkan **Nama Customer** dan **Nama Sales Person**.

---

## 💻 2. ATURAN FUNDAMENTAL APLIKASI DESKTOP (C# WPF - `SPJ APP`)

1. **Ukuran & Peluncuran Jendela**:
   - Aplikasi Desktop HARUS selalu diluncurkan dalam kondisi ter-maximize (`WindowState = WindowState.Maximized`).
2. **Navigasi & Highlighting Menu Utama**:
   - Menu navigasi utama di bagian atas HARUS secara visual menonjolkan menu yang sedang aktif saat ini dengan latar **Executive Indigo (`#4F46E5`)**, teks **Putih Tebal (`FontWeights.Bold`)**.
   - **Anti-Re-Click**: Jika pengguna mengeklik menu untuk halaman yang sedang aktif/terbuka di layar, sistem HARUS mengabaikan klik tersebut agar tidak melakukan reload/re-query berulang.
3. **Laporan Kinerja Beranda (`HomePage.xaml`)**:
   - Beranda harus menampilkan Laporan Kinerja Operasional Bulan Berjalan yang menyajikan 4 Kartu KPI (Total Omset, Omset Lunas, Omset Tempo, Dalam Pengiriman), Tabel Breakdown Status Nota, dan Tabel Kinerja per Sales Person (Omset & Jumlah Kunjungan).
4. **Keamanan Sesi & Log Out (`CurrentUserService` & `BackgroundSyncService`)**:
   - Tombol **Log Out** berwarna merah harus selalu tersedia di header kanan atas dan menu `⚙️ Pengaturan`.
   - Saat Log Out: matikan timer sync background, hapus sesi user, dan buka kembali `LoginWindow` secara bersih.

---

## 📱 3. ATURAN FUNDAMENTAL APLIKASI MOBILE ANDROID (Kotlin Jetpack Compose - `SPJ-APP-APK-AS`)

1. **Format Tampilan & Resolusi Nama**:
   - Layar `SalesLedgerScreen.kt` (Daftar Penjualan) dan `VisitsScreen.kt` (Log Kunjungan) HARUS menampilkan **Nama Customer** (`customerName`), bukan ID GUID (`customerId`).
2. **Pengiriman Order & Check-in Mobile**:
   - Penjualan yang di-input sales di mobile masuk dengan status awal **`SO`** (Draft Sales Order) dan otomatis masuk ke antrean inbox Desktop (`InboxSalesOrderPage`).
   - Check-in kunjungan sales merekam koordinat GPS (`latitude`, `longitude`) dan foto lokasi, disimpan ke `visit_logs_queue`.

---

## 🔄 4. ATURAN SINKRONISASI DATA, PAGINATION & SUPABASE API

1. **Batas Limit PostgREST 1.000 Data (Batch Pagination)**:
   - Supabase / PostgREST memiliki batas maksimum (ceiling) 1.000 record per query default.
   - Semua fungsi penarikan data penuh (`FetchAllFromSupabaseAsync<T>()`) WAJIB menggunakan query looping `.Range(offset, offset + limit - 1)` sampai seluruh data benar-benar ditarik tanpa ada data terpotong.
2. **Pesan Informasi Progress Sync Real-Time**:
   - Setiap sinkronisasi (baik sync background 5 menit sekali, sync manual menu, atau tarik data penuh di pengaturan) HARUS memancarkan pesan event progress real-time (`SyncService.OnSyncProgress`) agar pengguna tahu entitas data dan halaman batch mana yang sedang diunduh.
3. **Penyembunyian Indikator Setelah Sync**:
   - Indikator progress bar di sudut kanan bawah status bar HARUS disembunyikan (`Visibility.Collapsed`) begitu sync selesai, dan menampilkan pesan penutup centang hijau `✅ Sync selesai` selama 5 detik.

---

## 🛡️ 5. HAK AKSES ROLE & OTORISASI (ROLE PERMISSIONS)

1. **Pengeditan & Penghapusan Master Data Sales**:
   - Opsi untuk merubah (`Edit`) atau menghapus (`Hapus`) Master Data SalesPerson HANYA diizinkan dan ditampilkan untuk pengguna dengan role **`MANAGER`**, **`OWNER`**, **`DEVELOPER`**, dan **`ADMIN`**.
   - Pengguna dengan role **`SALES`** biasa dilarang menghapus atau mengubah data sales person lain.
