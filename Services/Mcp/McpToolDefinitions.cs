namespace StokBarangMAUI.Services.Mcp
{
    /// <summary>
    /// Tool definitions untuk LLM function calling (OpenAI format).
    /// LLM dapat daftar tools ini di system prompt, lalu bisa "call" salah satu.
    /// App intercept tool_call, eksekusi via McpClient, kirim result balik ke LLM.
    /// </summary>
    public static class McpToolDefinitions
    {
        /// <summary>JSON array of tool definitions (OpenAI function calling format).</summary>
        public static readonly string ToolsJson = """
[
  {
    "type": "function",
    "function": {
      "name": "get_progress_resume",
      "description": "Ambil ringkasan progres per segment (6 segment). Return: per segment kabel/T7/T9 plan vs progress.",
      "parameters": { "type": "object", "properties": {}, "required": [] }
    }
  },
  {
    "type": "function",
    "function": {
      "name": "search_site",
      "description": "Cari site/rute di RESUME. Return: detail progres kabel/T7/T9 per rute yang match.",
      "parameters": {
        "type": "object",
        "properties": {
          "keyword": { "type": "string", "description": "Site ID atau nama rute (e.g. '0244', 'JC2', 'JAW-CJV-0172')" }
        },
        "required": ["keyword"]
      }
    }
  },
  {
    "type": "function",
    "function": {
      "name": "get_stok_summary",
      "description": "Ambil ringkasan stok material semua gudang. Return: per material diterima/keluar/sisa.",
      "parameters": { "type": "object", "properties": {}, "required": [] }
    }
  },
  {
    "type": "function",
    "function": {
      "name": "get_progress_harian",
      "description": "Ambil log progres harian. Bisa filter by tanggal atau keyword.",
      "parameters": {
        "type": "object",
        "properties": {
          "keyword": { "type": "string", "description": "Keyword pencarian (site, rute, material)" },
          "date_intent": { "type": "string", "enum": ["date_today","date_yesterday","date_week"], "description": "Filter tanggal" }
        },
        "required": []
      }
    }
  },
  {
    "type": "function",
    "function": {
      "name": "get_surat_jalan",
      "description": "Ambil data surat jalan. Bisa filter by keyword atau tanggal.",
      "parameters": {
        "type": "object",
        "properties": {
          "keyword": { "type": "string", "description": "NoSJ, nama barang, pengirim, penerima, jenis" },
          "date_intent": { "type": "string", "enum": ["date_today","date_yesterday","date_week"], "description": "Filter tanggal" }
        },
        "required": []
      }
    }
  }
]
""";

        /// <summary>System prompt tambahan yang menjelaskan tools ke LLM.</summary>
        public static readonly string ToolSystemPrompt = """
Kamu adalah asisten project FTTH (Fiber To The Home). Kamu punya akses ke data project via tools.

ATURAN:
- Kalau user tanya data spesifik (progres, stok, surat jalan), PANGGIL TOOL yang sesuai.
- JANGAN mengarang angka. Kalau tidak tahu, panggil tool.
- Jawab dalam bahasa Indonesia, singkat, pakai emoji.
- Format angka pakai titik ribuan Indonesia (44.000 bukan 44,000).

TOOLS TERSEDIA:
- get_progress_resume: ringkasan progres 6 segment
- search_site: cari site/rute tertentu
- get_stok_summary: stok material semua gudang
- get_progress_harian: log harian (filter tanggal/keyword)
- get_surat_jalan: data SJ (filter tanggal/keyword)
""";
    }
}
