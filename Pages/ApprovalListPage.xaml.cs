using StokBarangMAUI.Models;
using StokBarangMAUI.Services;
using System.Text.Json;

namespace StokBarangMAUI.Pages
{
    public partial class ApprovalListPage : ContentPage
    {
        private readonly ApprovalService _approval;
        private readonly AuthService _auth;
        private readonly UploadCoordinator _upload;
        private readonly ProjectConfig _project;
        private readonly DraftService _drafts;

        public ApprovalListPage(ApprovalService approval, AuthService auth, 
            UploadCoordinator upload, ProjectConfig project, DraftService drafts)
        {
            InitializeComponent();
            _approval = approval;
            _auth = auth;
            _upload = upload;
            _project = project;
            _drafts = drafts;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadApprovalsAsync();
        }

        private async Task LoadApprovalsAsync()
        {
            try
            {
                Loader.IsVisible = true;
                Loader.IsRunning = true;
                ApprovalList.Children.Clear();

                var approvals = await _approval.GetPendingApprovalsAsync(_project.Id);
                
                if (approvals.Count == 0)
                {
                    EmptyState.IsVisible = true;
                    return;
                }

                EmptyState.IsVisible = false;
                LblHeader.Text = $"Menunggu Konfirmasi ({approvals.Count})";

                foreach (var item in approvals)
                {
                    ApprovalList.Children.Add(BuildApprovalCard(item));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                Loader.IsVisible = false;
                Loader.IsRunning = false;
            }
        }

        private View BuildApprovalCard(PendingApproval approval)
        {
            var card = new Border
            {
                BackgroundColor = (Color)Application.Current!.Resources["CardBg"],
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                Stroke = new SolidColorBrush((Color)Application.Current.Resources["BorderClr"]),
                StrokeThickness = 1,
                Padding = new Thickness(16)
            };

            var content = new VerticalStackLayout { Spacing = 10 };

            // Header: Type + Date
            var header = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }) };
            
            var typeIcon = approval.Type switch
            {
                "SuratJalan" => "📦",
                "Progress" => "📊",
                "Absensi" => "��",
                _ => "📄"
            };
            
            header.Add(new Label 
            { 
                Text = $"{typeIcon} {approval.Type}", 
                FontSize = 16, 
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color)Application.Current.Resources["TextPrimary"]
            }, 0, 0);
            
            header.Add(new Label 
            { 
                Text = approval.CreatedAt.ToString("dd/MM HH:mm"), 
                FontSize = 12,
                TextColor = (Color)Application.Current.Resources["TextMuted"]
            }, 1, 0);
            
            content.Add(header);

            // Created by
            content.Add(new Label
            {
                Text = $"Dibuat oleh: {approval.CreatedBy}",
                FontSize = 13,
                TextColor = (Color)Application.Current.Resources["TextMuted"]
            });

            // Summary
            content.Add(new Label
            {
                Text = approval.Summary,
                FontSize = 14,
                TextColor = (Color)Application.Current.Resources["TextPrimary"],
                LineBreakMode = LineBreakMode.WordWrap
            });

            // Action buttons (only for admin)
            if (_auth.CanEdit)
            {
                var btnRow = new Grid 
                { 
                    ColumnDefinitions = new ColumnDefinitionCollection(
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = new GridLength(10) },
                        new ColumnDefinition { Width = GridLength.Star }),
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var btnApprove = new Border
                {
                    BackgroundColor = Color.FromArgb("#10B981"),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                    Padding = new Thickness(0, 12)
                };
                btnApprove.Content = new Label 
                { 
                    Text = "✓ Approve", 
                    FontSize = 14, 
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White, 
                    HorizontalOptions = LayoutOptions.Center 
                };
                btnApprove.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () => await OnApprove(approval))
                });

                var btnReject = new Border
                {
                    BackgroundColor = Color.FromArgb("#EF4444"),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                    Padding = new Thickness(0, 12)
                };
                btnReject.Content = new Label 
                { 
                    Text = "✗ Reject", 
                    FontSize = 14, 
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White, 
                    HorizontalOptions = LayoutOptions.Center 
                };
                btnReject.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () => await OnReject(approval))
                });

                btnRow.Add(btnApprove, 0, 0);
                btnRow.Add(btnReject, 2, 0);
                content.Add(btnRow);
            }

            card.Content = content;
            return card;
        }

        private async Task OnApprove(PendingApproval approval)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            bool confirm = await DisplayAlert("Approve", 
                $"Approve draft dari {approval.CreatedBy}?\n\nData akan langsung diupload ke Google Sheets.", 
                "Approve", "Batal");
            
            if (!confirm) return;

            try
            {
                Loader.IsVisible = true;
                Loader.IsRunning = true;

                // Approve di database
                await _approval.ApproveAsync(approval.Id, _auth.CurrentEmail ?? "");

                // Upload data ke Google Sheets
                bool uploaded = await UploadApprovedDataAsync(approval);
                
                if (uploaded)
                {
                    // Delete approval setelah berhasil upload
                    await _approval.DeleteAsync(approval.Id);
                    await DisplayAlert("✅ Berhasil", "Draft telah diapprove dan diupload ke Google Sheets.", "OK");
                }
                else
                {
                    await DisplayAlert("⚠️ Upload Gagal", "Draft diapprove tapi gagal upload. Coba lagi nanti.", "OK");
                }

                await LoadApprovalsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                Loader.IsVisible = false;
                Loader.IsRunning = false;
            }
        }

        private async Task OnReject(PendingApproval approval)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            string reason = await DisplayPromptAsync("Reject", 
                "Alasan reject (opsional):", 
                "Reject", "Batal", 
                placeholder: "Contoh: Data tidak lengkap");
            
            if (reason == null) return; // User cancelled

            try
            {
                Loader.IsVisible = true;
                Loader.IsRunning = true;

                await _approval.RejectAsync(approval.Id, _auth.CurrentEmail ?? "", reason ?? "");
                await _approval.DeleteAsync(approval.Id);
                
                await DisplayAlert("Rejected", "Draft telah direject.", "OK");
                await LoadApprovalsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                Loader.IsVisible = false;
                Loader.IsRunning = false;
            }
        }

        private async Task<bool> UploadApprovedDataAsync(PendingApproval approval)
        {
            try
            {
                // Deserialize data berdasarkan type
                if (approval.Type == "SuratJalan")
                {
                    var drafts = JsonSerializer.Deserialize<List<SuratJalanDraft>>(approval.DraftDataJson);
                    if (drafts == null || drafts.Count == 0) return false;
                    
                    // Upload via UploadCoordinator
                    var (sent, failed, _) = await _upload.UploadAllAsync(_project, _project.DriveFolderIdSuratJalan);
                    return failed == 0;
                }
                else if (approval.Type == "Progress")
                {
                    var drafts = JsonSerializer.Deserialize<List<ProgressDraft>>(approval.DraftDataJson);
                    if (drafts == null || drafts.Count == 0) return false;
                    
                    var (sent, failed, _) = await _upload.UploadAllAsync(_project, "");
                    return failed == 0;
                }
                else if (approval.Type == "Absensi")
                {
                    var drafts = JsonSerializer.Deserialize<List<AbsensiDraft>>(approval.DraftDataJson);
                    if (drafts == null || drafts.Count == 0) return false;
                    
                    var (sent, failed, _) = await _upload.UploadAllAsync(_project, _project.DriveFolderIdAbsensi);
                    return failed == 0;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async void OnRefresh(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await LoadApprovalsAsync();
        }

        private async void OnBack(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await Navigation.PopAsync();
        }
    }
}
