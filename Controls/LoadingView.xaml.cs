namespace StokBarangMAUI.Controls;

public partial class LoadingView : ContentView
{
    public static readonly BindableProperty LoadingTextProperty =
        BindableProperty.Create(nameof(LoadingText), typeof(string), typeof(LoadingView), "Memuat data...");

    public LoadingView()
    {
        InitializeComponent();
    }

    public string LoadingText
    {
        get => (string)GetValue(LoadingTextProperty);
        set => SetValue(LoadingTextProperty, value);
    }
}
