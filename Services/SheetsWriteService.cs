using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StokBarangMAUI.Services
{
    // Wrapper tipis untuk Google Sheets API v4: append rows ke sheet tertentu.
    public class SheetsWriteService
    {
        private readonly GoogleOAuthService _oauth;
        private readonly HttpClient         _http = new();

        public SheetsWriteService(GoogleOAuthService oauth)
        {
            _oauth = oauth;
        }

        // sheetName = nama tab persis (bukan GID). Misal "Surat Jalan" atau "Progress".
        // rows = list of rows; each row = list of cell values (string/int/double/bool).
        // Pakai null untuk PRESERVE cell existing (jangan overwrite formula autofill).
        public async Task<(bool Ok, string Msg)> AppendRowsAsync(
            string spreadsheetId, string sheetName, List<List<object?>> rows)
        {
            if (string.IsNullOrWhiteSpace(spreadsheetId)) return (false, "Spreadsheet ID kosong.");
            if (string.IsNullOrWhiteSpace(sheetName))     return (false, "Sheet name kosong.");
            if (rows.Count == 0) return (true, "Tidak ada baris.");

            var token = await _oauth.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return (false, "Belum login Google atau token expired.");

            var range = $"{Uri.EscapeDataString(sheetName)}!A1";
            var url = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{range}:append" +
                      $"?valueInputOption=USER_ENTERED&insertDataOption=INSERT_ROWS";

            var body = JsonSerializer.Serialize(new { values = rows });
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var resp = await _http.SendAsync(req);
                if (resp.IsSuccessStatusCode) return (true, "OK");
                var err = await resp.Content.ReadAsStringAsync();
                return (false, $"Sheets API {(int)resp.StatusCode}: {Truncate(err, 200)}");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        private static string Truncate(string s, int max) =>
            s.Length <= max ? s : s[..max] + "…";
    }
}
