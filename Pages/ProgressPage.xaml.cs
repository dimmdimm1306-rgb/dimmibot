using System.Text;
using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class ProgressPage : ContentPage
    {
        private readonly GoogleSheetsService _sheets;
        private readonly ProjectConfig       _project;
        private List<ProgressResumeItem>     _all = new();
        private List<ProgressItem>           _dailyProgress = new();
        private List<DailyProgressGroup>     _dailyGroups = new();
        private ProgressResumeTotal          _total = new();
        private string _lastChartHtml = "";
        private string _searchText    = "";
        private string _chartMode     = "semua"; // semua | kabel | t7 | t9
        private string _viewMode      = "segment"; // segment | harian

        private static readonly string[] _months =
            { "", "Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Agu", "Sep", "Okt", "Nov", "Des" };

        public ProgressPage(GoogleSheetsService sheets, ProjectConfig project)
        {
            InitializeComponent();
            _sheets  = sheets;
            _project = project;
            RootTabbedPage.ProjectNameUpdated += OnProjectNameUpdated;
        }

        private void OnProjectNameUpdated(ProjectConfig p)
        {
            if (p.Id != _project.Id) return;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try { LblProjectName.Text = p.Name; } catch { }
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                LblTheme.Text = App.Theme.ThemeIcon;
                LblProjectName.Text = _project.Name;
                UpdateChartOptionPills();
                if (_all.Count == 0) await LoadData(false);
                else { BuildTotalCard(); BuildChart(_all); BuildDailyGroups(); ApplySearch(); }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ProgressPage] OnAppearing: {ex.Message}"); }
        }

        private async Task LoadData(bool force)
        {
            try
            {
                Loader.IsVisible = true;
                Loader.IsRunning = true;

                var data = await _sheets.FetchAsync(force);
                _all           = MergeSegments(data.ProgressResume);
                _total         = data.ResumeTotal;
                _dailyProgress = data.Progress;
                BuildDailyGroups();

                BuildTotalCard();
                BuildChart(_all);
                ApplySearch();
                if (!string.IsNullOrEmpty(data.LoadWarning))
                    await DisplayAlert("Info Data", data.LoadWarning, "OK");
            }
            catch (Exception ex) { await DisplayAlert("Gagal", ex.Message, "OK"); }
            finally
            {
                Loader.IsVisible = false;
                Loader.IsRunning = false;
            }
        }

        private List<ProgressResumeItem> MergeSegments(List<ProgressResumeItem> resumeData)
        {
            if (_project.SegmentNames.Count > 0)
            {
                var byNo   = resumeData.ToDictionary(x => x.No);
                var result = new List<ProgressResumeItem>();
                var keys   = _project.SegmentNames.Keys
                    .Select(k => int.TryParse(k, out var n) ? n : -1)
                    .Where(n => n > 0).OrderBy(n => n);

                foreach (var no in keys)
                {
                    if (byNo.TryGetValue(no, out var item))
                        result.Add(item);
                    else
                    {
                        _project.SegmentNames.TryGetValue(no.ToString(), out var rute);
                        result.Add(new ProgressResumeItem { No = no, Rute = rute ?? "" });
                    }
                }
                return result.Count > 0 ? result : resumeData;
            }
            return resumeData;
        }

        // ── Chart options: switch material category shown in the line chart ──
        private void OnChartOption(object sender, TappedEventArgs e)
        {
            try
            {
                string mode = (e.Parameter as string ?? "").ToLowerInvariant();
                if (mode != "semua" && mode != "kabel" && mode != "t7" && mode != "t9")
                {
                    if      (sender == OptSemua) mode = "semua";
                    else if (sender == OptT7)    mode = "t7";
                    else if (sender == OptT9)    mode = "t9";
                    else if (sender == OptKabel) mode = "kabel";
                    else return;
                }
                if (mode == _chartMode) return;
                _chartMode = mode;
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                UpdateChartOptionPills();
                BuildChart(_all);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProgressPage] OnChartOption: {ex}");
            }
        }

        private void UpdateChartOptionPills()
        {
            try
            {
                var res    = Application.Current?.Resources;
                Color card2  = TryColor(res, "CardBg2",   "#162032");
                Color border = TryColor(res, "BorderClr", "#334155");
                Color muted  = TryColor(res, "TextMuted", "#64748B");
                var   active = Color.FromArgb("#1D4ED8");
                var   bordBr = new SolidColorBrush(border);
                var   trans  = (Brush)Brush.Transparent;

                void Apply(Border pill, Label lbl, bool on)
                {
                    pill.BackgroundColor = on ? active : card2;
                    pill.Stroke          = on ? trans  : bordBr;
                    lbl.TextColor        = on ? Colors.White : muted;
                }
                Apply(OptSemua, LblOptSemua, _chartMode == "semua");
                Apply(OptKabel, LblOptKabel, _chartMode == "kabel");
                Apply(OptT7,    LblOptT7,    _chartMode == "t7");
                Apply(OptT9,    LblOptT9,    _chartMode == "t9");

                LblChartTitle.Text = _chartMode switch
                {
                    "semua" => "Progress Total per Segment (%)",
                    "kabel" => "Progress Kabel per Segment",
                    "t7"    => "Progress Tiang 7m per Segment",
                    "t9"    => "Progress Tiang 9m per Segment",
                    _       => "Progress per Segment"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProgressPage] UpdateChartOptionPills: {ex}");
            }
        }

        private static Color TryColor(ResourceDictionary? res, string key, string fallback)
        {
            if (res != null && res.TryGetValue(key, out var v))
            {
                if (v is Color c) return c;
                if (v is SolidColorBrush sb) return sb.Color;
            }
            return Color.FromArgb(fallback);
        }

        // ── Total summary card ───────────────────────────────────────────
        private void BuildTotalCard()
        {
            var t = _total;
            bool hasData = t.KabelPlan > 0 || t.T7Plan > 0 || t.T9Plan > 0;
            if (!hasData) { TotalCard.IsVisible = false; return; }

            double kblPct = t.KabelPlan > 0 ? (double)t.KabelProgress / t.KabelPlan : 0;
            double t7Pct  = t.T7Plan    > 0 ? (double)t.T7Progress    / t.T7Plan    : 0;
            double t9Pct  = t.T9Plan    > 0 ? (double)t.T9Progress    / t.T9Plan    : 0;

            TotalKabelBar.Progress = kblPct;
            TotalKabelPct.Text     = $"{kblPct:P0}";
            TotalKabelVal.Text     = $"{t.KabelProgress:N0} / {t.KabelPlan:N0} m";

            TotalT7Bar.Progress    = t7Pct;
            TotalT7Pct.Text        = $"{t7Pct:P0}";
            TotalT7Val.Text        = $"{t.T7Progress:N0} / {t.T7Plan:N0} btg";

            TotalT9Bar.Progress    = t9Pct;
            TotalT9Pct.Text        = $"{t9Pct:P0}";
            TotalT9Val.Text        = $"{t.T9Progress:N0} / {t.T9Plan:N0} btg";

            TotalCard.IsVisible = true;
        }

        // ── Chart builder ────────────────────────────────────────────────
        private void BuildChart(List<ProgressResumeItem> items)
        {
            if (items.Count == 0) { ChartCard.IsVisible = false; return; }

            var (labelsJs, datasetsJs) = BuildTimeSeriesData(items);
            if (string.IsNullOrEmpty(datasetsJs))
            {
                // Keep card + pills visible so user can switch back; show placeholder.
                _lastChartHtml          = "";
                ChartWebView.Source     = null;
                ChartWebView.IsVisible  = false;
                LblChartEmpty.IsVisible = true;
                ChartCard.IsVisible     = true;
                return;
            }
            LblChartEmpty.IsVisible = false;
            ChartWebView.IsVisible  = true;

            bool isDark = App.Theme.IsDark;
            var bg      = isDark ? "#1E293B" : "#FFFFFF";
            var gridClr = isDark ? "rgba(255,255,255,0.07)" : "rgba(0,0,0,0.06)";
            var textClr = isDark ? "#94A3B8" : "#374151";
            var tickClr = isDark ? "#64748B" : "#94A3B8";

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head>");
            sb.Append("<meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1'>");
            sb.Append("<script src='https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js'></script>");
            sb.Append(BuildChartCss(bg, textClr, false, isDark));
            sb.Append("</head><body>");
            sb.Append(BuildLegendButtonHtml());
            sb.Append("<canvas id='c'></canvas>");
            sb.Append(BuildLegendPanelHtml());
            sb.Append("<script>");
            sb.Append($"Chart.defaults.color='{textClr}';Chart.defaults.font.size=9;");
            sb.Append($"const chart=new Chart(document.getElementById('c'),{{type:'line',");
            sb.Append($"data:{{labels:[{labelsJs}],datasets:[{datasetsJs}]}},");
            sb.Append($"options:{{responsive:true,maintainAspectRatio:false,");
            sb.Append($"animation:{{duration:700,easing:'easeOutQuart'}},");
            sb.Append($"interaction:{{mode:'index',intersect:false,axis:'x'}},");
            sb.Append($"plugins:{{");
            sb.Append($"legend:{{display:false}},");
            sb.Append($"tooltip:{{enabled:true,mode:'index',intersect:false,axis:'x',padding:6,boxPadding:3,boxWidth:7,boxHeight:7,cornerRadius:6,caretSize:4,displayColors:true,bodyFont:{{size:9}},titleFont:{{size:10,weight:'bold'}},titleMarginBottom:4,");
            sb.Append($"filter:function(item){{var d=item.dataset;if(!d.delta)return true;return d.delta[item.dataIndex]>0;}},");
            sb.Append($"callbacks:{{");
            sb.Append($"title:function(c){{return '📅 '+c[0].label}},");
            sb.Append($"label:function(ctx){{var d=ctx.dataset;var i=ctx.dataIndex;var u=d.unit||'m';var pct=ctx.parsed.y.toFixed(1)+'%';var s=' '+d.label+': '+pct;if(u==='%'){{var a=(d.dKb?d.dKb[i]:0),b=(d.dT7?d.dT7[i]:0),c=(d.dT9?d.dT9[i]:0);if(a||b||c)s+=' • '+a+','+b+','+c;}}else{{if(d.meters)s+=' • '+d.meters[i]+' '+u;var add=d.delta?d.delta[i]:0;if(add>0)s+=' (+'+add+' '+u+')';}}return s;}}");
            sb.Append($"}}}}");
            sb.Append($"}},");
            sb.Append($"scales:{{");
            sb.Append($"x:{{ticks:{{color:'{tickClr}',font:{{size:8}},maxRotation:45,maxTicksLimit:10}},grid:{{color:'{gridClr}'}}}},");
            sb.Append($"y:{{min:0,max:120,ticks:{{stepSize:20,callback:function(v){{return v+'%'}},color:'{tickClr}',font:{{size:8}}}},grid:{{color:'{gridClr}'}}}}");
            sb.Append($"}}}}}});");
            sb.Append(BuildLegendJs());
            sb.Append("</script></body></html>");

            _lastChartHtml = sb.ToString();
            ChartWebView.HeightRequest = 260;
            ChartWebView.Source        = new HtmlWebViewSource { Html = _lastChartHtml };
            ChartCard.IsVisible        = true;
        }

        // Bangun time-series per segment dari data.Progress harian (filter by _chartMode)
        private (string labels, string datasets) BuildTimeSeriesData(List<ProgressResumeItem> segments)
        {
            if (_chartMode == "semua") return BuildCombinedTimeSeriesData(segments);

            int PlanOf(ProgressResumeItem s) => _chartMode switch
            {
                "t7" => s.T7Plan,
                "t9" => s.T9Plan,
                _    => s.KabelPlan
            };
            var matLabel = _chartMode switch { "t7" => "Tiang 7m", "t9" => "Tiang 9m", _ => "Kabel" };
            var unit     = _chartMode == "kabel" ? "m" : "btg";

            // Kumpulkan progres harian per segmentNo → tanggal → total (filter material)
            var dailyByNo = new Dictionary<int, Dictionary<DateTime, int>>();

            foreach (var item in _dailyProgress)
            {
                if (item.SortDate == DateTime.MinValue) continue;
                if (!MatchesChartMode(item.NamaBarang)) continue;
                var date = item.SortDate.Date;

                foreach (var seg in segments)
                {
                    if (!SegmentNameMatch(item.Segment, seg.Rute)) continue;
                    if (!dailyByNo.ContainsKey(seg.No))
                        dailyByNo[seg.No] = new Dictionary<DateTime, int>();
                    dailyByNo[seg.No].TryGetValue(date, out var prev);
                    dailyByNo[seg.No][date] = prev + item.Progres;
                    break;
                }
            }

            if (dailyByNo.Count == 0) return ("", "");

            var allDates = dailyByNo.Values
                .SelectMany(d => d.Keys)
                .Distinct().OrderBy(d => d).ToList();

            var labelsJs = "'Mulai'," + string.Join(",", allDates.Select(d => $"'{d.Day} {_months[d.Month]}'"));

            var sb = new StringBuilder();
            bool first = true;

            foreach (var seg in segments.OrderBy(s => s.No))
            {
                if (!dailyByNo.ContainsKey(seg.No)) continue;
                var byDate   = dailyByNo[seg.No];
                var plan     = PlanOf(seg);
                int totalCum = byDate.Values.Sum();
                if (plan <= 0 && totalCum <= 0) continue;
                int denom    = plan > 0 ? plan : totalCum;

                int cum    = 0;
                var pts    = new List<string> { "0" };
                var mts    = new List<string> { "0" };
                var dailyDelta = new List<string> { "0" };

                var ic = System.Globalization.CultureInfo.InvariantCulture;
                foreach (var date in allDates)
                {
                    byDate.TryGetValue(date, out var m);
                    cum += m;
                    pts.Add(Math.Min((double)cum / denom * 100, 120).ToString("F1", ic));
                    mts.Add(cum.ToString(ic));
                    dailyDelta.Add(m.ToString(ic));
                }

                if (!first) sb.Append(',');
                var color = seg.CardColorMid;
                var shortName = ShortenRute(seg.Rute, seg.No);
                sb.Append($"{{label:'{shortName.Replace("'", "\\'")}'");
                sb.Append($",segNo:{seg.No},plan:{denom},mat:'{matLabel}',unit:'{unit}'");
                sb.Append($",meters:[{string.Join(",", mts)}],delta:[{string.Join(",", dailyDelta)}]");
                sb.Append($",data:[{string.Join(",", pts)}]");
                sb.Append($",borderColor:'{color}',backgroundColor:'{color}15'");
                sb.Append($",fill:false,tension:0.35,borderWidth:2");
                sb.Append($",pointRadius:4,pointHoverRadius:8,pointBackgroundColor:'{color}',pointHitRadius:14}}");
                first = false;
            }

            return (labelsJs, sb.ToString());
        }

        // Mode "Semua" — gabung Kabel + Tiang 7m + Tiang 9m jadi 1 line per segment.
        // Per tanggal: persen = rata-rata dari (cum/plan) yang plan-nya > 0.
        private (string labels, string datasets) BuildCombinedTimeSeriesData(List<ProgressResumeItem> segments)
        {
            // 3 dictionaries per segmentNo: kabel / t7 / t9 (date → daily progres)
            var kbDaily = new Dictionary<int, Dictionary<DateTime, int>>();
            var t7Daily = new Dictionary<int, Dictionary<DateTime, int>>();
            var t9Daily = new Dictionary<int, Dictionary<DateTime, int>>();

            foreach (var item in _dailyProgress)
            {
                if (item.SortDate == DateTime.MinValue) continue;
                var s = (item.NamaBarang ?? "").Trim().ToLowerInvariant();
                if (s.Length == 0) continue;

                Dictionary<int, Dictionary<DateTime, int>>? bucket = null;
                if      ((s.Contains("tiang") || s.Contains("t7")) && s.Contains('7')) bucket = t7Daily;
                else if ((s.Contains("tiang") || s.Contains("t9")) && s.Contains('9')) bucket = t9Daily;
                else if (s.Contains("kabel") || (!s.Contains("tiang") && !s.Contains("terminasi"))) bucket = kbDaily;
                if (bucket == null) continue;

                var date = item.SortDate.Date;
                foreach (var seg in segments)
                {
                    if (!SegmentNameMatch(item.Segment, seg.Rute)) continue;
                    if (!bucket.ContainsKey(seg.No)) bucket[seg.No] = new Dictionary<DateTime, int>();
                    bucket[seg.No].TryGetValue(date, out var prev);
                    bucket[seg.No][date] = prev + item.Progres;
                    break;
                }
            }

            var allDates = kbDaily.Values.Concat(t7Daily.Values).Concat(t9Daily.Values)
                .SelectMany(d => d.Keys).Distinct().OrderBy(d => d).ToList();
            if (allDates.Count == 0) return ("", "");

            var labelsJs = "'Mulai'," + string.Join(",", allDates.Select(d => $"'{d.Day} {_months[d.Month]}'"));
            var sb = new StringBuilder();
            bool first = true;
            var ic = System.Globalization.CultureInfo.InvariantCulture;

            foreach (var seg in segments.OrderBy(s => s.No))
            {
                bool hasAny = kbDaily.ContainsKey(seg.No) || t7Daily.ContainsKey(seg.No) || t9Daily.ContainsKey(seg.No);
                if (!hasAny) continue;
                if (seg.KabelPlan <= 0 && seg.T7Plan <= 0 && seg.T9Plan <= 0) continue;

                kbDaily.TryGetValue(seg.No, out var kbDates);
                t7Daily.TryGetValue(seg.No, out var t7Dates);
                t9Daily.TryGetValue(seg.No, out var t9Dates);

                int cumKb = 0, cumT7 = 0, cumT9 = 0;
                var pts     = new List<string> { "0" };
                var delta   = new List<string> { "0" };
                var deltaKb = new List<string> { "0" };
                var deltaT7 = new List<string> { "0" };
                var deltaT9 = new List<string> { "0" };

                foreach (var date in allDates)
                {
                    int dKb = (kbDates != null && kbDates.TryGetValue(date, out var bk)) ? bk : 0;
                    int dT7 = (t7Dates != null && t7Dates.TryGetValue(date, out var b7)) ? b7 : 0;
                    int dT9 = (t9Dates != null && t9Dates.TryGetValue(date, out var b9)) ? b9 : 0;
                    cumKb += dKb; cumT7 += dT7; cumT9 += dT9;

                    double sum = 0; int n = 0;
                    if (seg.KabelPlan > 0) { sum += Math.Min((double)cumKb / seg.KabelPlan, 1.2); n++; }
                    if (seg.T7Plan    > 0) { sum += Math.Min((double)cumT7 / seg.T7Plan,    1.2); n++; }
                    if (seg.T9Plan    > 0) { sum += Math.Min((double)cumT9 / seg.T9Plan,    1.2); n++; }
                    var pct = n > 0 ? sum / n * 100 : 0;
                    pts.Add(pct.ToString("F1", ic));

                    deltaKb.Add(dKb.ToString(ic));
                    deltaT7.Add(dT7.ToString(ic));
                    deltaT9.Add(dT9.ToString(ic));
                    delta.Add((dKb + dT7 + dT9).ToString(ic));
                }

                if (!first) sb.Append(',');
                var color = seg.CardColorMid;
                var shortName = ShortenRute(seg.Rute, seg.No);
                sb.Append($"{{label:'{shortName.Replace("'", "\\'")}'");
                sb.Append($",segNo:{seg.No},mat:'Semua',unit:'%'");
                sb.Append($",delta:[{string.Join(",", delta)}]");
                sb.Append($",dKb:[{string.Join(",", deltaKb)}]");
                sb.Append($",dT7:[{string.Join(",", deltaT7)}]");
                sb.Append($",dT9:[{string.Join(",", deltaT9)}]");
                sb.Append($",data:[{string.Join(",", pts)}]");
                sb.Append($",borderColor:'{color}',backgroundColor:'{color}15'");
                sb.Append($",fill:false,tension:0.35,borderWidth:2");
                sb.Append($",pointRadius:4,pointHoverRadius:8,pointBackgroundColor:'{color}',pointHitRadius:14}}");
                first = false;
            }

            return (labelsJs, sb.ToString());
        }

        // Material filter sesuai mode chart aktif. Toleran terhadap variasi penulisan
        // (mis. "Kabel 24C", "Tiang 7M", "tiang 9 m", "T7", dst).
        private bool MatchesChartMode(string namaBarang)
        {
            var n = (namaBarang ?? "").Trim();
            if (n.Length == 0) return _chartMode == "kabel"; // anggap default kabel
            var s = n.ToLowerInvariant();
            return _chartMode switch
            {
                "t7" => (s.Contains("tiang") || s.Contains("t7")) && s.Contains('7'),
                "t9" => (s.Contains("tiang") || s.Contains("t9")) && s.Contains('9'),
                _    => s.Contains("kabel") || (!s.Contains("tiang") && !s.Contains("terminasi"))
            };
        }

        private static string ShortenRute(string rute, int no)
        {
            if (string.IsNullOrWhiteSpace(rute)) return $"Seg {no}";
            var first = rute.Split(new[] { " - ", " – ", " / " }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return first.Length > 14 ? first[..14] + "…" : first;
        }

        private static bool SegmentNameMatch(string seg, string rute)
        {
            if (string.IsNullOrWhiteSpace(seg)) return false;
            if (seg.Equals(rute, StringComparison.OrdinalIgnoreCase)) return true;
            if (seg.Contains(rute, StringComparison.OrdinalIgnoreCase)) return true;
            if (rute.Contains(seg, StringComparison.OrdinalIgnoreCase)) return true;
            var kw = rute.Split(new[] { " - ", " – ", "-" }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(k => k.Trim()).Where(k => k.Length > 3);
            return kw.Any(k => seg.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        // ── Tap chart → fullscreen ───────────────────────────────────────
        private async void OnChartTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_lastChartHtml)) return;
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                var html = BuildFullscreenChartHtml();
                if (string.IsNullOrEmpty(html)) return;
                var page = new ChartPage($"Progress – {_project.Name}", html);
                await Navigation.PushModalAsync(page);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProgressPage] OnChartTapped: {ex}");
            }
        }

        private string BuildFullscreenChartHtml()
        {
            bool isDark = App.Theme.IsDark;
            var bg      = isDark ? "#0F172A" : "#F0F4F8";
            var gridClr = isDark ? "rgba(255,255,255,0.07)" : "rgba(0,0,0,0.07)";
            var textClr = isDark ? "#CBD5E1" : "#374151";
            var tickClr = isDark ? "#94A3B8" : "#6B7280";

            var (labelsJs, datasetsJs) = BuildTimeSeriesData(_all);
            if (string.IsNullOrEmpty(datasetsJs))
                return $"<html><body style='background:{bg};color:{textClr};display:flex;align-items:center;justify-content:center;height:100vh;font-family:sans-serif'>Belum ada data harian</body></html>";

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head>");
            sb.Append("<meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1'>");
            sb.Append("<script src='https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js'></script>");
            sb.Append(BuildChartCss(bg, textClr, true, isDark));
            sb.Append("</head><body>");
            sb.Append(BuildLegendButtonHtml());
            sb.Append("<canvas id='c'></canvas>");
            sb.Append(BuildLegendPanelHtml());
            sb.Append("<script>");
            sb.Append($"Chart.defaults.color='{textClr}';Chart.defaults.font.size=11;");
            sb.Append($"const chart=new Chart(document.getElementById('c'),{{type:'line',");
            sb.Append($"data:{{labels:[{labelsJs}],datasets:[{datasetsJs}]}},");
            sb.Append($"options:{{responsive:true,maintainAspectRatio:false,");
            sb.Append($"animation:{{duration:800,easing:'easeOutQuart'}},");
            sb.Append($"interaction:{{mode:'index',intersect:false,axis:'x'}},");
            sb.Append($"plugins:{{");
            sb.Append($"legend:{{display:false}},");
            sb.Append($"tooltip:{{enabled:true,mode:'index',intersect:false,axis:'x',padding:8,boxPadding:4,boxWidth:9,boxHeight:9,cornerRadius:6,caretSize:5,displayColors:true,bodyFont:{{size:11}},titleFont:{{size:12,weight:'bold'}},titleMarginBottom:5,");
            sb.Append($"filter:function(item){{var d=item.dataset;if(!d.delta)return true;return d.delta[item.dataIndex]>0;}},");
            sb.Append($"callbacks:{{");
            sb.Append($"title:function(c){{return '📅 '+c[0].label}},");
            sb.Append($"label:function(ctx){{var d=ctx.dataset;var i=ctx.dataIndex;var u=d.unit||'m';var pct=ctx.parsed.y.toFixed(1)+'%';var s=' '+d.label+': '+pct;if(u==='%'){{var a=(d.dKb?d.dKb[i]:0),b=(d.dT7?d.dT7[i]:0),c=(d.dT9?d.dT9[i]:0);if(a||b||c)s+=' • '+a+','+b+','+c+' hari ini';}}else{{if(d.meters){{var pln=d.plan?' / '+d.plan+' '+u:'';s+=' • '+d.meters[i]+' '+u+pln;}}var add=d.delta?d.delta[i]:0;if(add>0)s+=' (+'+add+' '+u+' hari ini)';}}return s;}}");
            sb.Append($"}}}}");
            sb.Append($"}},");
            sb.Append($"scales:{{");
            sb.Append($"x:{{ticks:{{color:'{tickClr}',font:{{size:10}},maxRotation:45,maxTicksLimit:14}},grid:{{color:'{gridClr}'}}}},");
            sb.Append($"y:{{min:0,max:120,ticks:{{stepSize:20,callback:function(v){{return v+'%'}},color:'{tickClr}',font:{{size:10}}}},grid:{{color:'{gridClr}'}}}}");
            sb.Append($"}}}}}});");
            sb.Append(BuildLegendJs());
            sb.Append("</script></body></html>");
            return sb.ToString();
        }

        // ── Helpers for chart legend popup ──────────────────────────────
        private static string BuildChartCss(string bg, string textClr, bool fullscreen, bool isDark)
        {
            int pad = fullscreen ? 6 : 4;

            var btnBg     = isDark ? "#1E3A6E"            : "#2563EB";
            var btnBdr    = isDark ? "#2563EB"            : "#1D4ED8";
            var btnAct    = isDark ? "#1D4ED8"            : "#1E40AF";
            var panelBg   = isDark ? "rgba(15,23,42,0.97)" : "rgba(255,255,255,0.98)";
            var panelBdr  = isDark ? "#334155"            : "#CBD5E1";
            var panelShdw = isDark ? "0 8px 24px rgba(0,0,0,0.4)" : "0 8px 24px rgba(15,23,42,0.12)";
            var headTxt   = isDark ? "#94A3B8"            : "#64748B";
            var headBdr   = isDark ? "#334155"            : "#E2E8F0";
            var rowTxt    = isDark ? "#CBD5E1"            : "#1E293B";
            var rowAct    = isDark ? "rgba(59,130,246,0.18)" : "rgba(59,130,246,0.12)";
            var colorBdr  = isDark ? "rgba(255,255,255,0.15)" : "rgba(15,23,42,0.10)";
            var togOffBg  = isDark ? "#334155"            : "#E2E8F0";
            var togOffTxt = isDark ? "#94A3B8"            : "#64748B";

            return $"<style>*{{box-sizing:border-box;margin:0;padding:0}}html,body{{height:100%;background:{bg};font-family:sans-serif;display:flex;flex-direction:column;padding:{pad}px}}" +
                   $"canvas{{flex:1;min-height:0}}" +
                   $".lgWrap{{position:relative;display:flex;justify-content:flex-end;margin-bottom:4px;z-index:50}}" +
                   $".lgBtn{{display:inline-flex;align-items:center;gap:6px;padding:7px 14px;border:1.5px solid {btnBdr};background:{btnBg};color:#fff;border-radius:14px;font-size:11px;font-weight:bold;cursor:pointer}}" +
                   $".lgBtn:active{{background:{btnAct}}}" +
                   $".lgBtn .lgIcon{{font-size:13px;line-height:1;display:inline-block;transform:translateY(-0.5px)}}" +
                   $".lgPanel{{position:absolute;top:36px;right:0;left:0;background:{panelBg};border:1px solid {panelBdr};border-radius:14px;padding:6px;max-height:340px;overflow-y:auto;z-index:100;display:none;box-shadow:{panelShdw}}}" +
                   $".lgPanel.show{{display:block}}" +
                   $".lgHead{{display:flex;justify-content:space-between;padding:6px 8px;color:{headTxt};font-size:10px;border-bottom:1px solid {headBdr};margin-bottom:4px}}" +
                   $".lgHead button{{background:transparent;border:none;color:#3B82F6;font-size:11px;font-weight:bold;cursor:pointer;padding:0 6px}}" +
                   $".lgRow{{display:flex;align-items:center;padding:11px 12px;border-radius:9px;cursor:pointer;gap:10px;color:{rowTxt};font-size:12px}}" +
                   $".lgRow:active{{background:{rowAct}}}" +
                   $".lgColor{{width:18px;height:18px;border-radius:5px;flex-shrink:0;border:2px solid {colorBdr}}}" +
                   $".lgName{{flex:1;line-height:1.3}}" +
                   $".lgTog{{font-size:11px;font-weight:bold;min-width:38px;text-align:center;padding:5px 9px;border-radius:8px}}" +
                   $".lgTog.on{{color:#fff;background:#22C55E}}" +
                   $".lgTog.off{{color:{togOffTxt};background:{togOffBg}}}</style>";
        }

        private static string BuildLegendButtonHtml() =>
            "<div class='lgWrap'><button id='lgBtn' class='lgBtn' onclick='toggleLg(event)'>" +
            "<span class='lgIcon'>&#9881;</span><span id='lgLbl'>Atur Tampilan</span></button></div>";

        private static string BuildLegendPanelHtml() =>
            "<div id='lgPanel' class='lgPanel'>" +
            "<div class='lgHead'><span>Pilih yang ingin ditampilkan</span>" +
            "<span><button onclick='allLg(true)'>Semua</button>" +
            "<button onclick='allLg(false)'>Kosong</button></span></div>" +
            "<div id='lgList'></div></div>";

        private static string BuildLegendJs() =>
            "function activeDs(){return chart.data.datasets.filter(function(d){return !d._matHidden;});}" +
            "function buildLg(){var list=document.getElementById('lgList');list.innerHTML='';" +
            "activeDs().forEach(function(d){var row=document.createElement('div');row.className='lgRow';" +
            "var on=!d.hidden;row.innerHTML='<div class=\"lgColor\" style=\"background:'+d.borderColor+'\"></div>'+" +
            "'<div class=\"lgName\">'+(d.fullLabel||d.label)+'</div>'+" +
            "'<div class=\"lgTog '+(on?'on':'off')+'\">'+(on?'TAMPIL':'SEMBUNYI')+'</div>';" +
            "row.onclick=function(){d.hidden=!d.hidden;chart.update();buildLg();updBtn();};list.appendChild(row);});" +
            "updBtn();}" +
            "function updBtn(){var ds=activeDs();var v=ds.filter(function(d){return !d.hidden;}).length;" +
            "document.getElementById('lgLbl').textContent='Atur Tampilan ('+v+'/'+ds.length+')';}" +
            "function toggleLg(e){if(e){e.stopPropagation();}var p=document.getElementById('lgPanel');p.classList.toggle('show');if(p.classList.contains('show'))buildLg();}" +
            "function allLg(show){activeDs().forEach(function(d){d.hidden=!show;});chart.update();buildLg();}" +
            "document.addEventListener('click',function(e){var p=document.getElementById('lgPanel');if(!p.classList.contains('show'))return;if(p.contains(e.target))return;p.classList.remove('show');});" +
            "updBtn();";

        // ── Segment tap (navigate to detail) ────────────────────────────
        private async void OnSegmentTapped(object sender, TappedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.BindingContext is not ProgressResumeItem item) return;

            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await border.ScaleTo(0.95, 70, Easing.CubicOut);
            await border.ScaleTo(1.0,  80, Easing.CubicIn);

            var page = new SpanDetailPage(_sheets, item.No, item.Rute.Trim(), item.CardColorMid);
            await Navigation.PushAsync(page);
        }

        // ── Segment info → chart popup per-span ─────────────────────────
        private async void OnSegmentInfo(object sender, TappedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.BindingContext is not ProgressResumeItem item) return;
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            var html = BuildSegmentSpanChartHtml(item);
            if (string.IsNullOrEmpty(html))
            {
                await DisplayAlert($"Segment {item.No}", "Belum ada data progress harian untuk segment ini.", "OK");
                return;
            }
            var page = new ChartPage($"Progress Span – Seg {item.No} · {item.RuteShort}", html);
            await Navigation.PushModalAsync(page);
        }

        private static string CanonMat(string nb)
        {
            var s = (nb ?? "").Trim();
            if (s.Length == 0) return "Kabel";
            var l = s.ToLowerInvariant();
            if (l.Contains("kabel")) return "Kabel";
            if (l.Contains("tiang") && l.Contains('7')) return "Tiang 7m";
            if (l.Contains("tiang") && l.Contains('9')) return "Tiang 9m";
            if (l.Contains("t7")) return "Tiang 7m";
            if (l.Contains("t9")) return "Tiang 9m";
            return s;
        }

        private static string MatUnit(string canonMat) => canonMat == "Kabel" ? "m" : "btg";

        private string BuildSegmentSpanChartHtml(ProgressResumeItem seg)
        {
            var segItems = _dailyProgress
                .Where(p => p.SortDate != DateTime.MinValue && SegmentNameMatch(p.Segment, seg.Rute))
                .ToList();
            if (segItems.Count == 0) return "";

            // Kanonisasi material → 3 kategori (Kabel / Tiang 7m / Tiang 9m).
            // Susun order: Kabel selalu pertama jika ada, lalu T7, lalu T9.
            var canonOrder = new[] { "Kabel", "Tiang 7m", "Tiang 9m" };
            var matTypes = segItems.Select(p => CanonMat(p.NamaBarang))
                                   .Distinct()
                                   .OrderBy(m => Array.IndexOf(canonOrder, m) is int i && i >= 0 ? i : 99)
                                   .ToList();

            var allDates  = segItems.Select(p => p.SortDate.Date).Distinct().OrderBy(d => d).ToList();
            var labelsJs  = "'Mulai'," + string.Join(",", allDates.Select(d => $"'{d.Day} {_months[d.Month]}'"));
            var lastDate  = allDates[^1]; // dipakai untuk auto-hide span tanpa kerjaan hari ini

            string[] palette = {
                "#3B82F6","#22C55E","#F59E0B","#EF4444","#A855F7",
                "#EC4899","#06B6D4","#84CC16","#FF7849","#14B8A6",
                "#F472B6","#8B5CF6","#FB923C","#34D399","#60A5FA"
            };
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            var allDs = new StringBuilder();
            bool firstDs = true;
            int pi = 0;

            foreach (var mat in matTypes)
            {
                // Group by span → date → cumulative untuk material kanon ini
                var bySpan = new Dictionary<string, Dictionary<DateTime, int>>();
                foreach (var p in segItems)
                {
                    if (CanonMat(p.NamaBarang) != mat) continue;
                    var span = string.IsNullOrWhiteSpace(p.Span) ? "(all)" : p.Span;
                    if (!bySpan.ContainsKey(span)) bySpan[span] = new();
                    bySpan[span].TryGetValue(p.SortDate.Date, out var prev);
                    bySpan[span][p.SortDate.Date] = prev + p.Progres;
                }

                bool isFirstMat = mat == matTypes[0];
                var unit = MatUnit(mat);

                foreach (var (spanName, byDate) in bySpan.OrderBy(x => x.Key))
                {
                    int total = 0;
                    foreach (var d in allDates) { byDate.TryGetValue(d, out var m); total += m; }
                    if (total <= 0) continue;

                    int cum = 0;
                    var pts   = new List<string> { "0" };
                    var mts   = new List<string> { "0" };
                    var delta = new List<string> { "0" };
                    foreach (var d in allDates)
                    {
                        byDate.TryGetValue(d, out var m);
                        cum += m;
                        pts.Add(Math.Min(cum * 100.0 / total, 100).ToString("F1", ic));
                        mts.Add(cum.ToString(ic));
                        delta.Add(m.ToString(ic));
                    }

                    // Auto-hide span yang tidak ada kerjaan pada tanggal terakhir (anggap "hari ini").
                    byDate.TryGetValue(lastDate, out var lastDelta);
                    bool hideByDefault = !isFirstMat || lastDelta <= 0;

                    var color = palette[pi++ % palette.Length];
                    var label = (spanName.Length > 18 ? spanName[..18] + "…" : spanName).Replace("'", "\\'");
                    var fullLabel = spanName.Replace("'", "\\'");
                    var matJs = mat.Replace("'", "\\'");
                    if (!firstDs) allDs.Append(',');
                    allDs.Append($"{{label:'{label}',fullLabel:'{fullLabel}',mat:'{matJs}',unit:'{unit}',total:{total},");
                    allDs.Append($"hidden:{(hideByDefault ? "true" : "false")},");
                    allDs.Append($"data:[{string.Join(",", pts)}],");
                    allDs.Append($"meters:[{string.Join(",", mts)}],delta:[{string.Join(",", delta)}],");
                    allDs.Append($"borderColor:'{color}',backgroundColor:'{color}15',");
                    allDs.Append($"fill:false,tension:0.35,borderWidth:2,");
                    allDs.Append($"pointRadius:4,pointHoverRadius:8,pointBackgroundColor:'{color}',pointHitRadius:14}}");
                    firstDs = false;
                }
            }
            if (firstDs) return "";

            bool isDark = App.Theme.IsDark;
            var bg      = isDark ? "#0F172A" : "#F0F4F8";
            var textClr = isDark ? "#CBD5E1" : "#374151";
            var tickClr = isDark ? "#94A3B8" : "#6B7280";
            var gridClr = isDark ? "rgba(255,255,255,0.07)" : "rgba(0,0,0,0.07)";
            var btnBg   = isDark ? "#1E293B" : "#E2E8F0";
            var btnTxt  = isDark ? "#CBD5E1" : "#374151";

            // Material toggle buttons (kanon: Kabel / Tiang 7m / Tiang 9m)
            var btns = new StringBuilder();
            foreach (var mat in matTypes)
            {
                var isFirst = mat == matTypes[0];
                btns.Append($"<button class='mb' data-mat='{mat.Replace("'", "\\'")}'");
                if (isFirst) btns.Append($" style='background:#1D4ED8;color:white'");
                btns.Append($" onclick='showMat(this)'>{mat}</button>");
            }

            var firstMatJs = matTypes[0].Replace("'", "\\'");

            var html = new StringBuilder();
            html.Append("<!DOCTYPE html><html><head>");
            html.Append("<meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1'>");
            html.Append("<script src='https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js'></script>");
            html.Append(BuildChartCss(bg, textClr, true, isDark));
            html.Append($"<style>.matRow{{display:flex;gap:6px;flex-wrap:wrap;margin-bottom:4px}}");
            html.Append($".mb{{padding:7px 14px;border:none;border-radius:14px;font-size:11px;font-weight:bold;cursor:pointer;background:{btnBg};color:{btnTxt}}}");
            html.Append($"</style></head><body>");
            html.Append($"<div class='matRow'>{btns}</div>");
            html.Append(BuildLegendButtonHtml());
            html.Append("<canvas id='c'></canvas>");
            html.Append(BuildLegendPanelHtml());
            html.Append("<script>");
            html.Append($"window._currentMat='{firstMatJs}';");
            html.Append($"Chart.defaults.color='{textClr}';Chart.defaults.font.size=11;");
            html.Append($"const chart=new Chart(document.getElementById('c'),{{type:'line',");
            html.Append($"data:{{labels:[{labelsJs}],datasets:[{allDs}]}},");
            html.Append($"options:{{responsive:true,maintainAspectRatio:false,");
            html.Append($"animation:{{duration:600,easing:'easeOutQuart'}},");
            html.Append($"interaction:{{mode:'index',intersect:false,axis:'x'}},");
            html.Append($"plugins:{{");
            html.Append($"legend:{{display:false}},");
            html.Append($"tooltip:{{enabled:true,mode:'index',intersect:false,axis:'x',padding:8,boxPadding:4,boxWidth:9,boxHeight:9,cornerRadius:6,caretSize:5,displayColors:true,bodyFont:{{size:11}},titleFont:{{size:12,weight:'bold'}},titleMarginBottom:5,");
            html.Append($"filter:function(item){{var d=item.dataset;if(d.mat&&window._currentMat&&d.mat!==window._currentMat)return false;if(!d.delta)return true;return d.delta[item.dataIndex]>0;}},");
            html.Append($"callbacks:{{");
            html.Append($"title:function(c){{var d=c[0].dataset;return (d.mat||'')+' • 📅 '+c[0].label}},");
            html.Append($"label:function(ctx){{var d=ctx.dataset;var u=d.unit||'m';var pct=ctx.parsed.y.toFixed(1)+'%';var s=' '+(d.fullLabel||d.label)+': '+pct;if(u!=='%' && d.meters){{var tot=d.total?' / '+d.total+' '+u:'';s+=' • '+d.meters[ctx.dataIndex]+' '+u+tot;}}var add=d.delta?d.delta[ctx.dataIndex]:0;if(add>0){{s+=' (+'+add+(u==='%'?'':' '+u)+' hari ini)';}}return s;}}");
            html.Append($"}}}}");
            html.Append($"}},");
            html.Append($"scales:{{");
            html.Append($"x:{{ticks:{{color:'{tickClr}',font:{{size:10}},maxRotation:45,maxTicksLimit:14}},grid:{{color:'{gridClr}'}}}},");
            html.Append($"y:{{min:0,max:120,ticks:{{stepSize:20,callback:function(v){{return v+'%'}},color:'{tickClr}',font:{{size:10}}}},grid:{{color:'{gridClr}'}}}}");
            html.Append($"}}}}}});");
            html.Append($"function showMat(btn){{");
            html.Append($"const mat=btn.dataset.mat;");
            html.Append($"window._currentMat=mat;");
            html.Append($"chart.data.datasets.forEach(function(ds){{ds._matHidden=ds.mat!==mat;ds.hidden=ds._matHidden;}});");
            html.Append($"chart.update();");
            html.Append($"document.querySelectorAll('.mb').forEach(function(b){{b.style.background=b.dataset.mat===mat?'#1D4ED8':'{btnBg}';b.style.color=b.dataset.mat===mat?'white':'{btnTxt}';}});");
            html.Append($"if(document.getElementById('lgPanel').classList.contains('show'))buildLg();");
            html.Append($"updBtn();");
            html.Append($"}}");
            html.Append("chart.data.datasets.forEach(function(ds){ds._matHidden=ds.mat!==window._currentMat;});");
            html.Append(BuildLegendJs());
            html.Append("</script></body></html>");
            return html.ToString();
        }

        private async void OnUpdateTapped(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await LoadData(true);
        }

        private void OnThemeToggle(object sender, TappedEventArgs e)
        {
            try
            {
                App.Theme.Toggle();
                LblTheme.Text = App.Theme.ThemeIcon;
                BuildChart(_all);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ProgressPage] ThemeToggle: {ex.Message}"); }
        }

        // ── Daily grouping & view-mode toggle ───────────────────────────
        private void BuildDailyGroups()
        {
            _dailyGroups = _dailyProgress
                .Where(p => p.SortDate != DateTime.MinValue)
                .GroupBy(p => p.SortDate.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new DailyProgressGroup
                {
                    Date  = g.Key,
                    Items = g.ToList()
                })
                .ToList();
        }

        private void OnSwitchMode(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not string mode || mode == _viewMode) return;
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            _viewMode = mode;

            bool isSeg = _viewMode == "segment";
            ModeSegBtn.BackgroundColor    = isSeg ? Color.FromArgb("#1D4ED8") : Colors.Transparent;
            ModeHarianBtn.BackgroundColor = isSeg ? Colors.Transparent : Color.FromArgb("#1D4ED8");
            var res = Application.Current?.Resources;
            var muted = res != null && res.TryGetValue("TextMuted", out var tm) ? (Color)tm : Colors.Gray;
            LblModeSeg.TextColor    = isSeg ? Colors.White : muted;
            LblModeHarian.TextColor = isSeg ? muted : Colors.White;

            SegmentList.IsVisible = isSeg;
            HarianList.IsVisible  = !isSeg;
            SearchProgress.Placeholder = isSeg
                ? "Cari segment, rute, span, kabupaten..."
                : "Cari hari, tanggal, nama barang...";

            ApplySearch();
        }

        private async void OnDailyTapped(object sender, TappedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.BindingContext is not DailyProgressGroup g) return;
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            await border.ScaleTo(0.96, 60, Easing.CubicOut);
            await border.ScaleTo(1.0,  60, Easing.CubicIn);
            await Navigation.PushAsync(new DailyDetailPage(g, _project.Name));
        }

        // ── Search / filter ─────────────────────────────────────────────
        private void ApplySearch()
        {
            if (_viewMode == "harian") { ApplyHarianSearch(); return; }

            if (_all.Count == 0) { LblSegCount.Text = "0 segment"; SpanSuggestionsCard.IsVisible = false; return; }
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                SegmentList.ItemsSource = _all;
                LblSegCount.Text = $"{_all.Count} segment";
                SpanSuggestionsCard.IsVisible = false;
                SpanSuggestionsRow.Children.Clear();
                return;
            }
            var q = _searchText.Trim();

            // Build span suggestions from daily progress (unique span+segment combos)
            var spanMatches = new Dictionary<string, (string segName, int segNo, string segRute, string color)>();
            foreach (var p in _dailyProgress)
            {
                if (string.IsNullOrWhiteSpace(p.Span)) continue;
                bool match = p.Span.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                             p.Homebase.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                             p.Segment.Contains(q, StringComparison.OrdinalIgnoreCase);
                if (!match) continue;
                var seg = _all.FirstOrDefault(s => SegmentNameMatch(p.Segment, s.Rute));
                if (seg == null) continue;
                var key = $"{seg.No}|{p.Span}";
                if (!spanMatches.ContainsKey(key))
                    spanMatches[key] = (p.Segment, seg.No, seg.Rute, seg.CardColorMid);
            }

            BuildSpanSuggestionChips(spanMatches);

            var extraNos = new HashSet<int>(spanMatches.Values.Select(v => v.segNo));
            var filtered = _all.Where(x =>
                x.Rute.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                x.No.ToString().Contains(q) ||
                extraNos.Contains(x.No)).ToList();
            SegmentList.ItemsSource = filtered;
            LblSegCount.Text = $"{filtered.Count} / {_all.Count}";
        }

        private void BuildSpanSuggestionChips(Dictionary<string, (string segName, int segNo, string segRute, string color)> matches)
        {
            SpanSuggestionsRow.Children.Clear();
            if (matches.Count == 0)
            {
                SpanSuggestionsCard.IsVisible = false;
                return;
            }

            LblSpanSuggestionsTitle.Text = $"Span ditemukan ({matches.Count})";
            SpanSuggestionsCard.IsVisible = true;

            var res    = Application.Current?.Resources;
            var muted  = res != null && res.TryGetValue("TextMuted",   out var v1) ? (Color)v1 : Color.FromArgb("#94A3B8");
            var border = res != null && res.TryGetValue("BorderClr",   out var v2) ? (Color)v2 : Color.FromArgb("#334155");
            var card2  = res != null && res.TryGetValue("CardBg2",     out var v3) ? (Color)v3 : Color.FromArgb("#0B1220");

            int shown = 0;
            foreach (var kv in matches.OrderBy(x => x.Value.segNo).ThenBy(x => x.Key))
            {
                if (shown++ > 30) break; // cap
                var key  = kv.Key;
                var info = kv.Value;
                var spanName = key.Split('|', 2)[1];
                var spanShort = spanName.Length > 20 ? spanName[..20] + "…" : spanName;

                var chip = new Border
                {
                    BackgroundColor = card2,
                    Stroke          = new SolidColorBrush(Color.FromArgb(info.color)),
                    StrokeThickness = 1.5,
                    StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                    Padding         = new Thickness(12, 8),
                };

                var stack = new VerticalStackLayout { Spacing = 2 };
                stack.Children.Add(new Label
                {
                    Text           = $"Seg {info.segNo} · {spanShort}",
                    FontSize       = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor      = Color.FromArgb(info.color),
                });
                var ruteShort = info.segRute.Length > 24 ? info.segRute[..24] + "…" : info.segRute;
                stack.Children.Add(new Label
                {
                    Text      = ruteShort,
                    FontSize  = 9,
                    TextColor = muted,
                });
                chip.Content = stack;

                var capturedNo   = info.segNo;
                var capturedRute = info.segRute;
                var capturedClr  = info.color;
                var capturedSpan = spanName;
                chip.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () =>
                    {
                        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                        await chip.ScaleTo(0.94, 60, Easing.CubicOut);
                        await chip.ScaleTo(1.0,  60, Easing.CubicIn);
                        var page = new SpanDetailPage(_sheets, capturedNo, capturedRute.Trim(), capturedClr) { InitialSearch = capturedSpan };
                        await Navigation.PushAsync(page);
                    })
                });
                SpanSuggestionsRow.Children.Add(chip);
            }
        }

        private void ApplyHarianSearch()
        {
            SpanSuggestionsCard.IsVisible = false;
            if (_dailyGroups.Count == 0)
            {
                LblHariCount.Text = "0 hari";
                HarianList.ItemsSource = Array.Empty<DailyProgressGroup>();
                return;
            }
            IEnumerable<DailyProgressGroup> f = _dailyGroups;
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var q = _searchText.Trim();
                f = f.Where(g =>
                    g.HariName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    g.TanggalText.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    g.ShortLabel.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    g.Items.Any(x =>
                        (x.NamaBarang ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        (x.Span       ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        (x.Segment    ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)));
            }
            var list = f.ToList();
            HarianList.ItemsSource = list;
            LblHariCount.Text = string.IsNullOrWhiteSpace(_searchText)
                ? $"{list.Count} hari"
                : $"{list.Count} / {_dailyGroups.Count}";
        }

        private void OnProgressSearchChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue ?? "";
            BtnClearProgress.IsVisible = _searchText.Length > 0;
            ApplySearch();
        }

        private void OnClearProgressSearch(object sender, TappedEventArgs e)
        {
            SearchProgress.Text        = "";
            _searchText                = "";
            BtnClearProgress.IsVisible = false;
            ApplySearch();
        }

        private async void OnAiBotClicked(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await Navigation.PushModalAsync(new AiChatPopup());
        }

        private async void OnHamburger(object sender, TappedEventArgs e)
            => await (RootTabbedPage.OpenDrawer?.Invoke() ?? Task.CompletedTask);
    }
}
