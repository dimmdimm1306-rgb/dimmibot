using SQLite;
using StokBarangMAUI.Models;

namespace StokBarangMAUI.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _db;

private async Task<SQLiteAsyncConnection> Init()
{
    if (_db != null) return _db;
    var dbPath = Path.Combine(FileSystem.AppDataDirectory, "stokbarang.db3");
    _db = new SQLiteAsyncConnection(dbPath);
    await _db.CreateTableAsync<Barang>();
    return _db;
}

public async Task<List<Barang>> GetAllAsync()
{
    var db = await Init();
    return await db.Table<Barang>().ToListAsync();
}

public async Task<Barang?> GetByIdAsync(int id)
{
    var db = await Init();
    return await db.Table<Barang>().Where(b => b.Id == id).FirstOrDefaultAsync();
}

public async Task<int> SaveAsync(Barang barang)
{
    var db = await Init();
    if (barang.Id == 0)
        return await db.InsertAsync(barang);
    return await db.UpdateAsync(barang);
}

public async Task<int> DeleteAsync(Barang barang)
{
    var db = await Init();
    return await db.DeleteAsync(barang);
}
    }
}
