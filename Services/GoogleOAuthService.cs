using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StokBarangMAUI.Services
{
    // OAuth 2.0 PKCE flow untuk Google Sheets + Drive API.
    //
    // SETUP DI GOOGLE CLOUD CONSOLE (sekali saja):
    //   1. https://console.cloud.google.com → buat project (atau pakai existing)
    //   2. APIs & Services → Library → enable "Google Sheets API" + "Google Drive API"
    //   3. APIs & Services → OAuth consent screen
    //      - User type: External
    //      - Scopes: tambah .../auth/spreadsheets dan .../auth/drive.file
    //      - Test users: tambah email kamu (selama mode "Testing")
    //   4. APIs & Services → Credentials → Create Credentials → OAuth client ID
    //      - Type: Android
    //      - Package name:   com.companyname.stokbarangmaui
    //      - SHA-1 fingerprint: dari keystore signing APK (lihat docs MAUI)
    //   5. Copy Client ID, paste ke konstanta ClientId di bawah.
    //
    // REDIRECT URI:
    //   Pakai reverse-DNS dari Client ID → "com.googleusercontent.apps.<ID>:/oauth2redirect"
    //   Atau pakai custom scheme app sendiri (perlu Activity intent filter di AndroidManifest).
    public class GoogleOAuthService
    {
        // Client ID dari Cloud Console (Android type) — package com.companyname.stokbarangmaui.
        public const string ClientId    = "1046374458759-qud9n6fqcibk2372lneg6vjdj0ae7ure.apps.googleusercontent.com";
        // Reverse-DNS dari Client ID (skema yang diterima Google untuk Android OAuth).
        public const string RedirectUri = "com.googleusercontent.apps.1046374458759-qud9n6fqcibk2372lneg6vjdj0ae7ure:/oauth2redirect";

        private const string AuthEndpoint  = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
        // drive (full) needed to list subfolders user already created.
        // For production: switch to drive.file + Picker API to avoid sensitive scope review.
        private const string Scopes        = "https://www.googleapis.com/auth/spreadsheets " +
                                             "https://www.googleapis.com/auth/drive " +
                                             "https://www.googleapis.com/auth/userinfo.email";

        private const string KeyAccess  = "google.access_token";
        private const string KeyRefresh = "google.refresh_token";
        private const string KeyExpiry  = "google.expiry_ticks";
        private const string KeyEmail   = "google.email";

        private readonly HttpClient _http = new();

        public string? AccountEmail => Preferences.Get(KeyEmail, "");
        public bool    IsSignedIn   => !string.IsNullOrWhiteSpace(Preferences.Get(KeyRefresh, ""));

        public bool IsConfigured => !ClientId.StartsWith("REPLACE_WITH");

        // Memulai OAuth flow → buka browser → user grant → kembali ke app via redirect.
        public async Task<(bool Ok, string Msg)> SignInAsync()
        {
            if (!IsConfigured) return (false, "OAuth Client ID belum diset di GoogleOAuthService.cs");

            try
            {
                // PKCE
                var verifier  = GenerateCodeVerifier();
                var challenge = ToBase64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

                var authUrl = $"{AuthEndpoint}" +
                              $"?client_id={Uri.EscapeDataString(ClientId)}" +
                              $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                              $"&response_type=code" +
                              $"&scope={Uri.EscapeDataString(Scopes)}" +
                              $"&access_type=offline" +
                              $"&prompt=consent" +
                              $"&code_challenge={challenge}" +
                              $"&code_challenge_method=S256";

                var result = await WebAuthenticator.Default.AuthenticateAsync(
                    new Uri(authUrl), new Uri(RedirectUri));

                if (!result.Properties.TryGetValue("code", out var code) || string.IsNullOrEmpty(code))
                    return (false, "Tidak dapat code dari Google.");

                // Tukar code → token
                var form = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"]     = ClientId,
                    ["code"]          = code,
                    ["redirect_uri"]  = RedirectUri,
                    ["grant_type"]    = "authorization_code",
                    ["code_verifier"] = verifier,
                });
                var resp = await _http.PostAsync(TokenEndpoint, form);
                var json = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    return (false, $"Token exchange gagal: {json}");

                var tok = JsonSerializer.Deserialize<TokenResponse>(json);
                if (tok?.AccessToken == null) return (false, "Token kosong.");

                Preferences.Set(KeyAccess,  tok.AccessToken);
                Preferences.Set(KeyExpiry,  DateTime.UtcNow.AddSeconds(tok.ExpiresIn - 60).Ticks);
                if (!string.IsNullOrWhiteSpace(tok.RefreshToken))
                    Preferences.Set(KeyRefresh, tok.RefreshToken);

                // Ambil email user
                try
                {
                    var emReq = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
                    emReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok.AccessToken);
                    var emResp = await _http.SendAsync(emReq);
                    if (emResp.IsSuccessStatusCode)
                    {
                        var info = JsonSerializer.Deserialize<UserInfo>(await emResp.Content.ReadAsStringAsync());
                        if (!string.IsNullOrWhiteSpace(info?.Email)) Preferences.Set(KeyEmail, info!.Email);
                    }
                }
                catch { }

                return (true, "Login Google sukses.");
            }
            catch (TaskCanceledException) { return (false, "Login dibatalkan."); }
            catch (Exception ex)          { return (false, $"Error: {ex.Message}"); }
        }

        // Returns access_token; auto-refresh kalau expired.
        public async Task<string?> GetAccessTokenAsync()
        {
            var expiry = Preferences.Get(KeyExpiry, 0L);
            var token  = Preferences.Get(KeyAccess, "");
            if (DateTime.UtcNow.Ticks < expiry && !string.IsNullOrWhiteSpace(token))
                return token;

            var refresh = Preferences.Get(KeyRefresh, "");
            if (string.IsNullOrWhiteSpace(refresh)) return null;

            try
            {
                var form = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"]     = ClientId,
                    ["refresh_token"] = refresh,
                    ["grant_type"]    = "refresh_token",
                });
                var resp = await _http.PostAsync(TokenEndpoint, form);
                if (!resp.IsSuccessStatusCode) return null;
                var tok = JsonSerializer.Deserialize<TokenResponse>(await resp.Content.ReadAsStringAsync());
                if (tok?.AccessToken == null) return null;
                Preferences.Set(KeyAccess, tok.AccessToken);
                Preferences.Set(KeyExpiry, DateTime.UtcNow.AddSeconds(tok.ExpiresIn - 60).Ticks);
                return tok.AccessToken;
            }
            catch { return null; }
        }

        public void SignOut()
        {
            Preferences.Remove(KeyAccess);
            Preferences.Remove(KeyRefresh);
            Preferences.Remove(KeyExpiry);
            Preferences.Remove(KeyEmail);
        }
        
        public async Task SignOutAsync()
        {
            SignOut();
            await Task.CompletedTask;
        }

        // ── helpers ─────────────────────────────────────────────────────
        private static string GenerateCodeVerifier()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return ToBase64Url(bytes);
        }

        private static string ToBase64Url(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]  public string  AccessToken  { get; set; } = "";
            [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
            [JsonPropertyName("expires_in")]    public int     ExpiresIn    { get; set; }
            [JsonPropertyName("token_type")]    public string? TokenType    { get; set; }
        }
        private class UserInfo
        {
            [JsonPropertyName("email")] public string? Email { get; set; }
        }
    }
}
