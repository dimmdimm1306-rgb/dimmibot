using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using StokBarangMAUI.Models.Bot;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Cuaca via Open-Meteo (gratis, no API key).
    /// Flow:
    ///   - "cuaca [kota]" → langsung jawab
    ///   - "cuaca hari ini" / "cuaca" → tanya kota dulu
    ///   - User reply kota → fetch geocode → forecast → format
    /// </summary>
    public class WeatherFlow : IBotFlow
    {
        private static readonly HttpClient _http = CreateClient();

        private static HttpClient CreateClient()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            c.DefaultRequestHeaders.Add("User-Agent", "FTTH-StokBarang-MAUI/2.9 (cuaca-bot)");
            c.DefaultRequestHeaders.Add("Accept", "application/json");
            return c;
        }

        public BotIntent Handles => BotIntent.Weather;
        public BotPattern Pattern => BotPattern.SingleShot;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            ctx.TryGetValue("city", out var city);

            // Kalau gak ada kota, tanya dulu
            if (string.IsNullOrWhiteSpace(city))
            {
                BotState.Save(nameof(BotIntent.Weather), "askCity", new());
                return new BotResponse
                {
                    Text = "🌤️ Posisi kamu di kota apa?\nKetik nama kota — misalnya `Brebes`, `Jakarta`, atau `Surakarta`.",
                    HasPendingState = true
                };
            }

            return await FetchWeather(city);
        }

        public async Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
        {
            if (state.Step == "askCity")
            {
                var city = userMessage.Trim();
                if (string.IsNullOrWhiteSpace(city) || city.Length > 50) return null;
                BotState.Clear();
                return await FetchWeather(city);
            }
            return null;
        }

        // ── Fetch logic ─────────────────────────────────────────────

        private async Task<BotResponse> FetchWeather(string city)
        {
            try
            {
                Console.WriteLine($"[Weather] Fetching for city='{city}'");

                // 1. Geocode kota → lat/lon (Open-Meteo geocoding API)
                var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=id&format=json";
                Console.WriteLine($"[Weather] Geocoding: {geoUrl}");

                var geoResp = await _http.GetAsync(geoUrl);
                if (!geoResp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Weather] Geocode HTTP {(int)geoResp.StatusCode}");
                    return BotResponse.Text_($"❌ Gagal cari lokasi `{city}` (HTTP {(int)geoResp.StatusCode}). Coba lagi.");
                }

                var geoJson = await geoResp.Content.ReadAsStringAsync();
                using var geoDoc = JsonDocument.Parse(geoJson);
                if (!geoDoc.RootElement.TryGetProperty("results", out var results) ||
                    results.ValueKind != JsonValueKind.Array ||
                    results.GetArrayLength() == 0)
                {
                    return BotResponse.Text_($"🔍 Kota `{city}` tidak ketemu. Coba nama lain misal `Jakarta`, `Surabaya`.");
                }

                var first = results[0];
                var lat = first.GetProperty("latitude").GetDouble();
                var lon = first.GetProperty("longitude").GetDouble();
                var nameFound = first.TryGetProperty("name", out var n) ? n.GetString() : city;
                var admin = first.TryGetProperty("admin1", out var a) ? a.GetString() : null;
                var country = first.TryGetProperty("country", out var c) ? c.GetString() : null;
                Console.WriteLine($"[Weather] Geocoded: {nameFound} ({lat},{lon})");

                // 2. Fetch forecast (current + daily)
                // Pakai InvariantCulture supaya decimal pakai titik bukan koma
                var latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var fcUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}" +
                            $"&current=temperature_2m,relative_humidity_2m,precipitation,weather_code,wind_speed_10m" +
                            $"&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,precipitation_probability_max" +
                            $"&timezone=Asia%2FJakarta&forecast_days=2";
                Console.WriteLine($"[Weather] Forecast: {fcUrl}");

                var fcResp = await _http.GetAsync(fcUrl);
                if (!fcResp.IsSuccessStatusCode)
                {
                    var errBody = await fcResp.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Weather] Forecast HTTP {(int)fcResp.StatusCode}: {errBody}");
                    return BotResponse.Text_($"❌ Cuaca server error (HTTP {(int)fcResp.StatusCode}). Coba `cuaca {city}` lagi 1-2 menit lagi.");
                }

                var fcJson = await fcResp.Content.ReadAsStringAsync();
                using var fcDoc = JsonDocument.Parse(fcJson);
                var current = fcDoc.RootElement.GetProperty("current");
                var daily = fcDoc.RootElement.GetProperty("daily");

                return BotResponse.Text_(FormatWeather(nameFound, admin, country, current, daily));
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[Weather] Timeout: {ex.Message}");
                return BotResponse.Text_("⏱️ Timeout — server cuaca lambat atau koneksi internet HP lemah. Coba lagi nanti.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[Weather] HTTP error: {ex.Message}");
                return BotResponse.Text_($"📡 Tidak bisa akses server cuaca. Cek koneksi internet HP. ({ex.Message})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Weather] Unexpected: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                return BotResponse.Text_($"❌ Error cuaca: {ex.GetType().Name} — {ex.Message}");
            }
        }

        // ── Formatter ───────────────────────────────────────────────

        private static string FormatWeather(string? name, string? admin, string? country,
            JsonElement current, JsonElement daily)
        {
            var temp = current.GetProperty("temperature_2m").GetDouble();
            var rh = current.GetProperty("relative_humidity_2m").GetDouble();
            var precip = current.GetProperty("precipitation").GetDouble();
            var code = current.GetProperty("weather_code").GetInt32();
            var wind = current.GetProperty("wind_speed_10m").GetDouble();

            var (icon, desc) = WeatherCodeToText(code);

            var sb = new StringBuilder();
            sb.AppendLine($"{icon} CUACA {name?.ToUpperInvariant()}");
            if (!string.IsNullOrEmpty(admin)) sb.AppendLine($"📍 {admin}{(country != null ? ", " + country : "")}");
            sb.AppendLine();
            sb.AppendLine($"Saat ini: {desc}");
            sb.AppendLine($"🌡️  {temp:N1}°C · 💧 {rh:N0}% · 💨 {wind:N0} km/j");
            if (precip > 0) sb.AppendLine($"🌧️  Hujan {precip:N1} mm");
            sb.AppendLine();

            // Daily forecast (today + tomorrow)
            var dailyCode = daily.GetProperty("weather_code");
            var dailyMax = daily.GetProperty("temperature_2m_max");
            var dailyMin = daily.GetProperty("temperature_2m_min");
            var dailyPrecip = daily.GetProperty("precipitation_sum");
            var dailyProb = daily.GetProperty("precipitation_probability_max");

            for (int i = 0; i < Math.Min(2, dailyCode.GetArrayLength()); i++)
            {
                var label = i == 0 ? "Hari ini" : "Besok";
                var (dIcon, dDesc) = WeatherCodeToText(dailyCode[i].GetInt32());
                var dMax = dailyMax[i].GetDouble();
                var dMin = dailyMin[i].GetDouble();
                var dPrecip = dailyPrecip[i].GetDouble();
                int? dProb = dailyProb[i].ValueKind == JsonValueKind.Number ? dailyProb[i].GetInt32() : null;

                sb.AppendLine($"{dIcon} {label}: {dDesc}");
                sb.Append($"   🔥 {dMax:N0}° / ❄️ {dMin:N0}°");
                if (dProb.HasValue) sb.Append($" · ☔ {dProb.Value}%");
                sb.AppendLine();
                if (dPrecip > 0) sb.AppendLine($"   Total hujan: {dPrecip:N1} mm");
            }

            return sb.ToString().TrimEnd();
        }

        // WMO weather code → emoji + Indonesian description
        // Reference: https://open-meteo.com/en/docs
        private static (string icon, string desc) WeatherCodeToText(int code) => code switch
        {
            0 => ("☀️", "Cerah"),
            1 => ("🌤️", "Cerah berawan"),
            2 => ("⛅", "Berawan sebagian"),
            3 => ("☁️", "Mendung"),
            45 or 48 => ("🌫️", "Berkabut"),
            51 or 53 or 55 => ("🌦️", "Gerimis"),
            56 or 57 => ("🌨️", "Gerimis dingin"),
            61 => ("🌧️", "Hujan ringan"),
            63 => ("🌧️", "Hujan sedang"),
            65 => ("🌧️", "Hujan deras"),
            66 or 67 => ("🌨️", "Hujan dingin"),
            71 or 73 or 75 => ("❄️", "Salju"),
            77 => ("❄️", "Butiran salju"),
            80 => ("🌦️", "Hujan ringan setempat"),
            81 => ("🌧️", "Hujan setempat"),
            82 => ("⛈️", "Hujan deras setempat"),
            85 or 86 => ("🌨️", "Salju setempat"),
            95 => ("⛈️", "Petir"),
            96 or 99 => ("⛈️", "Petir + hujan es"),
            _ => ("🌡️", $"Kondisi tidak diketahui (code {code})"),
        };
    }
}
