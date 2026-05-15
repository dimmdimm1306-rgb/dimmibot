using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using StokBarangMAUI.Models.Bot;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Cuaca via Open-Meteo (gratis, no API key).
    /// Bisa jawab: cuaca sekarang, besok, 7 hari, hujan jam berapa, prediksi cuaca.
    /// </summary>
    public class WeatherFlow : IBotFlow
    {
        private static readonly HttpClient _http = CreateClient();

        private static HttpClient CreateClient()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            c.DefaultRequestHeaders.Add("User-Agent", "FTTH-StokBarang-MAUI/3.2 (cuaca-indonesia)");
            c.DefaultRequestHeaders.Add("Accept", "application/json");
            return c;
        }

        public BotIntent Handles => BotIntent.Weather;
        public BotPattern Pattern => BotPattern.SingleShot;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            ctx.TryGetValue("city", out var city);
            var mode = DetectMode(userMessage);

            if (string.IsNullOrWhiteSpace(city))
            {
                BotState.Save(nameof(BotIntent.Weather), "askCity",
                    new Dictionary<string, string> { ["mode"] = mode });
                return new BotResponse
                {
                    Text = "🌤️ Posisi kamu di kota/kabupaten mana?\n" +
                           "Contoh: `Brebes`, `Ciamis`, `Sragen`, `Jakarta`.\n\n" +
                           "Saya bisa cek cuaca sekarang, besok, prediksi 7 hari, dan peluang hujan per jam.",
                    HasPendingState = true
                };
            }

            return await FetchWeather(city, mode);
        }

        public async Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
        {
            if (state.Step == "askCity")
            {
                var city = userMessage.Trim();
                if (string.IsNullOrWhiteSpace(city) || city.Length > 50) return null;
                var mode = state.Get("mode") ?? "today";
                BotState.Clear();
                return await FetchWeather(city, mode);
            }
            return null;
        }

        private static string DetectMode(string message)
        {
            var lower = (message ?? "").ToLowerInvariant();
            if (Regex.IsMatch(lower, @"\b(7\s*hari|seminggu|minggu\s+ini|prediksi|forecast|prakiraan)\b")) return "week";
            if (Regex.IsMatch(lower, @"\b(besok|tomorrow)\b")) return "tomorrow";
            if (Regex.IsMatch(lower, @"\b(jam\s+berapa|per\s+jam|hourly|nanti\s+(sore|malam|siang)|hujan\s+jam)\b")) return "hourly";
            return "today";
        }

        private async Task<BotResponse> FetchWeather(string city, string mode)
        {
            try
            {
                var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=3&language=id&format=json";
                var geoResp = await _http.GetAsync(geoUrl);
                if (!geoResp.IsSuccessStatusCode)
                    return BotResponse.Text_($"❌ Gagal cari lokasi `{city}` (HTTP {(int)geoResp.StatusCode}).");

                using var geoDoc = JsonDocument.Parse(await geoResp.Content.ReadAsStringAsync());
                if (!geoDoc.RootElement.TryGetProperty("results", out var results) ||
                    results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
                    return BotResponse.Text_($"🔍 Lokasi `{city}` tidak ketemu. Coba nama kabupaten/kota lebih lengkap.");

                // Prioritaskan Indonesia kalau ada beberapa hasil.
                JsonElement first = results[0];
                foreach (var r in results.EnumerateArray())
                {
                    if (r.TryGetProperty("country_code", out var cc) &&
                        cc.GetString()?.Equals("ID", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        first = r;
                        break;
                    }
                }

                var lat = first.GetProperty("latitude").GetDouble();
                var lon = first.GetProperty("longitude").GetDouble();
                var nameFound = first.TryGetProperty("name", out var n) ? n.GetString() : city;
                var admin1 = first.TryGetProperty("admin1", out var a1) ? a1.GetString() : null;
                var admin2 = first.TryGetProperty("admin2", out var a2) ? a2.GetString() : null;
                var country = first.TryGetProperty("country", out var c) ? c.GetString() : null;

                var latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var fcUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}" +
                            "&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,weather_code,wind_speed_10m" +
                            "&hourly=temperature_2m,precipitation_probability,precipitation,weather_code,wind_speed_10m" +
                            "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,precipitation_probability_max,wind_speed_10m_max" +
                            "&timezone=Asia%2FJakarta&forecast_days=7";

                var fcResp = await _http.GetAsync(fcUrl);
                if (!fcResp.IsSuccessStatusCode)
                {
                    var body = await fcResp.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Weather] Forecast error {(int)fcResp.StatusCode}: {body}");
                    return BotResponse.Text_($"❌ Gagal ambil cuaca (HTTP {(int)fcResp.StatusCode}). Coba lagi nanti.");
                }

                using var fcDoc = JsonDocument.Parse(await fcResp.Content.ReadAsStringAsync());
                return BotResponse.Text_(FormatWeather(nameFound, admin2, admin1, country,
                    fcDoc.RootElement.GetProperty("current"),
                    fcDoc.RootElement.GetProperty("hourly"),
                    fcDoc.RootElement.GetProperty("daily"), mode));
            }
            catch (TaskCanceledException)
            {
                return BotResponse.Text_("⏱️ Timeout — server cuaca/koneksi HP lambat. Coba ulang.");
            }
            catch (HttpRequestException ex)
            {
                return BotResponse.Text_($"📡 Tidak bisa akses server cuaca. Cek internet HP. ({ex.Message})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Weather] {ex}");
                return BotResponse.Text_($"❌ Error cuaca: {ex.GetType().Name} — {ex.Message}");
            }
        }

        private static string FormatWeather(string? name, string? admin2, string? admin1, string? country,
            JsonElement current, JsonElement hourly, JsonElement daily, string mode)
        {
            var temp = current.GetProperty("temperature_2m").GetDouble();
            var feels = current.GetProperty("apparent_temperature").GetDouble();
            var rh = current.GetProperty("relative_humidity_2m").GetDouble();
            var precip = current.GetProperty("precipitation").GetDouble();
            var code = current.GetProperty("weather_code").GetInt32();
            var wind = current.GetProperty("wind_speed_10m").GetDouble();
            var (icon, desc) = WeatherCodeToText(code);

            var sb = new StringBuilder();
            sb.AppendLine($"{icon} CUACA {name?.ToUpperInvariant()}");
            var loc = string.Join(", ", new[] { admin2, admin1, country }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (!string.IsNullOrEmpty(loc)) sb.AppendLine($"📍 {loc}");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"Sekarang: {desc}");
            sb.AppendLine($"🌡️ Suhu      : {temp:N1}°C (terasa {feels:N1}°C)");
            sb.AppendLine($"💧 Lembap    : {rh:N0}%");
            sb.AppendLine($"💨 Angin     : {wind:N0} km/jam");
            sb.AppendLine($"🌧️ Hujan now : {precip:N1} mm");
            sb.AppendLine();

            if (mode == "hourly") AppendHourly(sb, hourly, 12);
            else if (mode == "week") AppendDaily(sb, daily, 7);
            else if (mode == "tomorrow") AppendDaily(sb, daily, 2, onlyTomorrow: true);
            else
            {
                AppendDaily(sb, daily, 2);
                sb.AppendLine();
                AppendHourly(sb, hourly, 6);
            }

            sb.AppendLine();
            sb.AppendLine(BuildAdvice(current, hourly, daily));
            return sb.ToString().TrimEnd();
        }

        private static void AppendDaily(StringBuilder sb, JsonElement daily, int days, bool onlyTomorrow = false)
        {
            var codes = daily.GetProperty("weather_code");
            var max = daily.GetProperty("temperature_2m_max");
            var min = daily.GetProperty("temperature_2m_min");
            var rain = daily.GetProperty("precipitation_sum");
            var prob = daily.GetProperty("precipitation_probability_max");
            var wind = daily.GetProperty("wind_speed_10m_max");

            sb.AppendLine(onlyTomorrow ? "📅 PREDIKSI BESOK" : "📅 PREDIKSI HARIAN");
            int start = onlyTomorrow ? 1 : 0;
            int end = Math.Min(days, codes.GetArrayLength());
            for (int i = start; i < end; i++)
            {
                var label = i == 0 ? "Hari ini" : i == 1 ? "Besok" : $"H+{i}";
                var (ic, tx) = WeatherCodeToText(codes[i].GetInt32());
                sb.AppendLine($"{ic} {label}: {tx}");
                sb.AppendLine($"   Suhu {min[i].GetDouble():N0}-{max[i].GetDouble():N0}°C · Hujan {prob[i].GetInt32()}% · {rain[i].GetDouble():N1} mm · Angin {wind[i].GetDouble():N0} km/j");
            }
        }

        private static void AppendHourly(StringBuilder sb, JsonElement hourly, int count)
        {
            var time = hourly.GetProperty("time");
            var prob = hourly.GetProperty("precipitation_probability");
            var rain = hourly.GetProperty("precipitation");
            var codes = hourly.GetProperty("weather_code");
            var temps = hourly.GetProperty("temperature_2m");

            var now = DateTime.UtcNow.AddHours(7);
            int start = 0;
            for (int i = 0; i < time.GetArrayLength(); i++)
            {
                if (DateTime.TryParse(time[i].GetString(), out var t) && t >= now.AddHours(-1))
                {
                    start = i;
                    break;
                }
            }

            sb.AppendLine("🕐 PREDIKSI PER JAM");
            int end = Math.Min(start + count, time.GetArrayLength());
            for (int i = start; i < end; i++)
            {
                var tLabel = DateTime.TryParse(time[i].GetString(), out var t) ? t.ToString("HH:mm") : time[i].GetString();
                var (ic, tx) = WeatherCodeToText(codes[i].GetInt32());
                sb.AppendLine($"{ic} {tLabel} · {temps[i].GetDouble():N0}°C · hujan {prob[i].GetInt32()}% · {rain[i].GetDouble():N1}mm · {tx}");
            }
        }

        private static string BuildAdvice(JsonElement current, JsonElement hourly, JsonElement daily)
        {
            var todayProb = daily.GetProperty("precipitation_probability_max")[0].GetInt32();
            var todayRain = daily.GetProperty("precipitation_sum")[0].GetDouble();
            var wind = current.GetProperty("wind_speed_10m").GetDouble();
            if (todayProb >= 70 || todayRain >= 10)
                return "💡 Saran kerja lapangan: peluang hujan tinggi. Siapkan jas hujan, cover material, dan prioritaskan pekerjaan indoor/administrasi kalau awan mulai gelap.";
            if (todayProb >= 40 || todayRain > 0)
                return "💡 Saran kerja lapangan: ada potensi hujan. Aman kerja, tapi pantau langit dan amankan kabel/material dari air.";
            if (wind >= 25)
                return "💡 Saran kerja lapangan: angin cukup kencang. Hati-hati pekerjaan tiang/ketinggian.";
            return "💡 Saran kerja lapangan: cuaca relatif aman. Tetap pantau perubahan lokal, terutama sore/malam.";
        }

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
