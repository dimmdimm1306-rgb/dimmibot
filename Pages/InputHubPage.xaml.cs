using StokBarangMAUI.Models;
using StokBarangMAUI.Services;
using System.Text.Json;

namespace StokBarangMAUI.Pages
{
    public partial class InputHubPage : ContentPage
    {
        private readonly DraftService         _drafts;
        private readonly ProjectConfig        _project;
        private readonly GoogleSheetsService  _sheets;
        private readonly GoogleOAuthService   _gauth;
        private readonly UploadCoordinator    _upload;
        private readonly DriveUploadService   _drive;
        private readonly AuthService          _auth;
        private readonly ApprovalService      _approval;

        public InputHubPage(DraftService drafts, ProjectConfig project,
                            GoogleSheetsService sheets, GoogleOAuthService gauth,
                            UploadCoordinator upload, DriveUploadService drive,
                            AuthService auth, ApprovalService approval)
        {
            InitializeComponent();
            _drafts   = drafts;
            _project  = project;
            _sheets   = sheets;
            _gauth    = gauth;
            _upload   = upload;
            _drive    = drive;
            _auth     = auth;
            _approval = approval;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LblHeader.Text = _project.Name;
            RefreshGoogleUi();
            await RefreshBadgesAsync();
        }

        private string FolderIdKey   => $"drive.folder.{_project.Id}.id";
        private string FolderNameKey => $"drive.folder.{_project.Id}.name";

        private string SelectedFolderId   => Preferences.Get(FolderIdKey, "");
        private string SelectedFolderName => Preferences.Get(FolderNameKey, "");

        private void RefreshGoogleUi()
        {
            if (_gauth.IsSignedIn)
            {
                LblGoogleStatus.Text       = $"✓ Login: {_gauth.AccountEmail}";
                BtnGoogleSignIn.IsVisible  = false;
                BtnGoogleSignOut.IsVisible = true;
                FolderCard.IsVisible       = !string.IsNullOrWhiteSpace(_project.DriveFolderIdSuratJalan);

                var folderName = SelectedFolderName;
                LblFolder.Text = string.IsNullOrWhiteSpace(folderName)
                    ? "Belum dipilih — wajib pilih sebelum upload"
                    : folderName;
                LblFolder.TextColor = string.IsNullOrWhiteSpace(folderName)
                    ? Color.FromArgb("#F59E0B")
                    : (Color)Application.Current!.Resources["TextPrimary"];

                BtnUploadAll.IsVisible = true;
                
                // Show approval button for admin
                BtnApprovals.IsVisible = _auth.CanEdit;
            }
            else
            {
                LblGoogleStatus.Text = _gauth.IsConfigured
                    ? "Sign in Google untuk akses Input & Upload."
                    : "⚠ OAuth Client ID belum diset di kode.";
                BtnGoogleSignIn.IsVisible  = _gauth.IsConfigured;
                BtnGoogleSignOut.IsVisible = false;
                FolderCard.IsVisible       = false;
                BtnUploadAll.IsVisible     = false;
                BtnApprovals.IsVisible     = false;
            }
        }

        private async Task RefreshBadgesAsync()
        {
            try
            {
                var sj = await _drafts.LoadSuratJalanAsync(_project.Id);
                var pg = await _drafts.LoadProgressAsync(_project.Id);
                BadgeSJ.IsVisible  = sj.Count > 0;
                LblBadgeSJ.Text    = sj.Count.ToString();
                BadgePG.IsVisible  = pg.Count > 0;
                LblBadgePG.Text    = pg.Count.ToString();
                
                // Badge for pending approvals (admin only)
                if (_auth.CanEdit)
                {
                    var pendingCount = await _approval.GetPendingCountAsync(_project.Id);
                    BadgeApproval.IsVisible = pendingCount > 0;
                    LblBadgeApproval.Text = pendingCount.ToString();
                }
            }
            catch { }
        }

        private async Task<bool> RequireSignInAsync()
        {
            if (_gauth.IsSignedIn) return true;
            await DisplayAlert("Sign in dulu", "Sign in Google untuk pakai fitur Input.", "OK");
            return false;
        }

        private async void OnGoogleSignIn(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            var (ok, msg) = await _gauth.SignInAsync();
            await DisplayAlert(ok ? "Sukses" : "Gagal", msg, "OK");
            RefreshGoogleUi();
        }

        private async void OnGoogleSignOut(object sender, TappedEventArgs e)
        {
            bool ok = await DisplayAlert("Sign out", "Logout dari akun Google?", "Ya", "Batal");
            if (!ok) return;
            _gauth.SignOut();
            RefreshGoogleUi();
        }

        private async void OnPickFolder(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            if (string.IsNullOrWhiteSpace(_project.DriveFolderIdSuratJalan))
            { await DisplayAlert("Belum di-set", "Drive root folder belum di-set di project config.", "OK"); return; }
            if (!await RequireSignInAsync()) return;

            var picker = new DriveFolderPickerPage(_drive, _project.DriveFolderIdSuratJalan);
            await Navigation.PushModalAsync(picker);
            var result = await picker.WaitForResultAsync();
            if (result is { } pick)
            {
                Preferences.Set(FolderIdKey,   pick.Id);
                Preferences.Set(FolderNameKey, pick.Name);
                RefreshGoogleUi();
            }
        }

        private async void OnOpenSuratJalan(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            if (!await RequireSignInAsync()) return;
            await Navigation.PushAsync(new SuratJalanInputPage(_gauth, _drafts, _project, _sheets));
            await RefreshBadgesAsync();
        }

        private async void OnOpenProgress(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            if (!await RequireSignInAsync()) return;
            await Navigation.PushAsync(new ProgressInputPage(_gauth, _drafts, _project, _sheets));
            await RefreshBadgesAsync();
        }

        private async void OnUploadAll(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            var sj = await _drafts.LoadSuratJalanAsync(_project.Id);
            var pg = await _drafts.LoadProgressAsync(_project.Id);
            int total = sj.Count + pg.Count;
            if (total == 0) { await DisplayAlert("Info", "Tidak ada draft untuk dikirim.", "OK"); return; }

            bool hasPhoto = sj.Any(d => !string.IsNullOrWhiteSpace(d.PhotoPath) && File.Exists(d.PhotoPath));
            if (hasPhoto && string.IsNullOrWhiteSpace(SelectedFolderId))
            {
                await DisplayAlert("Pilih folder dulu",
                    "Ada draft Surat Jalan dengan foto. Pilih folder Drive tujuan dulu sebelum upload.", "OK");
                return;
            }

            // CEK APAKAH USER ADALAH ADMIN
            bool isAdmin = _auth.CanEdit;
            
            if (!isAdmin)
            {
                // Non-admin: Submit untuk approval
                await SubmitForApprovalAsync(sj, pg);
                return;
            }

            // ADMIN: KONFIRMASI WAJIB & UPLOAD LANGSUNG
            if (sj.Count > 0)
            {
                var detailList = new System.Text.StringBuilder();
                detailList.AppendLine("SURAT JALAN YANG AKAN DIUPLOAD:\n");
                
                foreach (var draft in sj.Take(10))
                {
                    detailList.AppendLine($"- {draft.Tanggal:dd/MM/yyyy} - No: {draft.NoSJ}");
                    detailList.AppendLine($"  Pengirim: {draft.Pengirim}");
                    detailList.AppendLine($"  Penerima: {draft.Penerima}");
                    detailList.AppendLine($"  Segment: {draft.Segment}");
                    if (draft.Items?.Count > 0)
                    {
                        detailList.AppendLine($"  Item: {draft.Items.Count} barang");
                        foreach (var item in draft.Items.Take(3))
                            detailList.AppendLine($"    * {item.NamaBarang} ({item.Qty} pcs, {item.Jenis})");
                        if (draft.Items.Count > 3)
                            detailList.AppendLine($"    * ... dan {draft.Items.Count - 3} item lainnya");
                    }
                    if (!string.IsNullOrWhiteSpace(draft.PhotoPath))
                        detailList.AppendLine($"  Foto: Ada");
                    detailList.AppendLine();
                }
                
                if (sj.Count > 10)
                    detailList.AppendLine($"... dan {sj.Count - 10} surat jalan lainnya");
                
                bool reviewConfirm = await DisplayAlert(
                    "KONFIRMASI UPLOAD",
                    detailList.ToString(),
                    "Lanjutkan", "Batal");
                
                if (!reviewConfirm) return;
            }

            string folderInfo = string.IsNullOrWhiteSpace(SelectedFolderName) ? "(root)" : SelectedFolderName;
            bool confirm = await DisplayAlert("Upload Final",
                $"Kirim {total} draft ({sj.Count} SJ + {pg.Count} Progress)?\n\nFolder foto: {folderInfo}\n\nData akan dikirim ke Google Sheets!",
                "Ya, Kirim Sekarang", "Batal");
            if (!confirm) return;

            BtnUploadAll.IsEnabled = false;
            try
            {
                var folderId = string.IsNullOrWhiteSpace(SelectedFolderId)
                    ? _project.DriveFolderIdSuratJalan
                    : SelectedFolderId;
                var (sent, failed, lastErr) = await _upload.UploadAllAsync(_project, folderId);
                var msg = $"Terkirim: {sent}\nGagal: {failed}";
                if (failed > 0) msg += $"\n\nError terakhir:\n{lastErr}";
                await DisplayAlert(failed == 0 ? "Sukses" : "Selesai dengan error", msg, "OK");
                await RefreshBadgesAsync();
            }
            finally { BtnUploadAll.IsEnabled = true; }
        }

        private async Task SubmitForApprovalAsync(List<SuratJalanDraft> sj, List<ProgressDraft> pg)
        {
            try
            {
                var summary = new System.Text.StringBuilder();
                summary.AppendLine($"{sj.Count} Surat Jalan");
                summary.AppendLine($"{pg.Count} Progress");
                
                bool confirm = await DisplayAlert(
                    "Submit untuk Approval",
                    $"Draft Anda akan dikirim ke admin untuk diapprove:\n\n{summary}\n\nLanjutkan?",
                    "Ya, Submit", "Batal");
                
                if (!confirm) return;

                BtnUploadAll.IsEnabled = false;
                
                // Submit Surat Jalan
                if (sj.Count > 0)
                {
                    var sjSummary = $"{sj.Count} Surat Jalan - {sj.First().Tanggal:dd/MM/yyyy}";
                    await _approval.SubmitForApprovalAsync(
                        _project.Id,
                        _gauth.AccountEmail ?? "Unknown",
                        "SuratJalan",
                        sj,
                        sjSummary
                    );
                }

                // Submit Progress
                if (pg.Count > 0)
                {
                    var pgSummary = $"{pg.Count} Progress - {pg.First().Tanggal:dd/MM/yyyy}";
                    await _approval.SubmitForApprovalAsync(
                        _project.Id,
                        _gauth.AccountEmail ?? "Unknown",
                        "Progress",
                        pg,
                        pgSummary
                    );
                }

                var adminEmails = string.Join(", ", StokBarangMAUI.Services.AuthService.GetWhitelistSnapshot());
                await DisplayAlert(
                    "Berhasil Submit",
                    $"Draft Anda telah dikirim ke admin ({adminEmails}) untuk diapprove.\n\nAnda akan mendapat notifikasi setelah diapprove.",
                    "OK");
                
                await RefreshBadgesAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                BtnUploadAll.IsEnabled = true;
            }
        }

        private async void OnOpenApprovals(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            if (!_auth.CanEdit)
            {
                await DisplayAlert("Akses Ditolak", "Hanya admin yang bisa mengakses halaman approval.", "OK");
                return;
            }
            
            await Navigation.PushAsync(new ApprovalListPage(_approval, _auth, _upload, _project, _drafts));
            await RefreshBadgesAsync();
        }

        private async void OnAiBotClicked(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await Navigation.PushModalAsync(new AiChatPopup());
        }

        private async void OnHamburger(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            if (RootTabbedPage.OpenDrawer != null) await RootTabbedPage.OpenDrawer();
        }
    }
}
