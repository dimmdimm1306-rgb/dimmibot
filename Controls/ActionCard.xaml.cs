using Microsoft.Maui.Graphics;

namespace StokBarangMAUI.Controls;

public partial class ActionCard : ContentView
{
    public static readonly BindableProperty CardTitleProperty =
        BindableProperty.Create(nameof(CardTitle), typeof(string), typeof(ActionCard), string.Empty);

    public static readonly BindableProperty CardSubtitleProperty =
        BindableProperty.Create(nameof(CardSubtitle), typeof(string), typeof(ActionCard), string.Empty);

    public static readonly BindableProperty IconTextProperty =
        BindableProperty.Create(nameof(IconText), typeof(string), typeof(ActionCard), string.Empty);

    public static readonly BindableProperty AccentColorProperty =
        BindableProperty.Create(nameof(AccentColor), typeof(Color), typeof(ActionCard), Color.FromArgb("#22D3EE"));

    public ActionCard()
    {
        InitializeComponent();
    }

    public string CardTitle
    {
        get => (string)GetValue(CardTitleProperty);
        set => SetValue(CardTitleProperty, value);
    }

    public string CardSubtitle
    {
        get => (string)GetValue(CardSubtitleProperty);
        set => SetValue(CardSubtitleProperty, value);
    }

    public string IconText
    {
        get => (string)GetValue(IconTextProperty);
        set => SetValue(IconTextProperty, value);
    }

    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }
}
