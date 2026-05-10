namespace StokBarangMAUI.Models
{
    // Model untuk menyimpan draft yang menunggu approval dari admin
    public class PendingApproval
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string ProjectId { get; set; } = "";
        public string CreatedBy { get; set; } = ""; // Email user yang buat draft
        public string Type { get; set; } = ""; // "SuratJalan" | "Progress" | "Absensi"
        public string Status { get; set; } = "Pending"; // "Pending" | "Approved" | "Rejected"
        public string ApprovedBy { get; set; } = ""; // Email admin yang approve
        public DateTime? ApprovedAt { get; set; }
        public string RejectionReason { get; set; } = "";
        
        // JSON serialized data dari draft
        public string DraftDataJson { get; set; } = "";
        
        // Summary untuk ditampilkan di list
        public string Summary { get; set; } = "";
    }
}
