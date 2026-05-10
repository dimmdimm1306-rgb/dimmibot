namespace StokBarangMAUI.Models
{
    // Konvensi satuan material FTTH (Indonesia):
    //   m   — kabel/fiber/drop core (per meter)
    //   btg — tiang/pole (per batang)
    //   pcs — ODP, ODC, OTB, Closure, Splitter, Konektor/Adapter,
    //         Pigtail, Patchcord, Strength Clamp, X Frame, dll.
    public static class MaterialUnit
    {
        public static string Get(string? itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName)) return "pcs";
            var n = itemName.ToUpperInvariant();

            // Pigtail & Patchcord punya angka meter di nama, tapi
            // dijual per-pcs — cek lebih dulu sebelum fallback "kabel".
            if (n.Contains("PIGTAIL") || n.Contains("PATCH")) return "pcs";

            if (n.Contains("TIANG") || n.Contains("POLE")) return "btg";

            if (n.Contains("KABEL")     || n.Contains("CABLE")     ||
                n.Contains("FIBER")     || n.Contains("DROP CORE") ||
                n.Contains("DROPCORE")  || n.Contains("DROP WIRE") ||
                n.Contains("BC WIRE"))
                return "m";

            return "pcs";
        }
    }
}
