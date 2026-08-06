# Panduan Proyek & Arsitektur - Aplikasi Android Sales SPJ

Dokumen ini adalah panduan teknis dan arsitektur untuk pengembangan aplikasi Android Sales SPJ. Tujuannya adalah untuk memastikan aplikasi mobile berfungsi sebagai klien yang efektif dalam ekosistem SPJ, di mana aplikasi desktop (WPF) bertindak sebagai *single source of truth*.

## Prinsip Arsitektur Fundamental

1.  **Aplikasi Desktop adalah Master Data:** Aplikasi desktop adalah pemegang data utama dan satu-satunya sistem yang boleh memodifikasi data final (misalnya, mengubah status pesanan, memverifikasi pembayaran).
2.  **Aplikasi Android Mengirim "Change Requests":** Aplikasi Android **TIDAK** memodifikasi tabel data utama (seperti `sales`, `deliveries`, `payments`) secara langsung. Sebaliknya, ia mengirimkan "permintaan" untuk membuat data baru, yang kemudian akan diproses oleh aplikasi desktop.
3.  **Interaksi via Antrian (Queue):** Mekanisme komunikasi utama dari Android ke backend adalah dengan memasukkan data ke dalam sebuah "tabel antrian" di Supabase. Aplikasi desktop akan memantau tabel ini, memvalidasi, dan memproses permintaan tersebut.

## Alur Kerja Utama: Membuat Sales Order (SO)

Ini adalah alur kerja utama dan satu-satunya untuk aplikasi Android pada fase ini.

1.  **Login Salesperson:**
    - Aplikasi启动时，会显示一个登录界面。
    - Salesperson memasukkan username/ID dan password.
    - Aplikasi melakukan verifikasi kredensial terhadap tabel `sales_persons` di Supabase.
    - **Catatan Keamanan:** Saat ini password disimpan sebagai plain text di kolom `password`. **Ini adalah utang teknis.** Aplikasi Android harus dirancang untuk siap beralih ke sistem password yang di-hash.

2.  **Mengambil Data Master (Read-Only):**
    - Setelah login, aplikasi harus mengunduh dan menyimpan cache data master dari Supabase secara periodik (atau saat startup).
    - Data yang perlu diambil:
        - `products` (untuk daftar produk dan harga)
        - `customers` (untuk daftar pelanggan)
    - Data ini bersifat **read-only** di aplikasi Android.

3.  **Membuat Sales Order Baru:**
    - Salesperson membuat nota penjualan baru.
    - Aplikasi harus secara otomatis memberikan status `SO` (Sales Order) pada nota ini. Aplikasi Android **TIDAK BOLEH** menggunakan status lain.
    - Salesperson memilih pelanggan dari daftar yang sudah di-cache.
    - Salesperson menambahkan item produk dari daftar yang sudah di-cache.

4.  **Mengirim "Change Request":**
    - Setelah selesai, tombol "Simpan" atau "Kirim" akan mem-paketkan data Sales Order.
    - Data ini **TIDAK** dimasukkan ke tabel `sales` dan `sales_details`.
    - Sebaliknya, aplikasi akan membuat satu baris data baru di tabel antrian bernama **`sales_orders_queue`**.

## Desain Tabel Antrian: `sales_orders_queue`

Aplikasi Android perlu berinteraksi dengan tabel ini. Pastikan tabel ini ada di Supabase.

**Tujuan:** Menampung permintaan Sales Order baru dari semua klien mobile.

**Struktur Kolom yang Direkomendasikan:**

| Nama Kolom        | Tipe Data         | Deskripsi                                                                    | Contoh                                                  |
| ----------------- | ----------------- | ---------------------------------------------------------------------------- | ------------------------------------------------------- |
| `id`              | `uuid` (Primary)  | ID unik untuk setiap permintaan.                                             | `uuid_generate_v4()`                                    |
| `created_at`      | `timestamptz`     | Waktu saat permintaan dibuat.                                                | `now()`                                                 |
| `sales_person_id` | `uuid` (FK)       | ID dari `sales_persons` yang membuat SO. Diambil dari user yang sedang login.  | `uuid-dari-sales-person`                                |
| `customer_id`     | `uuid` (FK)       | ID dari `customers` yang menjadi tujuan SO.                                  | `uuid-dari-customer`                                    |
| `order_date`      | `date`            | Tanggal pemesanan.                                                           | `2026-08-06`                                            |
| `notes`           | `text`            | Catatan tambahan untuk pesanan.                                              | `"Tolong kirim sebelum jam 5 sore"`                       |
| `details`         | `jsonb`           | Array JSON yang berisi detail semua item produk dalam pesanan.               | `[{"product_id": "...", "quantity": 10, "price": 5000}]` |
| `status`          | `text`            | Status permintaan dalam antrian. Default-nya `pending`.                      | `pending`                                               |
| `error_message`   | `text`            | Jika desktop gagal memproses, pesan error bisa disimpan di sini untuk debug. | `null`                                                  |

**Alur Data:**
1.  **Android App:** `INSERT` ke `sales_orders_queue` dengan `status = 'pending'`.
2.  **Desktop App:** Secara periodik `SELECT` dari `sales_orders_queue` di mana `status = 'pending'`.
3.  **Desktop App:** Untuk setiap baris, ia akan:
    - Memvalidasi data.
    - Membuat record baru di tabel `sales` dan `sales_details`.
    - Mengubah `status` di `sales_orders_queue` menjadi `processed` atau `error`.

## Ringkasan Tugas untuk Tim Android

- **Fitur:**
    1.  Implementasikan layar login untuk `sales_persons`.
    2.  Buat mekanisme caching/sinkronisasi data read-only untuk `products` dan `customers`.
    3.  Bangun UI untuk membuat Sales Order (pilih pelanggan, tambah produk, set kuantitas).
    4.  Saat menyimpan, kirim data sebagai baris baru ke tabel `sales_orders_queue` di Supabase.
- **Arsitektur:**
    - Gunakan arsitektur yang umum di Android (misalnya, MVVM dengan Repository Pattern).
    - Repository harus mengarah ke Supabase client untuk mengambil data master dan mengirim `Change Request`.
- **Konvensi:**
    - Selalu gunakan status `SO` untuk nota baru.
    - Pastikan semua ID (`product_id`, `customer_id`, `sales_person_id`) yang dikirim adalah UUID yang valid dari data master.
