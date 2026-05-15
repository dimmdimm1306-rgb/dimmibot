using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using StokBarangMAUI.Models.Bot;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow: Hari, tanggal, bulan, tahun, jam + event kalender Indonesia.
    /// Pakai WIB (UTC+7). 0 LLM tokens.
    ///
    /// Bisa jawab:
    /// - hari/tanggal/jam sekarang
    /// - besok/kemarin/lusa tanggal berapa
    /// - hari ini ada event apa
    /// - tahun ini ada berapa event
    /// - event bulan ini / minggu ini
    /// </summary>
    public class TimeQueryFlow : IBotFlow
    {
        public BotIntent Handles => BotIntent.TimeQuery;
        public BotPattern Pattern => BotPattern.SingleShot;

        public Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            ctx.TryGetValue("subQuery", out var sub);
            var lower = (sub ?? userMessage).ToLowerInvariant();
            var now = DateTime.UtcNow.AddHours(7);
            var id = new CultureInfo("id-ID");
            var target = ResolveTargetDate(lower, now);
            var events = CalendarEvents.ForYear(target.Year);

            var sb = new StringBuilder();

            // Event queries
            if (HasEventIntent(lower))
            {
                if (Regex.IsMatch(lower, @"\b(setahun|tahun\s+ini|1\s+tahun|tahun\s+\d{4})\b"))
                    return Task.FromResult(BotResponse.Text_(FormatYearEvents(target.Year, events, id)));

                if (Regex.IsMatch(lower, @"\b(bulan\s+ini|bulan\s+apa|bulan\s+\w+)\b") && !lower.Contains("tanggal"))
                    return Task.FromResult(BotResponse.Text_(FormatMonthEvents(target, events, id)));

                if (Regex.IsMatch(lower, @"\b(minggu\s+ini|pekan\s+ini|seminggu)\b"))
                    return Task.FromResult(BotResponse.Text_(FormatWeekEvents(target, events, id)));

                return Task.FromResult(BotResponse.Text_(FormatDayEvents(target, events, id, lower)));
            }

            // Specific time/date questions
            if (lower.Contains("jam") || lower.Contains("waktu") || lower.Contains("pukul"))
            {
                sb.AppendLine($"🕐 Sekarang jam {now:HH:mm} WIB");
                sb.AppendLine($"📅 {now.ToString("dddd, dd MMMM yyyy", id)}");
                AppendTodayEventSummary(sb, now, events, id);
            }
            else if (lower.Contains("besok") || lower.Contains("kemarin") || lower.Contains("lusa"))
            {
                sb.AppendLine($"📅 {LabelForRelative(lower)} adalah {target.ToString("dddd, dd MMMM yyyy", id)}");
                sb.AppendLine($"🕐 Sekarang {now:HH:mm} WIB");
                AppendTargetEventSummary(sb, target, events, id);
            }
            else if (lower.Contains("hari"))
            {
                sb.AppendLine($"📅 Hari ini {now.ToString("dddd", id)}");
                sb.AppendLine($"🗓️ Tanggal {now.ToString("dd MMMM yyyy", id)}");
                sb.AppendLine($"🕐 Jam {now:HH:mm} WIB");
                AppendTodayEventSummary(sb, now, events, id);
            }
            else if (lower.Contains("tanggal") || lower.Contains("tgl"))
            {
                sb.AppendLine($"📅 Tanggal {target.ToString("dd MMMM yyyy", id)}");
                sb.AppendLine($"📌 Hari {target.ToString("dddd", id)}");
                sb.AppendLine($"🕐 Sekarang {now:HH:mm} WIB");
                AppendTargetEventSummary(sb, target, events, id);
            }
            else if (lower.Contains("bulan"))
            {
                sb.AppendLine($"📅 Bulan {now.ToString("MMMM yyyy", id)}");
                sb.AppendLine($"Hari ini {now.ToString("dddd", id)}, tanggal {now:dd}");
                AppendMonthEventSummary(sb, now, events, id);
            }
            else if (lower.Contains("tahun"))
            {
                sb.AppendLine($"📅 Tahun {now.Year}");
                sb.AppendLine($"Hari ini {now.ToString("dddd, dd MMMM yyyy", id)} · {now:HH:mm} WIB");
                sb.AppendLine();
                sb.AppendLine(FormatYearEventCount(now.Year, events));
            }
            else
            {
                sb.AppendLine($"📅 {now.ToString("dddd, dd MMMM yyyy", id)}");
                sb.AppendLine($"🕐 Jam {now:HH:mm} WIB");
                AppendTodayEventSummary(sb, now, events, id);
            }

            return Task.FromResult(BotResponse.Text_(sb.ToString().TrimEnd()));
        }

        public Task<BotResponse?> ResumeAsync(string m, BotPendingState s) => Task.FromResult<BotResponse?>(null);

        private static bool HasEventIntent(string lower)
            => Regex.IsMatch(lower, @"\b(event|agenda|libur|hari\s+besar|peringatan|nasional|cuti|tanggal\s+merah|ada\s+apa)\b");

        private static DateTime ResolveTargetDate(string lower, DateTime now)
        {
            if (Regex.IsMatch(lower, @"\b(kemarin|yesterday)\b")) return now.Date.AddDays(-1);
            if (Regex.IsMatch(lower, @"\b(lusa)\b")) return now.Date.AddDays(2);
            if (Regex.IsMatch(lower, @"\b(besok|tomorrow)\b")) return now.Date.AddDays(1);

            var day = BotTokens.FindDay(lower);
            var monthName = BotTokens.FindMonth(lower);
            if (day.HasValue && monthName != null)
            {
                var month = CalendarEvents.MonthNumber(monthName);
                if (month >= 1)
                {
                    var yearMatch = Regex.Match(lower, @"\b(20\d{2})\b");
                    var year = yearMatch.Success && int.TryParse(yearMatch.Value, out var y) ? y : now.Year;
                    try { return new DateTime(year, month, day.Value); } catch { }
                }
            }

            return now.Date;
        }

        private static string LabelForRelative(string lower)
        {
            if (lower.Contains("kemarin")) return "Kemarin";
            if (lower.Contains("lusa")) return "Lusa";
            if (lower.Contains("besok")) return "Besok";
            return "Tanggal itu";
        }

        private static void AppendTodayEventSummary(StringBuilder sb, DateTime now, List<CalendarEvent> events, CultureInfo id)
        {
            sb.AppendLine();
            AppendTargetEventSummary(sb, now.Date, events, id);
        }

        private static void AppendTargetEventSummary(StringBuilder sb, DateTime date, List<CalendarEvent> events, CultureInfo id)
        {
            var dayEvents = events.Where(e => e.Date.Date == date.Date).OrderBy(e => e.Name).ToList();
            if (dayEvents.Count == 0)
            {
                sb.AppendLine($"📌 Event: tidak ada event nasional khusus untuk {date.ToString("dd MMMM", id)}.");
                var next = events.Where(e => e.Date.Date > date.Date).OrderBy(e => e.Date).FirstOrDefault();
                if (next != null)
                    sb.AppendLine($"➡️ Event terdekat: {next.Date.ToString("dd MMMM yyyy", id)} — {next.Name}");
                return;
            }

            sb.AppendLine($"🎉 Event {date.ToString("dd MMMM", id)}:");
            foreach (var e in dayEvents)
                sb.AppendLine($"   {e.Icon} {e.Name}" + (e.IsHoliday ? " (libur/tanggal merah)" : ""));
        }

        private static void AppendMonthEventSummary(StringBuilder sb, DateTime date, List<CalendarEvent> events, CultureInfo id)
        {
            var monthEvents = events.Where(e => e.Date.Year == date.Year && e.Date.Month == date.Month).OrderBy(e => e.Date).ToList();
            sb.AppendLine();
            if (monthEvents.Count == 0)
            {
                sb.AppendLine($"📌 Tidak ada event nasional khusus di {date.ToString("MMMM yyyy", id)}.");
                return;
            }
            sb.AppendLine($"🎉 Event bulan {date.ToString("MMMM yyyy", id)} ({monthEvents.Count}):");
            foreach (var e in monthEvents.Take(12))
                sb.AppendLine($"   {e.Date:dd} · {e.Icon} {e.Name}" + (e.IsHoliday ? " (libur)" : ""));
        }

        private static string FormatDayEvents(DateTime date, List<CalendarEvent> events, CultureInfo id, string lower)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📅 {date.ToString("dddd, dd MMMM yyyy", id)}");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
            AppendTargetEventSummary(sb, date, events, id);
            return sb.ToString().TrimEnd();
        }

        private static string FormatMonthEvents(DateTime date, List<CalendarEvent> events, CultureInfo id)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📅 EVENT BULAN {date.ToString("MMMM yyyy", id).ToUpperInvariant()}");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
            AppendMonthEventSummary(sb, date, events, id);
            return sb.ToString().TrimEnd();
        }

        private static string FormatWeekEvents(DateTime date, List<CalendarEvent> events, CultureInfo id)
        {
            int diff = (7 + (int)date.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            var start = date.Date.AddDays(-diff);
            var end = start.AddDays(6);
            var weekEvents = events.Where(e => e.Date.Date >= start && e.Date.Date <= end).OrderBy(e => e.Date).ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"📅 EVENT MINGGU INI");
            sb.AppendLine($"{start.ToString("dd MMM", id)} - {end.ToString("dd MMM yyyy", id)}");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
            if (weekEvents.Count == 0)
            {
                sb.AppendLine("📌 Tidak ada event nasional khusus minggu ini.");
            }
            else
            {
                foreach (var e in weekEvents)
                    sb.AppendLine($"{e.Date.ToString("ddd, dd MMM", id)} · {e.Icon} {e.Name}" + (e.IsHoliday ? " (libur)" : ""));
            }
            return sb.ToString().TrimEnd();
        }

        private static string FormatYearEvents(int year, List<CalendarEvent> events, CultureInfo id)
        {
            var sb = new StringBuilder();
            var holidays = events.Count(e => e.IsHoliday);
            sb.AppendLine($"📅 EVENT NASIONAL {year}");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"📊 Total event tercatat: {events.Count}");
            sb.AppendLine($"🔴 Libur/tanggal merah: {holidays}");
            sb.AppendLine($"📌 Peringatan biasa: {events.Count - holidays}");
            sb.AppendLine();
            foreach (var group in events.OrderBy(e => e.Date).GroupBy(e => e.Date.Month))
            {
                sb.AppendLine($"━━ {new DateTime(year, group.Key, 1).ToString("MMMM", id).ToUpperInvariant()} ━━");
                foreach (var e in group)
                    sb.AppendLine($"{e.Date:dd} · {e.Icon} {e.Name}" + (e.IsHoliday ? " (libur)" : ""));
                sb.AppendLine();
            }
            return sb.ToString().TrimEnd();
        }

        private static string FormatYearEventCount(int year, List<CalendarEvent> events)
        {
            var holidays = events.Count(e => e.IsHoliday);
            return $"🎉 Tahun {year} ada {events.Count} event nasional tercatat: {holidays} libur/tanggal merah dan {events.Count - holidays} peringatan biasa.";
        }
    }

    public record CalendarEvent(DateTime Date, string Name, bool IsHoliday, string Icon);

    public static class CalendarEvents
    {
        public static int MonthNumber(string month) => month.ToLowerInvariant() switch
        {
            "januari" or "jan" => 1,
            "februari" or "feb" => 2,
            "maret" or "mar" => 3,
            "april" or "apr" => 4,
            "mei" => 5,
            "juni" or "jun" => 6,
            "juli" or "jul" => 7,
            "agustus" or "agu" or "agt" => 8,
            "september" or "sep" or "sept" => 9,
            "oktober" or "okt" => 10,
            "november" or "nov" => 11,
            "desember" or "des" => 12,
            _ => -1
        };

        public static List<CalendarEvent> ForYear(int year)
        {
            // Data 2026 bersifat static offline untuk bot. Cuti bersama bisa berubah oleh SKB pemerintah,
            // jadi kita masukkan libur nasional + event umum Indonesia yang stabil.
            var list = new List<CalendarEvent>
            {
                new(new DateTime(year, 1, 1), "Tahun Baru Masehi", true, "🎆"),
                new(new DateTime(year, 2, 8), "Isra Mikraj Nabi Muhammad SAW", true, "🕌"),
                new(new DateTime(year, 2, 17), "Tahun Baru Imlek", true, "🏮"),
                new(new DateTime(year, 3, 19), "Hari Suci Nyepi", true, "🕯️"),
                new(new DateTime(year, 3, 21), "Idul Fitri 1447 H", true, "🕌"),
                new(new DateTime(year, 3, 22), "Idul Fitri 1447 H", true, "🕌"),
                new(new DateTime(year, 4, 3), "Wafat Isa Almasih", true, "✝️"),
                new(new DateTime(year, 4, 5), "Paskah", true, "✝️"),
                new(new DateTime(year, 5, 1), "Hari Buruh Internasional", true, "👷"),
                new(new DateTime(year, 5, 14), "Kenaikan Isa Almasih", true, "✝️"),
                new(new DateTime(year, 5, 27), "Idul Adha 1447 H", true, "🕌"),
                new(new DateTime(year, 5, 31), "Hari Raya Waisak", true, "☸️"),
                new(new DateTime(year, 6, 1), "Hari Lahir Pancasila", true, "🇮🇩"),
                new(new DateTime(year, 6, 16), "Tahun Baru Islam 1448 H", true, "🕌"),
                new(new DateTime(year, 8, 17), "Hari Kemerdekaan Republik Indonesia", true, "🇮🇩"),
                new(new DateTime(year, 8, 25), "Maulid Nabi Muhammad SAW", true, "🕌"),
                new(new DateTime(year, 12, 25), "Hari Raya Natal", true, "🎄"),

                // Event/peringatan nasional populer (bukan selalu libur)
                new(new DateTime(year, 1, 25), "Hari Gizi Nasional", false, "🥗"),
                new(new DateTime(year, 2, 9), "Hari Pers Nasional", false, "📰"),
                new(new DateTime(year, 3, 9), "Hari Musik Nasional", false, "🎵"),
                new(new DateTime(year, 4, 21), "Hari Kartini", false, "🌺"),
                new(new DateTime(year, 5, 2), "Hari Pendidikan Nasional", false, "🎓"),
                new(new DateTime(year, 5, 20), "Hari Kebangkitan Nasional", false, "🇮🇩"),
                new(new DateTime(year, 6, 5), "Hari Lingkungan Hidup Sedunia", false, "🌱"),
                new(new DateTime(year, 7, 23), "Hari Anak Nasional", false, "🧒"),
                new(new DateTime(year, 8, 10), "Hari Veteran Nasional", false, "🎖️"),
                new(new DateTime(year, 10, 1), "Hari Kesaktian Pancasila", false, "🇮🇩"),
                new(new DateTime(year, 10, 2), "Hari Batik Nasional", false, "🧵"),
                new(new DateTime(year, 10, 28), "Hari Sumpah Pemuda", false, "🤝"),
                new(new DateTime(year, 11, 10), "Hari Pahlawan", false, "🎖️"),
                new(new DateTime(year, 11, 25), "Hari Guru Nasional", false, "👩‍🏫"),
                new(new DateTime(year, 12, 22), "Hari Ibu", false, "🌷"),
            };

            if (year != 2026)
            {
                // Untuk tahun selain 2026: pertahankan event fixed-date yang stabil,
                // hapus event keagamaan bergerak supaya tidak salah tanggal.
                list = list.Where(e => !e.Name.Contains("Idul", StringComparison.OrdinalIgnoreCase)
                                    && !e.Name.Contains("Isra", StringComparison.OrdinalIgnoreCase)
                                    && !e.Name.Contains("Imlek", StringComparison.OrdinalIgnoreCase)
                                    && !e.Name.Contains("Nyepi", StringComparison.OrdinalIgnoreCase)
                                    && !e.Name.Contains("Waisak", StringComparison.OrdinalIgnoreCase)
                                    && !e.Name.Contains("Maulid", StringComparison.OrdinalIgnoreCase)
                                    && !e.Name.Contains("Islam", StringComparison.OrdinalIgnoreCase)
                                    && !e.Name.Contains("Paskah", StringComparison.OrdinalIgnoreCase)
                                    && !e.Name.Contains("Wafat", StringComparison.OrdinalIgnoreCase)
                                    && !e.Name.Contains("Kenaikan", StringComparison.OrdinalIgnoreCase))
                           .ToList();
            }

            return list.OrderBy(e => e.Date).ToList();
        }
    }
}
