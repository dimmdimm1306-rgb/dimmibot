using SQLite;
namespace StokBarangMAUI.Models
{
    public class StockTransaction
    {
        [PrimaryKey, AutoIncrement] public int    Id            { get; set; }
        public string Type          { get; set; } = "MASUK"; // MASUK | KELUAR | TRANSFER
        public int    MaterialId    { get; set; }
        public int    WarehouseId   { get; set; }
        public int    ToWarehouseId { get; set; }
        public int    Qty           { get; set; }
        public string Date          { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        public string Notes         { get; set; } = "";
        public string ProjectName   { get; set; } = "";

        [Ignore] public string MaterialName    { get; set; } = "";
        [Ignore] public string WarehouseName   { get; set; } = "";
        [Ignore] public string ToWarehouseName { get; set; } = "";
        [Ignore] public string TypeColor  => Type == "MASUK" ? "#16A34A" : Type == "KELUAR" ? "#DC2626" : "#2563EB";
        [Ignore] public string TypeBg     => Type == "MASUK" ? "#DCFCE7" : Type == "KELUAR" ? "#FEE2E2" : "#DBEAFE";
        [Ignore] public string QtyDisplay => Type == "MASUK" ? $"+{Qty:N0}" : Type == "KELUAR" ? $"-{Qty:N0}" : $"⇄{Qty:N0}";
        [Ignore] public string SubTitle   => Type == "TRANSFER"
            ? $"{WarehouseName} → {ToWarehouseName}"
            : WarehouseName;
    }
}
