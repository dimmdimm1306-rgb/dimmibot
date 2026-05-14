using System.Text;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 5: Alamat gudang. Pattern A (single-shot).
    ///   - "alamat brebes" → kirim 1 alamat saja
    ///   - "alamat" / "alamat semua" → list semua 6 gudang
    /// </summary>
    public class AlamatFlow : IBotFlow
    {
        private readonly McpClient _mcp;

        public AlamatFlow(McpClient mcp) { _mcp = mcp; }

        public BotIntent Handles => BotIntent.AlamatGudang;
        public BotPattern Pattern => BotPattern.SingleShot;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled)
                return BotResponse.Text_("⚠️ GDrive Reader belum aktif. Aktifkan di AI Settings dulu ya.");

            ctx.TryGetValue("gudang", out var gudangFilter);

            try
            {
                var result = await _mcp.ReadAlamatAsync(gudangFilter);
                if (result?.Data == null || result.Data.Count == 0)
                {
                    return BotResponse.Text_(string.IsNullOrEmpty(gudangFilter)
                        ? "📭 Sheet Alamat kosong."
                        : $"🔍 Alamat gudang `{gudangFilter}` tidak ketemu.");
                }

                return BotResponse.Text_(FormatAlamat(gudangFilter, result.Data));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AlamatFlow] error: {ex.Message}");
                return BotResponse.Text_($"❌ Gagal ambil data alamat: {ex.Message}");
            }
        }

        public Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
            => Task.FromResult<BotResponse?>(null); // single-shot, no resume

        // ── Formatter ───────────────────────────────────────────────

        private static string FormatAlamat(string? filter, List<Dictionary<string, object>> rows)
        {
            var sb = new StringBuilder();
            if (string.IsNullOrEmpty(filter))
            {
                sb.AppendLine("🏢 ALAMAT SEMUA GUDANG");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"🏢 ALAMAT GUDANG {filter.ToUpperInvariant()}");
                sb.AppendLine();
            }

            int shown = 0;
            foreach (var row in rows.Take(10))
            {
                var gudang = BotFormatters.FindCol(row, "GUDANG");
                if (gudang == "-") gudang = BotFormatters.FindCol(row, "Nama");
                var segment = BotFormatters.FindCol(row, "SEGMENT");
                var alamat = BotFormatters.FindCol(row, "ALAMAT");

                if (gudang == "-" && alamat == "-") continue;

                if (shown > 0) sb.AppendLine();
                sb.AppendLine($"📍 {gudang}");
                if (segment != "-" && segment.Length > 1) sb.AppendLine($"   Segment: {segment}");
                if (alamat != "-") sb.AppendLine($"   {alamat}");
                shown++;
            }

            if (shown == 0)
                sb.AppendLine("(Data alamat kosong atau format kolom berubah.)");

            return sb.ToString().TrimEnd();
        }
    }
}
