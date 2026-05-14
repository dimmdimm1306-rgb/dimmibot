using System.Globalization;
using System.Text;
using StokBarangMAUI.Models.Bot;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow: Hari, tanggal, bulan, tahun, jam.
    /// Pakai WIB (UTC+7). 0 LLM tokens.
    /// </summary>
    public class TimeQueryFlow : IBotFlow
    {
        public BotIntent Handles => BotIntent.TimeQuery;
        public BotPattern Pattern => BotPattern.SingleShot;

        public Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            ctx.TryGetValue("subQuery", out var sub);
            var lower = (sub ?? userMessage).ToLowerInvariant();
            var wib = DateTime.UtcNow.AddHours(7);
            var idCulture = new CultureInfo("id-ID");

            var sb = new StringBuilder();

            // Specific queries
            if (lower.Contains("jam") || lower.Contains("waktu") || lower.Contains("pukul"))
            {
                sb.AppendLine($"🕐 Sekarang jam {wib:HH:mm} WIB");
                sb.AppendLine($"📅 {wib.ToString("dddd, dd MMMM yyyy", idCulture)}");
            }
            else if (lower.Contains("hari"))
            {
                sb.AppendLine($"📅 Hari ini **{wib.ToString("dddd", idCulture)}**, {wib.ToString("dd MMMM yyyy", idCulture)}");
                sb.AppendLine($"🕐 Jam {wib:HH:mm} WIB");
            }
            else if (lower.Contains("tanggal") || lower.Contains("tgl"))
            {
                sb.AppendLine($"📅 Tanggal {wib:dd MMMM yyyy}".Replace(":dd", $":{wib.Day}").Replace("dd MMMM", wib.ToString("dd MMMM", idCulture)));
                sb.AppendLine($"Hari {wib.ToString("dddd", idCulture)} · Jam {wib:HH:mm} WIB");
            }
            else if (lower.Contains("bulan"))
            {
                sb.AppendLine($"📅 Bulan {wib.ToString("MMMM yyyy", idCulture)}");
                sb.AppendLine($"Tanggal {wib:dd}, hari {wib.ToString("dddd", idCulture)}");
            }
            else if (lower.Contains("tahun"))
            {
                sb.AppendLine($"📅 Tahun {wib.Year}");
                sb.AppendLine($"{wib.ToString("dddd, dd MMMM yyyy", idCulture)} · {wib:HH:mm} WIB");
            }
            else
            {
                // Generic
                sb.AppendLine($"🕐 {wib.ToString("dddd, dd MMMM yyyy", idCulture)}");
                sb.AppendLine($"⏰ Jam {wib:HH:mm} WIB");
            }

            return Task.FromResult(BotResponse.Text_(sb.ToString().TrimEnd()));
        }

        public Task<BotResponse?> ResumeAsync(string m, BotPendingState s) => Task.FromResult<BotResponse?>(null);
    }
}
