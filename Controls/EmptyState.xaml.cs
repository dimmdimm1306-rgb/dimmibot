namespace StokBarangMAUI.Controls;

public partial class EmptyState : ContentView
{
    public static readonly BindableProperty EmptyTitleProperty =
        BindableProperty.Create(nameof(EmptyTitle), typeof(string), typeof(EmptyState), "Data kosong");

    public static readonly BindableProperty EmptyMessageProperty =
        BindableProperty.Create(nameof(EmptyMessage), typeof(string), typeof(EmptyState), "Belum ada data untuk ditampilkan.");

    public EmptyState()
    {
        InitializeComponent();
    }

    public string EmptyTitle
    {
        get => (string)GetValue(EmptyTitleProperty);
        set => SetValue(EmptyTitleProperty, value);
    }

    public string EmptyMessage
    {
        get => (string)GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    }
}
