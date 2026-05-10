using StokBarangMAUI.Models;

namespace StokBarangMAUI.Pages
{
    public partial class SuratJalanDetailPage : ContentPage
    {
        private readonly SuratJalanGroup _group;

        public SuratJalanDetailPage(SuratJalanGroup group)
        {
            InitializeComponent();
            _group = group;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            var badgeColor = Color.FromArgb(_group.BadgeColor);

            StripColor.Color            = badgeColor;
            BadgeBorder.BackgroundColor = badgeColor;
            LblNama.Text                = _group.NamaBarang;
            LblSegment.Text             = _group.Segment;
            LblJenis.Text               = _group.JenisPendek;
            LblTanggal.Text             = _group.Tanggal;
            LblNoSJ.Text                = _group.HasNoSJ ? _group.NoSJ : "–";
            LblPengirim.Text            = string.IsNullOrWhiteSpace(_group.Pengirim)  ? "–" : _group.Pengirim;
            LblPenerima.Text            = string.IsNullOrWhiteSpace(_group.Penerima)  ? "–" : _group.Penerima;
            LblSegmentFull.Text         = _group.Segment;

            if (string.IsNullOrWhiteSpace(_group.Keterangan))
            {
                KetDivider.IsVisible = false;
                KetRow.IsVisible     = false;
            }
            else
            {
                LblKeterangan.Text = _group.Keterangan;
            }

            BtnBukaSJ.IsVisible = _group.HasLink;

            BuildItemsPanel();
        }

        private void BuildItemsPanel()
        {
            ItemsPanel.Children.Clear();
            bool first = true;

            foreach (var item in _group.Items)
            {
                if (!first)
                    ItemsPanel.Children.Add(new BoxView
                    {
                        HeightRequest   = 1,
                        Color           = Color.FromArgb("#2563EB30"),
                        Margin          = new Thickness(0, 6),
                    });
                first = false;

                var row = new Grid { ColumnSpacing = 10 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var nameLbl = new Label
                {
                    Text            = item.NamaBarang,
                    FontSize        = 14,
                    FontAttributes  = FontAttributes.Bold,
                    TextColor       = Colors.White,
                    LineBreakMode   = LineBreakMode.WordWrap,
                    VerticalOptions = LayoutOptions.Center,
                };

                var qtyBorder = new Border
                {
                    BackgroundColor = Color.FromArgb("#162032"),
                    StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Stroke          = new SolidColorBrush(Color.FromArgb("#2563EB")),
                    StrokeThickness = 1,
                    Padding         = new Thickness(12, 6),
                    VerticalOptions = LayoutOptions.Center,
                    Content         = new Label
                    {
                        Text           = item.QtyText,
                        FontSize       = 14,
                        FontAttributes = FontAttributes.Bold,
                        TextColor      = Color.FromArgb("#93C5FD"),
                    },
                };

                Grid.SetColumn(qtyBorder, 1);
                row.Children.Add(nameLbl);
                row.Children.Add(qtyBorder);
                ItemsPanel.Children.Add(row);
            }
        }

        private async void OnBukaSJ(object sender, TappedEventArgs e)
        {
            var preview = new DrivePreviewPage(
                _group.DriveUrl,
                title:    _group.NamaBarang,
                subtitle: $"{_group.Segment}  •  {_group.Tanggal}");
            await Navigation.PushModalAsync(preview, animated: true);
        }
    }
}
