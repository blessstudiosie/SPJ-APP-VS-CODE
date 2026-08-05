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
