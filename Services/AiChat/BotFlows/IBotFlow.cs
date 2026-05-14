using StokBarangMAUI.Models.Bot;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Contract untuk satu bot flow. Setiap intent map ke 1 flow.
    /// Flow boleh punya multi-step (pakai BotState).
    /// </summary>
    public interface IBotFlow
    {
        /// <summary>Intent yang flow ini handle.</summary>
        BotIntent Handles { get; }

        /// <summary>
        /// Pattern dari decision tree (untuk debugging/logging).
        /// </summary>
        BotPattern Pattern { get; }

        /// <summary>
        /// Eksekusi flow. ctx: data yang sudah di-extract router (e.g. query, segment, dll).
        /// Return BotResponse.
        /// </summary>
        Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx);

        /// <summary>
        /// Resume flow yang lagi pending (multi-step). state: state dari Preferences.
        /// Return null kalau user input gak nyambung dengan state — router akan reset.
        /// </summary>
        Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state);
    }
}
