require('dotenv').config();

const requiredEnv = [
  'GOOGLE_SHEET_ID',
  'GOOGLE_DRIVE_FOLDER_ID',
];

const missing = requiredEnv.filter((key) => !process.env[key]);
const hasServiceAccount = process.env.GOOGLE_SERVICE_ACCOUNT_EMAIL && process.env.GOOGLE_PRIVATE_KEY;
const hasOAuth = process.env.GOOGLE_OAUTH_CLIENT_FILE && process.env.GOOGLE_OAUTH_TOKEN_FILE;
const placeholders = requiredEnv.filter((key) => {
  const value = process.env[key] || '';
  return value.includes('isi_') || value.includes('ISI_') || value.includes('project-id');
});
const authPlaceholders = ['GOOGLE_SERVICE_ACCOUNT_EMAIL', 'GOOGLE_PRIVATE_KEY'].filter((key) => {
  const value = process.env[key] || '';
  return value.includes('ISI_') || value.includes('project-id');
});

if (missing.length) {
  console.log('Konfigurasi belum lengkap. Isi nilai berikut di file .env:');
  for (const key of missing) console.log(`- ${key}`);
  process.exit(1);
}

if (!hasServiceAccount && !hasOAuth) {
  console.log('Pilih salah satu metode login Google di file .env:');
  console.log('- Service Account: isi GOOGLE_SERVICE_ACCOUNT_EMAIL dan GOOGLE_PRIVATE_KEY');
  console.log('- OAuth: isi GOOGLE_OAUTH_CLIENT_FILE dan GOOGLE_OAUTH_TOKEN_FILE');
  process.exit(1);
}

if (placeholders.length || (hasServiceAccount && authPlaceholders.length)) {
  console.log('Konfigurasi masih berisi contoh/template. Ganti nilai berikut di file .env:');
  for (const key of [...placeholders, ...authPlaceholders]) console.log(`- ${key}`);
  process.exit(1);
}

console.log('Konfigurasi .env terlihat lengkap. Jalankan: npm start');