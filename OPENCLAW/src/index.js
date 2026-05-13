require('dotenv').config();

const fs = require('fs');
const path = require('path');
const { Readable } = require('stream');
const qrcode = require('qrcode-terminal');
const pino = require('pino');
const { google } = require('googleapis');
const {
  default: makeWASocket,
  DisconnectReason,
  downloadMediaMessage,
  fetchLatestBaileysVersion,
  useMultiFileAuthState,
} = require('@whiskeysockets/baileys');
const { getFiberAnswer, isFiberQuestion } = require('./fiber-knowledge');
const { askAI, aiEnabled } = require('./ai-consultant');

const REQUIRED_ENV = [
  'GOOGLE_SHEET_ID',
  'GOOGLE_DRIVE_FOLDER_ID',
];

const sheetName = process.env.SHEET_NAME || 'Arsip';
const dataSheetId = process.env.DATA_SHEET_ID || '';
const dataSheetTabs = (process.env.DATA_SHEET_TABS || process.env.DATA_SHEET_NAME || 'Sheet1')
  .split(',').map((s) => s.trim()).filter(Boolean);
const dataUpdateInterval = parseInt(process.env.DATA_UPDATE_INTERVAL || '86400000', 10);
const allowedChat = process.env.ALLOWED_GROUP_OR_NUMBER || '';
const logChatId = process.env.LOG_CHAT_ID !== 'false';
const sendReply = process.env.SEND_REPLY === 'true';
const casualReply = process.env.CASUAL_REPLY === 'true';
const requireMention = process.env.REQUIRE_MENTION !== 'false';

// cachedData[tab] = { headers: string[], rows: string[][] }
let cachedData = {};
let lastDataUpdate = 0;

function checkEnv() {
  const missing = REQUIRED_ENV.filter((key) => !process.env[key]);
  const hasServiceAccount = process.env.GOOGLE_SERVICE_ACCOUNT_EMAIL && process.env.GOOGLE_PRIVATE_KEY;
  const hasOAuth = process.env.GOOGLE_OAUTH_CLIENT_FILE && process.env.GOOGLE_OAUTH_TOKEN_FILE;
  if (missing.length) {
    throw new Error(`Env belum lengkap: ${missing.join(', ')}`);
  }
  if (!hasServiceAccount && !hasOAuth) {
    throw new Error('Isi Service Account atau OAuth: GOOGLE_SERVICE_ACCOUNT_EMAIL + GOOGLE_PRIVATE_KEY, atau GOOGLE_OAUTH_CLIENT_FILE + GOOGLE_OAUTH_TOKEN_FILE');
  }
}

function parseCaption(text) {
  const original = text.trim();
  const lines = original.split(/\r?\n/).map((line) => line.trim()).filter(Boolean);
  const fieldMap = parseFieldMap(lines);
  if (Object.keys(fieldMap).length) return parseStructuredReport(original, fieldMap);

  return parseFreeTextReport(original);
}

function parseFieldMap(lines) {
  const aliases = {
    tanggal: 'tanggal',
    tgl: 'tanggal',
    jenis: 'jenis',
    status: 'jenis',
    sj: 'noSuratJalan',
    suratjalan: 'noSuratJalan',
    nosuratjalan: 'noSuratJalan',
    no: 'noSuratJalan',
    dari: 'pengirim',
    pengirim: 'pengirim',
    mandor: 'pengirim',
    ke: 'penerima',
    tujuan: 'penerima',
    penerima: 'penerima',
    barang: 'namaBarang',
    namabarang: 'namaBarang',
    item: 'namaBarang',
    kode: 'kodeBarang',
    kodebarang: 'kodeBarang',
    jumlah: 'jumlah',
    qty: 'jumlah',
    banyak: 'jumlah',
    ket: 'keterangan',
    keterangan: 'keterangan',
    kurang: 'keterangan',
  };
  const result = {};
  for (const line of lines) {
    const match = line.match(/^([^:=]+)\s*[:=]\s*(.+)$/);
    if (!match) continue;
    const key = match[1].toLowerCase().replace(/[^a-z0-9]/g, '');
    const mapped = aliases[key];
    if (mapped) result[mapped] = match[2].trim();
  }
  return result;
}

function normalizeJenis(value = '') {
  const lower = value.toLowerCase();
  if (/(keluar|out|kirim|bawa|dibawa|ke\s+)/.test(lower)) return 'KELUAR';
  if (/(masuk|in|terima|datang)/.test(lower)) return 'MASUK';
  return '';
}

function parseAmount(value = '') {
  const match = String(value).replace(/[.,]/g, '').match(/\d+/);
  if (!match) return null;
  const jumlah = Number(match[0]);
  return Number.isFinite(jumlah) && jumlah > 0 ? jumlah : null;
}

function parseStructuredReport(original, fieldMap) {
  const jumlah = parseAmount(fieldMap.jumlah);
  if (!jumlah) return null;
  return {
    tanggal: fieldMap.tanggal || '',
    jenis: normalizeJenis(fieldMap.jenis || original) || 'KELUAR',
    noSuratJalan: fieldMap.noSuratJalan || '',
    pengirim: fieldMap.pengirim || '',
    penerima: fieldMap.penerima || '',
    namaBarang: fieldMap.namaBarang || fieldMap.kodeBarang || 'BELUM DIISI',
    kodeBarang: fieldMap.kodeBarang || '',
    jumlah,
    satuan: detectSatuan(original),
    keterangan: fieldMap.keterangan || '',
    inputStatus: buildInputStatus(fieldMap, jumlah),
    teksAsli: original,
  };
}

function parseFreeTextReport(original) {
  const parts = original.split(/\s+/);
  const jumlah = parseAmount(original);
  if (!jumlah || parts.length < 2) return null;

  const lowerParts = parts.map((part) => part.toLowerCase());
  const keIndex = lowerParts.findIndex((part) => ['ke', 'tujuan'].includes(part));
  const dariIndex = lowerParts.findIndex((part) => ['dari', 'mandor'].includes(part));
  const amountIndex = parts.findIndex((part) => /\d/.test(part));
  const penerima = keIndex >= 0 ? parts.slice(keIndex + 1).join(' ') : '';
  const pengirim = dariIndex >= 0 ? parts[dariIndex + 1] || '' : '';
  const beforeAmount = amountIndex > 0 ? parts.slice(0, amountIndex) : [];
  const ignored = new Set(['laporan', 'barang', 'kirim', 'dikirim', 'bawa', 'dibawa', 'masuk', 'keluar']);
  const barangParts = beforeAmount.filter((part) => !ignored.has(part.toLowerCase()));

  return {
    tanggal: '',
    jenis: normalizeJenis(original) || 'KELUAR',
    noSuratJalan: '',
    pengirim,
    penerima,
    namaBarang: barangParts.join(' ') || 'BELUM DIISI',
    kodeBarang: '',
    jumlah,
    satuan: detectSatuan(original),
    keterangan: buildFreeTextNote({ original, penerima, pengirim, barangParts }),
    inputStatus: 'PERLU DILENGKAPI',
    teksAsli: original,
  };
}

function isLikelyInventoryReport(text) {
  const original = text.trim();
  const lines = original.split(/\r?\n/).map((line) => line.trim()).filter(Boolean);
  const fieldMap = parseFieldMap(lines);
  const structuredKeys = ['jenis', 'noSuratJalan', 'pengirim', 'penerima', 'namaBarang', 'kodeBarang', 'jumlah'];
  const structuredCount = structuredKeys.filter((key) => fieldMap[key]).length;
  if (structuredCount >= 3 && fieldMap.jumlah) return true;

  const lower = original.toLowerCase();
  const hasAmount = parseAmount(original) !== null;
  const hasMovementWord = /\b(masuk|keluar|kirim|dikirim|bawa|dibawa|laporan)\b/.test(lower);
  const hasDestination = /\b(ke|tujuan|dari|mandor)\b/.test(lower);
  return hasAmount && (hasMovementWord || hasDestination);
}

function getMentionedJids(message) {
  return message.extendedTextMessage?.contextInfo?.mentionedJid
    || message.imageMessage?.contextInfo?.mentionedJid
    || message.documentMessage?.contextInfo?.mentionedJid
    || [];
}

function isBotMentioned(message, sock) {
  if (!requireMention) return true;
  const botId = sock.user?.id?.split(':')[0];
  if (!botId) return false;
  return getMentionedJids(message).some((jid) => jid.split('@')[0] === botId);
}

function removeMentions(text) {
  return text.replace(/@\d+/g, '').trim();
}

function detectSatuan(text) {
  const lower = text.toLowerCase();
  if (/\b(btg|batang)\b/.test(lower)) return 'batang';
  if (/\b(pcs|pc|buah)\b/.test(lower)) return 'pcs';
  if (/\b(m|meter)\b/.test(lower)) return 'meter';
  if (/\b(roll|rol)\b/.test(lower)) return 'roll';
  return '';
}

function buildInputStatus(fieldMap, jumlah) {
  const missing = [];
  if (!fieldMap.noSuratJalan) missing.push('no surat jalan');
  if (!fieldMap.pengirim) missing.push('pengirim');
  if (!fieldMap.penerima) missing.push('penerima');
  if (!fieldMap.namaBarang && !fieldMap.kodeBarang) missing.push('nama barang');
  if (!jumlah) missing.push('jumlah');
  return missing.length ? `PERLU DILENGKAPI: ${missing.join(', ')}` : 'LENGKAP';
}

function buildFreeTextNote({ original, penerima, pengirim, barangParts }) {
  const notes = ['Input bebas/mandor, mohon cek ulang.'];
  if (!barangParts.length) notes.push('Nama barang belum jelas.');
  if (!penerima) notes.push('Tujuan/penerima belum jelas.');
  if (!pengirim) notes.push('Pengirim/mandor belum jelas.');
  notes.push(`Teks asli: ${original}`);
  return notes.join(' ');
}

function getLegacyReply(parsed) {
  return `Tersimpan: ${parsed.jenis} ${parsed.namaBarang} ${parsed.jumlah}${parsed.satuan ? ` ${parsed.satuan}` : ''}${parsed.penerima ? ` ke ${parsed.penerima}` : ''}.`;
}

function buildDuplicateKey(parsed) {
  return [
    parsed.tanggal,
    parsed.jenis,
    parsed.noSuratJalan,
    parsed.pengirim,
    parsed.penerima,
    parsed.namaBarang,
    parsed.kodeBarang,
    parsed.jumlah,
  ].map((value) => String(value || '').trim().toLowerCase()).join('|');
}

async function isDuplicateReport(sheets, parsed) {
  const response = await sheets.spreadsheets.values.get({
    spreadsheetId: extractId(process.env.GOOGLE_SHEET_ID, 'sheet'),
    range: `${sheetName}!B:I`,
  });
  const rows = response.data.values || [];
  const newKey = buildDuplicateKey(parsed);
  return rows.slice(1).some((row) => {
    const existing = {
      tanggal: row[0] || '',
      jenis: row[1] || '',
      noSuratJalan: row[2] || '',
      pengirim: row[3] || '',
      penerima: row[4] || '',
      namaBarang: row[5] || '',
      kodeBarang: row[6] || '',
      jumlah: row[7] || '',
    };
    return buildDuplicateKey(existing) === newKey;
  });
}

function extractId(value, type) {
  if (!value) return value;
  const folderMatch = value.match(/folders\/([a-zA-Z0-9_-]+)/);
  if (type === 'folder' && folderMatch) return folderMatch[1];
  const sheetMatch = value.match(/\/d\/([a-zA-Z0-9_-]+)/);
  if (type === 'sheet' && sheetMatch) return sheetMatch[1];
  return value;
}

function getCaption(message) {
  return message.imageMessage?.caption || message.documentMessage?.caption || '';
}

function getText(message) {
  return message.conversation || message.extendedTextMessage?.text || '';
}

function getMediaMessage(message) {
  if (message.imageMessage) return message;
  if (message.documentMessage?.mimetype?.startsWith('image/')) return message;
  return null;
}

function bufferToStream(buffer) {
  return Readable.from(buffer);
}

async function safeReply(sock, chatId, text) {
  if (!sendReply) return;
  await sock.sendMessage(chatId, { text });
}

function createCasualReply(text) {
  const lower = text.toLowerCase();
  if (/^(halo|hallo|hai|hei|p|ping)\b/.test(lower)) {
    return 'Aktif. Kalau mau input barang, kirim formatnya.';
  }
  if (lower.includes('carane') || lower.includes('gimana') || lower.includes('piye')) {
    return 'Formatnya gampang: jenis, sj, dari, ke, barang, jumlah. Minimal contoh: kabel 24000 ke brebes.';
  }
  if (lower.includes('makasih') || lower.includes('thanks') || lower.includes('suwun')) {
    return 'Oke.';
  }
  if (lower.includes('tes') || lower.includes('test')) {
    return 'Tes masuk. Bot aktif.';
  }
  return 'Oke. Kalau ini data barang, kirim pakai format yang jelas.';
}

function createServiceAccountAuth() {
  const privateKey = process.env.GOOGLE_PRIVATE_KEY.replace(/\\n/g, '\n');
  return new google.auth.JWT({
    email: process.env.GOOGLE_SERVICE_ACCOUNT_EMAIL,
    key: privateKey,
    scopes: [
      'https://www.googleapis.com/auth/spreadsheets',
      'https://www.googleapis.com/auth/drive.file',
    ],
  });
}

function createOAuthAuth() {
  const clientConfig = JSON.parse(fs.readFileSync(process.env.GOOGLE_OAUTH_CLIENT_FILE, 'utf8'));
  const token = JSON.parse(fs.readFileSync(process.env.GOOGLE_OAUTH_TOKEN_FILE, 'utf8'));
  const config = clientConfig.installed || clientConfig.web;

  const auth = new google.auth.OAuth2(
    config.client_id,
    config.client_secret,
    config.redirect_uris?.[0] || 'http://localhost'
  );
  auth.setCredentials(token);
  return auth;
}

function createGoogleClients() {
  const auth = process.env.GOOGLE_OAUTH_CLIENT_FILE && process.env.GOOGLE_OAUTH_TOKEN_FILE
    ? createOAuthAuth()
    : createServiceAccountAuth();

  return {
    drive: google.drive({ version: 'v3', auth }),
    sheets: google.sheets({ version: 'v4', auth }),
  };
}

async function uploadPhoto(drive, buffer, parsed) {
  const safeName = `${new Date().toISOString()}-${parsed.jenis}-${parsed.namaBarang}-${parsed.penerima}`.replace(/[^a-z0-9_.-]+/gi, '-');
  const response = await drive.files.create({
    requestBody: {
      name: `${safeName}.jpg`,
      parents: [extractId(process.env.GOOGLE_DRIVE_FOLDER_ID, 'folder')],
    },
    media: {
      mimeType: 'image/jpeg',
      body: bufferToStream(buffer),
    },
    fields: 'id, webViewLink',
  });

  await drive.permissions.create({
    fileId: response.data.id,
    requestBody: { role: 'reader', type: 'anyone' },
  });

  return `https://drive.google.com/uc?id=${response.data.id}`;
}

async function appendSheetRow(sheets, parsed, photoUrl, sender) {
  const photoCell = photoUrl ? `=IMAGE("${photoUrl}")` : '';
  await sheets.spreadsheets.values.append({
    spreadsheetId: extractId(process.env.GOOGLE_SHEET_ID, 'sheet'),
    range: `${sheetName}!A:Q`,
    valueInputOption: 'USER_ENTERED',
    requestBody: {
      values: [[
        new Date().toLocaleString('id-ID', { timeZone: 'Asia/Jakarta' }),
        parsed.tanggal || new Date().toLocaleDateString('id-ID', { timeZone: 'Asia/Jakarta' }),
        parsed.jenis,
        parsed.noSuratJalan,
        parsed.pengirim,
        parsed.penerima,
        parsed.namaBarang,
        parsed.kodeBarang,
        parsed.jumlah,
        parsed.satuan,
        parsed.keterangan,
        parsed.inputStatus,
        'PENDING',
        parsed.teksAsli,
        photoCell,
        photoUrl || '',
        sender,
      ]],
    },
  });
}

async function ensureAuthDir() {
  const authDir = path.join(process.cwd(), 'auth_info');
  if (!fs.existsSync(authDir)) fs.mkdirSync(authDir, { recursive: true });
  return authDir;
}

// Tab dengan baris title merged di atas (header sebenarnya di baris 2)
const TAB_HEADER_ROW = {
  'Surat Jalan': 2,
};

async function fetchTab(sheets, spreadsheetId, tab) {
  try {
    const response = await sheets.spreadsheets.values.get({
      spreadsheetId,
      range: `${tab}!A:Z`,
    });
    const rows = response.data.values || [];
    if (rows.length === 0) {
      console.log(`⚠️  Tab "${tab}" kosong.`);
      return { headers: [], rows: [] };
    }
    const headerRowIdx = (TAB_HEADER_ROW[tab] || 1) - 1;
    const headerRow = rows[headerRowIdx] || [];
    const headers = headerRow.map((h) => String(h || '').trim());
    const data = rows.slice(headerRowIdx + 1);
    console.log(`✅ Fetched ${data.length} baris dari "${tab}" (header row ${headerRowIdx + 1}).`);
    return { headers, rows: data };
  } catch (error) {
    console.error(`❌ Tab "${tab}" gagal: ${error.message}`);
    return { headers: [], rows: [] };
  }
}

async function fetchDataFromSpreadsheet(sheets) {
  if (!dataSheetId) {
    console.log('DATA_SHEET_ID tidak diset, skip fetch data spreadsheet.');
    return {};
  }

  const spreadsheetId = extractId(dataSheetId, 'sheet');
  const results = await Promise.allSettled(
    dataSheetTabs.map((tab) => fetchTab(sheets, spreadsheetId, tab))
  );

  const out = {};
  dataSheetTabs.forEach((tab, i) => {
    const r = results[i];
    out[tab] = r.status === 'fulfilled' ? r.value : { headers: [], rows: [] };
  });
  return out;
}

function isCacheEmpty() {
  return !cachedData || Object.values(cachedData).every((t) => !t || !t.rows || t.rows.length === 0);
}

async function updateDataCache(sheets) {
  const now = Date.now();
  if (now - lastDataUpdate < dataUpdateInterval && !isCacheEmpty()) {
    console.log('Cache masih fresh, skip update.');
    return;
  }

  console.log('Updating data cache dari spreadsheet...');
  cachedData = await fetchDataFromSpreadsheet(sheets);
  lastDataUpdate = now;
}

function colLetter(i) {
  let s = '';
  let n = i;
  while (n >= 0) {
    s = String.fromCharCode(65 + (n % 26)) + s;
    n = Math.floor(n / 26) - 1;
  }
  return s;
}

function searchData(query) {
  if (isCacheEmpty()) {
    return 'Data belum tersedia. Bot sedang sync data.';
  }

  const lowerQuery = query.toLowerCase().trim();
  console.log(`Searching for: "${lowerQuery}"`);

  const dateFilter = parseDateQuery(lowerQuery);
  if (dateFilter) console.log(`Date filter detected: ${dateFilter.full}`);

  // Non-date keyword (kalau query hanya tanggal, keywordOnly jadi kosong)
  const keywordOnly = lowerQuery.replace(
    /\b(hari ini|kemarin|today|yesterday|\d{1,2}\s*(jan|feb|mar|apr|mei|may|jun|jul|agu|aug|sep|okt|oct|nov|des|dec))\b/gi,
    ''
  ).trim();

  const perTab = {};
  let totalHits = 0;

  for (const tab of Object.keys(cachedData)) {
    const { headers, rows } = cachedData[tab] || {};
    if (!rows || rows.length === 0) continue;

    const matches = rows.filter((row) => {
      const dateCell = String(row[0] || '');
      if (dateFilter) {
        if (!matchDate(dateCell, dateFilter)) return false;
        if (!keywordOnly) return true;
      }
      if (!keywordOnly && !dateFilter) {
        return row.some((cell) => String(cell || '').toLowerCase().includes(lowerQuery));
      }
      return row.some((cell) => String(cell || '').toLowerCase().includes(keywordOnly));
    });

    if (matches.length > 0) {
      perTab[tab] = { headers: headers || [], matches };
      totalHits += matches.length;
    }
  }

  console.log(`Found ${totalHits} results across ${Object.keys(perTab).length} tabs`);

  if (totalHits === 0) {
    return `Tidak ada data ditemukan untuk: ${query}`;
  }

  return formatMultiTabResults(perTab, query, totalHits);
}

function headerIndex(headers, name) {
  const target = name.toUpperCase().replace(/\s+/g, '');
  return headers.findIndex((h) => h && h.toUpperCase().replace(/\s+/g, '') === target);
}

function formatSuratJalanBlock(matches, headers, MAX_PER_TAB) {
  const iTgl = headerIndex(headers, 'Tanggal');
  const iSeg = headerIndex(headers, 'Segment');
  const iBrg = headerIndex(headers, 'Nama Barang');
  const iQty = headerIndex(headers, 'QTY');
  const iJns = headerIndex(headers, 'Jenis');
  const iSJ  = headerIndex(headers, 'NO_SJ');
  const iPgr = headerIndex(headers, 'PENGIRIM');
  const iPnr = headerIndex(headers, 'PENERIMA');
  const iKet = headerIndex(headers, 'Keterangan');
  const iDrv = headerIndex(headers, 'DRIVE');

  const lines = [];
  const shown = matches.slice(0, MAX_PER_TAB);
  const cell = (row, n) => (n >= 0 ? String(row[n] || '').trim() : '');

  shown.forEach((row, idx) => {
    const tgl = cell(row, iTgl);
    const seg = cell(row, iSeg);
    const brg = cell(row, iBrg);
    const qty = cell(row, iQty);
    const jns = cell(row, iJns);
    const sj  = cell(row, iSJ);
    const pgr = cell(row, iPgr);
    const pnr = cell(row, iPnr);
    const ket = cell(row, iKet);
    const drv = cell(row, iDrv);

    lines.push('');
    lines.push(`📄 ${idx + 1}. ${tgl || '-'}${seg ? ` | Seg ${seg}` : ''}`);
    if (brg || qty) lines.push(`   📦 ${brg || '-'}${qty ? ` (${qty})` : ''}`);
    const meta = [jns && `📋 ${jns}`, sj && `No.SJ: ${sj}`].filter(Boolean).join(' | ');
    if (meta) lines.push(`   ${meta}`);
    if (pgr || pnr) lines.push(`   👤 ${pgr || '-'} → ${pnr || '-'}`);
    if (ket) lines.push(`   📝 ${ket}`);
    if (drv && /^https?:\/\//i.test(drv)) lines.push(`   🔗 ${drv}`);
  });

  if (matches.length > MAX_PER_TAB) {
    lines.push('');
    lines.push(`   ... +${matches.length - MAX_PER_TAB} lainnya`);
  }
  return lines.join('\n');
}

function formatGenericTabBlock(matches, headers, MAX_PER_TAB) {
  const lines = [];
  const shown = matches.slice(0, MAX_PER_TAB);
  shown.forEach((row, idx) => {
    const cellLines = [];
    for (let i = 0; i < row.length; i++) {
      const val = String(row[i] || '').trim();
      if (!val) continue;
      const label = headers[i] ? headers[i] : colLetter(i);
      cellLines.push(`   ${label}: ${val}`);
    }
    lines.push('');
    lines.push(`${idx + 1}.`);
    lines.push(cellLines.join('\n'));
  });
  if (matches.length > MAX_PER_TAB) {
    lines.push(`   ... +${matches.length - MAX_PER_TAB} lainnya`);
  }
  return lines.join('\n');
}

function formatMultiTabResults(perTab, query, totalHits) {
  const MAX_PER_TAB = 5;
  const lines = [`📋 Ditemukan ${totalHits} hasil untuk "${query}":`];

  for (const tab of Object.keys(perTab)) {
    const { headers, matches } = perTab[tab];
    const more = matches.length > MAX_PER_TAB ? `, tampil ${MAX_PER_TAB} pertama` : '';
    lines.push('');
    lines.push(`━━━ ${tab} (${matches.length} hasil${more}) ━━━`);

    const block = tab === 'Surat Jalan'
      ? formatSuratJalanBlock(matches, headers, MAX_PER_TAB)
      : formatGenericTabBlock(matches, headers, MAX_PER_TAB);
    lines.push(block);
  }

  return lines.join('\n').trim();
}

function parseDateQuery(query) {
  const today = new Date();
  
  if (/\b(hari ini|today)\b/.test(query)) {
    return formatDateForMatch(today);
  }
  
  if (/\b(kemarin|yesterday)\b/.test(query)) {
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);
    return formatDateForMatch(yesterday);
  }
  
  // Parse "9 mei", "9 may", etc
  const monthMatch = query.match(/(\d{1,2})\s*(jan|feb|mar|apr|mei|may|jun|jul|agu|aug|sep|okt|oct|nov|des|dec)/i);
  if (monthMatch) {
    const day = parseInt(monthMatch[1]);
    const monthMap = {
      jan: 0, feb: 1, mar: 2, apr: 3, mei: 4, may: 4, jun: 5,
      jul: 6, agu: 7, aug: 7, sep: 8, okt: 9, oct: 9, nov: 10, des: 11, dec: 11
    };
    const month = monthMap[monthMatch[2].toLowerCase()];
    if (month !== undefined) {
      const date = new Date(today.getFullYear(), month, day);
      return formatDateForMatch(date);
    }
  }
  
  return null;
}

function formatDateForMatch(date) {
  const days = ['Minggu', 'Senin', 'Selasa', 'Rabu', 'Kamis', 'Jumat', 'Sabtu'];
  const months = ['Januari', 'Februari', 'Maret', 'April', 'Mei', 'Juni', 'Juli', 'Agustus', 'September', 'Oktober', 'November', 'Desember'];
  
  return {
    day: date.getDate(),
    month: months[date.getMonth()],
    dayName: days[date.getDay()],
    full: `${days[date.getDay()]}, ${date.getDate()} ${months[date.getMonth()]}`
  };
}

function matchDate(dateStr, dateFilter) {
  const lower = dateStr.toLowerCase();
  return lower.includes(dateFilter.day.toString()) && 
         (lower.includes(dateFilter.month.toLowerCase()) || 
          lower.includes(dateFilter.dayName.toLowerCase()));
}

function isDataQuery(text) {
  const lower = text.toLowerCase();
  // Hanya trigger jika ada kata "cek" diikuti lokasi/tanggal/progress
  return /\bcek\b/.test(lower);
}

function isInventoryReport(text) {
  const original = text.trim();
  const lower = original.toLowerCase();

  // Cek format field terstruktur (jenis:, sj:, barang:, jumlah:, dll)
  const lines = original.split(/\r?\n/).map((line) => line.trim()).filter(Boolean);
  const fieldMap = parseFieldMap(lines);
  const structuredKeys = ['jenis', 'noSuratJalan', 'pengirim', 'penerima', 'namaBarang', 'kodeBarang', 'jumlah'];
  const structuredCount = structuredKeys.filter((key) => fieldMap[key]).length;
  if (structuredCount >= 3 && fieldMap.jumlah) return true;

  // Cek kata masuk/keluar/dibawa di AWAL kalimat
  if (/^(masuk|keluar|dibawa|kirim|dikirim|laporan)\b/.test(lower)) return true;

  // Cek format cepat: ada angka + kata pergerakan + tujuan
  const hasAmount = parseAmount(original) !== null;
  const hasMovementWord = /\b(masuk|keluar|kirim|dikirim|bawa|dibawa)\b/.test(lower);
  const hasDestination = /\b(ke|tujuan|dari|mandor)\b/.test(lower);
  return hasAmount && hasMovementWord && hasDestination;
}

async function startBot() {
  checkEnv();
  const { drive, sheets } = createGoogleClients();
  const { state, saveCreds } = await useMultiFileAuthState(await ensureAuthDir());
  const { version } = await fetchLatestBaileysVersion();

  console.log(`Memulai WA bot dengan Baileys version: ${version.join('.')}`);
  
  // Initial data fetch
  if (dataSheetId) {
    console.log('Melakukan initial fetch data dari spreadsheet...');
    await updateDataCache(sheets);
    
    // Setup periodic update
    setInterval(async () => {
      console.log('Periodic update data cache...');
      await updateDataCache(sheets);
    }, dataUpdateInterval);
  }
  
  const qrTimer = setTimeout(() => {
    console.log('QR belum muncul. Jika ini terus terjadi, cek koneksi internet atau hapus folder auth_info lalu jalankan ulang.');
  }, 30000);

  const sock = makeWASocket({
    auth: state,
    version,
    logger: pino({ level: 'silent' }),
    printQRInTerminal: false,
  });

  sock.ev.on('creds.update', saveCreds);
  sock.ev.on('connection.update', ({ connection, lastDisconnect, qr }) => {
    if (connection) console.log(`Status koneksi WA: ${connection}`);
    if (qr) {
      clearTimeout(qrTimer);
      console.log('Scan QR berikut dengan WhatsApp:');
      qrcode.generate(qr, { small: true });
    }
    if (connection === 'open') {
      clearTimeout(qrTimer);
      console.log('WA bot siap dipakai.');
    }
    if (connection === 'close') {
      clearTimeout(qrTimer);
      console.log('Koneksi WA tertutup:', lastDisconnect?.error?.message || 'tanpa detail error');
      const shouldReconnect = lastDisconnect?.error?.output?.statusCode !== DisconnectReason.loggedOut;
      if (shouldReconnect) startBot().catch(console.error);
      else console.log('Bot logout. Hapus auth_info lalu scan ulang jika ingin login lagi.');
    }
  });

  sock.ev.on('messages.upsert', async ({ messages }) => {
    const msg = messages[0];
    if (!msg?.message || msg.key.fromMe) return;

    const chatId = msg.key.remoteJid;
    if (logChatId) console.log(`Pesan masuk dari chatId: ${chatId}`);
    if (allowedChat && chatId !== allowedChat) return;

    const mediaMessage = getMediaMessage(msg.message);
    const caption = getCaption(msg.message);
    const text = removeMentions(caption || getText(msg.message));
    
    console.log(`Text diterima: "${text}"`);
    
    if (!text) return;

    const botMentioned = isBotMentioned(msg.message, sock);
    console.log(`Bot mentioned: ${botMentioned}, requireMention: ${requireMention}`);
    
    if (!isBotMentioned(msg.message, sock)) return;

    // ── 1. CEK PROGRESS (harus ada kata "cek") ──────────────────────────────
    if (isDataQuery(text)) {
      console.log(`[PROGRESS] query: "${text}"`);
      await updateDataCache(sheets);
      const queryText = text
        .replace(/\b(cek|cari|data|info|status|progress|progres|site|rute|segment)\b/gi, '')
        .trim();
      console.log(`Clean query: "${queryText}"`);
      const result = searchData(queryText || text);
      await safeReply(sock, chatId, result);
      return;
    }

    // ── 2. INPUT ARSIP (masuk/keluar/dibawa di awal, atau format field) ──────
    if (isInventoryReport(text)) {
      console.log(`[ARSIP] input: "${text}"`);
      const parsed = parseCaption(text);
      if (!parsed) {
        await safeReply(sock, chatId, 'Format tidak dikenali. Contoh: keluar kabel 24000 ke brebes');
        return;
      }
      try {
        if (await isDuplicateReport(sheets, parsed)) {
          await safeReply(sock, chatId, 'Data ini sudah ada.');
          return;
        }
        let photoUrl = '';
        if (mediaMessage) {
          const buffer = await downloadMediaMessage(msg, 'buffer', {}, { logger: pino({ level: 'silent' }) });
          photoUrl = await uploadPhoto(drive, buffer, parsed);
        }
        await appendSheetRow(sheets, parsed, photoUrl, msg.key.participant || chatId);
        console.log(getLegacyReply(parsed));
        await safeReply(sock, chatId, getLegacyReply(parsed));
      } catch (error) {
        console.error(error);
        await safeReply(sock, chatId, 'Gagal menyimpan data. Cek terminal/log bot.');
      }
      return;
    }

    // ── 3. SEMUA PERTANYAAN BEBAS → AI ───────────────────────────────────────
    console.log(`[AI] pertanyaan bebas: "${text}"`);

    // Coba knowledge base lokal dulu (instant)
    const localAnswer = getFiberAnswer(text);
    if (localAnswer) {
      console.log(`[AI] jawab dari knowledge base`);
      await safeReply(sock, chatId, localAnswer);
      return;
    }

    // Tanya AI
    if (aiEnabled) {
      try {
        const aiAnswer = await askAI(text);
        if (aiAnswer) {
          console.log(`[AI] jawab dari AI`);
          await safeReply(sock, chatId, aiAnswer);
          return;
        }
      } catch (error) {
        console.error('AI error:', error.message);
      }
      // AI gagal semua - beri tahu user
      await safeReply(sock, chatId, '⚠️ AI sedang sibuk, coba lagi sebentar.');
      return;
    }

    // Fallback casual reply
    if (casualReply) {
      await safeReply(sock, chatId, createCasualReply(text));
    }
  });
}

startBot().catch((error) => {
  console.error(error.message);
  process.exit(1);
});