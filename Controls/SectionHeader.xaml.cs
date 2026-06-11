namespace StokBarangMAUI.Controls;

public partial class SectionHeader : ContentView
{
    public static readonly BindableProperty HeaderTitleProperty =
        BindableProperty.Create(nameof(HeaderTitle), typeof(string), typeof(SectionHeader), string.Empty);

    public static readonly BindableProperty HeaderSubtitleProperty =
        BindableProperty.Create(nameof(HeaderSubtitle), typeof(string), typeof(SectionHeader), string.Empty);

    public SectionHeader()
    {
        InitializeComponent();
    }

    public string HeaderTitle
    {
        get => (string)GetValue(HeaderTitleProperty);
        set => SetValue(HeaderTitleProperty, value);
    }

    public string HeaderSubtitle
    {
        get => (string)GetValue(HeaderSubtitleProperty);
        set => SetValue(HeaderSubtitleProperty, value);
    }
}
