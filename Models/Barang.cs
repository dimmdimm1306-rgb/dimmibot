using SQLite;
using System.ComponentModel.DataAnnotations;

namespace StokBarangMAUI.Models
{
    public class Barang
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kode barang harus diisi")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Kode barang harus antara 1-50 karakter")]
        public string KodeBarang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nama barang harus diisi")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Nama barang harus antara 1-100 karakter")]
        public string NamaBarang { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Stok tidak boleh negatif")]
        public int Stok { get; set; }

        public bool IsValid(out List<ValidationResult> results)
        {
            var context = new ValidationContext(this);
            results = new List<ValidationResult>();
            return Validator.TryValidateObject(this, context, results, true);
        }
    }
}