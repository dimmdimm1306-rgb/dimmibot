namespace StokBarangMAUI.Controls;

public partial class SearchFilterBar : ContentView
{
    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(SearchFilterBar), "Cari cepat...");

    public static readonly BindableProperty SearchTextProperty =
        BindableProperty.Create(nameof(SearchText), typeof(string), typeof(SearchFilterBar), string.Empty, BindingMode.TwoWay);

    public event EventHandler<TextChangedEventArgs>? SearchTextChanged;

    public SearchFilterBar()
    {
        InitializeComponent();
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (SearchText != e.NewTextValue)
            SearchText = e.NewTextValue ?? string.Empty;

        SearchTextChanged?.Invoke(this, e);
    }
}
