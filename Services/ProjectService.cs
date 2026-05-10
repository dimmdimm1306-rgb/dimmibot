using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StokBarangMAUI.Models;

namespace StokBarangMAUI.Services
{
    public class ProjectService
    {
        private static readonly string FilePath =
            Path.Combine(FileSystem.AppDataDirectory, "projects.json");

        private static readonly string RemoteConfigRefPath =
            Path.Combine(FileSystem.AppDataDirectory, "remote_config_ref.json");

        private List<ProjectConfig>? _cache;

        private const string OldMainId   = "13XJL_oUh1qyfTfq5Cqz4EuwvNLiIyKjkRbYLwRHyQOQ";
        private const string OldResumeId = "1eyRssjlL_UCrs8t2D33ChQg4rl8JnRUfwlbwHlatIcg";
        private const string NewMainId   = "1kWcBTcjSQIhQtmvUTyvszBii0c4BRkd5";
        private const string NewResumeId = "1d9GKDxcYGwURcVp-BvSYW4W0YQiNVZt_";

        // ── REMOTE CONFIG URL (Hardcoded) ────────────────────────────────
        // URL ini akan otomatis di-sync setiap kali app dibuka.
        // Ganti URL ini dengan Google Drive share link atau GitHub raw URL
        // yang berisi JSON ProjectConfig.
        // 
        // Contoh Google Drive: https://drive.google.com/file/d/FILE_ID/view
        // Contoh GitHub raw: https://raw.githubusercontent.com/user/repo/main/config.json
        //
        // Kosongkan jika tidak ingin auto-sync dari remote.
        private const string HARDCODED_REMOTE_CONFIG_URL = "https://drive.google.com/file/d/1cyeQkLcvmFdUgqOOCd2gloG2jBEH-xZz/view?usp=sharing";

        // ── Remote Config Ref ────────────────────────────────────────────
        // Simpan URL yang return JSON ProjectConfig langsung.
        // Bisa Google Drive share link, GitHub raw, atau URL apapun.
        public class RemoteConfigRef
        {
            public string Url { get; set; } = "";
        }

        public async Task<RemoteConfigRef?> GetRemoteConfigRefAsync()
        {
            try
            {
                if (!File.Exists(RemoteConfigRefPath)) return null;
                var json = await File.ReadAllTextAsync(RemoteConfigRefPath);
                return JsonSerializer.Deserialize<RemoteConfigRef>(json);
            }
            catch { return null; }
        }

        public async Task SaveRemoteConfigRefAsync(string url)
        {
            var json = JsonSerializer.Serialize(new RemoteConfigRef { Url = url });
            await File.WriteAllTextAsync(RemoteConfigRefPath, json);
        }

        // ── Sync dari remote URL (background, tidak blocking) ────────────
        public async Task TrySyncFromRemoteAsync(GoogleSheetsService sheets)
        {
            try
            {
                // Prioritas 1: Gunakan hardcoded URL jika ada
                string? urlToSync = null;
                if (!string.IsNullOrWhiteSpace(HARDCODED_REMOTE_CONFIG_URL))
                {
                    urlToSync = HARDCODED_REMOTE_CONFIG_URL;
                }
                else
                {
                    // Prioritas 2: Gunakan URL dari remote config ref (user-defined)
                    var @ref = await GetRemoteConfigRefAsync();
                    if (@ref != null && !string.IsNullOrWhiteSpace(@ref.Url))
                        urlToSync = @ref.Url;
                }

                if (string.IsNullOrWhiteSpace(urlToSync)) return;

                var remote = await sheets.FetchRemoteProjectConfigFromUrlAsync(urlToSync);
                if (remote == null) return;

                var list = await GetAllAsync();
                var existing = list.FirstOrDefault(p => p.Id == remote.Id);
                if (existing != null)
                    list[list.IndexOf(existing)] = remote;
                else
                    list.Add(remote);

                _cache = list;
                await PersistAsync();
            }
            catch { /* silent */ }
        }

        // ── CRUD ─────────────────────────────────────────────────────────
        public async Task<List<ProjectConfig>> GetAllAsync()
        {
            if (_cache != null) return _cache;

            if (File.Exists(FilePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(FilePath);
                    _cache   = JsonSerializer.Deserialize<List<ProjectConfig>>(json) ?? new();
                }
                catch { _cache = new(); }
            }

            if (_cache == null || _cache.Count == 0)
            {
                _cache = DefaultProjects();
                await PersistAsync();
            }
            else
            {
                bool migrated = false;
                foreach (var p in _cache)
                {
                    if (p.SpreadsheetId == OldMainId)
                    { p.SpreadsheetId = NewMainId; migrated = true; }
                    if (p.ResumeSpreadsheetId == OldResumeId)
                    { p.ResumeSpreadsheetId = NewResumeId; migrated = true; }
                    if (p.SpreadsheetId == NewMainId && string.IsNullOrWhiteSpace(p.GidAktualStok))
                    { p.GidAktualStok = "1692657123"; migrated = true; }
                    if (p.SpreadsheetId == NewMainId && string.IsNullOrWhiteSpace(p.DriveFolderIdSuratJalan))
                    { p.DriveFolderIdSuratJalan = "1LCfHKCK5hm_f4iqyOXuUo5o4xtBgmMAE"; migrated = true; }
                    if (p.Name == "FTTH JAWA TENGAH")
                    { p.Name = "Project"; migrated = true; }
                    if (p.Description == "Program Percepatan FTTH Regional Jawa Tengah & DIY")
                    { p.Description = ""; migrated = true; }
                }
                if (migrated) await PersistAsync();
            }

            return _cache;
        }

        public async Task SaveAsync(ProjectConfig project)
        {
            var list = await GetAllAsync();
            var idx  = list.FindIndex(p => p.Id == project.Id);
            if (idx >= 0) list[idx] = project;
            else          list.Add(project);
            await PersistAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var list = await GetAllAsync();
            var project = list.FirstOrDefault(p => p.Id == id);
            if (project == null) return;

            // Delete draft files for this project
            string Safe(string s) => string.Concat(s.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            var safeId = Safe(project.Id);
            
            DeleteFile(Path.Combine(FileSystem.AppDataDirectory, $"draft_sj_{safeId}.json"));
            DeleteFile(Path.Combine(FileSystem.AppDataDirectory, $"draft_pg_{safeId}.json"));
            DeleteFile(Path.Combine(FileSystem.AppDataDirectory, $"draft_ab_{safeId}.json"));

            // Delete Google Sheets cache
            if (!string.IsNullOrWhiteSpace(project.SpreadsheetId))
            {
                var cacheFile = Path.Combine(FileSystem.AppDataDirectory, 
                    $"cache_{GetSafeHash(project.SpreadsheetId)}.json");
                DeleteFile(cacheFile);
            }

            // Delete span cache files for this project
            if (!string.IsNullOrWhiteSpace(project.SpreadsheetId))
            {
                var hash = GetSafeHash(project.SpreadsheetId);
                var spanFiles = Directory.GetFiles(FileSystem.AppDataDirectory, $"span_*_{hash}.json");
                foreach (var file in spanFiles)
                {
                    DeleteFile(file);
                }
            }

            list.RemoveAll(p => p.Id == id);
            await PersistAsync();
        }

        public async Task CleanupOrphanedCaches()
        {
            var appDataDir = FileSystem.AppDataDirectory;
            
            // Collect valid spreadsheet hashes from existing projects
            var projects = await GetAllAsync();
            var validHashes = new HashSet<string>();
            foreach (var p in projects)
            {
                if (!string.IsNullOrWhiteSpace(p.SpreadsheetId))
                {
                    validHashes.Add(GetSafeHash(p.SpreadsheetId));
                }
            }

            // Clean main cache files (cache_{hash}.json)
            var cacheFiles = Directory.GetFiles(appDataDir, "cache_*.json");
            foreach (var file in cacheFiles)
            {
                var fileName = Path.GetFileName(file);
                // Extract hash from "cache_{hash}.json" (6 = "cache_".Length, 5 = ".json".Length)
                var hashPart = fileName.Substring(6, fileName.Length - 6 - 5);
                if (validHashes.Contains(hashPart)) continue;
                DeleteFile(file);
            }

            // Clean span cache files (span_{segment}_{hash}.json)
            var spanFiles = Directory.GetFiles(appDataDir, "span_*.json");
            foreach (var file in spanFiles)
            {
                var fileName = Path.GetFileName(file);
                var parts = fileName.Split('_');
                if (parts.Length >= 3)
                {
                    // Hash is in parts[2], remove ".json" suffix
                    var hashPart = parts[2].Substring(0, parts[2].Length - 5);
                    if (validHashes.Contains(hashPart)) continue;
                }
                DeleteFile(file);
            }
        }

        private static void DeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static string GetSafeHash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash)
                .Replace('/', '_')
                .Replace('+', '-')
                .Replace("=", ""); // Remove padding with string replace (empty char literal invalid)
        }

        private async Task PersistAsync()
        {
            var json = JsonSerializer.Serialize(_cache,
                new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(FilePath, json);
        }

        private static List<ProjectConfig> DefaultProjects() => new()
        {
            new ProjectConfig
            {
                Id          = "ftth-jawatengah-2024",
                Name        = "Project",
                Description = "",
                Icon        = "📡",
                Color       = "#1D4ED8",

                SpreadsheetId = NewMainId,
                GidStok        = "1736939395",
                GidAktualStok  = "1692657123",
                GidSuratJalan  = "1213940465",
                GidProgress    = "1637178585",

                ResumeSpreadsheetId = NewResumeId,
                GidResume           = "1316342418",

                DriveFolderIdSuratJalan = "1LCfHKCK5hm_f4iqyOXuUo5o4xtBgmMAE",

                SegmentGids = new()
                {
                    { "1", "1111450998" },
                    { "2", "1943599466" },
                    { "3", "1686621433" },
                    { "4", "842756326"  },
                    { "5", "1472813414" },
                    { "6", "251176161"  },
                },
                SegmentNames = new()
                {
                    { "1", "CIREBON - BREBES - TEGAL - PEKALONGAN - INDRAMAYU - SEMARANG" },
                    { "2", "TASIKMALAYA - BANJAR" },
                    { "3", "BANYUMAS - CILACAP - KEBUMEN - PURWOREJO" },
                    { "4", "SUKOHARJO - KLATEN - SURAKARTA - WONOGIRI" },
                    { "5", "SRAGEN - KARANG ANYAR" },
                    { "6", "GROBOGAN - BLORA" },
                }
            }
        };
    }
}
