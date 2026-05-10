using SQLite;
using StokBarangMAUI.Models;

namespace StokBarangMAUI.Services
{
    public class DashboardStats
    {
        public int TotalWarehouses   { get; set; }
        public int TotalMaterials    { get; set; }
        public int TotalTransactions { get; set; }
        public int LowStockCount     { get; set; }
        public int EmptyStockCount   { get; set; }
        public int TodayTransactions { get; set; }
    }

    public class StockDatabaseService
    {
        private SQLiteAsyncConnection? _db;

        private async Task InitAsync()
        {
            if (_db != null) return;
            var path = Path.Combine(FileSystem.AppDataDirectory, "stockgudang.db");
            _db = new SQLiteAsyncConnection(path);
            await _db.CreateTableAsync<Warehouse>();
            await _db.CreateTableAsync<Material>();
            await _db.CreateTableAsync<StockTransaction>();
            await _db.CreateTableAsync<StockBalance>();
            await SeedAsync();
        }

        private async Task SeedAsync()
        {
            if (await _db!.Table<Warehouse>().CountAsync() > 0) return;
            var warehouses = new[]
            {
                new Warehouse { Name = "BREBES",      Location = "Brebes, Jawa Tengah" },
                new Warehouse { Name = "SRAGEN",      Location = "Sragen, Jawa Tengah" },
                new Warehouse { Name = "PURWOKERTO",  Location = "Banyumas, Jawa Tengah" },
                new Warehouse { Name = "SUKOHARJO",   Location = "Sukoharjo, Jawa Tengah" },
                new Warehouse { Name = "GROBOGAN",    Location = "Grobogan, Jawa Tengah" },
                new Warehouse { Name = "TASIKMALAYA", Location = "Tasikmalaya, Jawa Barat" },
            };
            foreach (var w in warehouses) await _db.InsertAsync(w);

            var materials = new[]
            {
                new Material { Name = "Kabel 24C",         Unit = "meter", Category = "Kabel",    MinStock = 1000 },
                new Material { Name = "Closure 24C",       Unit = "pcs",   Category = "Aksesori", MinStock = 10 },
                new Material { Name = "ODP 8 Port",        Unit = "pcs",   Category = "Aksesori", MinStock = 5 },
                new Material { Name = "Pigtail SC/UPC",    Unit = "pcs",   Category = "Aksesori", MinStock = 10 },
                new Material { Name = "Connector SC/UPC",  Unit = "pcs",   Category = "Aksesori", MinStock = 10 },
                new Material { Name = "Tiang 7m",          Unit = "btg",   Category = "Tiang",    MinStock = 5 },
                new Material { Name = "Tiang 9m",          Unit = "btg",   Category = "Tiang",    MinStock = 5 },
            };
            foreach (var m in materials) await _db.InsertAsync(m);
        }

        // ── Warehouses ──────────────────────────────────────────────────
        public async Task<List<Warehouse>> GetWarehousesAsync()
        {
            await InitAsync();
            return await _db!.Table<Warehouse>().OrderBy(w => w.Name).ToListAsync();
        }

        public async Task SaveWarehouseAsync(Warehouse w)
        {
            await InitAsync();
            if (w.Id == 0) await _db!.InsertAsync(w);
            else           await _db!.UpdateAsync(w);
        }

        public async Task DeleteWarehouseAsync(Warehouse w)
        {
            await InitAsync();
            await _db!.DeleteAsync(w);
            await _db!.Table<StockBalance>().DeleteAsync(b => b.WarehouseId == w.Id);
        }

        // ── Materials ───────────────────────────────────────────────────
        public async Task<List<Material>> GetMaterialsAsync()
        {
            await InitAsync();
            return await _db!.Table<Material>().OrderBy(m => m.Name).ToListAsync();
        }

        public async Task SaveMaterialAsync(Material m)
        {
            await InitAsync();
            if (m.Id == 0) await _db!.InsertAsync(m);
            else           await _db!.UpdateAsync(m);
        }

        public async Task DeleteMaterialAsync(Material m)
        {
            await InitAsync();
            await _db!.DeleteAsync(m);
            await _db!.Table<StockBalance>().DeleteAsync(b => b.MaterialId == m.Id);
        }

        // ── Transactions ─────────────────────────────────────────────────
        public async Task<(bool ok, string msg)> AddTransactionAsync(StockTransaction tx)
        {
            await InitAsync();
            if (tx.Type == "KELUAR" || tx.Type == "TRANSFER")
            {
                var bal = await GetBalanceValueAsync(tx.WarehouseId, tx.MaterialId);
                if (bal < tx.Qty)
                    return (false, tx.Type == "TRANSFER"
                        ? $"Stok gudang asal tidak cukup. Tersedia: {bal:N0}"
                        : $"Stok tidak cukup. Tersedia: {bal:N0}");
            }

            // Atomic: insert + balance adjust wrapped in SQLite transaction
            await _db!.RunInTransactionAsync(conn =>
            {
                conn.Insert(tx);
                if (tx.Type == "MASUK")
                    AdjustBalanceSync(conn, tx.WarehouseId, tx.MaterialId, tx.Qty);
                else if (tx.Type == "KELUAR")
                    AdjustBalanceSync(conn, tx.WarehouseId, tx.MaterialId, -tx.Qty);
                else if (tx.Type == "TRANSFER")
                {
                    AdjustBalanceSync(conn, tx.WarehouseId,   tx.MaterialId, -tx.Qty);
                    AdjustBalanceSync(conn, tx.ToWarehouseId, tx.MaterialId,  tx.Qty);
                }
            });
            return (true, "Transaksi berhasil disimpan.");
        }

        private static void AdjustBalanceSync(SQLite.SQLiteConnection conn, int warehouseId, int materialId, int delta)
        {
            var b = conn.Table<StockBalance>()
                .Where(x => x.WarehouseId == warehouseId && x.MaterialId == materialId)
                .FirstOrDefault();
            if (b == null)
            {
                conn.Insert(new StockBalance { WarehouseId = warehouseId, MaterialId = materialId, Balance = delta });
            }
            else
            {
                b.Balance += delta;
                conn.Update(b);
            }
        }

        private async Task<int> GetBalanceValueAsync(int warehouseId, int materialId)
        {
            var b = await _db!.Table<StockBalance>()
                .Where(x => x.WarehouseId == warehouseId && x.MaterialId == materialId)
                .FirstOrDefaultAsync();
            return b?.Balance ?? 0;
        }

        public async Task<List<StockTransaction>> GetTransactionsAsync(
            int? warehouseId = null, int? materialId = null, string? type = null, int limit = 200)
        {
            await InitAsync();
            var all        = await _db!.Table<StockTransaction>().OrderByDescending(t => t.Id).ToListAsync();
            var warehouses = await GetWarehousesAsync();
            var materials  = await GetMaterialsAsync();

            if (warehouseId.HasValue)
                all = all.Where(t => t.WarehouseId == warehouseId || t.ToWarehouseId == warehouseId).ToList();
            if (materialId.HasValue)
                all = all.Where(t => t.MaterialId == materialId).ToList();
            if (type != null)
                all = all.Where(t => t.Type == type).ToList();

            foreach (var t in all)
            {
                t.MaterialName    = materials.FirstOrDefault(m => m.Id == t.MaterialId)?.Name   ?? "-";
                t.WarehouseName   = warehouses.FirstOrDefault(w => w.Id == t.WarehouseId)?.Name  ?? "-";
                t.ToWarehouseName = warehouses.FirstOrDefault(w => w.Id == t.ToWarehouseId)?.Name ?? "";
            }
            return all.Take(limit).ToList();
        }

        // ── Balances ─────────────────────────────────────────────────────
        public async Task<List<StockBalance>> GetBalancesAsync(int? warehouseId = null)
        {
            await InitAsync();
            var warehouses = await GetWarehousesAsync();
            var materials  = await GetMaterialsAsync();
            var balances   = await _db!.Table<StockBalance>().ToListAsync();

            if (warehouseId.HasValue)
                balances = balances.Where(b => b.WarehouseId == warehouseId).ToList();

            foreach (var b in balances)
            {
                var mat = materials.FirstOrDefault(m => m.Id == b.MaterialId);
                var wh  = warehouses.FirstOrDefault(w => w.Id == b.WarehouseId);
                b.MaterialName  = mat?.Name     ?? "";
                b.Unit          = mat?.Unit      ?? "";
                b.Category      = mat?.Category  ?? "";
                b.MinStock      = mat?.MinStock   ?? 0;
                b.WarehouseName = wh?.Name        ?? "";
            }
            return balances
                .Where(b => !string.IsNullOrEmpty(b.MaterialName))
                .OrderBy(b => b.WarehouseName).ThenBy(b => b.MaterialName)
                .ToList();
        }

        // ── Dashboard ────────────────────────────────────────────────────
        public async Task<DashboardStats> GetStatsAsync()
        {
            await InitAsync();
            var balances = await GetBalancesAsync();
            var txCount  = await _db!.Table<StockTransaction>().CountAsync();
            var today    = DateTime.Now.ToString("dd/MM/yyyy");
            var todayTx  = await _db.Table<StockTransaction>()
                               .Where(t => t.Date.StartsWith(today)).CountAsync();
            return new DashboardStats
            {
                TotalWarehouses   = await _db.Table<Warehouse>().CountAsync(),
                TotalMaterials    = await _db.Table<Material>().CountAsync(),
                TotalTransactions = txCount,
                LowStockCount     = balances.Count(b => b.IsLow),
                EmptyStockCount   = balances.Count(b => b.IsEmpty),
                TodayTransactions = todayTx,
            };
        }

        // ── Export CSV ───────────────────────────────────────────────────
        private static string F(string? s)
        {
            var v = s ?? "";
            return (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
                ? $"\"{v.Replace("\"", "\"\"")}\"" : v;
        }

        public async Task<string> ExportCsvAsync(int? warehouseId = null)
        {
            var balances = await GetBalancesAsync(warehouseId);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Gudang,Material,Satuan,Kategori,Stok,Min Stok,Status");
            foreach (var b in balances)
                sb.AppendLine($"{F(b.WarehouseName)},{F(b.MaterialName)},{F(b.Unit)},{F(b.Category)},{b.Balance},{b.MinStock},{F(b.StatusText)}");
            return sb.ToString();
        }

        public async Task<string> ExportTransactionCsvAsync(int? warehouseId = null)
        {
            var txs = await GetTransactionsAsync(warehouseId: warehouseId, limit: 1000);
            var sb  = new System.Text.StringBuilder();
            sb.AppendLine("Tanggal,Tipe,Material,Gudang Asal,Gudang Tujuan,Qty,Proyek,Catatan");
            foreach (var t in txs)
                sb.AppendLine($"{F(t.Date)},{F(t.Type)},{F(t.MaterialName)},{F(t.WarehouseName)},{F(t.ToWarehouseName)},{t.Qty},{F(t.ProjectName)},{F(t.Notes)}");
            return sb.ToString();
        }
    }
}
