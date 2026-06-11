using Microsoft.Maui.Graphics;

namespace StokBarangMAUI.Controls;

public partial class StatCard : ContentView
{
    public static readonly BindableProperty CardTitleProperty =
        BindableProperty.Create(nameof(CardTitle), typeof(string), typeof(StatCard), string.Empty);

    public static readonly BindableProperty CardValueProperty =
        BindableProperty.Create(nameof(CardValue), typeof(string), typeof(StatCard), "-");

    public static readonly BindableProperty CardCaptionProperty =
        BindableProperty.Create(nameof(CardCaption), typeof(string), typeof(StatCard), string.Empty);

    public static readonly BindableProperty IconTextProperty =
        BindableProperty.Create(nameof(IconText), typeof(string), typeof(StatCard), string.Empty);

    public static readonly BindableProperty AccentColorProperty =
        BindableProperty.Create(nameof(AccentColor), typeof(Color), typeof(StatCard), Color.FromArgb("#22D3EE"));

    public static readonly BindableProperty AccentBackgroundProperty =
        BindableProperty.Create(nameof(AccentBackground), typeof(Color), typeof(StatCard), Color.FromArgb("#0B2A4A"));

    public static readonly BindableProperty CardBackgroundProperty =
        BindableProperty.Create(nameof(CardBackground), typeof(Color), typeof(StatCard), Color.FromArgb("#0E1A2B"));

    public StatCard()
    {
        InitializeComponent();
    }

    public string CardTitle
    {
        get => (string)GetValue(CardTitleProperty);
        set => SetValue(CardTitleProperty, value);
    }

    public string CardValue
    {
        get => (string)GetValue(CardValueProperty);
        set => SetValue(CardValueProperty, value);
    }

    public string CardCaption
    {
        get => (string)GetValue(CardCaptionProperty);
        set => SetValue(CardCaptionProperty, value);
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

    public Color AccentBackground
    {
        get => (Color)GetValue(AccentBackgroundProperty);
        set => SetValue(AccentBackgroundProperty, value);
    }

    public Color CardBackground
    {
        get => (Color)GetValue(CardBackgroundProperty);
        set => SetValue(CardBackgroundProperty, value);
    }
}
