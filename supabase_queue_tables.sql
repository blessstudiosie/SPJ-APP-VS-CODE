-- =========================================================
-- SQL Migration Script untuk Tabel Antrian Mobile di Supabase
-- Ekosistem SPJ APP (Desktop C# & Mobile Android Kotlin)
-- =========================================================

-- 1. Tabel Antrian Sales Order (Pesan Penjualan dari Mobile)
CREATE TABLE IF NOT EXISTS public.sales_orders_queue (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sales_person_id UUID NULL,
    customer_id UUID NULL,
    customer_name TEXT NOT NULL DEFAULT '',
    items_json JSONB NOT NULL DEFAULT '[]'::jsonb,
    total_amount NUMERIC(15,2) NOT NULL DEFAULT 0.00,
    status TEXT NOT NULL DEFAULT 'PENDING', -- PENDING, APPROVED, REJECTED
    notes TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NULL
);

-- 2. Tabel Antrian Log Kunjungan Sales (Visit Logs dari Mobile)
CREATE TABLE IF NOT EXISTS public.visit_logs_queue (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sales_person_id UUID NULL,
    sales_person_name TEXT NOT NULL DEFAULT '',
    customer_id UUID NULL,
    customer_name TEXT NOT NULL DEFAULT '',
    is_new_customer BOOLEAN NOT NULL DEFAULT false,
    latitude DOUBLE PRECISION NOT NULL DEFAULT 0,
    longitude DOUBLE PRECISION NOT NULL DEFAULT 0,
    photo_url TEXT NULL,
    notes TEXT NULL,
    status TEXT NOT NULL DEFAULT 'PENDING', -- PENDING, APPROVED, REJECTED
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NULL
);

-- Indexing untuk performa pencarian antrian berstatus PENDING
CREATE INDEX IF NOT EXISTS idx_so_queue_status ON public.sales_orders_queue(status);
CREATE INDEX IF NOT EXISTS idx_visit_queue_status ON public.visit_logs_queue(status);
