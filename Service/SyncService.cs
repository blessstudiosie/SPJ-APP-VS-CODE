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
            var remoteIds = remoteList.Select(item => item.Id).ToHashSet();
            var remoteByName = remoteList.ToDictionary(item => item.Name, item => item, StringComparer.OrdinalIgnoreCase);

            foreach (var local in localItems)
            {
                if (!Guid.TryParse(local.Id, out var id))
                    throw new InvalidOperationException($"ID sales person tidak valid: '{local.Id}'.");

                // Kalau ID lokal tidak ada di server tapi NAMA sudah ada (misal karena database lokal
                // pernah di-reset lalu data ini dibuat ulang), adopsi ID milik server - jangan insert baru.
                if (!remoteIds.Contains(id) && remoteByName.TryGetValue(local.Name, out var existingByName))
                {
                    await ReassignLocalSalesPersonIdAsync(localDb, local, existingByName.Id);
                    id = existingByName.Id;
                }

                var remote = new SalesPerson { Id = id, Name = local.Name, Phone = local.Phone, Email = local.Email, TargetOmset = local.TargetOmset, Role = local.Role, Password = local.Password };
                if (remoteIds.Contains(id)) await supabase.From<SalesPerson>().Update(remote);
                else await supabase.From<SalesPerson>().Insert(remote);
                local.IsSynced = true;
                await localDb.UpdateAsync(local);
            }
            return localItems.Count;
        }

        private static async Task ReassignLocalSalesPersonIdAsync(SQLiteAsyncConnection localDb, LocalSalesPerson local, Guid newId)
        {
            string oldId = local.Id;
            string newIdStr = newId.ToString();

            await localDb.RunInTransactionAsync(conn =>
            {
                var customers = conn.Table<LocalCustomer>().Where(c => c.SalesPersonId == oldId).ToList();
                foreach (var c in customers) { c.SalesPersonId = newIdStr; conn.Update(c); }

                var sales = conn.Table<LocalSale>().Where(s => s.SalesPersonId == oldId).ToList();
                foreach (var s in sales) { s.SalesPersonId = newIdStr; conn.Update(s); }

                var deliveries = conn.Table<LocalDelivery>().ToList();
                foreach (var d in deliveries)
                {
                    bool changed = false;
                    if (d.DriverId == oldId) { d.DriverId = newIdStr; changed = true; }
                    if (d.HelperId == oldId) { d.HelperId = newIdStr; changed = true; }
                    if (d.CheckerId == oldId) { d.CheckerId = newIdStr; changed = true; }
                    if (changed) conn.Update(d);
                }

                conn.Delete(local);
                local.Id = newIdStr;
                conn.InsertOrReplace(local);
            });
        }
        public static async Task<int> SyncCustomersAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var supabase = await SupabaseService.GetClient();
            var localItems = await localDb.Table<LocalCustomer>().ToListAsync();
            var remoteList = (await supabase.From<Customer>().Get()).Models;
            var remoteIds = remoteList.Select(item => item.Id).ToHashSet();
            var remoteByName = remoteList.ToDictionary(item => item.Name, item => item, StringComparer.OrdinalIgnoreCase);

            foreach (var local in localItems)
            {
                if (!Guid.TryParse(local.Id, out var id))
                    throw new InvalidOperationException($"ID customer tidak valid: '{local.Id}'.");

                if (!remoteIds.Contains(id) && remoteByName.TryGetValue(local.Name, out var existingByName))
                {
                    await ReassignLocalCustomerIdAsync(localDb, local, existingByName.Id);
                    id = existingByName.Id;
                }

                var remote = new Customer
                {
                    Id = id,
                    Name = local.Name,
                    OwnerName = local.OwnerName,
                    Phone = local.Phone,
                    Address = local.Address,
                    JalurPengiriman = local.JalurPengiriman,
                    Latitude = local.Latitude,
                    Longitude = local.Longitude,
                    SalesPersonId = ParseOptionalGuid(local.SalesPersonId, "sales person customer"),
                    LimitPiutang = local.LimitPiutang
                };
                if (remoteIds.Contains(id)) await supabase.From<Customer>().Update(remote);
                else await supabase.From<Customer>().Insert(remote);
                local.IsSynced = true;
                await localDb.UpdateAsync(local);
            }
            return localItems.Count;
        }

        private static async Task ReassignLocalCustomerIdAsync(SQLiteAsyncConnection localDb, LocalCustomer local, Guid newId)
        {
            string oldId = local.Id;
            string newIdStr = newId.ToString();

            await localDb.RunInTransactionAsync(conn =>
            {
                var sales = conn.Table<LocalSale>().Where(s => s.CustomerId == oldId).ToList();
                foreach (var s in sales) { s.CustomerId = newIdStr; conn.Update(s); }

                conn.Delete(local);
                local.Id = newIdStr;
                conn.InsertOrReplace(local);
            });
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
            var remoteSales = (await supabase.From<Sale>().Get()).Models.ToDictionary(sale => sale.Id);
            var remoteDetails = (await supabase.From<SaleDetail>().Get()).Models
                .GroupBy(detail => detail.SaleId)
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var localSale in localSales)
            {
                if (!Guid.TryParse(localSale.Id, out var saleId))
                    throw new InvalidOperationException($"ID penjualan lokal tidak valid: '{localSale.Id}'.");

                var remoteSale = ToRemoteSale(localSale, saleId);
                if (remoteSales.ContainsKey(saleId))
                    await supabase.From<Sale>().Update(remoteSale);
                else
                    await supabase.From<Sale>().Insert(remoteSale);

                var detailsForSale = localDetails.Where(detail => detail.SaleId == localSale.Id).ToList();
                if (remoteDetails.TryGetValue(saleId, out var existingDetails))
                {
                    foreach (var existingDetail in existingDetails)
                        await supabase.From<SaleDetail>().Delete(existingDetail);
                }

                var remoteSaleDetails = detailsForSale.Select(ToRemoteSaleDetail).ToList();
                if (remoteSaleDetails.Count > 0)
                    await supabase.From<SaleDetail>().Insert(remoteSaleDetails);

                localSale.IsSynced = true;
                await localDb.UpdateAsync(localSale);
                result.SalesPushed++;
                result.DetailsPushed += remoteSaleDetails.Count;
            }

            return result;
        }

        public static async Task<int> SyncPaymentsAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var supabase = await SupabaseService.GetClient();
            var localItems = await localDb.Table<LocalPayment>().ToListAsync();
            var remoteIds = (await supabase.From<Payment>().Get()).Models.Select(item => item.Id).ToHashSet();

            foreach (var local in localItems)
            {
                if (!Guid.TryParse(local.Id, out var id) || !Guid.TryParse(local.SaleId, out var saleId))
                    throw new InvalidOperationException($"Pembayaran '{local.Id}' memiliki ID yang tidak valid.");
                var remote = new Payment { Id = id, SaleId = saleId, PaymentDate = local.PaymentDate, Amount = local.Amount, PaymentMethod = local.PaymentMethod, Status = local.Status, Notes = local.Notes };
                if (remoteIds.Contains(id)) await supabase.From<Payment>().Update(remote);
                else await supabase.From<Payment>().Insert(remote);
                local.IsSynced = true;
                await localDb.UpdateAsync(local);
            }
            return localItems.Count;
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
            var remoteById = remoteResponse.Models.ToDictionary(product => product.Id);

            foreach (var localProduct in localProducts.Where(product => !product.IsSynced))
            {
                if (!Guid.TryParse(localProduct.Id, out var productId))
                    continue;

                var remoteProduct = ToRemoteProduct(localProduct, productId);
                if (remoteById.TryGetValue(productId, out var currentRemote))
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
                var localProduct = localProducts.FirstOrDefault(product => product.Id == remoteProduct.Id.ToString());
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
            if (!Guid.TryParse(localProduct.Id, out var productId))
                throw new ArgumentException("ID produk lokal tidak valid.", nameof(localProduct));

            var supabase = await SupabaseService.GetClient();
            var remoteProduct = ToRemoteProduct(localProduct, productId);
            await supabase.From<Product>().Update(remoteProduct);

            var localDb = await LocalDatabaseService.GetConnection();
            localProduct.IsSynced = true;
            localProduct.LastSyncedUpdatedAt = null;
            await localDb.UpdateAsync(localProduct);
        }

        public static async Task AcceptServerVersionAsync(Product serverProduct)
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var localProduct = await localDb.FindAsync<LocalProduct>(serverProduct.Id.ToString());
            var mappedProduct = ToLocalProduct(serverProduct, localProduct);

            if (localProduct is null)
                await localDb.InsertAsync(mappedProduct);
            else
                await localDb.UpdateAsync(mappedProduct);
        }

        private static bool HasServerChanged(LocalProduct local, Product remote) =>
            local.LastSyncedUpdatedAt.HasValue && remote.UpdatedAt.HasValue &&
            remote.UpdatedAt.Value > local.LastSyncedUpdatedAt.Value;

        private static Product ToRemoteProduct(LocalProduct local, Guid id) => new()
        {
            Id = id,
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
            Id = remote.Id.ToString(),
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

        private static Sale ToRemoteSale(LocalSale local, Guid id) => new()
        {
            Id = id,
            Nota = local.Nota,
            CustomerId = ParseOptionalGuid(local.CustomerId, "customer"),
            OrderDate = local.OrderDate,
            DeliveryDate = local.DeliveryDate,
            Status = local.Status,
            SalesPersonId = ParseOptionalGuid(local.SalesPersonId, "sales person"),
            Total = local.Total,
            Paid = local.Paid,
            Remaining = local.Remaining,
            Description = local.Description
        };

        private static SaleDetail ToRemoteSaleDetail(LocalSalesDetail local)
        {
            if (!Guid.TryParse(local.Id, out var id) ||
                !Guid.TryParse(local.SaleId, out var saleId) ||
                !Guid.TryParse(local.ProductId, out var productId))
            {
                throw new InvalidOperationException($"Detail penjualan '{local.Id}' memiliki ID yang tidak valid.");
            }

            return new SaleDetail
            {
                Id = id,
                SaleId = saleId,
                ProductId = productId,
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
    }
}
