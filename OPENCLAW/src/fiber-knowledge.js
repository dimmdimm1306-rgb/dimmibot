// Knowledge base untuk pertanyaan teknis fiber optik

const fiberKnowledge = {
  odc: {
    keywords: ['odc', 'optical distribution cabinet'],
    answer: `📦 *ODC (Optical Distribution Cabinet)*

ODC adalah lemari distribusi optik yang berfungsi sebagai titik distribusi kabel fiber optik dari jaringan feeder ke jaringan distribusi.

🔹 *Fungsi Utama:*
- Tempat terminasi kabel feeder
- Distribusi ke beberapa area coverage
- Splicing dan patching fiber
- Proteksi sambungan fiber

🔹 *Kapasitas:*
- Biasanya 96-288 core
- Bisa lebih besar tergantung kebutuhan

🔹 *Lokasi:*
- Dipasang outdoor (tiang/ground)
- Strategis untuk coverage area
- Dekat dengan cluster pelanggan

🔹 *Komponen:*
- Splitter (1:8, 1:16, 1:32)
- Splice tray
- Patch panel
- Cable management`
  },
  
  odp: {
    keywords: ['odp', 'optical distribution point'],
    answer: `📍 *ODP (Optical Distribution Point)*

ODP adalah kotak distribusi kabel fiber optik yang lebih kecil dari ODC, berfungsi sebagai titik distribusi terakhir sebelum ke pelanggan (ONU/ONT).

🔹 *Fungsi Utama:*
- Terminasi kabel distribusi dari ODC
- Distribusi ke pelanggan (dropcore)
- Splicing fiber

🔹 *Kapasitas:*
- Biasanya 8-16 core
- Untuk 8-16 pelanggan (dengan splitter 1:2)

🔹 *Lokasi:*
- Tiang listrik/telkom
- Dekat rumah pelanggan
- 1 ODP untuk 1 cluster kecil

🔹 *Jenis:*
- ODP Aerial (tiang)
- ODP Underground (tanam)
- ODP Wall Mount (dinding)`
  },
  
  otp: {
    keywords: ['otp', 'optical termination point'],
    answer: `🏠 *OTP (Optical Termination Point)*

OTP adalah kotak terminasi di sisi pelanggan, tempat kabel dropcore dari ODP masuk ke rumah pelanggan.

🔹 *Fungsi:*
- Terminasi kabel dropcore
- Proteksi sambungan
- Koneksi ke ONT/modem

🔹 *Lokasi:*
- Dinding luar rumah pelanggan
- Indoor (dalam rumah)

🔹 *Kapasitas:*
- 1-2 core (untuk 1 pelanggan)`
  },
  
  ont: {
    keywords: ['ont', 'onu', 'optical network terminal', 'optical network unit'],
    answer: `📡 *ONT/ONU (Optical Network Terminal/Unit)*

ONT adalah perangkat di sisi pelanggan yang mengubah sinyal optik menjadi sinyal listrik untuk digunakan oleh perangkat pelanggan.

�� *Fungsi:*
- Konversi sinyal optik ke elektrik
- Router/WiFi untuk pelanggan
- Port LAN, USB, Phone

🔹 *Jenis:*
- Single port (1 LAN)
- Multi port (4 LAN + WiFi)
- Voice (dengan port telepon)

🔹 *Lokasi:*
- Indoor di rumah pelanggan
- Terhubung ke OTP via patchcord`
  },
  
  splitter: {
    keywords: ['splitter', 'optical splitter', 'plc splitter'],
    answer: `🔀 *Optical Splitter (PLC Splitter)*

Splitter adalah perangkat pasif yang membagi sinyal optik dari 1 input menjadi beberapa output.

🔹 *Ratio Umum:*
- 1:2 (1 jadi 2)
- 1:4 (1 jadi 4)
- 1:8 (1 jadi 8)
- 1:16 (1 jadi 16)
- 1:32 (1 jadi 32)

�� *Lokasi:*
- Di ODC (splitter besar 1:16, 1:32)
- Di ODP (splitter kecil 1:2, 1:4, 1:8)

🔹 *Loss:*
- 1:2 = ~3.5 dB
- 1:4 = ~7 dB
- 1:8 = ~10.5 dB
- 1:16 = ~14 dB
- 1:32 = ~17.5 dB`
  },
  
  dropcore: {
    keywords: ['dropcore', 'drop core', 'kabel dropcore'],
    answer: `🔌 *Kabel Dropcore*

Dropcore adalah kabel fiber optik dari ODP ke rumah pelanggan (OTP/ONT).

🔹 *Karakteristik:*
- Kabel kecil, fleksibel
- Biasanya 1-2 core
- Outdoor rated
- Self-supporting (ada wire)

🔹 *Jenis:*
- FTTH Drop Cable
- Figure 8 (dengan messenger wire)
- Round drop cable

🔹 *Panjang:*
- Maksimal 100-200 meter
- Dari ODP ke rumah pelanggan

�� *Instalasi:*
- Aerial (udara via tiang)
- Underground (tanam dalam pipa)
- Indoor (dalam rumah)`
  },
  
  otdr: {
    keywords: ['otdr', 'optical time domain reflectometer'],
    answer: `📊 *OTDR (Optical Time Domain Reflectometer)*

OTDR adalah alat ukur untuk menganalisis kabel fiber optik, mendeteksi loss, break, dan kualitas sambungan.

🔹 *Fungsi:*
- Ukur panjang kabel
- Deteksi break/putus
- Ukur loss splice/connector
- Analisis kualitas link

🔹 *Parameter:*
- Loss (dB)
- Distance (km/meter)
- Reflectance (dB)
- ORL (Optical Return Loss)

🔹 *Kapan Dipakai:*
- Commissioning jaringan baru
- Troubleshooting gangguan
- Maintenance preventif
- Quality assurance`
  },
  
  olp: {
    keywords: ['olp', 'optical loss power', 'power meter'],
    answer: `🔋 *OLP/Power Meter (Optical Loss Power)*

Power meter adalah alat ukur daya optik (dalam dBm) untuk mengecek kekuatan sinyal fiber.

🔹 *Fungsi:*
- Ukur daya optik (dBm)
- Cek kualitas sinyal
- Verifikasi loss

🔹 *Standar Daya:*
- OLT TX: +2 sampai +7 dBm
- ONT RX: -8 sampai -28 dBm
- Ideal: -15 sampai -25 dBm

🔹 *Penggunaan:*
- Ukur di ODP
- Ukur di ONT
- Bandingkan dengan standar`
  },
  
  ftth: {
    keywords: ['ftth', 'fiber to the home'],
    answer: `🏡 *FTTH (Fiber To The Home)*

FTTH adalah teknologi jaringan fiber optik yang ditarik langsung sampai ke rumah pelanggan.

🔹 *Topologi:*
OLT → Feeder → ODC → Distribution → ODP → Dropcore → OTP → ONT

🔹 *Keuntungan:*
- Bandwidth besar (sampai 1 Gbps+)
- Latency rendah
- Stabil, tidak terpengaruh cuaca
- Future proof

🔹 *Komponen Utama:*
- OLT (di sentral)
- ODC (distribusi area)
- ODP (distribusi cluster)
- ONT (di rumah pelanggan)

🔹 *Teknologi:*
- GPON (Gigabit PON)
- XG-PON (10G PON)
- Passive Optical Network`
  }
};

function getFiberAnswer(query) {
  const lowerQuery = query.toLowerCase().trim();
  
  // Check each knowledge entry
  for (const [key, entry] of Object.entries(fiberKnowledge)) {
    if (entry.keywords.some(keyword => lowerQuery.includes(keyword))) {
      return entry.answer;
    }
  }
  
  // Check for general questions
  if (lowerQuery.includes('apa itu') || lowerQuery.includes('apa sih') || 
      lowerQuery.includes('jelaskan') || lowerQuery.includes('what is')) {
    return null; // Let it check knowledge base
  }
  
  return null;
}

function isFiberQuestion(query) {
  const lowerQuery = query.toLowerCase();
  
  // Check if contains fiber-related keywords
  const fiberKeywords = ['odc', 'odp', 'otp', 'ont', 'onu', 'splitter', 'dropcore', 
                         'otdr', 'olp', 'ftth', 'fiber', 'optik', 'optical'];
  
  return fiberKeywords.some(keyword => lowerQuery.includes(keyword));
}

module.exports = {
  getFiberAnswer,
  isFiberQuestion
};
