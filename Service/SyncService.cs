using SPJ_APP.Model;
using SQLite;

namespace SPJ_APP.Service
{
    public sealed class ConflictItem
    {
        public LocalProduct LocalVersion { get; init; } = null!;
        public Product ServerVersion { get; init; } = null!;
    }

    public sealed class ProductSyncResult
    {
        public int PushedCount { get; internal set; }
        public int PulledCount { get; internal set; }
        public List<ConflictItem> Conflicts { get; } = new();
    }

    public sealed class SyncSummary
    {
        public int ProductsPushed { get; internal set; }
        public int ProductsPulled { get; internal set; }
        public int SalesPersonsPushed { get; internal set; }
        public int CustomersPushed { get; internal set; }
        public int PaymentsPushed { get; internal set; }
        public int DeliveriesPushed { get; internal set; }
        public int SalesPushed { get; internal set; }
        public int SaleDetailsPushed { get; internal set; }
        public int PurchaseOrdersPushed { get; internal set; }
        public int ActivityLogsPushed { get; internal set; }
        public int Conflicts { get; internal set; }

        public string ToDisplayText() =>
            $"Sinkronisasi selesai. Sales: {SalesPersonsPushed}, customer: {CustomersPushed}, produk dikirim: {ProductsPushed}, " +
            $"produk diambil: {ProductsPulled}, penjualan: {SalesPushed}, detail penjualan: {SaleDetailsPushed}, " +
            $"pembayaran: {PaymentsPushed}, pengiriman: {DeliveriesPushed}, PO: {PurchaseOrdersPushed}, log aktivitas: {ActivityLogsPushed}, konflik: {Conflicts}.";
    }

    // Tetap berada di file ini karena merupakan hasil internal dari SyncService.
    public sealed class SalesSyncResult
    {
        public int SalesPushed { get; internal set; }
        public int DetailsPushed { get; internal set; }
    }

    public static class SyncService
    {
        public static async Task<(SyncSummary, List<ConflictItem>)> SyncAllAsync()
        {
            await PullPendingPaymentsAsync();
            var salesPersonsPushed = await SyncSalesPersonsAsync();
            var customersPushed = await SyncCustomersAsync();
            var productResult = await SyncProductsAsync();
            var salesResult = await SyncSalesAsync();
            var paymentsPushed = await SyncPaymentsAsync();
            var deliveriesPushed = await SyncDeliveriesAsync();
            var purchaseOrdersPushed = await SyncPurchaseOrdersAsync();
            var activityLogsPushed = await SyncActivityLogsAsync();
            await SyncSalesOrdersQueueAsync();
            await SyncVisitLogsQueueAsync();

            var summary = new SyncSummary
            {
                ProductsPushed = productResult.PushedCount,
                ProductsPulled = productResult.PulledCount,
                SalesPersonsPushed = salesPersonsPushed,
                CustomersPushed = customersPushed,
                SalesPushed = salesResult.SalesPushed,
                SaleDetailsPushed = salesResult.DetailsPushed,
                PaymentsPushed = paymentsPushed,
                DeliveriesPushed = deliveriesPushed,
                PurchaseOrdersPushed = purchaseOrdersPushed,
                ActivityLogsPushed = activityLogsPushed,
                Conflicts = productResult.Conflicts.Count,
            };
            return (summary, productResult.Conflicts);
        }

        public static async Task<int> SyncPurchaseOrdersAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var supabase = await SupabaseService.GetClient();
            var localPOs = await localDb.Table<LocalPurchaseOrder>().Where(po => !po.IsSynced).ToListAsync();
            if (localPOs.Count == 0) return 0;

            var allLocalDetails = await localDb.Table<LocalPurchaseOrderDetail>().ToListAsync();
            var remotePOIds = (await supabase.From<PurchaseOrder>().Get()).Models.Select(item => item.Id).ToHashSet();

            foreach (var localPO in localPOs)
            {
                if (!Guid.TryParse(localPO.Id, out var poId))
                    throw new InvalidOperationException($"ID PO tidak valid: '{localPO.Id}'.");

                var remotePO = new PurchaseOrder
                {
                    Id = poId,
                    PurchaseOrderNumber = localPO.PurchaseOrderNumber,
                    OrderDate = localPO.OrderDate,
                    Notes = localPO.Notes,
                    TotalQty = localPO.TotalQty,
                    CreatedAt = localPO.CreatedAt ?? DateTime.UtcNow
                };

                if (remotePOIds.Contains(poId))
                    await supabase.From<PurchaseOrder>().Update(remotePO);
                else
                    await supabase.From<PurchaseOrder>().Insert(remotePO);

                // Delete existing details and insert new ones
                await supabase.From<PurchaseOrderDetail>().Where(x => x.PurchaseOrderId == poId).Delete();

                var detailsForPO = allLocalDetails.Where(d => d.PurchaseOrderId == localPO.Id);
                var remoteDetails = detailsForPO.Select(d => new PurchaseOrderDetail
                {
                    Id = Guid.TryParse(d.Id, out var detailId) ? detailId : Guid.NewGuid(),
                    PurchaseOrderId = poId,
                    ProductId = Guid.Parse(d.ProductId),
                    QtyCalculated = d.QtyCalculated,
                    QtyRatio = d.QtyRatio,
                    Notes = d.Notes
                }).ToList();

                if (remoteDetails.Any())
                    await supabase.From<PurchaseOrderDetail>().Insert(remoteDetails);

                localPO.IsSynced = true;
                await localDb.UpdateAsync(localPO);
            }

            return localPOs.Count;
        }

        public static async Task<int> SyncSalesPersonsAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var supabase = await SupabaseService.GetClient();
            var localItems = await localDb.Table<LocalSalesPerson>().ToListAsync();
            var remoteList = (await supabase.From<SalesPerson>().Get()).Models;
            var remoteNames = remoteList.Select(item => item.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            int pushedCount = 0;
            foreach (var local in localItems)
            {
                // Keamanan 1: Lewati jika Nama kosong/null agar tidak memicu not-null constraint di Supabase (code 23502)
                if (string.IsNullOrWhiteSpace(local.Name))
                {
                    continue;
                }

                // Keamanan 2: Akun Developer khusus lokal - jangan kirim ke Supabase
                if (string.Equals(local.Role, "DEVELOPER", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(local.Name, "blessstudiosie", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(local.Name, "Developer", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    string cleanName = local.Name.Trim();
                    var remote = new SalesPerson
                    {
                        Name = cleanName,
                        Phone = string.IsNullOrWhiteSpace(local.Phone) ? "-" : local.Phone,
                        Email = string.IsNullOrWhiteSpace(local.Email) ? "-" : local.Email,
                        TargetOmset = local.TargetOmset,
                        Role = string.IsNullOrWhiteSpace(local.Role) ? "SALES" : local.Role,
                        Password = local.Password
                    };

                    if (remoteNames.Contains(cleanName))
                    {
                        await supabase.From<SalesPerson>().Where(x => x.Name == cleanName).Update(remote);
                    }
                    else
                    {
                        await supabase.From<SalesPerson>().Insert(remote);
                        remoteNames.Add(cleanName);
                    }

                    local.IsSynced = true;
                    await localDb.UpdateAsync(local);
                    pushedCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncSalesPersons] Gagal sync baris '{local.Name}': {ex.Message}");
                }
            }
            return pushedCount;
        }


        public static async Task<int> SyncCustomersAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var supabase = await SupabaseService.GetClient();
            var localItems = await localDb.Table<LocalCustomer>().ToListAsync();
            var remoteList = (await supabase.From<Customer>().Get()).Models;
            var remoteNames = remoteList.Select(item => item.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            int pushedCount = 0;
            foreach (var local in localItems)
            {
                if (string.IsNullOrWhiteSpace(local.Name))
                {
                    continue;
                }

                try
                {
                    string cleanName = local.Name.Trim();
                    var remote = new Customer
                    {
                        Name = cleanName,
                        OwnerName = local.OwnerName ?? "",
                        Phone = local.Phone ?? "",
                        Address = local.Address ?? "",
                        JalurPengiriman = local.JalurPengiriman ?? "",
                        Latitude = local.Latitude,
                        Longitude = local.Longitude,
                        SalesPerson = local.SalesPersonId,
                        LimitPiutang = local.LimitPiutang
                    };

                    if (remoteNames.Contains(cleanName))
                    {
                        await supabase.From<Customer>().Where(x => x.Name == cleanName).Update(remote);
                    }
                    else
                    {
                        await supabase.From<Customer>().Insert(remote);
                        remoteNames.Add(cleanName);
                    }

                    local.IsSynced = true;
                    await localDb.UpdateAsync(local);
                    pushedCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncCustomers] Gagal sync customer '{local.Name}': {ex.Message}");
                }
            }
            return pushedCount;
        }


        /// <summary>
        /// Menjadikan data penjualan lokal sebagai sumber data untuk transaksi yang ada di perangkat ini.
        /// Header sales dikirim lebih dahulu, kemudian seluruh detail nota tersebut diganti di server.
        /// </summary>
        public static async Task<SalesSyncResult> SyncSalesAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var supabase = await SupabaseService.GetClient();
            var result = new SalesSyncResult();

            var localSales = await localDb.Table<LocalSale>().ToListAsync();
            var localDetails = await localDb.Table<LocalSalesDetail>().ToListAsync();
            var remoteSales = (await supabase.From<Sale>().Get()).Models.ToDictionary(sale => sale.Nota, StringComparer.OrdinalIgnoreCase);
            var remoteDetails = (await supabase.From<SaleDetail>().Get()).Models
                .Where(detail => !string.IsNullOrWhiteSpace(detail.Nota))
                .GroupBy(detail => detail.Nota)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var localSale in localSales)
            {
                if (string.IsNullOrWhiteSpace(localSale.Nota))
                {
                    continue;
                }

                try
                {
                    var remoteSale = ToRemoteSale(localSale);
                    if (remoteSales.ContainsKey(localSale.Nota))
                        await supabase.From<Sale>().Where(x => x.Nota == localSale.Nota).Update(remoteSale);
                    else
                        await supabase.From<Sale>().Insert(remoteSale);

                    var detailsForSale = localDetails.Where(detail => detail.SaleId == localSale.Id || detail.SaleId == localSale.Nota).ToList();
                    if (remoteDetails.TryGetValue(localSale.Nota, out var existingDetails))
                    {
                        foreach (var existingDetail in existingDetails)
                            await supabase.From<SaleDetail>().Where(x => x.Id == existingDetail.Id).Delete();
                    }

                    var remoteSaleDetails = detailsForSale.Select(ToRemoteSaleDetail).ToList();
                    if (remoteSaleDetails.Count > 0)
                        await supabase.From<SaleDetail>().Insert(remoteSaleDetails);

                    localSale.IsSynced = true;
                    await localDb.UpdateAsync(localSale);
                    result.SalesPushed++;
                    result.DetailsPushed += remoteSaleDetails.Count;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncSales] Gagal sync nota '{localSale.Nota}': {ex.Message}");
                }
            }

            return result;
        }

        public static async Task<int> SyncPaymentsAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var supabase = await SupabaseService.GetClient();
            var localItems = await localDb.Table<LocalPayment>().ToListAsync();
            var remoteIds = (await supabase.From<Payment>().Get()).Models.Select(item => item.Id).ToHashSet();

            int pushedCount = 0;
            foreach (var local in localItems)
            {
                if (string.IsNullOrWhiteSpace(local.Id) || !Guid.TryParse(local.Id, out var id) || !Guid.TryParse(local.SaleId, out var saleId))
                    continue;

                try
                {
                    var remote = new Payment { Id = id, SaleId = saleId, PaymentDate = local.PaymentDate, Amount = local.Amount, PaymentMethod = local.PaymentMethod, Status = local.Status, Notes = local.Notes };
                    if (remoteIds.Contains(id)) await supabase.From<Payment>().Where(x => x.Id == id).Update(remote);
                    else await supabase.From<Payment>().Insert(remote);
                    local.IsSynced = true;
                    await localDb.UpdateAsync(local);
                    pushedCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncPayments] Gagal sync payment '{local.Id}': {ex.Message}");
                }
            }
            return pushedCount;
        }


        public static async Task<List<LocalPayment>> PullPendingPaymentsAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var supabase = await SupabaseService.GetClient();
            var pendingPayments = (await supabase.From<Payment>().Get()).Models
                .Where(payment => string.Equals(payment.Status, "PENDING", StringComparison.OrdinalIgnoreCase));

            foreach (var remote in pendingPayments)
            {
                var local = await localDb.FindAsync<LocalPayment>(remote.Id.ToString());
                var mapped = new LocalPayment
                {
                    Id = remote.Id.ToString(),
                    SaleId = remote.SaleId.ToString(),
                    PaymentDate = remote.PaymentDate,
                    Amount = remote.Amount,
                    PaymentMethod = remote.PaymentMethod,
                    Status = "PENDING",
                    Notes = remote.Notes,
                    IsSynced = true
                };
                if (local is null) await localDb.InsertAsync(mapped);
                else await localDb.UpdateAsync(mapped);
            }

            return await localDb.Table<LocalPayment>().Where(payment => payment.Status == "PENDING").ToListAsync();
        }

        public static async Task ConfirmPaymentAsync(LocalPayment payment, bool approve)
        {
            if (!Guid.TryParse(payment.Id, out var paymentId) || !Guid.TryParse(payment.SaleId, out var saleId))
                throw new InvalidOperationException("ID pembayaran tidak valid.");

            var localDb = await LocalDatabaseService.GetConnection();
            var localSale = await localDb.FindAsync<LocalSale>(payment.SaleId);
            if (approve && (localSale is null || localSale.Status != "TEMPO"))
                throw new InvalidOperationException("Pembayaran hanya dapat disetujui untuk nota lokal berstatus TEMPO.");

            var supabase = await SupabaseService.GetClient();
            var remote = new Payment
            {
                Id = paymentId,
                SaleId = saleId,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                Status = approve ? "APPROVED" : "REJECTED",
                Notes = payment.Notes
            };
            await supabase.From<Payment>().Update(remote);

            await localDb.RunInTransactionAsync(conn =>
            {
                var localPayment = conn.Find<LocalPayment>(payment.Id);
                if (localPayment is null || localPayment.Status != "PENDING") return;

                localPayment.Status = remote.Status;
                localPayment.IsSynced = true;
                conn.Update(localPayment);

                if (!approve) return;
                var sale = conn.Find<LocalSale>(payment.SaleId)!;
                sale.Paid += payment.Amount;
                sale.Remaining = Math.Max(0, sale.Total - sale.Paid);
                if (sale.Remaining == 0) sale.Status = "DONE";
                sale.UpdatedAt = DateTime.Now;
                sale.IsSynced = false;
                conn.Update(sale);
            });
        }

        public static async Task<int> SyncDeliveriesAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var supabase = await SupabaseService.GetClient();
            var localDeliveries = await localDb.Table<LocalDelivery>().Where(d => !d.IsSynced).ToListAsync();
            var allLocalDetails = await localDb.Table<LocalDeliveryDetail>().ToListAsync();

            if (localDeliveries.Count == 0) return 0;

            var detailsByDeliveryId = allLocalDetails
                .GroupBy(d => d.DeliveryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Handle potential duplicate delivery numbers before sending to server
            var remoteDeliveryNumbers = (await supabase.From<Delivery>().Get()).Models
                .Select(item => item.DeliveryNumber)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var deliveryCount = 0;
            var detailCount = 0;

            foreach (var local in localDeliveries)
            {
                if (!Guid.TryParse(local.Id, out var id))
                    throw new InvalidOperationException($"ID pengiriman tidak valid: '{local.Id}'.");

                // Ensure delivery number is unique on the server before upserting
                if (remoteDeliveryNumbers.Contains(local.DeliveryNumber))
                {
                    var baseNumber = local.DeliveryNumber;
                    do
                    {
                        local.DeliveryNumber = $"{baseNumber}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
                    } while (remoteDeliveryNumbers.Contains(local.DeliveryNumber));
                    await localDb.UpdateAsync(local);
                }

                var detailsForRpc = new List<object>();
                if (detailsByDeliveryId.TryGetValue(local.Id, out var details))
                {
                    detailsForRpc.AddRange(details.Select(ToRemoteDeliveryDetail));
                    detailCount += details.Count;
                }

                var parameters = new Dictionary<string, object?>
                {
                    { "p_id", id },
                    { "p_delivery_number", local.DeliveryNumber },
                    { "p_driver_id", ParseOptionalGuid(local.DriverId, "driver") },
                    { "p_helper_id", ParseOptionalGuid(local.HelperId, "helper") },
                    { "p_checker_id", ParseOptionalGuid(local.CheckerId, "checker") },
                    { "p_status", local.Status },
                    { "p_closed_at", local.ClosedAt },
                    { "p_details", Newtonsoft.Json.JsonConvert.SerializeObject(detailsForRpc) }
                };

                await supabase.Rpc("upsert_delivery_with_details", parameters);

                local.IsSynced = true;
                await localDb.UpdateAsync(local);
                deliveryCount++;

                // Add the newly assigned number to the set to prevent collisions within the same sync batch
                remoteDeliveryNumbers.Add(local.DeliveryNumber);
            }
            return deliveryCount + detailCount;
        }

        public static async Task<ProductSyncResult> SyncProductsAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var supabase = await SupabaseService.GetClient();
            var result = new ProductSyncResult();

            var localProducts = await localDb.Table<LocalProduct>().ToListAsync();
            var remoteResponse = await supabase.From<Product>().Get();
            var remoteByName = remoteResponse.Models.ToDictionary(product => product.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var localProduct in localProducts.Where(product => !product.IsSynced))
            {
                var remoteProduct = ToRemoteProduct(localProduct);
                if (remoteByName.TryGetValue(localProduct.Name, out var currentRemote))
                {
                    if (HasServerChanged(localProduct, currentRemote))
                    {
                        result.Conflicts.Add(new ConflictItem
                        {
                            LocalVersion = localProduct,
                            ServerVersion = currentRemote
                        });
                        continue;
                    }

                    await supabase.From<Product>().Update(remoteProduct);
                }
                else
                {
                    await supabase.From<Product>().Insert(remoteProduct);
                }

                localProduct.IsSynced = true;
                localProduct.LastSyncedUpdatedAt = null;
                await localDb.UpdateAsync(localProduct);
                result.PushedCount++;
            }

            // Ambil ulang agar updated_at dari server menjadi patokan sinkronisasi berikutnya.
            remoteResponse = await supabase.From<Product>().Get();
            foreach (var remoteProduct in remoteResponse.Models)
            {
                var localProduct = localProducts.FirstOrDefault(product => string.Equals(product.Name, remoteProduct.Name, StringComparison.OrdinalIgnoreCase));
                if (localProduct is not null && !localProduct.IsSynced)
                    continue; // Konflik menunggu pilihan pengguna.

                var mappedProduct = ToLocalProduct(remoteProduct, localProduct);
                if (localProduct is null)
                    await localDb.InsertAsync(mappedProduct);
                else
                    await localDb.UpdateAsync(mappedProduct);

                result.PulledCount++;
            }

            return result;
        }

        public static async Task ForcePushAsync(LocalProduct localProduct)
        {
            var supabase = await SupabaseService.GetClient();
            var remoteProduct = ToRemoteProduct(localProduct);
            await supabase.From<Product>().Update(remoteProduct);

            var localDb = await LocalDatabaseService.GetConnection();
            localProduct.IsSynced = true;
            localProduct.LastSyncedUpdatedAt = null;
            await localDb.UpdateAsync(localProduct);
        }

        public static async Task AcceptServerVersionAsync(Product serverProduct)
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var localProducts = await localDb.Table<LocalProduct>().ToListAsync();
            var localProduct = localProducts.FirstOrDefault(p => string.Equals(p.Name, serverProduct.Name, StringComparison.OrdinalIgnoreCase));
            var mappedProduct = ToLocalProduct(serverProduct, localProduct);

            if (localProduct is null)
                await localDb.InsertAsync(mappedProduct);
            else
                await localDb.UpdateAsync(mappedProduct);
        }

        private static bool HasServerChanged(LocalProduct local, Product remote) =>
            local.LastSyncedUpdatedAt.HasValue && remote.UpdatedAt.HasValue &&
            remote.UpdatedAt.Value > local.LastSyncedUpdatedAt.Value;

        private static Product ToRemoteProduct(LocalProduct local) => new()
        {
            Name = local.Name,
            StokReady = local.StokReady,
            StokFisik = local.StokFisik,
            HargaR = local.HargaR,
            HargaSg = local.HargaSg,
            HargaG = local.HargaG,
            HargaP = local.HargaP,
            Kategori = local.Kategori,
            Satuan = local.Satuan,
            SatuanBesar = local.SatuanBesar,
            QtyRatio = local.QtyRatio,
            Status = local.Status,
            Description = local.Description
        };

        private static LocalProduct ToLocalProduct(Product remote, LocalProduct? existing) => new()
        {
            Id = existing?.Id ?? Guid.NewGuid().ToString(),
            Name = remote.Name,
            StokReady = remote.StokReady,
            StokFisik = remote.StokFisik,
            HargaR = remote.HargaR,
            HargaSg = remote.HargaSg,
            HargaG = remote.HargaG,
            HargaP = remote.HargaP,
            Kategori = remote.Kategori,
            Satuan = remote.Satuan,
            SatuanBesar = remote.SatuanBesar,
            QtyRatio = remote.QtyRatio,
            Status = remote.Status,
            Description = remote.Description,
            UpdatedAt = remote.UpdatedAt,
            IsSynced = true,
            LastSyncedUpdatedAt = remote.UpdatedAt
        };

        private static Sale ToRemoteSale(LocalSale local) => new()
        {
            Nota = local.Nota,
            CustomerName = local.CustomerId,
            OrderDate = local.OrderDate,
            DeliveryDate = local.DeliveryDate,
            Status = local.Status,
            SalesPerson = local.SalesPersonId,
            Total = local.Total,
            Paid = local.Paid,
            Remaining = local.Remaining,
            Description = local.Description
        };

        private static SaleDetail ToRemoteSaleDetail(LocalSalesDetail local)
        {
            return new SaleDetail
            {
                Id = string.IsNullOrEmpty(local.Id) ? Guid.NewGuid().ToString() : local.Id,
                Nota = local.SaleId, // SaleId maps to Nota in local SQLite
                ItemName = local.ProductId,
                Qty = local.Qty,
                Price = local.Price,
                PriceCategory = local.PriceCategory,
                Subtotal = local.Subtotal
            };
        }


        private static Guid? ParseOptionalGuid(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (Guid.TryParse(value, out var id))
                return id;

            throw new InvalidOperationException($"ID {fieldName} tidak valid: '{value}'.");
        }


        private static DeliveryDetail ToRemoteDeliveryDetail(LocalDeliveryDetail local)
        {
            if (!Guid.TryParse(local.Id, out var id) || !Guid.TryParse(local.DeliveryId, out var deliveryId) || !Guid.TryParse(local.SaleId, out var saleId))
                throw new InvalidOperationException($"Detail pengiriman '{local.Id}' memiliki ID yang tidak valid.");
            return new DeliveryDetail { Id = id, DeliveryId = deliveryId, SaleId = saleId };
        }

        public static async Task<int> SyncActivityLogsAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var supabase = await SupabaseService.GetClient();
            var localItems = await localDb.Table<LocalActivityLog>().Where(l => l.IsSynced == false).ToListAsync();

            foreach (var local in localItems)
            {
                if (!Guid.TryParse(local.Id.ToString(), out var id))
                    continue;

                var remote = new ActivityLog { Id = id, UserName = local.UserName, Action = local.Action, Details = local.Details, CreatedAt = local.CreatedAt };
                await supabase.From<ActivityLog>().Insert(remote);

                local.IsSynced = true;
                await localDb.UpdateAsync(local);
            }
            return localItems.Count;
        }

        public static async Task<int> SyncSalesOrdersQueueAsync()
        {
            try
            {
                var localDb = await LocalDatabaseService.GetConnection();
                var supabase = await SupabaseService.GetClient();

                var remoteQueues = await supabase.From<SalesOrderQueue>().Get();

                if (remoteQueues?.Models == null || !remoteQueues.Models.Any())
                    return 0;

                int pulled = 0;
                foreach (var remote in remoteQueues.Models)
                {
                    var existing = await localDb.Table<LocalSalesOrderQueue>()
                                                .Where(q => q.Id == remote.Id.ToString())
                                                .FirstOrDefaultAsync();

                    if (existing == null)
                    {
                        var localQueue = new LocalSalesOrderQueue
                        {
                            Id = remote.Id.ToString(),
                            SalesPersonId = remote.SalesPersonId?.ToString(),
                            CustomerId = remote.CustomerId?.ToString(),
                            CustomerName = remote.CustomerName,
                            ItemsJson = remote.ItemsJson,
                            TotalAmount = remote.TotalAmount,
                            Status = remote.Status,
                            Notes = remote.Notes,
                            CreatedAt = remote.CreatedAt,
                            UpdatedAt = remote.UpdatedAt,
                            IsSynced = true
                        };
                        await localDb.InsertAsync(localQueue);
                        pulled++;
                    }
                    else
                    {
                        existing.Status = remote.Status;
                        existing.ItemsJson = remote.ItemsJson;
                        existing.TotalAmount = remote.TotalAmount;
                        existing.Notes = remote.Notes;
                        existing.UpdatedAt = remote.UpdatedAt;
                        existing.IsSynced = true;
                        await localDb.UpdateAsync(existing);
                    }
                }
                return pulled;
            }
            catch
            {
                return 0;
            }
        }

        public static async Task<int> SyncVisitLogsQueueAsync()
        {
            try
            {
                var localDb = await LocalDatabaseService.GetConnection();
                var supabase = await SupabaseService.GetClient();

                var remoteVisits = await supabase.From<VisitLogQueue>().Get();

                if (remoteVisits?.Models == null || !remoteVisits.Models.Any())
                    return 0;

                int pulled = 0;
                foreach (var remote in remoteVisits.Models)
                {
                    var existing = await localDb.Table<LocalVisitLogQueue>()
                                                .Where(v => v.Id == remote.Id.ToString())
                                                .FirstOrDefaultAsync();

                    if (existing == null)
                    {
                        var localVisit = new LocalVisitLogQueue
                        {
                            Id = remote.Id.ToString(),
                            SalesPersonId = remote.SalesPersonId?.ToString(),
                            SalesPersonName = remote.SalesPersonName,
                            CustomerId = remote.CustomerId?.ToString(),
                            CustomerName = remote.CustomerName,
                            IsNewCustomer = remote.IsNewCustomer,
                            Latitude = remote.Latitude,
                            Longitude = remote.Longitude,
                            PhotoUrl = remote.PhotoUrl,
                            Notes = remote.Notes,
                            Status = remote.Status,
                            CreatedAt = remote.CreatedAt,
                            UpdatedAt = remote.UpdatedAt,
                            IsSynced = true
                        };
                        await localDb.InsertAsync(localVisit);
                        pulled++;
                    }
                    else
                    {
                        existing.Status = remote.Status;
                        existing.Notes = remote.Notes;
                        existing.UpdatedAt = remote.UpdatedAt;
                        existing.IsSynced = true;
                        await localDb.UpdateAsync(existing);
                    }
                }
                return pulled;
            }
            catch
            {
                return 0;
            }
        }

        public static async Task PushQueueStatusToSupabaseAsync(string queueId, string newStatus, string queueType = "SO")
        {
            try
            {
                var supabase = await SupabaseService.GetClient();
                if (!Guid.TryParse(queueId, out var id)) return;

                if (queueType == "SO")
                {
                    await supabase.From<SalesOrderQueue>()
                                  .Where(x => x.Id == id)
                                  .Set(x => x.Status, newStatus)
                                  .Update();
                }
                else if (queueType == "VISIT")
                {
                    await supabase.From<VisitLogQueue>()
                                  .Where(x => x.Id == id)
                                  .Set(x => x.Status, newStatus)
                                  .Update();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error pushing queue status: {ex.Message}");
            }
        }

        /// <summary>
        /// Menarik SELURUH data dari Supabase cloud (SalesPerson, Customer, Produk, Sales, SalesDetail, Payment, Queue)
        /// dan menyimpannya ke database SQLite lokal perangkat ini.
        /// </summary>
        public static async Task<string> PullAllFromSupabaseAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var supabase = await SupabaseService.GetClient();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== HASIL TARIK FULL DATA SUPABASE ===");

            // 1. Pull SalesPersons
            try
            {
                var remoteUsers = (await supabase.From<SalesPerson>().Get()).Models;
                int userCount = 0;
                foreach (var remote in remoteUsers)
                {
                    if (string.IsNullOrWhiteSpace(remote.Name)) continue;

                    string cleanName = remote.Name.Trim();
                    var localUser = await localDb.Table<LocalSalesPerson>()
                                                 .Where(u => u.Name == cleanName)
                                                 .FirstOrDefaultAsync();

                    if (localUser == null)
                    {
                        var newUser = new LocalSalesPerson
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = cleanName,
                            Phone = string.IsNullOrWhiteSpace(remote.Phone) ? "-" : remote.Phone,
                            Email = string.IsNullOrWhiteSpace(remote.Email) ? "-" : remote.Email,
                            TargetOmset = remote.TargetOmset,
                            Role = string.IsNullOrWhiteSpace(remote.Role) ? "SALES" : remote.Role,
                            Password = remote.Password,
                            IsSynced = true
                        };
                        await localDb.InsertAsync(newUser);
                        userCount++;
                    }
                    else
                    {
                        if (localUser.Role != "DEVELOPER" && localUser.Name != "blessstudiosie")
                        {
                            localUser.Phone = string.IsNullOrWhiteSpace(remote.Phone) ? localUser.Phone : remote.Phone;
                            localUser.Email = string.IsNullOrWhiteSpace(remote.Email) ? localUser.Email : remote.Email;
                            localUser.TargetOmset = remote.TargetOmset;
                            localUser.Role = string.IsNullOrWhiteSpace(remote.Role) ? localUser.Role : remote.Role;
                            localUser.Password = remote.Password ?? localUser.Password;
                            localUser.IsSynced = true;
                            await localDb.UpdateAsync(localUser);
                            userCount++;
                        }
                    }
                }
                sb.AppendLine($"✓ SalesPerson: {userCount} akun ditarik.");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"× SalesPerson: {ex.Message}");
            }

            // 2. Pull Customers
            try
            {
                var remoteCustomers = (await supabase.From<Customer>().Get()).Models;
                int customerCount = 0;
                foreach (var remote in remoteCustomers)
                {
                    if (string.IsNullOrWhiteSpace(remote.Name)) continue;

                    string cleanName = remote.Name.Trim();
                    var localCustomer = await localDb.Table<LocalCustomer>()
                                                     .Where(c => c.Name == cleanName)
                                                     .FirstOrDefaultAsync();

                    if (localCustomer == null)
                    {
                        var newCust = new LocalCustomer
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = cleanName,
                            OwnerName = remote.OwnerName ?? "",
                            Phone = remote.Phone ?? "",
                            Address = remote.Address ?? "",
                            JalurPengiriman = remote.JalurPengiriman ?? "",
                            Latitude = remote.Latitude,
                            Longitude = remote.Longitude,
                            SalesPersonId = remote.SalesPerson,
                            LimitPiutang = remote.LimitPiutang,
                            IsSynced = true
                        };
                        await localDb.InsertAsync(newCust);
                    }
                    else
                    {
                        localCustomer.OwnerName = remote.OwnerName ?? localCustomer.OwnerName;
                        localCustomer.Phone = remote.Phone ?? localCustomer.Phone;
                        localCustomer.Address = remote.Address ?? localCustomer.Address;
                        localCustomer.JalurPengiriman = remote.JalurPengiriman ?? localCustomer.JalurPengiriman;
                        localCustomer.Latitude = remote.Latitude;
                        localCustomer.Longitude = remote.Longitude;
                        localCustomer.SalesPersonId = remote.SalesPerson;
                        localCustomer.LimitPiutang = remote.LimitPiutang;
                        localCustomer.IsSynced = true;
                        await localDb.UpdateAsync(localCustomer);
                    }
                    customerCount++;
                }
                sb.AppendLine($"✓ Customer: {customerCount} pelanggan ditarik.");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"× Customer: {ex.Message}");
            }

            // 3. Pull Products
            try
            {
                var remoteProducts = (await supabase.From<Product>().Get()).Models;
                int prodCount = 0;
                foreach (var remote in remoteProducts)
                {
                    if (string.IsNullOrWhiteSpace(remote.Name)) continue;

                    string cleanName = remote.Name.Trim();
                    var localProd = await localDb.Table<LocalProduct>()
                                                 .Where(p => p.Name == cleanName)
                                                 .FirstOrDefaultAsync();

                    var mapped = ToLocalProduct(remote, localProd);
                    if (localProd == null) await localDb.InsertAsync(mapped);
                    else await localDb.UpdateAsync(mapped);
                    prodCount++;
                }
                sb.AppendLine($"✓ Produk: {prodCount} item ditarik.");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"× Produk: {ex.Message}");
            }

            // 4. Pull Sales & SaleDetails
            try
            {
                var remoteSales = (await supabase.From<Sale>().Get()).Models;
                var remoteDetails = (await supabase.From<SaleDetail>().Get()).Models;
                var detailsGrouped = remoteDetails.Where(d => !string.IsNullOrWhiteSpace(d.Nota))
                                                  .GroupBy(d => d.Nota)
                                                  .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                int saleCount = 0;
                foreach (var remoteSale in remoteSales)
                {
                    if (string.IsNullOrWhiteSpace(remoteSale.Nota)) continue;

                    string cleanNota = remoteSale.Nota.Trim();
                    var localSale = await localDb.Table<LocalSale>()
                                                 .Where(s => s.Nota == cleanNota)
                                                 .FirstOrDefaultAsync();

                    string saleId = localSale?.Id ?? Guid.NewGuid().ToString();
                    var newSale = new LocalSale
                    {
                        Id = saleId,
                        Nota = cleanNota,
                        CustomerId = remoteSale.CustomerName,
                        SalesPersonId = remoteSale.SalesPerson,
                        OrderDate = remoteSale.OrderDate,
                        DeliveryDate = remoteSale.DeliveryDate,
                        Status = remoteSale.Status,
                        Total = remoteSale.Total,
                        Paid = remoteSale.Paid,
                        Remaining = remoteSale.Remaining,
                        Description = remoteSale.Description,
                        UpdatedAt = DateTime.Now,
                        IsSynced = true
                    };

                    if (localSale == null) await localDb.InsertAsync(newSale);
                    else await localDb.UpdateAsync(newSale);

                    if (detailsGrouped.TryGetValue(cleanNota, out var details))
                    {
                        foreach (var detail in details)
                        {
                            var existingDetail = await localDb.Table<LocalSalesDetail>()
                                                              .Where(d => d.Id == detail.Id || (d.SaleId == saleId && d.ProductId == detail.ItemName))
                                                              .FirstOrDefaultAsync();

                            var mappedDetail = new LocalSalesDetail
                            {
                                Id = detail.Id ?? Guid.NewGuid().ToString(),
                                SaleId = saleId,
                                ProductId = detail.ItemName,
                                Qty = detail.Qty,
                                Price = detail.Price,
                                PriceCategory = detail.PriceCategory,
                                Subtotal = detail.Subtotal
                            };

                            if (existingDetail == null) await localDb.InsertAsync(mappedDetail);
                            else await localDb.UpdateAsync(mappedDetail);
                        }
                    }
                    saleCount++;
                }
                sb.AppendLine($"✓ Transaksi Nota (Sales): {saleCount} nota ditarik.");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"× Transaksi Nota: {ex.Message}");
            }

            // 5. Pull Payments
            try
            {
                var remotePayments = (await supabase.From<Payment>().Get()).Models;
                int payCount = 0;
                foreach (var remote in remotePayments)
                {
                    var local = await localDb.FindAsync<LocalPayment>(remote.Id.ToString());
                    var mapped = new LocalPayment
                    {
                        Id = remote.Id.ToString(),
                        SaleId = remote.SaleId.ToString(),
                        PaymentDate = remote.PaymentDate,
                        Amount = remote.Amount,
                        PaymentMethod = remote.PaymentMethod,
                        Status = remote.Status,
                        Notes = remote.Notes,
                        IsSynced = true
                    };

                    if (local == null) await localDb.InsertAsync(mapped);
                    else await localDb.UpdateAsync(mapped);
                    payCount++;
                }
                sb.AppendLine($"✓ Pembayaran (Payments): {payCount} record ditarik.");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"× Pembayaran: {ex.Message}");
            }

            // 6. Pull Queues
            await SyncSalesOrdersQueueAsync();
            await SyncVisitLogsQueueAsync();
            sb.AppendLine("✓ Antrean Pesanan & Kunjungan Sales ditarik.");

            return sb.ToString();
        }
    }
}


