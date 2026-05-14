using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.AiChat.BotFlows;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat
{
    /// <summary>
    /// Central orchestrator bot. Pipeline:
    ///   1. Cek pending state, kalau ada → resume flow yang sesuai.
    ///   2. Klasifikasi intent dari user message (BotIntentRouter).
    ///   3. Cari IBotFlow yang handle intent itu, execute.
    ///   4. Kalau flow gak ketemu / intent = ChatGeneral → return null (caller fallback ke LLM).
    /// </summary>
    public class BotEngine
    {
        private readonly McpClient _mcp;
        private readonly Dictionary<BotIntent, IBotFlow> _flows;

        public BotEngine(McpClient mcp)
        {
            _mcp = mcp;
            _flows = new Dictionary<BotIntent, IBotFlow>
            {
                [BotIntent.AlamatGudang] = new AlamatFlow(mcp),
                [BotIntent.ProgresTotal] = new ProgresTotalFlow(mcp),
                [BotIntent.ProgresOutstanding] = new ProgresOutstandingFlow(mcp),
                [BotIntent.ProgresDone] = new ProgresDoneFlow(mcp),
                [BotIntent.ProgresSiteSearch] = new ProgresSiteSearchFlow(mcp),
                [BotIntent.ProgresDate] = new ProgresDateFlow(mcp),
                [BotIntent.StokMaterial] = new StokMaterialFlow(mcp),
                [BotIntent.StokGudang] = new StokGudangFlow(mcp),
                [BotIntent.StokKritis] = new StokKritisFlow(mcp),
                [BotIntent.StokMenu] = new StokMenuFlow(mcp),
                [BotIntent.KebutuhanHomebase] = new KebutuhanFlow(mcp, kurangOnly: false),
                [BotIntent.KebutuhanKurang] = new KebutuhanFlow(mcp, kurangOnly: true),
                [BotIntent.SuratJalanDate] = new SuratJalanFlow(mcp, BotIntent.SuratJalanDate),
                [BotIntent.SuratJalanJenis] = new SuratJalanFlow(mcp, BotIntent.SuratJalanJenis),
                [BotIntent.SuratJalanNomor] = new SuratJalanFlow(mcp, BotIntent.SuratJalanNomor),
                [BotIntent.SuratJalanOrang] = new SuratJalanFlow(mcp, BotIntent.SuratJalanOrang),
                [BotIntent.SuratJalanLatest] = new SuratJalanFlow(mcp, BotIntent.SuratJalanLatest),
                [BotIntent.SuratJalanMenu] = new SuratJalanFlow(mcp, BotIntent.SuratJalanMenu),
                [BotIntent.Weather] = new WeatherFlow(),
                [BotIntent.TimeQuery] = new TimeQueryFlow(),
                [BotIntent.ChatGeneral] = new LlmFallbackFlow(mcp),
                [BotIntent.Unknown] = new LlmFallbackFlow(mcp),
            };
        }

        /// <summary>
        /// Try handle user message. Return null kalau flow gak nge-handle (caller fallback ke LLM).
        /// </summary>
        public async Task<BotResponse?> TryHandleAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return null;

            // ── 1. Resume pending state (multi-step flow) ─────────────
            var pending = BotState.Load();
            if (pending != null && !pending.IsExpired)
            {
                // Map flow name → IBotFlow instance
                if (Enum.TryParse<BotIntent>(pending.FlowName, out var pendingIntent) &&
                    _flows.TryGetValue(pendingIntent, out var pendingFlow))
                {
                    var resumed = await pendingFlow.ResumeAsync(userMessage, pending);
                    if (resumed != null)
                    {
                        if (!resumed.HasPendingState) BotState.Clear();
                        return resumed;
                    }
                    // Flow tidak nge-handle → cancel state, fall through ke classification
                    BotState.Clear();
                }
                else
                {
                    BotState.Clear();
                }
            }

            // ── 2. Classify intent ───────────────────────────────────
            var match = BotIntentRouter.Classify(userMessage);

            // ── 3. System commands (handle di engine, gak butuh flow) ─
            if (match.Intent == BotIntent.Cancel)
            {
                BotState.Clear();
                return BotResponse.Text_("✅ OK, dibatalkan.");
            }

            if (match.Intent == BotIntent.Help)
                return BotResponse.Text_(BuildHelpMenu());

            // ── 4. Execute matched flow ──────────────────────────────
            if (_flows.TryGetValue(match.Intent, out var flow))
            {
                var resp = await flow.ExecuteAsync(userMessage, match.Context);

                // Save state kalau flow expect lanjutan
                if (resp.HasPendingState)
                {
                    var saved = BotState.Load();
                    if (saved == null ||
                        !saved.FlowName.Equals(match.Intent.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        BotState.Save(match.Intent.ToString(), step: "next", ctx: match.Context);
                    }
                }
                else
                    BotState.Clear();

                return resp;
            }

            // ── 5. Intent tanpa flow registered → null (LLM fallback) ─
            return null;
        }

        // ── Help menu ─────────────────────────────────────────────────

        private static string BuildHelpMenu()
        {
            return
                "🤖 BANTUAN BOT — Apa yang bisa kamu tanyain\n\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                "📊 PROGRES\n" +
                "  • `progres total` — overall project\n" +
                "  • `progres yang belum` — site outstanding\n" +
                "  • `progres selesai` — site sudah 100%\n" +
                "  • `site 0244` — cari site tertentu\n" +
                "  • `progres tanggal 17 mei` / `progres kemarin`\n" +
                "  • `progres brebes` — segment tertentu\n\n" +
                "📦 STOK MATERIAL\n" +
                "  • `stok kabel 24c` — material di semua gudang\n" +
                "  • `stok di brebes` — semua material di gudang\n" +
                "  • `stok habis` — yang kritis\n" +
                "  • `cek stok` — menu interaktif\n\n" +
                "📋 KEBUTUHAN MATERIAL (MRF)\n" +
                "  • `kebutuhan brebes` — per homebase\n" +
                "  • `material kurang` — kekurangan project\n\n" +
                "📜 SURAT JALAN\n" +
                "  • `sj terakhir` — 5 SJ paling baru\n" +
                "  • `sj kemarin` / `sj hari ini`\n" +
                "  • `sj masuk` / `sj keluar` / `sj dibawa`\n" +
                "  • `sj-001` — cari nomor SJ\n\n" +
                "🏢 ALAMAT\n" +
                "  • `alamat brebes`\n" +
                "  • `alamat` (semua)\n\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━\n" +
                "Tanya bebas juga boleh — bot akan jawab sebisanya.\n" +
                "Ketik `batal` kalau lagi nyangkut di menu.";
        }
    }
}
