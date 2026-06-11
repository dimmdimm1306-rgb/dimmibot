using Microsoft.Maui.Graphics;

namespace StokBarangMAUI.Controls;

public partial class StatusBadge : ContentView
{
    public static readonly BindableProperty BadgeTextProperty =
        BindableProperty.Create(nameof(BadgeText), typeof(string), typeof(StatusBadge), string.Empty);

    public static readonly BindableProperty BadgeTextColorProperty =
        BindableProperty.Create(nameof(BadgeTextColor), typeof(Color), typeof(StatusBadge), Color.FromArgb("#6EE7B7"));

    public static readonly BindableProperty BadgeBackgroundProperty =
        BindableProperty.Create(nameof(BadgeBackground), typeof(Color), typeof(StatusBadge), Color.FromArgb("#073B31"));

    public StatusBadge()
    {
        InitializeComponent();
    }

    public string BadgeText
    {
        get => (string)GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    public Color BadgeTextColor
    {
        get => (Color)GetValue(BadgeTextColorProperty);
        set => SetValue(BadgeTextColorProperty, value);
    }

    public Color BadgeBackground
    {
        get => (Color)GetValue(BadgeBackgroundProperty);
        set => SetValue(BadgeBackgroundProperty, value);
    }
}
