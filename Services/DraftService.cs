using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using StokBarangMAUI.Models;

namespace StokBarangMAUI.Services
{
    // Storage offline untuk draft Surat Jalan & Progress sebelum dikirim ke spreadsheet.
    // Data disimpan per-project di file JSON di AppDataDirectory.
    // Foto disimpan di subfolder "draft_photos/".
    public class DraftService
    {
        private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

        private static string SuratJalanFile(string projectId) =>
            Path.Combine(FileSystem.AppDataDirectory, $"draft_sj_{Safe(projectId)}.json");
        private static string ProgressFile(string projectId) =>
            Path.Combine(FileSystem.AppDataDirectory, $"draft_pg_{Safe(projectId)}.json");
        private static string AbsensiFile(string projectId) =>
            Path.Combine(FileSystem.AppDataDirectory, $"draft_ab_{Safe(projectId)}.json");
        private static string PhotoDir =>
            Path.Combine(FileSystem.AppDataDirectory, "draft_photos");

        private static string Safe(string s) =>
            string.Concat(s.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));

        // ── Surat Jalan ─────────────────────────────────────────────────
        public async Task<List<SuratJalanDraft>> LoadSuratJalanAsync(string projectId)
        {
            try
            {
                var path = SuratJalanFile(projectId);
                if (!File.Exists(path)) return new();
                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<List<SuratJalanDraft>>(json) ?? new();
            }
            catch { return new(); }
        }

        public async Task SaveSuratJalanAsync(string projectId, SuratJalanDraft draft)
        {
            var list = await LoadSuratJalanAsync(projectId);
            var idx  = list.FindIndex(x => x.Id == draft.Id);
            if (idx >= 0) list[idx] = draft;
            else          list.Add(draft);
            await PersistAsync(SuratJalanFile(projectId), list);
        }

        public async Task DeleteSuratJalanAsync(string projectId, string id)
        {
            var list = await LoadSuratJalanAsync(projectId);
            var item = list.FirstOrDefault(x => x.Id == id);
            list.RemoveAll(x => x.Id == id);
            if (item != null && !string.IsNullOrWhiteSpace(item.PhotoPath))
                TryDeleteFile(item.PhotoPath);
            await PersistAsync(SuratJalanFile(projectId), list);
        }

        // ── Progress ────────────────────────────────────────────────────
        public async Task<List<ProgressDraft>> LoadProgressAsync(string projectId)
        {
            try
            {
                var path = ProgressFile(projectId);
                if (!File.Exists(path)) return new();
                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<List<ProgressDraft>>(json) ?? new();
            }
            catch { return new(); }
        }

        public async Task SaveProgressAsync(string projectId, ProgressDraft draft)
        {
            var list = await LoadProgressAsync(projectId);
            var idx  = list.FindIndex(x => x.Id == draft.Id);
            if (idx >= 0) list[idx] = draft;
            else          list.Add(draft);
            await PersistAsync(ProgressFile(projectId), list);
        }

        public async Task DeleteProgressAsync(string projectId, string id)
        {
            var list = await LoadProgressAsync(projectId);
            list.RemoveAll(x => x.Id == id);
            await PersistAsync(ProgressFile(projectId), list);
        }

        // ── Absensi ─────────────────────────────────────────────────────
        public async Task<List<AbsensiDraft>> LoadAbsensiAsync(string projectId)
        {
            try
            {
                var path = AbsensiFile(projectId);
                if (!File.Exists(path)) return new();
                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<List<AbsensiDraft>>(json) ?? new();
            }
            catch { return new(); }
        }

        public async Task SaveAbsensiAsync(string projectId, AbsensiDraft draft)
        {
            var list = await LoadAbsensiAsync(projectId);
            var idx  = list.FindIndex(x => x.Id == draft.Id);
            if (idx >= 0) list[idx] = draft;
            else          list.Add(draft);
            await PersistAsync(AbsensiFile(projectId), list);
        }

        public async Task DeleteAbsensiAsync(string projectId, string id)
        {
            var list = await LoadAbsensiAsync(projectId);
            var item = list.FirstOrDefault(x => x.Id == id);
            list.RemoveAll(x => x.Id == id);
            if (item != null && !string.IsNullOrWhiteSpace(item.PhotoPath))
                TryDeleteFile(item.PhotoPath);
            await PersistAsync(AbsensiFile(projectId), list);
        }

        // ── Photo storage ───────────────────────────────────────────────
        // Copy foto dari source FileResult ke folder app, return absolute path baru.
        // Pakai Guid + ms timestamp supaya filename unik kalau user pick beberapa foto sekaligus.
        public async Task<string> SavePhotoAsync(FileResult source, string draftId)
        {
            Directory.CreateDirectory(PhotoDir);
            var ext  = Path.GetExtension(source.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
            var stamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
            var dest = Path.Combine(PhotoDir, $"sj_{draftId}_{stamp}_{unique}{ext}");
            using var src = await source.OpenReadAsync();
            using var dst = File.Create(dest);
            await src.CopyToAsync(dst);
            return dest;
        }

        private static void TryDeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static async Task PersistAsync<T>(string path, T data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, _opts);
                await File.WriteAllTextAsync(path, json);
            }
            catch { /* disk full / permission — abaikan */ }
        }

        // ── Cleanup orphaned photos ───────────────────────────────────
        public async Task CleanupOrphanedPhotosAsync()
        {
            var photoDir = PhotoDir;
            if (!Directory.Exists(photoDir)) return;

            var referencedPhotos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Scan all draft JSON files in AppDataDirectory
            var appDataDir = FileSystem.AppDataDirectory;
            var draftFiles = Directory.GetFiles(appDataDir, "draft_*.json");

            foreach (var file in draftFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    if (file.Contains("draft_sj_", StringComparison.OrdinalIgnoreCase))
                    {
                        var drafts = JsonSerializer.Deserialize<List<SuratJalanDraft>>(json);
                        if (drafts == null) continue;
                        foreach (var draft in drafts)
                            if (!string.IsNullOrWhiteSpace(draft.PhotoPath))
                                referencedPhotos.Add(draft.PhotoPath);
                    }
                    else if (file.Contains("draft_ab_", StringComparison.OrdinalIgnoreCase))
                    {
                        var drafts = JsonSerializer.Deserialize<List<AbsensiDraft>>(json);
                        if (drafts == null) continue;
                        foreach (var draft in drafts)
                            if (!string.IsNullOrWhiteSpace(draft.PhotoPath))
                                referencedPhotos.Add(draft.PhotoPath);
                    }
                    // Progress drafts don't have photos, skip draft_pg_ files
                }
                catch { /* ignore corrupt or unreadable files */ }
            }

            // Delete photos not referenced by any draft
            foreach (var photoPath in Directory.GetFiles(photoDir))
            {
                if (!referencedPhotos.Contains(photoPath))
                    TryDeleteFile(photoPath);
            }
        }
    }
}
