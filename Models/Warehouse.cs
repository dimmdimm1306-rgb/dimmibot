using SQLite;
namespace StokBarangMAUI.Models
{
    public class Warehouse
    {
        [PrimaryKey, AutoIncrement] public int    Id       { get; set; }
        public string Name     { get; set; } = "";
        public string Location { get; set; } = "";
    }
}
