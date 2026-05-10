using SQLite;
namespace StokBarangMAUI.Models
{
    public class StockBalance
    {
        [PrimaryKey, AutoIncrement] public int Id          { get; set; }
        public int WarehouseId { get; set; }
        public int MaterialId  { get; set; }
        public int Balance     { get; set; }

        [Ignore] public string MaterialName  { get; set; } = "";
        [Ignore] public string WarehouseName { get; set; } = "";
        [Ignore] public string Unit          { get; set; } = "";
        [Ignore] public string Category      { get; set; } = "";
        [Ignore] public int    MinStock      { get; set; }
        [Ignore] public bool   IsLow   => MinStock > 0 && Balance > 0 && Balance <= MinStock;
        [Ignore] public bool   IsEmpty => Balance <= 0;
        [Ignore] public string StatusColor => IsEmpty ? "#DC2626" : IsLow ? "#D97706" : "#16A34A";
        [Ignore] public string StatusText  => IsEmpty ? "Habis"   : IsLow ? "Kurang" : "OK";
        [Ignore] public string StatusBg    => IsEmpty ? "#FEE2E2" : IsLow ? "#FEF3C7" : "#DCFCE7";
    }
}
