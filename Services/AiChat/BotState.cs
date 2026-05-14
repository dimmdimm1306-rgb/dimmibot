using System.Text.Json;
using System.Text.Json.Serialization;

namespace StokBarangMAUI.Services.AiChat
{
    /// <summary>
    /// State untuk multi-step bot flow. Disimpan di Preferences.
    /// Auto-expire setelah 5 menit. Dibuang otomatis kalau user reset (menu/help/cancel).
    /// </summary>
    public class BotPendingState
    {
        [JsonPropertyName("flow")]
        public string FlowName { get; set; } = "";

        [JsonPropertyName("step")]
        public string Step { get; set; } = "";

        [JsonPropertyName("ctx")]
        public Dictionary<string, string> Context { get; set; } = new();

        [JsonPropertyName("expiry")]
        public DateTime ExpiryUtc { get; set; } = DateTime.UtcNow.AddMinutes(5);

        public bool IsExpired => DateTime.UtcNow >= ExpiryUtc;

        public string? Get(string key) => Context.TryGetValue(key, out var v) ? v : null;
        public int GetInt(string key, int def = 0) =>
            Context.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : def;
    }

    /// <summary>
    /// Wrapper Preferences untuk bot state. Static helper.
    /// </summary>
    public static class BotState
    {
        private const string KEY = "bot_pending_flow_v2";
        private static readonly TimeSpan EXPIRY = TimeSpan.FromMinutes(5);

        private static readonly JsonSerializerOptions _opts = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static BotPendingState? Load()
        {
            var raw = Preferences.Get(KEY, "");
            if (string.IsNullOrWhiteSpace(raw)) return null;
            try
            {
                var state = JsonSerializer.Deserialize<BotPendingState>(raw, _opts);
                if (state == null) return null;
                if (state.IsExpired) { Clear(); return null; }
                return state;
            }
            catch
            {
                Clear();
                return null;
            }
        }

        public static void Save(string flowName, string step, Dictionary<string, string>? ctx = null)
        {
            var state = new BotPendingState
            {
                FlowName = flowName,
                Step = step,
                Context = ctx ?? new(),
                ExpiryUtc = DateTime.UtcNow.Add(EXPIRY),
            };
            var json = JsonSerializer.Serialize(state, _opts);
            Preferences.Set(KEY, json);
        }

        public static void Clear()
        {
            Preferences.Remove(KEY);
        }

        public static bool HasPending() => Load() != null;
    }
}
