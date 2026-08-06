# SPJ App Project Guidelines and Memory

This document serves as a persistent memory and guideline for development within the SPJ App project. It covers architectural decisions, implemented features, and conventions to ensure consistency and maintainability.

## Project Overview
The SPJ App is a WPF desktop application built with C#. It utilizes a local SQLite database for a local-first approach and synchronizes data with a Supabase backend.

## Key Architectural Decisions & Patterns

### 1. Data Synchronization Strategy
- **Local-First Approach:** Data is primarily managed and persisted locally in SQLite, providing offline capabilities and faster responsiveness. Changes are then synchronized with the remote Supabase database.
- **SyncService (`Service/SyncService.cs`):** Contains the core business logic for pushing local changes to Supabase and pulling remote changes. It handles various data models (Products, Sales, Customers, Deliveries, etc.).
- **BackgroundSyncService (`Service/BackgroundSyncService.cs`):** Implemented as a **singleton**. It manages an automatic background synchronization process, triggering `SyncService.SyncAllAsync()` periodically. Its lifecycle is managed by `App.xaml.cs`.
- **Supabase RPC for Data Integrity:** For complex, transactional server-side operations (like upserting a header and its related details atomically), Supabase RPCs (Remote Procedure Calls/Stored Procedures) are preferred.
  - **Example:** `upsert_delivery_with_details` RPC is used for syncing `Delivery` and `DeliveryDetail` to prevent inconsistencies and race conditions. This offloads complex transactional logic to the database.

### 2. Database Management
- **Local Database Service (`Service/LocalDatabaseService.cs`):** Responsible for managing the local SQLite database connection and ensuring table creation on startup.
- **Supabase Service (`Service/SupabaseService.cs`):** Provides a singleton `Supabase.Client` instance for interacting with the Supabase backend.

### 3. User Interface (WPF)
- **Navigation:** The main content is managed by a `ContentControl` named `AreaKonten` in `MainWindow.xaml`. Navigation between views typically involves setting `AreaKonten.Content` to a new instance of a `Page`.
- **Thread Safety:** All UI updates from background threads must use `Dispatcher.Invoke` to ensure thread safety.

### 4. Error Handling
- **Global Exception Handler:** `App.xaml.cs` includes a `DispatcherUnhandledException` handler to catch and display unhandled exceptions gracefully.
- **Specific Error Handling:** Feature-specific error handling often involves `try-catch` blocks with `MessageBox.Show` via `DialogHelper`.

## Phase 1: Implemented Features Summary

### 1. Automatic Background Sync
- **Implementation:** `BackgroundSyncService` was refactored into a singleton and started in `App.xaml.cs`. `MainWindow.xaml.cs` now subscribes to its status updates without managing its lifecycle.
- **Impact:** Reduces manual intervention, keeps local data fresher.

### 2. Sync Stability Enhancements
- **Focus Area:** Primarily `SyncDeliveriesAsync` in `SyncService.cs`.
- **Problem Addressed:** Race conditions and data inconsistencies during `Delivery` and `DeliveryDetail` synchronization.
- **Solution:** Replaced complex client-side logic with a call to a Supabase RPC named `upsert_delivery_with_details`. This function performs an atomic upsert of the delivery header and its details on the server.
- **Action Required (DBA):** Ensure the `upsert_delivery_with_details` function is correctly deployed and maintained on the Supabase instance.

### 3. Database Backup Feature
- **BackupService (`Service/BackupService.cs`):** Refactored to accept user-specified destination paths.
  - `BackupLocalDatabaseAsync(string destinationPath)`: Copies the local SQLite database file.
  - `ExportLocalDataAsJsonAsync(string destinationPath)`: Exports all local data models to a single JSON file (snapshot of local state).
- **UI Integration:** Added "Backup Database Lokal..." and "Ekspor Data Lokal (JSON)..." buttons to `View/SettingsWindow.xaml` with corresponding logic in `View/SettingsWindow.xaml.cs`.

### 4. Initial Application Setup
- **AppInitializationService (`Service/AppInitializationService.cs`):** Enhanced to manage the first-run initialization process.
  - Performs local DB verification, default data seeding, and an initial *blocking* data sync from Supabase.
  - Reports progress via `InitializationProgressChanged` event.
- **UI Integration:** `MainWindow.xaml` was updated with a full-screen `LoadingOverlay`, and `MainWindow.xaml.cs` manages showing/hiding this overlay, subscribing to progress updates, and calling the initialization service. Provides user feedback during potentially long startup tasks.

## Conventions for Future Development
- **ID Management:** Always use `Guid.TryParse` for string-to-GUID conversions to prevent `InvalidOperationException`.
- **Atomic Operations:** For operations involving multiple related database records, especially across a network (to Supabase), prioritize server-side atomic operations (e.g., Supabase RPCs) over multiple client-side API calls to maintain data integrity.
- **New Features & Sync:** When implementing new data-related features, always consider how they will be integrated into the existing local-first and synchronization mechanisms. Design local models first, then remote, and ensure `SyncService` can handle them.
- **UI Responsiveness:** For long-running operations (like sync or complex calculations), ensure the UI remains responsive by running tasks asynchronously and updating the UI correctly using `Dispatcher.Invoke`.
- **Code Organization:** Follow existing `Service` and `View/Pages` directory structures.

### Task Management
- **PLAN.md:** Refer to `PLAN.md` at the project root for a detailed breakdown of current tasks, their stages, and completion status. This document will be used to track progress and identify unfinished work.

## Prinsip Arsitektur Fundamental (Core Architectural Principles)

### 1. Aplikasi Desktop sebagai "Single Source of Truth"
Aplikasi desktop ini dianggap sebagai pemegang data utama dan sumber kebenaran. Semua data yang valid dan final berasal dari aplikasi ini. Database lokal di aplikasi ini adalah master.

### 2. Interaksi Klien Mobile via "Change Requests"
Klien mobile (misalnya, aplikasi Android untuk sales di lapangan) tidak memodifikasi data di database utama secara langsung. Sebaliknya, mereka mengirimkan "permintaan perubahan" (change requests) ke sebuah sistem antrian pesan. Aplikasi desktop kemudian akan memproses permintaan ini (menyetujui, menolak, atau memodifikasi) dan mengaplikasikannya ke database master lokal.

## Aturan Bisnis & Konvensi (Business Rules & Conventions)

### 1. Status Nota (Invoice Statuses)
Status yang valid dan telah ditetapkan untuk sebuah Nota (penjualan/invoice) adalah sebagai berikut, dan tidak boleh ditambahkan status lain di luar daftar ini:
- `SO` (Sales Order): Permintaan penjualan awal dari klien mobile, belum dikonfirmasi.
- `ON PROSES`: Pesanan sedang disiapkan.
- `DALAM PENGIRIMAN`: Pesanan sedang dalam perjalanan ke pelanggan.
- `TEMPO`: Pengiriman selesai, pembayaran belum lunas (jatuh tempo).
- `DONE`: Pengiriman selesai dan pembayaran sudah lunas.

---
*Last updated by Gemini on 2026-08-04*
