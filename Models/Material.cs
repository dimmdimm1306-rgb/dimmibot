using SQLite;
namespace StokBarangMAUI.Models
{
    public class Material
    {
        [PrimaryKey, AutoIncrement] public int    Id       { get; set; }
        public string Name     { get; set; } = "";
        public string Unit     { get; set; } = "pcs";
        public string Category { get; set; } = "";
        public int    MinStock { get; set; } = 0;
    }
}
