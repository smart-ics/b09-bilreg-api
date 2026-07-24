# Buku Panduan: Deploy dan Konfigurasi Kiosk Antrian Admisi & Queue Display

**Untuk:** Implementor ICS dan IT Rumah Sakit  
**Bahasa:** Indonesia  
**Cara pakai:** Ikuti dari **Langkah 1** sampai **Langkah 12** berurutan. Jangan loncat ke deploy folder IIS sebelum perencanaan loket dan backend siap.

**Cakupan buku ini:** memasang, mengonfigurasi, menguji, dan memutar balik **Kiosk** (ambil nomor) serta **Queue Display** (layar panggilan).  
**Tidak dibahas di sini:** migrasi database Bilreg secara detail, SOP harian petugas Admisi, dan skala multi-server SignalR. Untuk runbook teknis penuh (bahasa Inggris), lihat dokumen terkait di akhir buku.

---

## Pendahuluan — apa yang Anda pasang

Di lapangan, antrean admisi rawat jalan melibatkan tiga sisi:

1. **Kiosk** — pasien memilih layanan dan mengambil nomor; tiket thermal dicetak di PC kiosk.  
2. **Officer (HIS MyHospital — Registrasi Rawat Jalan)** — petugas memanggil nomor (Call), memanggil ulang (Recall), lalu mulai pelayanan.  
3. **Queue Display** — layar di lobby menampilkan nomor yang dipanggil dan memutar suara (TTS).

Ketiganya berbicara ke **Bilreg API**. Yang “benar” selalu data di server. SignalR hanya mempercepat refresh Display; jika SignalR putus, Display tetap benar dengan polling.

Identitas setiap kiosk atau layar adalah **bagian akhir URL**, misalnya:

- `http://192.168.10.5/kiosk/loket-03` → stasiun kiosk bernama `loket-03`  
- `http://192.168.10.5/display/lobby-poli-1` → layar bernama `lobby-poli-1`

Buku ini memandu Anda menyiapkan Kiosk dan Display di IIS, menyelaraskan kode loket dengan petugas, lalu membuktikan alur: ambil nomor → Call → tampil di layar.

---

## Langkah 1 — Bentuk tim kecil dan isi data lapangan

Kumpulkan Implementor, IT RS (IIS/jaringan), dan perwakilan Admisi. Sepakati dulu peta lapangan di kertas atau spreadsheet. Kolom minimal:

1. Nama PC / TV fisik (lokasi)  
2. Jenis: Kiosk, Display, atau PC Officer  
3. ID path yang akan dipakai di URL (contoh `loket-03`, `lobby-poli-1`)  
4. Kode loket petugas (`LOKET-A`, `LOKET-B`, …)  
5. Service Point yang boleh dipilih di kiosk itu (contoh BPJS, UMUM)  
6. `WorkstationKey` PC petugas (harus unik per PC Officer)  
7. Catatan printer / speaker  

**Aturan yang tidak boleh dilanggar**

- ID path di URL harus sama persis dengan kunci di `devices.json` nanti.  
- Kode loket yang dipanggil petugas harus termasuk dalam `loketIds` Display yang relevan.  
- `WorkstationKey` + `LoketKey` di server Bilreg harus sama dengan yang diisi di config Officer HIS.

Contoh hasil Langkah 1:

| Perangkat | Jenis | ID path | Loket / layanan | Catatan |
|-----------|-------|---------|-----------------|---------|
| PC Lobby 1 | Kiosk | `loket-03` | BPJS, UMUM | Printer thermal USB |
| TV Lobby Poli | Display | `lobby-poli-1` | LOKET-A, LOKET-B | Speaker on |
| PC Loket A | Officer | — | Workstation `ADM-01` → LOKET-A | HIS MyHospital |

Jika tabel ini belum disepakati Admisi, **berhenti di sini**. Deploy tanpa peta loket hampir selalu gagal di uji Call → Display.

---

## Langkah 2 — Pastikan Bilreg API siap

Kerjakan di lingkungan target (integrasi atau produksi sesuai jadwal). Tanpa langkah ini, Kiosk/Display tidak berguna.

### 2.1 API hidup

Pastikan Bilreg dapat diakses dari jaringan kiosk/display (contoh: `http://<server>/bilregapi/...` atau URL site Anda).

### 2.2 Status rollout Admission Queue

Panggil (dengan JWT yang valid):

```http
GET /api/v1/admission-queue/rollout/status
Authorization: Bearer <token>
```

Lanjutkan hanya jika:

- `allSchemaReady` = `true`  
- `workstationMappingsUnique` = `true`  
- `workstationMappingCount` > 0 (jika petugas sudah akan Call)  
- `signalRRefreshEnabled` = `true` (disarankan agar Display cepat update; jika `false`, Display tetap jalan lewat poll)

Jika schema belum siap, minta tim backend menyelesaikan migrasi/seed sesuai runbook backend sebelum lanjut.

### 2.3 Isi mapping workstation di server

Di `appsettings` Bilreg (atau konfigurasi setara di server), isi sesuai peta Langkah 1:

```json
"AdmissionQueueApi": {
  "LegacyEndpointsEnabled": true,
  "SignalRRefreshEnabled": true,
  "Workstations": [
    { "WorkstationKey": "ADM-01", "LoketKey": "LOKET-A" },
    { "WorkstationKey": "ADM-02", "LoketKey": "LOKET-B" }
  ]
}
```

Artinya:

- PC petugas dengan kunci `ADM-01` hanya boleh mengoperasikan loket `LOKET-A`.  
- Setiap `WorkstationKey` unik; setiap `LoketKey` unik.

Restart Bilreg API setelah mengubah file konfigurasi. Tanpa baris yang cocok, tombol Call di Officer akan ditolak dengan pesan workstation tidak dikonfigurasi.

### 2.4 Service Point aktif

Pastikan minimal satu Service Point aktif (seed Ops), misalnya BPJS / UMUM, supaya kiosk punya pilihan layanan.

### 2.5 Hub SignalR ada

Display memakai hub di path (sesuaikan prefix site Anda):

```text
POST http://<server>/bilregapi/hubs/admission-queue/negotiate?negotiateVersion=1
```

Dengan JWT valid, harus mendapat HTTP **200** dan JSON berisi `connectionId`.  
Jika **404**, build/API belum memuat hub atau path salah (hub **bukan** di bawah `/api/hubs/...`).  
Jika **401**, token belum dikirim atau tidak valid — hub-nya sudah ada.

---

## Langkah 3 — Siapkan mesin build dan variabel lingkungan

Build biasanya dilakukan di mesin implementor/CI, bukan wajib di server IIS.

1. Ambil sumber monorepo `c013-kiosk-queue-display-web`.  
2. Pasang Node.js + pnpm sesuai versi proyek.  
3. Di folder monorepo:

```bash
cd c013-kiosk-queue-display-web
pnpm install
```

4. Buat file lingkungan untuk **Kiosk** dan **Display** (misalnya `.env` / `.env.production` per app). **Jangan commit token ke git.**

Isi minimal:

| Variabel | Contoh | Fungsi |
|----------|--------|--------|
| `VITE_BILREG_API_BASE` | `http://192.168.10.5/bilregapi/api` | Alamat REST Bilreg |
| `VITE_BILREG_TOKEN` | *(JWT yang disediakan Ops)* | Autentikasi REST + SignalR (V1) |

**Keamanan:** pada V1, token tertanam di hasil build. Batasi akses jaringan ke VLAN internal. Jika site tanpa HTTPS, jangan expose ke internet.

---

## Langkah 4 — Build paket Kiosk dan Display

Dari root monorepo:

```bash
pnpm build
```

Setelah sukses, periksa dua folder hasil:

- `apps/kiosk-web/dist/`  
- `apps/display-web/dist/`

Masing-masing **wajib** berisi:

- `index.html`  
- `assets/`  
- `version.json`  
- `web.config`  
- `devices.json`  

Jika salah satu hilang, jangan salin ke server. Perbaiki build dulu.

Catat nilai `version` di dalam `version.json` — nanti dipakai untuk arsip go-live dan cek auto-refresh.

---

## Langkah 5 — Backup folder lama di IIS

Di server web, sebelum menimpa:

1. Zip folder `C:\inetpub\wwwroot\kiosk` (jika sudah ada) dengan nama bertanggal.  
2. Zip folder `C:\inetpub\wwwroot\display` (jika sudah ada) dengan nama bertanggal.  
3. Simpan zip di lokasi yang disepakati IT (bukan hanya di desktop sementara).

Ini satu-satunya cara rollback cepat jika build baru bermasalah — **tanpa** menyentuh database.

---

## Langkah 6 — Siapkan situs IIS (sekali saja per server)

Gunakan **satu site IIS**, dua folder aplikasi (path-based, tanpa subdomain per perangkat).

### 6.1 Modul yang dibutuhkan

Pasang **IIS URL Rewrite** jika belum ada. Tanpa modul ini, URL seperti `/kiosk/loket-03` akan 404.

### 6.2 Buat struktur folder

```text
C:\inetpub\wwwroot\
├── kiosk\
└── display\
```

(Anda boleh memakai virtual directory/application di bawah site yang sama; yang penting path publik tetap `/kiosk/...` dan `/display/...`.)

### 6.3 Salin hasil build

1. Salin seluruh isi `apps/kiosk-web/dist/` → `C:\inetpub\wwwroot\kiosk\`  
2. Salin seluruh isi `apps/display-web/dist/` → `C:\inetpub\wwwroot\display\`  

Pastikan `web.config` ikut tersalin. Jangan hapus file itu.

### 6.4 Cache (disarankan)

- `index.html` dan `version.json` → sebaiknya `Cache-Control: no-cache`  
- File di `assets/` (nama sudah hashed) → boleh cache lama  

### 6.5 Uji deep link di browser biasa

Belum perlu mode kiosk. Dari PC di jaringan yang sama, buka:

```text
http://<host-atau-ip>/kiosk/<stationId-dari-peta>
http://<host-atau-ip>/display/<screenId-dari-peta>
```

Contoh:

```text
http://192.168.10.5/kiosk/loket-03
http://192.168.10.5/display/lobby-poli-1
```

**Berhasil** = halaman aplikasi muncul (bukan 404 IIS, bukan folder listing).  
Jika masih error ID perangkat, itu normal sampai `devices.json` diselaraskan di Langkah 7 — yang penting bukan 404 rewrite.

---

## Langkah 7 — Konfigurasi `devices.json` sesuai peta lapangan

Edit file di server (atau siapkan sebelum copy):

- `C:\inetpub\wwwroot\kiosk\devices.json`  
- `C:\inetpub\wwwroot\display\devices.json`  

Setiap ID di URL **harus** ada di file yang sesuai. ID tidak dikenal atau `role` salah → aplikasi sengaja **gagal boot** agar tidak menampilkan data salah.

### 7.1 Isi Kiosk

Untuk setiap stasiun di peta Langkah 1:

- kunci = ID path (`loket-03`, …)  
- `role` = `"kiosk"`  
- daftar Service Point / offerings = layanan yang boleh dipilih di kiosk itu  
- `printerProxyPort` = port proxy cetak di PC lokal (kosongkan jika memakai default **5050**)

Contoh (sesuaikan nama field dengan skema paket rilis Anda jika berbeda tipis):

```json
{
  "loket-03": {
    "role": "kiosk",
    "offerings": ["BPJS", "UMUM"],
    "printerProxyPort": 5050
  },
  "loket-07": {
    "role": "kiosk",
    "offerings": ["UMUM"],
    "printerProxyPort": 5050
  }
}
```

### 7.2 Isi Queue Display

Untuk setiap layar:

- kunci = ID path (`lobby-poli-1`, …)  
- `role` = `"display"`  
- `loketIds` = daftar kode loket yang ditampilkan (tidak boleh kosong)

```json
{
  "lobby-poli-1": {
    "role": "display",
    "loketIds": ["LOKET-A", "LOKET-B"]
  },
  "lobby-igd": {
    "role": "display",
    "loketIds": ["LOKET-C"]
  }
}
```

### 7.3 Cek silang sekali lagi

Buka kembali tabel Langkah 1. Pastikan:

- Setiap shortcut yang akan dipasang punya baris di `devices.json`.  
- Setiap `LoketKey` petugas yang akan diuji ada di `loketIds` Display yang relevan.  
- Officer HIS memakai `workstationKey` / `loketKey` yang sama dengan Bilreg `Workstations`.

Simpan file. Muat ulang halaman kiosk/display di browser.

---

## Langkah 8 — Konfigurasi Officer HIS (selaras, singkat)

Officer tidak di-deploy dari paket kiosk/display, tetapi **harus** diselaraskan:

1. Di `global_config.json` MyHospital, blok `admissionQueue`:  
   - `enabled`: `true`  
   - `useLegacySidebarFallback`: `false` (kecuali Ops meminta fallback)  
   - `workstationKey` dan `loketKey` sesuai PC petugas di Langkah 1 / Bilreg  
2. Restart / hard-refresh browser Officer jika perlu.  
3. Buka **Admisi → Registrasi Rawat Jalan** dan pastikan sidebar Admission Queue muncul serta worklist terisi (atau kosong valid untuk hari itu).

Tanpa langkah ini, uji E2E Call tidak bisa dilakukan.

---

## Langkah 9 — Pasang print proxy di setiap PC Kiosk

Cetak tiket **tidak** lewat server IIS. Setiap PC kiosk menjalankan proxy lokal ke printer thermal.

1. Instal dan jalankan layanan print proxy di PC kiosk.  
2. Pastikan listening di port yang sama dengan `printerProxyPort` di `devices.json` (default **5050**).  
3. Cek printer: kertas, daya, driver.  
4. Setelah Kiosk sudah bisa intake (Langkah 11), uji: ambil nomor → tiket keluar; reprint → nomor **tidak** berubah.

Jika printer belum siap di hari cutover, tulis **penundaan resmi** (nama penanggung jawab + tanggal target). Intake tetap boleh diuji tanpa cetak jika Admisi menyetujui.

---

## Langkah 10 — Pasang shortcut browser mode kiosk di PC lapangan

Di PC kiosk dan PC/TV display, buat shortcut Chrome atau Edge, contoh:

**Kiosk**

```text
chrome.exe --kiosk --edge-kiosk-type=fullscreen http://192.168.10.5/kiosk/loket-03
```

**Display**

```text
chrome.exe --kiosk --edge-kiosk-type=fullscreen http://192.168.10.5/display/lobby-poli-1
```

Ganti host dan ID path sesuai peta Anda. Satu perangkat fisik = satu shortcut dengan satu ID.

**Menambah perangkat baru nanti:** buat ID baru di `devices.json` + shortcut baru. Tidak perlu rebuild jika API base dan token masih sama.

Nyalakan shortcut. Pastikan:

- Kiosk menampilkan pilihan layanan (bukan error boot).  
- Display menampilkan kerangka layar (boleh belum ada nomor).  
- Volume speaker Display cukup terdengar di lobby.

---

## Langkah 11 — Uji penerimaan end-to-end (wajib)

Lakukan bersama Implementor + Admisi. Catat jam, Queue Label, dan nilai `version.json`.

### 11.1 Ambil nomor di Kiosk

1. Buka shortcut kiosk.  
2. Pilih Service Point.  
3. Ambil nomor.  

**Lulus jika:** label antrian tampil di kiosk; tiket keluar (atau penundaan printer sudah tertulis); nomor muncul di worklist Officer.

### 11.2 Call dari Officer

1. Di HIS, pilih entri Waiting yang baru.  
2. Tekan **Call**.  

**Lulus jika:** Display menampilkan label yang sama; audio TTS berbunyi **sekali** (jika audio diaktifkan).

### 11.3 Start Service

Tekan **Start Service** di Officer.  

**Lulus jika:** Display ikut berubah (lewat SignalR dan/atau poll dalam beberapa detik).

### 11.4 Selesai registrasi (jika skenario hari itu mengizinkan)

Selesaikan outcome registrasi Established sesuai prosedur Admisi.  

**Lulus jika:** entri hilang dari worklist Officer.

### 11.5 Cadangan tanpa SignalR (disarankan sekali)

Matikan sementara refresh SignalR (recycle hub atau set `SignalRRefreshEnabled: false` lalu restart API — koordinasikan dengan tim backend).  

**Lulus jika:** setelah Call berikutnya, Display tetap update lewat polling; layar tidak “kosong” hanya karena SignalR mati. Kembalikan flag ke `true` setelah uji.

### 11.6 Konflik Call ganda

Dengan loket masih Outstanding, coba Call nomor lain ke loket yang sama.  

**Lulus jika:** server menolak (**409**); Officer memuat ulang daftar/klaim.

### 11.7 Auto-refresh versi (opsional di hari yang sama atau saat rilis berikutnya)

Deploy build dengan `version.json` berbeda; biarkan kiosk/display idle.  

**Lulus jika:** browser melakukan soft-reload saat idle tanpa kunjungan manual.

Jika langkah 11.1–11.3 gagal, **jangan GO**. Perbaiki dulu (lihat Lampiran A).

---

## Langkah 12 — Putuskan GO / NO-GO dan arsipkan

### Checklist hari cutover

- [ ] Backup `kiosk` dan `display` lama tersimpan  
- [ ] `rollout/status` hijau (schema + workstation)  
- [ ] Dist lengkap di IIS (lima file/folder wajib)  
- [ ] Deep link `/kiosk/{id}` dan `/display/{id}` OK  
- [ ] `devices.json` selaras dengan peta Admisi  
- [ ] Officer `admissionQueue` aktif dan kunci loket cocok  
- [ ] Print proxy OK **atau** penundaan tertulis  
- [ ] Uji minimal lulus: intake → Call → Display (+ audio jika dipakai)  
- [ ] Arsip: `version.json`, host IIS, cuplikan `rollout/status`, log/foto uji  

### Keputusan

- **GO** — semua poin di atas hijau (printer boleh ditunda dengan pemilik jelas).  
- **NO-GO** — deep link gagal, ID perangkat hilang, rollout merah, atau Call tidak muncul di Display tanpa alasan yang diterima Ops.

Isi kontak site (untuk serah terima):

| Peran | Nama / Tim | Kontak |
|-------|------------|--------|
| Implementor ICS | | |
| IT RS (IIS / jaringan) | | |
| Admisi (loket & shortcut) | | |
| Owner printer thermal | | |

---

## Lampiran A — Jika ada masalah (troubleshooting)

Kerjakan dari atas ke bawah sesuai gejala.

**URL `/kiosk/...` atau `/display/...` 404**  
→ Pasang IIS URL Rewrite; pastikan `web.config` ada di folder aplikasi; pastikan path fisik benar.

**Aplikasi “gagal boot” / ID tidak dikenal**  
→ Samakan ID di shortcut dengan kunci di `devices.json`; pastikan `role` benar (`kiosk` atau `display`).

**Display tidak berubah setelah Call**  
→ Cek `loketIds` Display mencakup loket petugas; cek Network: REST snapshot dan negotiate tidak 401; pastikan Officer benar-benar sukses Call (bukan error workstation).

**Negotiate / SignalR merah**  
→ Pastikan URL hub `.../bilregapi/hubs/admission-queue` (bukan `.../api/hubs/...`); pastikan JWT Display valid.

**Call Officer: “Workstation is not configured”**  
→ Isi `AdmissionQueueApi:Workstations` di server; samakan dengan `workstationKey` / `loketKey` Officer; restart API.

**Tiket tidak keluar**  
→ Cek print proxy di `localhost:{port}`; cek printer; reprint tidak boleh membuat nomor baru.

**Setelah update masih tampilan lama**  
→ Pastikan `index.html` / `version.json` tidak di-cache agresif; hard refresh sekali; cek `version.json` baru sudah di server.

---

## Lampiran B — Rollback

| Situasi | Lakukan | Jangan |
|---------|---------|--------|
| Build kiosk/display buruk | Kembalikan zip backup folder `wwwroot/kiosk` dan/atau `wwwroot/display` | DROP tabel database |
| Salah `devices.json` | Perbaiki file, muat ulang / buka ulang shortcut | Rebuild kecuali diganti API/token |
| Regresi Officer | Sesuaikan flag `admissionQueue` di HIS | Ubah folder kiosk/display kecuali terkait |
| Bug backend | Ikuti rollback runbook backend | DROP tabel hanya karena masalah klien |

Setelah rollback folder, ulang Langkah 11.1–11.3 singkat untuk memastikan layanan kritis kembali.

---

## Lampiran C — Dokumen teknis terkait

| Dokumen | Isi |
|---------|-----|
| [TRACKER-ADMISSION-QUEUE-RUNBOOK.md](./TRACKER-ADMISSION-QUEUE-RUNBOOK.md) | Runbook lengkap backend + klien (EN) |
| [TRACKER-ADMISSION-QUEUE-IIS-CLIENT-CUTOVER-CHECKLIST.md](./TRACKER-ADMISSION-QUEUE-IIS-CLIENT-CUTOVER-CHECKLIST.md) | Checklist Go/No-Go IIS |
| [kiosk-queue-display-web.md](./kiosk-queue-display-web.md) | Keputusan arsitektur path IIS |
| [TRACKER-ADMISSION-QUEUE-API-V1.md](./TRACKER-ADMISSION-QUEUE-API-V1.md) | Kontrak API / SignalR |

---

*Akhir buku. Ikuti Langkah 1 → 12 berurutan; gunakan Lampiran hanya saat macet atau rollback.*
