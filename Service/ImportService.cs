using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    public class ImportResult
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount => TotalRows - SuccessCount;
        public List<string> Errors { get; } = new List<string>();
    }

    public static class ImportService
    {
        /// <summary>
        /// Imports customer data from a CSV file.
        /// Expected CSV Header/Format: Id,Name,Phone,Address
        /// </summary>
        /// <param name="filePath">Path to the CSV file.</param>
        /// <returns>An ImportResult object summarizing the outcome.</returns>
        public static async Task<ImportResult> ImportCustomersFromCsvAsync(string filePath)
        {
            var result = new ImportResult();
            var customersToInsert = new List<LocalCustomer>();

            var lines = await File.ReadAllLinesAsync(filePath);
            result.TotalRows = lines.Length - 1; // Subtract header row

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                var columns = line.Split(',');

                if (columns.Length != 4)
                {
                    result.Errors.Add($"Baris {i + 1}: Jumlah kolom tidak valid. Diharapkan 4, ditemukan {columns.Length}.");
                    continue;
                }

                try
                {
                    // Validate that the ID is a valid GUID, but keep it as a string.
                    if (!Guid.TryParse(columns[0], out _))
                    {
                        result.Errors.Add($"Baris {i + 1}: Format Id tidak valid '{columns[0]}'.");
                        continue;
                    }

                    var customer = new LocalCustomer
                    {
                        Id = columns[0],
                        Name = columns[1],
                        Phone = columns[2],
                        Address = columns[3],
                        UpdatedAt = DateTime.Now,
                        IsSynced = false
                    };
                    customersToInsert.Add(customer);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Baris {i + 1}: Terjadi error - {ex.Message}");
                }
            }

            result.SuccessCount = customersToInsert.Count;

            if (customersToInsert.Any())
            {
                var localDb = await LocalDatabaseService.GetConnection();
                await localDb.RunInTransactionAsync(db =>
                {
                    db.InsertAll(customersToInsert);
                });
            }

            return result;
        }

        /// <summary>
        /// Imports product data from a CSV file.
        /// Expected CSV Header/Format: Id,Name,StokFisik,HargaR,Kategori,Status
        /// </summary>
        /// <param name="filePath">Path to the CSV file.</param>
        /// <returns>An ImportResult object summarizing the outcome.</returns>
        public static async Task<ImportResult> ImportProductsFromCsvAsync(string filePath)
        {
            var result = new ImportResult();
            var productsToInsert = new List<LocalProduct>();

            var lines = await File.ReadAllLinesAsync(filePath);
            result.TotalRows = lines.Length - 1; // Subtract header row

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                var columns = line.Split(',');

                if (columns.Length != 6)
                {
                    result.Errors.Add($"Baris {i + 1}: Jumlah kolom tidak valid. Diharapkan 6, ditemukan {columns.Length}.");
                    continue;
                }

                try
                {
                    if (!Guid.TryParse(columns[0], out var id))
                    {
                        result.Errors.Add($"Baris {i + 1}: Format Id tidak valid '{columns[0]}'.");
                        continue;
                    }

                    if (!decimal.TryParse(columns[2], out var stok))
                    {
                        result.Errors.Add($"Baris {i + 1}: Format StokFisik tidak valid '{columns[2]}'.");
                        continue;
                    }

                    if (!decimal.TryParse(columns[3], out var harga))
                    {
                        result.Errors.Add($"Baris {i + 1}: Format HargaR tidak valid '{columns[3]}'.");
                        continue;
                    }

                    var product = new LocalProduct
                    {
                        Id = id.ToString(),
                        Name = columns[1],
                        StokFisik = stok,
                        StokReady = stok, // Asumsikan stok ready = stok fisik saat impor
                        HargaR = harga,
                        Kategori = columns[4],
                        Status = columns[5],
                        UpdatedAt = DateTime.Now,
                        IsSynced = false // Tandai sebagai belum disinkronisasi
                    };
                    productsToInsert.Add(product);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Baris {i + 1}: Terjadi error - {ex.Message}");
                }
            }

            result.SuccessCount = productsToInsert.Count;

            if (productsToInsert.Any())
            {
                var localDb = await LocalDatabaseService.GetConnection();
                await localDb.RunInTransactionAsync(db =>
                {
                    db.InsertAll(productsToInsert);
                });
            }

            return result;
        }
    }
}
