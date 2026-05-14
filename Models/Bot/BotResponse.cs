namespace StokBarangMAUI.Models.Bot
{
    /// <summary>
    /// Response dari bot flow. Ada teks utama + opsi suggestion (chip-style)
    /// + state untuk multi-step flow.
    /// </summary>
    public class BotResponse
    {
        /// <summary>Teks yang ditampilkan ke user (markdown-light, emoji rich).</summary>
        public string Text { get; init; } = "";

        /// <summary>Suggestion chips untuk quick reply (e.g. ["1", "2", "Brebes"]).</summary>
        public List<string> Suggestions { get; init; } = new();

        /// <summary>True kalau bot expect user reply lanjutan (multi-step flow).</summary>
        public bool HasPendingState { get; init; }

        /// <summary>Estimasi token output (untuk metering).</summary>
        public int EstimatedTokens => Math.Max(1, Text.Length / 4);

        public static BotResponse Text_(string text) => new() { Text = text };

        public static BotResponse Menu(string text, List<string> suggestions, bool pending = true) =>
            new() { Text = text, Suggestions = suggestions, HasPendingState = pending };

        public static BotResponse Empty() => new() { Text = "" };
    }
}
