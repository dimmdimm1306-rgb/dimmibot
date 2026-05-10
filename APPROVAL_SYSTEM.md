# Sistem Approval untuk Upload Data

## Overview

Sistem approval ini memungkinkan user non-admin untuk membuat draft (Surat Jalan, Progress, Kegiatan) yang harus diapprove oleh admin sebelum diupload ke Google Sheets.

## Alur Kerja

### 1. User Non-Admin (Email Biasa)
1. Sign in dengan Google di halaman ProjectsHomePage
2. Buat draft Surat Jalan / Progress / Kegiatan
3. Klik tombol "🚀 Kirim semua draft ke Spreadsheet"
4. Sistem akan otomatis submit draft untuk approval (tidak langsung upload)
5. User mendapat konfirmasi bahwa draft telah dikirim ke admin
6. Menunggu admin approve

### 2. Admin (Email Whitelist: dimmdimm1306@gmail.com)
1. Sign in dengan Google di halaman ProjectsHomePage
2. Tombol "Tambah Project" otomatis muncul (karena email di whitelist)
3. Di halaman Input, ada tombol "⚠️ Pending Approvals" dengan badge jumlah pending
4. Klik tombol tersebut untuk melihat daftar draft yang menunggu approval
5. Review detail setiap draft:
   - Siapa yang buat (email)
   - Tanggal dibuat
   - Jenis (Surat Jalan / Progress / Absensi)
   - Summary/ringkasan data
6. Pilih action:
   - **Approve**: Data langsung diupload ke Google Sheets
   - **Reject**: Data ditolak dengan alasan (opsional)

## File-file yang Dibuat

### 1. Models/PendingApproval.cs
Model untuk menyimpan draft yang menunggu approval:
- `Id`: Unique identifier
- `CreatedAt`: Waktu dibuat
- `ProjectId`: ID project
- `CreatedBy`: Email user yang buat
- `Type`: "SuratJalan" | "Progress" | "Absensi"
- `Status`: "Pending" | "Approved" | "Rejected"
- `ApprovedBy`: Email admin yang approve/reject
- `ApprovedAt`: Waktu approve/reject
- `RejectionReason`: Alasan reject (jika ditolak)
- `DraftDataJson`: Data draft dalam format JSON
- `Summary`: Ringkasan untuk ditampilkan di list

### 2. Services/ApprovalService.cs
Service untuk mengelola approval:
- `SubmitForApprovalAsync()`: Submit draft untuk approval
- `GetPendingApprovalsAsync()`: Get semua pending approvals
- `GetPendingCountAsync()`: Get jumlah pending (untuk badge)
- `ApproveAsync()`: Approve draft
- `RejectAsync()`: Reject draft
- `GetByIdAsync()`: Get approval by ID
- `DeleteAsync()`: Delete approval setelah diproses

Data disimpan di file JSON: `pending_approvals.json` di AppDataDirectory

### 3. Pages/ApprovalListPage.xaml & .cs
Halaman untuk admin melihat dan memproses pending approvals:
- List semua draft yang menunggu approval
- Tampilkan detail: type, created by, tanggal, summary
- Tombol Approve (hijau) dan Reject (merah)
- Badge jumlah pending di header
- Empty state jika tidak ada pending

### 4. Modifikasi InputHubPage
- Tambah parameter `AuthService` dan `ApprovalService` di constructor
- Tambah tombol "⚠️ Pending Approvals" (hanya untuk admin)
- Badge jumlah pending approvals
- Logic di `OnUploadAll()`:
  - Jika admin: konfirmasi detail → upload langsung
  - Jika non-admin: submit untuk approval → tidak upload

### 5. Modifikasi RootTabbedPage
- Resolve `ApprovalService` dari DI container
- Pass ke `InputHubPage` constructor

### 6. Modifikasi MauiProgram.cs
- Register `ApprovalService` sebagai Singleton di DI container

## Cara Kerja Detail

### Submit untuk Approval (Non-Admin)
```csharp
// User klik "Kirim semua draft"
// Sistem cek: apakah user admin?
bool isAdmin = _auth.CanEdit;

if (!isAdmin)
{
    // Submit untuk approval
    await _approval.SubmitForApprovalAsync(
        projectId: _project.Id,
        createdBy: _gauth.AccountEmail,
        type: "SuratJalan",
        draftData: suratJalanList,
        summary: "5 Surat Jalan - 08/05/2026"
    );
    
    // Draft disimpan di pending_approvals.json
    // User mendapat notifikasi bahwa draft telah dikirim ke admin
}
```

### Approve Draft (Admin)
```csharp
// Admin klik "Approve"
await _approval.ApproveAsync(approvalId, adminEmail);

// Upload data ke Google Sheets
bool uploaded = await UploadApprovedDataAsync(approval);

if (uploaded)
{
    // Delete approval dari pending list
    await _approval.DeleteAsync(approvalId);
}
```

### Reject Draft (Admin)
```csharp
// Admin klik "Reject" dan input alasan
await _approval.RejectAsync(approvalId, adminEmail, reason);

// Delete approval dari pending list
await _approval.DeleteAsync(approvalId);

// User tidak mendapat notifikasi otomatis (bisa ditambahkan nanti)
```

## Keamanan

1. **Whitelist Email Admin**: Hanya email di `AuthService.Whitelist` yang bisa:
   - Melihat tombol "Pending Approvals"
   - Mengakses halaman ApprovalListPage
   - Approve/reject draft
   - Upload langsung tanpa approval

2. **Email Non-Whitelist**: 
   - Bisa sign in dengan Google
   - Bisa membuat draft
   - Tidak bisa upload langsung
   - Harus submit untuk approval

3. **Tanpa Login**:
   - Bisa melihat semua data (Surat Jalan, Progress, Stok)
   - Tidak bisa akses fitur Input
   - Tidak bisa membuat draft

## Notifikasi (Future Enhancement)

Saat ini sistem belum memiliki notifikasi real-time. Untuk implementasi notifikasi:

1. **Push Notification**: 
   - Gunakan Firebase Cloud Messaging (FCM)
   - Admin mendapat notif saat ada draft baru
   - User mendapat notif saat draft diapprove/reject

2. **Email Notification**:
   - Kirim email ke admin saat ada draft baru
   - Kirim email ke user saat draft diapprove/reject

3. **In-App Notification**:
   - Badge di icon aplikasi
   - Notification center di dalam app

## Testing

### Test Case 1: Non-Admin Submit Draft
1. Sign in dengan email non-whitelist (contoh: user@example.com)
2. Buat draft Surat Jalan
3. Klik "Kirim semua draft"
4. Verify: Muncul dialog "Submit untuk Approval"
5. Verify: Draft tidak langsung upload ke Google Sheets
6. Verify: Draft tersimpan di pending_approvals.json

### Test Case 2: Admin Approve Draft
1. Sign in dengan email whitelist (dimmdimm1306@gmail.com)
2. Buka halaman Input
3. Verify: Tombol "Pending Approvals" muncul dengan badge
4. Klik tombol tersebut
5. Verify: Muncul list draft yang menunggu approval
6. Klik "Approve" pada salah satu draft
7. Verify: Data diupload ke Google Sheets
8. Verify: Draft hilang dari pending list

### Test Case 3: Admin Reject Draft
1. Sign in dengan email whitelist
2. Buka halaman Pending Approvals
3. Klik "Reject" pada salah satu draft
4. Input alasan reject
5. Verify: Draft hilang dari pending list
6. Verify: Data tidak diupload ke Google Sheets

### Test Case 4: Admin Upload Langsung
1. Sign in dengan email whitelist
2. Buat draft Surat Jalan
3. Klik "Kirim semua draft"
4. Verify: Muncul konfirmasi detail draft
5. Verify: Setelah konfirm, data langsung upload ke Google Sheets
6. Verify: Tidak masuk ke pending approvals

## Konfigurasi

### Menambah Admin Baru
Edit file `Services/AuthService.cs`:

```csharp
private static readonly HashSet<string> Whitelist = new(StringComparer.OrdinalIgnoreCase)
{
    "dimmdimm1306@gmail.com",
    "admin2@example.com",  // Tambah admin baru di sini
    "admin3@example.com",
};
```

### Mengubah Storage Location
Edit file `Services/ApprovalService.cs`:

```csharp
public ApprovalService()
{
    // Default: AppDataDirectory
    _approvalFile = Path.Combine(FileSystem.AppDataDirectory, "pending_approvals.json");
    
    // Alternatif: CacheDirectory (akan dihapus saat clear cache)
    // _approvalFile = Path.Combine(FileSystem.CacheDirectory, "pending_approvals.json");
}
```

## Troubleshooting

### Problem: Badge tidak update
**Solution**: Panggil `await RefreshBadgesAsync()` setelah approve/reject

### Problem: Draft tidak muncul di pending list
**Solution**: 
1. Cek file `pending_approvals.json` di AppDataDirectory
2. Verify `ProjectId` sesuai dengan project yang dipilih
3. Verify `Status` adalah "Pending"

### Problem: Upload gagal setelah approve
**Solution**:
1. Cek koneksi internet
2. Cek Google OAuth token masih valid
3. Cek log error di `lastErr` variable

## Changelog

### Version 1.0 (08 Mei 2026)
- ✅ Sistem approval untuk Surat Jalan dan Progress
- ✅ Halaman Pending Approvals untuk admin
- ✅ Badge jumlah pending approvals
- ✅ Submit untuk approval (non-admin)
- ✅ Approve/Reject draft (admin)
- ✅ Upload langsung untuk admin
- ✅ Konfirmasi detail sebelum upload (admin)

### Future Enhancements
- [ ] Push notification untuk admin dan user
- [ ] Email notification
- [ ] History approval (approved/rejected)
- [ ] Bulk approve/reject
- [ ] Filter dan search di pending list
- [ ] Export pending approvals ke Excel
