namespace StokBarangMAUI.Controls
{
    /// <summary>
    /// Label yang teks-nya bisa di-select (long-press / double-tap di Android).
    /// Di Android kelas ini mapped ke TextView dengan setTextIsSelectable(true).
    /// Di platform lain fall back ke Label biasa.
    /// </summary>
    public class SelectableLabel : Label
    {
    }
}
