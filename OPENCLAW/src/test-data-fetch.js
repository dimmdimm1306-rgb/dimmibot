require('dotenv').config();

const { google } = require('googleapis');
const fs = require('fs');

function extractId(value, type) {
  if (!value) return value;
  const folderMatch = value.match(/folders\/([a-zA-Z0-9_-]+)/);
  if (type === 'folder' && folderMatch) return folderMatch[1];
  const sheetMatch = value.match(/\/d\/([a-zA-Z0-9_-]+)/);
  if (type === 'sheet' && sheetMatch) return sheetMatch[1];
  return value;
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

async function testDataFetch() {
  console.log('=== Test Fetch Data Spreadsheet ===\n');

  const dataSheetId = process.env.DATA_SHEET_ID;
  const dataSheetName = process.env.DATA_SHEET_NAME || 'Sheet1';

  if (!dataSheetId) {
    console.log('❌ DATA_SHEET_ID tidak diset di .env');
    console.log('Tambahkan: DATA_SHEET_ID=1RC2Ylo4DjIAJkNMLe6v0jMnupJMcrP2v5aFTauhhcsg');
    return;
  }

  console.log(`📊 Spreadsheet ID: ${dataSheetId}`);
  console.log(`📄 Sheet Name: ${dataSheetName}\n`);

  try {
    const auth = process.env.GOOGLE_OAUTH_CLIENT_FILE && process.env.GOOGLE_OAUTH_TOKEN_FILE
      ? createOAuthAuth()
      : createServiceAccountAuth();

    const sheets = google.sheets({ version: 'v4', auth });

    console.log('🔄 Fetching data dari spreadsheet...\n');

    const response = await sheets.spreadsheets.values.get({
      spreadsheetId: extractId(dataSheetId, 'sheet'),
      range: `${dataSheetName}!A:I`,
    });

    const rows = response.data.values || [];

    if (rows.length === 0) {
      console.log('⚠️  Spreadsheet kosong atau tidak ada data');
      return;
    }

    console.log('✅ Berhasil fetch data!\n');
    console.log(`📝 Total baris: ${rows.length}`);
    console.log(`📋 Header: ${rows[0].join(' | ')}\n`);

    if (rows.length > 1) {
      console.log('📊 Sample data (5 baris pertama):\n');
      const sampleRows = rows.slice(1, 6);
      sampleRows.forEach((row, index) => {
        console.log(`${index + 1}. ${row[0] || '-'} | ${row[1] || '-'} | ${row[2] || '-'} | ${row[3] || '-'}`);
        console.log(`   Progress: ${row[4] || '-'} | Kab/Kota: ${row[7] || '-'} | Site: ${row[8] || '-'}\n`);
      });
    }

    console.log('✅ Test berhasil! Bot siap membaca data dari spreadsheet ini.');
    console.log('\n💡 Cara pakai:');
    console.log('   @bot cek [keyword]');
    console.log('   @bot cari site 12345');
    console.log('   @bot info brebes');

  } catch (error) {
    console.error('❌ Error:', error.message);
    
    if (error.message.includes('permission')) {
      console.log('\n💡 Solusi:');
      console.log('   1. Pastikan spreadsheet di-share ke Service Account email');
      console.log('   2. Atau pastikan OAuth account punya akses ke spreadsheet');
      console.log(`   3. Service Account: ${process.env.GOOGLE_SERVICE_ACCOUNT_EMAIL || 'tidak diset'}`);
    }
    
    if (error.message.includes('not found')) {
      console.log('\n💡 Solusi:');
      console.log('   1. Cek DATA_SHEET_ID sudah benar');
      console.log('   2. Cek DATA_SHEET_NAME sesuai dengan nama tab di spreadsheet');
    }
  }
}

testDataFetch().catch(console.error);
