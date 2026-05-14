using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 6: LLM fallback with tool calling (Pattern E).
    /// Kirim ke LLM dengan tools definition. Kalau LLM panggil tool,
    /// eksekusi via McpClient, kirim result balik, LLM format jawaban.
    /// Max 2 tool-call rounds untuk hemat token.
    /// </summary>
    public class LlmFallbackFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(45) };
        private const int MAX_TOOL_ROUNDS = 2;
        private const int MAX_HISTORY = 6; // 3 user-bot pairs

        // Shared conversation history (pruned)
        private static readonly List<JsonElement> _history = new();

        public LlmFallbackFlow(McpClient mcp) { _mcp = mcp; }

        public BotIntent Handles => BotIntent.ChatGeneral;
        public BotPattern Pattern => BotPattern.LlmFallback;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            var baseUrl = Preferences.Get("ai_base_url", "https://ai.dimmi.online/v1").TrimEnd('/');
            var model = Preferences.Get("ai_model", "gpt-4o-mini");

            // Build messages
            var messages = new List<object>
            {
                new { role = "system", content = McpToolDefinitions.ToolSystemPrompt }
            };

            // Add pruned history
            lock (_history)
            {
                foreach (var h in _history.TakeLast(MAX_HISTORY))
                    messages.Add(h);
            }

            messages.Add(new { role = "user", content = userMessage });

            // Parse tools
            var tools = JsonSerializer.Deserialize<JsonElement>(McpToolDefinitions.ToolsJson);

            try
            {
                string? finalAnswer = null;

                for (int round = 0; round <= MAX_TOOL_ROUNDS; round++)
                {
                    var body = new
                    {
                        model,
                        messages,
                        tools,
                        tool_choice = round < MAX_TOOL_ROUNDS ? "auto" : "none",
                        max_tokens = 800,
                        temperature = 0.3,
                    };

                    var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });

                    var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };

                    Console.WriteLine($"[LlmFallback] Round {round}: sending to {baseUrl}");
                    var resp = await _http.SendAsync(req);
                    var respJson = await resp.Content.ReadAsStringAsync();

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[LlmFallback] HTTP {(int)resp.StatusCode}: {respJson.Substring(0, Math.Min(200, respJson.Length))}");
                        return BotResponse.Text_($"❌ AI error: HTTP {(int)resp.StatusCode}");
                    }

                    using var doc = JsonDocument.Parse(respJson);
                    var choice = doc.RootElement.GetProperty("choices")[0];
                    var message = choice.GetProperty("message");
                    var finishReason = choice.GetProperty("finish_reason").GetString();

                    // Check for tool calls
                    if (finishReason == "tool_calls" || message.TryGetProperty("tool_calls", out var toolCalls))
                    {
                        if (!message.TryGetProperty("tool_calls", out toolCalls))
                            break;

                        // Add assistant message with tool_calls to history
                        messages.Add(JsonSerializer.Deserialize<object>(message.GetRawText())!);

                        // Execute each tool call
                        foreach (var tc in toolCalls.EnumerateArray())
                        {
                            var fnName = tc.GetProperty("function").GetProperty("name").GetString() ?? "";
                            var fnArgs = tc.GetProperty("function").GetProperty("arguments").GetString() ?? "{}";
                            var tcId = tc.GetProperty("id").GetString() ?? "";

                            Console.WriteLine($"[LlmFallback] Tool call: {fnName}({fnArgs})");
                            var toolResult = await ExecuteToolAsync(fnName, fnArgs);

                            messages.Add(new
                            {
                                role = "tool",
                                tool_call_id = tcId,
                                content = toolResult
                            });
                        }
                        continue; // next round with tool results
                    }

                    // Normal text response
                    finalAnswer = message.TryGetProperty("content", out var content)
                        ? content.GetString() ?? ""
                        : "";
                    break;
                }

                if (string.IsNullOrEmpty(finalAnswer))
                    finalAnswer = "🤔 Hmm, aku gak bisa jawab itu sekarang.";

                // Save to history (pruned)
                lock (_history)
                {
                    _history.Add(JsonSerializer.SerializeToElement(new { role = "user", content = userMessage }));
                    _history.Add(JsonSerializer.SerializeToElement(new { role = "assistant", content = finalAnswer }));
                    while (_history.Count > MAX_HISTORY)
                        _history.RemoveAt(0);
                }

                return BotResponse.Text_(finalAnswer);
            }
            catch (TaskCanceledException)
            {
                return BotResponse.Text_("⏱️ Timeout — server AI lama merespons. Coba lagi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LlmFallback] error: {ex.Message}");
                return BotResponse.Text_($"❌ Error AI: {ex.Message}");
            }
        }

        public Task<BotResponse?> ResumeAsync(string m, BotPendingState s) => Task.FromResult<BotResponse?>(null);

        // ── Tool execution ───────────────────────────────────────────

        private async Task<string> ExecuteToolAsync(string toolName, string argsJson)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, string>>(argsJson)
                    ?? new Dictionary<string, string>();

                SheetFilterResult? result = toolName switch
                {
                    "get_progress_resume" => await _mcp.ReadResumeAsync(),
                    "search_site" => await _mcp.SearchSiteResumeAsync(args.GetValueOrDefault("keyword", "")),
                    "get_stok_summary" => await _mcp.ReadAktualStokAsync(),
                    "get_progress_harian" => await _mcp.SearchProgressAsync(
                        args.GetValueOrDefault("keyword"),
                        args.GetValueOrDefault("date_intent"), 20),
                    "get_surat_jalan" => await _mcp.SearchSuratJalanAsync(
                        args.GetValueOrDefault("keyword"),
                        args.GetValueOrDefault("date_intent"), 15),
                    _ => null
                };

                if (result?.Data == null || result.Data.Count == 0)
                    return "Tidak ada data yang ditemukan.";

                // Compact JSON — kirim max 10 rows, strip null values
                var compact = result.Data.Take(10).Select(row =>
                    row.Where(kv => kv.Value != null &&
                        kv.Value.ToString() != "null" &&
                        !string.IsNullOrWhiteSpace(kv.Value.ToString()))
                    .ToDictionary(kv => kv.Key, kv => kv.Value)
                ).ToList();

                return JsonSerializer.Serialize(new
                {
                    total = result.RowsAfterFilter,
                    shown = compact.Count,
                    data = compact
                }, new JsonSerializerOptions { WriteIndented = false });
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static void ClearHistory()
        {
            lock (_history) { _history.Clear(); }
        }
    }
}
