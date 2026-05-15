using System.Text;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 1e: Progres yang sudah selesai (100%+). Mirror dari ProgresOutstandingFlow.
    /// </summary>
    public class ProgresDoneFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        private const int PAGE_SIZE = 5;

        public ProgresDoneFlow(McpClient mcp) { _mcp = mcp; }

        public BotIntent Handles => BotIntent.ProgresDone;
        public BotPattern Pattern => BotPattern.FilterDrillDown;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled)
                return BotResponse.Text_("⚠️ GDrive Reader belum aktif.");

            try
            {
                var result = await _mcp.ResumeBySiteDoneAsync(200);
                if (result?.Data == null || result.Data.Count == 0)
                    return BotResponse.Text_("😅 Belum ada rute yang selesai 100%.");

                var sb = new StringBuilder();
                sb.AppendLine($"✅ RUTE SELESAI — {result.Data.Count} rute sudah 100%");
                sb.AppendLine();

                int shown = 0;
                foreach (var row in result.Data.Take(PAGE_SIZE))
                {
                    var siteId = BotFormatters.FindCol(row, "SITE ID");
                    var rute = BotFormatters.FindCol(row, "Rute");
                    var kota = BotFormatters.FindCol(row, "KAB");
                    sb.AppendLine($"{shown + 1}. ✅ Selesai");
                    if (siteId != "-") sb.AppendLine($"   🆔 Site : {siteId}");
                    sb.AppendLine($"   📌 Rute : {rute}");
                    if (kota != "-") sb.AppendLine($"   📍 Kota : {kota}");
                    sb.AppendLine();
                    shown++;
                }

                if (result.Data.Count > shown)
                    sb.AppendLine($"\n📄 +{result.Data.Count - shown} rute lagi.");

                return BotResponse.Text_(sb.ToString().TrimEnd());
            }
            catch (Exception ex)
            {
                return BotResponse.Text_($"❌ Gagal: {ex.Message}");
            }
        }

        public Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
            => Task.FromResult<BotResponse?>(null);
    }
}
