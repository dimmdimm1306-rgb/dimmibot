using StokBarangMAUI.Models;

namespace StokBarangMAUI.Services
{
    public class UploadCoordinator
    {
        private readonly SheetsWriteService _sheets;
        private readonly DriveUploadService _drive;
        private readonly DraftService       _drafts;

        public UploadCoordinator(SheetsWriteService sheets, DriveUploadService drive, DraftService drafts)
        {
            _sheets = sheets;
            _drive  = drive;
            _drafts = drafts;
        }

        // ── Surat Jalan ─────────────────────────────────────────────────
        // 1 draft → N rows (1 per item, share metadata + foto link).
        // Format kolom: A=Tanggal, B=Segment, C=NamaBarang, D=QTY, E=Jenis,
        //               F=NO_SJ, G=PENGIRIM, H=PENERIMA, I=Keterangan, J=DRIVE
        public async Task<(bool Ok, string Msg)> UploadSuratJalanAsync(
            SuratJalanDraft draft, ProjectConfig project, string? photoFolderId = null)
        {
            string sheetName = string.IsNullOrWhiteSpace(project.SheetNameSuratJalan)
                ? "Surat Jalan" : project.SheetNameSuratJalan;

            string photoUrl = "";
            if (!string.IsNullOrWhiteSpace(draft.PhotoPath) && File.Exists(draft.PhotoPath))
            {
                var fname  = $"SJ_{Sanitize(draft.NoSJ)}_{draft.Tanggal:yyyyMMdd}.jpg";
                var folder = string.IsNullOrWhiteSpace(photoFolderId)
                    ? project.DriveFolderIdSuratJalan
                    : photoFolderId;
                var url    = await _drive.UploadPhotoAsync(draft.PhotoPath, fname, folder);
                if (url == null) return (false, "Upload foto ke Drive gagal.");
                photoUrl = url;
            }

            var rows = new List<List<object?>>();
            foreach (var item in draft.Items)
            {
                rows.Add(new List<object?>
                {
                    draft.Tanggal.ToString("dddd, d MMMM yyyy"),  // A: Tanggal
                    draft.Segment,                                  // B: Segment
                    item.NamaBarang,                                // C: Nama Barang
                    item.Qty,                                       // D: QTY
                    item.Jenis,                                     // E: Jenis
                    draft.NoSJ,                                     // F: NO_SJ
                    draft.Pengirim,                                 // G: PENGIRIM
                    draft.Penerima,                                 // H: PENERIMA
                    draft.Keterangan,                               // I: Keterangan
                    photoUrl                                        // J: DRIVE
                });
            }

            var (ok, msg) = await _sheets.AppendRowsAsync(project.SpreadsheetId, sheetName, rows);
            if (ok) await _drafts.DeleteSuratJalanAsync(project.Id, draft.Id);
            return (ok, msg);
        }

        // ── Progress ────────────────────────────────────────────────────
        // 1 draft → N rows (1 per material, share metadata).
        // Format kolom: A=Tanggal, B=(autofill), C=Span, D=Nama Barang, E=Progres,
        //               F=Keterangan, G=(autofill), H=(autofill), I=(autofill)
        // Kolom autofill (B, G, H, I) dikirim sebagai null → cell tidak ditulis,
        // formula array di sheet tetap jalan.
        public async Task<(bool Ok, string Msg)> UploadProgressAsync(
            ProgressDraft draft, ProjectConfig project)
        {
            string sheetName = string.IsNullOrWhiteSpace(project.SheetNameProgress)
                ? "Progress" : project.SheetNameProgress;

            var rows = new List<List<object?>>();
            foreach (var item in draft.Items)
            {
                rows.Add(new List<object?>
                {
                    draft.Tanggal.ToString("dddd, d MMMM yyyy"),  // A: Tanggal
                    null,                                           // B: Segment (autofill)
                    draft.Span,                                     // C: Span
                    item.NamaBarang,                                // D: Nama Barang
                    item.Progres,                                   // E: Progres
                    item.Keterangan,                                // F: Keterangan
                    // G,H,I autofill — tidak ditulis
                });
            }

            var (ok, msg) = await _sheets.AppendRowsAsync(project.SpreadsheetId, sheetName, rows);
            if (ok) await _drafts.DeleteProgressAsync(project.Id, draft.Id);
            return (ok, msg);
        }

        // ── Absensi ─────────────────────────────────────────────────────
        // Format kolom: A=Waktu, B=Segment, C=Pekerjaan, D=Caption,
        //               E=KetuaRegu(joined), F=Waspang, G=DriveURL
        public async Task<(bool Ok, string Msg)> UploadAbsensiAsync(
            AbsensiDraft draft, ProjectConfig project, string? photoFolderId = null)
        {
            string sheetName = string.IsNullOrWhiteSpace(project.SheetNameAbsensi)
                ? "Absensi" : project.SheetNameAbsensi;

            string photoUrl = "";
            if (!string.IsNullOrWhiteSpace(draft.PhotoPath) && File.Exists(draft.PhotoPath))
            {
                var fname  = $"AB_{draft.SavedAt:yyyyMMdd_HHmmss}.jpg";
                var folder = string.IsNullOrWhiteSpace(photoFolderId)
                    ? (string.IsNullOrWhiteSpace(project.DriveFolderIdAbsensi)
                       ? project.DriveFolderIdSuratJalan
                       : project.DriveFolderIdAbsensi)
                    : photoFolderId;
                var url    = await _drive.UploadPhotoAsync(draft.PhotoPath, fname, folder);
                if (url == null) return (false, "Upload foto absensi ke Drive gagal.");
                photoUrl = url;
            }

            var rows = new List<List<object?>>
            {
                new List<object?>
                {
                    draft.SavedAt.ToString("dddd, d MMMM yyyy HH:mm"),  // A: Waktu
                    draft.Segment,                                       // B: Segment
                    draft.Pekerjaan,                                     // C: Pekerjaan
                    draft.Caption,                                       // D: Caption
                    string.Join(", ", draft.KetuaRegu),                  // E: Ketua Regu
                    draft.Waspang,                                       // F: Waspang
                    photoUrl                                             // G: Drive URL
                }
            };

            var (ok, msg) = await _sheets.AppendRowsAsync(project.SpreadsheetId, sheetName, rows);
            if (ok) await _drafts.DeleteAbsensiAsync(project.Id, draft.Id);
            return (ok, msg);
        }

        // ── Batch upload semua draft ───────────────────────────────────
        public async Task<(int Sent, int Failed, string LastError)> UploadAllAsync(
            ProjectConfig project, string? photoFolderId = null)
        {
            int sent = 0, failed = 0;
            string lastErr = "";

            foreach (var d in await _drafts.LoadSuratJalanAsync(project.Id))
            {
                var (ok, msg) = await UploadSuratJalanAsync(d, project, photoFolderId);
                if (ok) sent++; else { failed++; lastErr = msg; }
            }
            foreach (var d in await _drafts.LoadProgressAsync(project.Id))
            {
                var (ok, msg) = await UploadProgressAsync(d, project);
                if (ok) sent++; else { failed++; lastErr = msg; }
            }
            foreach (var d in await _drafts.LoadAbsensiAsync(project.Id))
            {
                var (ok, msg) = await UploadAbsensiAsync(d, project, photoFolderId);
                if (ok) sent++; else { failed++; lastErr = msg; }
            }
            return (sent, failed, lastErr);
        }

        private static string Sanitize(string s) =>
            string.Concat(s.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
    }
}
