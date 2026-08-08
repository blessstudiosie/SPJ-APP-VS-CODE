using System.Globalization;
using SPJ_APP.Model;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SPJ_APP.Service
{
    public sealed class SalesPerformanceRow
    {
        public string SalesPersonId { get; init; } = string.Empty;
        public string SalesPersonName { get; init; } = "-";
        public decimal Omset { get; init; }
        public int NotaCount { get; init; }
        public int NotaMoreThan14Days { get; init; }
    }

    public sealed class SalesSalaryRow
    {
        public string SalesPersonId { get; init; } = string.Empty;
        public string SalesPersonName { get; init; } = "-";
        public decimal Omset { get; init; }
        public int KunjunganCount { get; init; }
        public decimal KomisiOmset { get; init; }
        public decimal KomisiKunjungan { get; init; }
        public decimal TotalGaji { get; init; }
    }

    public static class ReportService
    {
        public static async Task<List<SalesPerformanceRow>> GetSalesPerformanceAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var sales = await localDb.Table<LocalSale>().ToListAsync();
            var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();

            if (startDate.HasValue)
                sales = sales.Where(s => s.OrderDate >= startDate.Value.Date).ToList();

            if (endDate.HasValue)
                sales = sales.Where(s => s.OrderDate <= endDate.Value.Date.AddDays(1).AddTicks(-1)).ToList();

            var now = DateTime.Now;
            var threshold = now.AddDays(-14);

            var spById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var spByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sp in salesPersons)
            {
                if (!string.IsNullOrWhiteSpace(sp.Id)) spById[sp.Id.Trim()] = sp.Name?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(sp.Name)) spByName[sp.Name.Trim()] = sp.Name.Trim();
            }

            return sales
                .GroupBy(s => s.SalesPersonId)
                .Select(group =>
                {
                    string spName = SalesResolutionService.ResolveSalesPersonName(group.Key, spById, spByName);
                    return new SalesPerformanceRow
                    {
                        SalesPersonId = group.Key ?? string.Empty,
                        SalesPersonName = spName,
                        Omset = group.Sum(s => s.Total),
                        NotaCount = group.Count(),
                        NotaMoreThan14Days = group.Count(s => s.OrderDate < threshold)
                    };
                })
                .OrderByDescending(x => x.Omset)
                .ToList();
        }

        public static async Task<List<SalesSalaryRow>> GetSalaryPerformanceAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var sales = await localDb.Table<LocalSale>().ToListAsync();
            var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();

            if (startDate.HasValue)
                sales = sales.Where(s => s.OrderDate >= startDate.Value.Date).ToList();

            if (endDate.HasValue)
                sales = sales.Where(s => s.OrderDate <= endDate.Value.Date.AddDays(1).AddTicks(-1)).ToList();

            const decimal omsetRate = 0.015m;
            const decimal kunjunganRate = 25000m;

            var spById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var spByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sp in salesPersons)
            {
                if (!string.IsNullOrWhiteSpace(sp.Id)) spById[sp.Id.Trim()] = sp.Name?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(sp.Name)) spByName[sp.Name.Trim()] = sp.Name.Trim();
            }

            return sales
                .GroupBy(s => s.SalesPersonId)
                .Select(group =>
                {
                    string spName = SalesResolutionService.ResolveSalesPersonName(group.Key, spById, spByName);
                    var omset = group.Sum(s => s.Total);
                    var notaCount = group.Count();
                    var komisiOmset = omset * omsetRate;
                    var komisiKunjungan = notaCount * kunjunganRate;

                    return new SalesSalaryRow
                    {
                        SalesPersonId = group.Key ?? string.Empty,
                        SalesPersonName = spName,
                        Omset = omset,
                        KunjunganCount = notaCount,
                        KomisiOmset = komisiOmset,
                        KomisiKunjungan = komisiKunjungan,
                        TotalGaji = komisiOmset + komisiKunjungan
                    };

                })
                .OrderByDescending(x => x.TotalGaji)
                .ToList();
        }

        public static void GeneratePriceListPdf(List<LocalProduct> products, string filePath)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Text("Daftar Harga Produk")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .Column(column =>
                        {
                            column.Spacing(5);

                            column.Item().Text($"Tanggal: {DateTime.Now:dd/MM/yyyy}");
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Nama Produk");
                                    header.Cell().Element(CellStyle).Text("Kategori");
                                    header.Cell().Element(CellStyle).Text("Satuan");
                                    header.Cell().Element(CellStyle).AlignRight().Text("Harga R");
                                    header.Cell().Element(CellStyle).AlignRight().Text("Harga SG");
                                    header.Cell().Element(CellStyle).AlignRight().Text("Harga G");

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                    }
                                });

                                foreach (var product in products)
                                {
                                    table.Cell().Element(CellStyle).Text(product.Name);
                                    table.Cell().Element(CellStyle).Text(product.Kategori);
                                    table.Cell().Element(CellStyle).Text(product.Satuan);
                                    table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(product.HargaR));
                                    table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(product.HargaSg));
                                    table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(product.HargaG));

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                                    }
                                }
                            });
                        });
                    
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Halaman ");
                            x.CurrentPageNumber();
                        });
                });
            }).GeneratePdf(filePath);
        }

        public static string FormatCurrency(decimal value) => value.ToString("C0", new CultureInfo("id-ID"));
    }
}
