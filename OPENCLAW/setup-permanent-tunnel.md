# Setup Cloudflare Named Tunnel (URL Permanen)

## Step 1: Login ke Cloudflare
```powershell
cloudflared tunnel login
```
- Browser akan terbuka
- Login dengan akun Cloudflare (buat gratis di cloudflare.com)
- Pilih domain (atau buat subdomain gratis dari Cloudflare)

## Step 2: Buat Named Tunnel
```powershell
cloudflared tunnel create claw-api
```
- Ini akan membuat tunnel bernama "claw-api"
- Tunnel ID akan disimpan otomatis

## Step 3: Buat Config File
Buat file `config.yml` di folder `C:\Users\ASUS\.cloudflared\`:

```yaml
tunnel: claw-api
credentials-file: C:\Users\ASUS\.cloudflared\<TUNNEL-ID>.json

ingress:
  - hostname: claw-api.trycloudflare.com
    service: http://localhost:8080
  - service: http_status:404
```

## Step 4: Route DNS
```powershell
cloudflared tunnel route dns claw-api claw-api.trycloudflare.com
```

## Step 5: Jalankan Tunnel
```powershell
cloudflared tunnel run claw-api
```

## URL Permanen Anda:
`https://claw-api.trycloudflare.com`

URL ini **TIDAK AKAN BERUBAH** setelah restart!

---

## Auto-Start saat Windows Boot

Buat file `start-permanent-tunnel.ps1`:
```powershell
cloudflared tunnel run claw-api
```

Tambahkan ke Windows Startup atau buat Windows Service.
