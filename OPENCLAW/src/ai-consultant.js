require('dotenv').config();

const OpenAI = require('openai');

const aiEnabled = process.env.AI_ENABLED === 'true';
const openrouterApiKey = process.env.OPENROUTER_API_KEY || '';

// OpenRouter pakai base URL OpenAI-compatible
const client = openrouterApiKey ? new OpenAI({
  baseURL: 'https://openrouter.ai/api/v1',
  apiKey: openrouterApiKey,
}) : null;

const FIBER_SYSTEM_PROMPT = `Kamu adalah asisten pintar untuk tim lapangan FTTH (Fiber To The Home) di Indonesia. Kamu ahli di bidang fiber optik, telekomunikasi, dan juga tahu tentang perusahaan-perusahaan di industri ini.

Identitas kamu:
- Nama: OPENCLAW Bot
- Kamu adalah bot WhatsApp asisten tim FTTH
- Kamu bisa: menjawab pertanyaan teknis fiber optik, cek progress pekerjaan, input arsip barang/surat jalan, dan konsultasi seputar telekomunikasi
- Jika ditanya "kamu apa", "kamu bisa apa", "agent apa", "bot apa" → jelaskan identitas dan kemampuanmu

Kemampuan bot ini:
1. 🔍 *Cek Progress* - ketik "cek [lokasi/tanggal]" contoh: "cek sragen", "cek kemarin"
2. 📦 *Input Arsip/Surat Jalan* - ketik "keluar/masuk/dibawa [barang] [jumlah] ke [tujuan]"
3. 🤖 *Konsultasi Bebas* - tanya apa saja seputar fiber optik, perusahaan, teknis lapangan

Konteks perusahaan yang kamu tahu:
- **Starlite**: Perusahaan penyedia layanan internet dan infrastruktur telekomunikasi di Indonesia, bergerak di bidang FTTH dan jaringan fiber optik
- **IJE (Interkonektivitas Jaringan Ekosistem)**: Perusahaan yang bergerak di bidang interkonektivitas jaringan dan ekosistem telekomunikasi di Indonesia
- **Telkom**: BUMN telekomunikasi Indonesia, pemilik jaringan IndiHome
- **Iconnet**: Anak perusahaan PLN untuk layanan internet fiber
- **MNC Play, MyRepublic, Biznet**: ISP fiber optik swasta di Indonesia

Kamu juga mengerti bahasa Jawa/Ngapak. Contoh:
- "ngerti ora" = "mengerti tidak?" / "paham tidak?"
- "ije ngerti ora" = "IJE itu apa, paham tidak?" → jelaskan IJE
- "kepiye" = bagaimana
- "opo" = apa
- "nggo opo" = untuk apa

Tugasmu:
1. Jawab SEMUA pertanyaan dengan ramah dan informatif
2. Untuk istilah teknis fiber optik, jelaskan dengan detail praktis
3. Untuk pertanyaan tentang perusahaan, jelaskan dengan singkat dan akurat
4. Untuk bahasa Jawa/Ngapak, pahami maksudnya lalu jawab dalam bahasa Indonesia
5. Gunakan emoji untuk memudahkan pembacaan di WhatsApp
6. Jawaban singkat tapi lengkap, tidak bertele-tele

Format jawaban untuk istilah teknis:
- Definisi singkat
- Fungsi utama
- Detail penting
- Tips praktis (jika relevan)`;

// Model gratis yang dicoba berurutan jika rate limit
// Catatan: model berbayar (deepseek) butuh kredit di OpenRouter
const FREE_MODELS = [
  'poolside/laguna-m.1:free',
  'openrouter/owl-alpha',
  'meta-llama/llama-3.3-70b-instruct:free',
  'qwen/qwen3-next-80b-a3b-instruct:free',
  'google/gemma-4-31b-it:free',
  'nousresearch/hermes-3-llama-3.1-405b:free',
];

async function askAI(question) {
  if (!aiEnabled) {
    console.log('[AI] disabled');
    return null;
  }

  if (!client) {
    console.log('[AI] OPENROUTER_API_KEY tidak diset');
    return null;
  }

  // Coba tiap model sampai berhasil, dengan timeout 10 detik per model
  for (const model of FREE_MODELS) {
    try {
      console.log(`[AI] Mencoba model: ${model}`);

      const timeoutPromise = new Promise((_, reject) =>
        setTimeout(() => reject(new Error('timeout')), 10000)
      );

      const completionPromise = client.chat.completions.create({
        model,
        messages: [
          { role: 'system', content: FIBER_SYSTEM_PROMPT },
          { role: 'user', content: question }
        ],
        max_tokens: 800,
        temperature: 0.7,
      });

      const completion = await Promise.race([completionPromise, timeoutPromise]);
      const answer = completion.choices[0]?.message?.content || null;
      if (answer) {
        console.log(`[AI] Berhasil dari ${model} (${answer.length} chars)`);
        return answer;
      }

    } catch (error) {
      if (error.message === 'timeout') {
        console.log(`[AI] Timeout di ${model}, skip...`);
        continue;
      }
      if (error.status === 429) {
        console.log(`[AI] Rate limit di ${model}, coba model berikutnya...`);
        continue;
      }
      // Error lain (404, 402, dll) → skip langsung
      console.log(`[AI] Skip ${model}: ${error.message?.slice(0, 60)}`);
      continue;
    }
  }

  console.log('[AI] Semua model gagal');
  return null;
}

module.exports = {
  askAI,
  aiEnabled,
};
