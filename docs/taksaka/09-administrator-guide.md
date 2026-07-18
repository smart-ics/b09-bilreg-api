# Taksaka — Administrator Guide (Panduan Administrator)

**Audience:** Administrator rumah sakit (EDP / IT operasional)  
**Level:** Praktis — langkah demi langkah  
**Basis:** Implementasi aktual di `src/taksaka.backend` dan `src/taksaka.frontend`

> Platform Taksaka saat ini **berjalan dan dapat di-deploy**, tetapi mesin pemrosesan job (antrean, penjadwal, eksekusi worker) masih **stub**. Panduan ini menjelaskan apa yang bisa dilakukan hari ini dan apa yang menunggu rilis berikutnya.

---

## Apa itu Taksaka?

Taksaka adalah **platform pemrosesan background** untuk MyHospital. Modul bisnis (Bilreg, integrasi BPJS, SATUSEHAT, dll.) akan mengirim pekerjaan ke Taksaka; Taksaka mengatur eksekusi, monitoring, dan pemulihan.

Bukan bagian dari modul klinis — ini **pusat kontrol operasional** untuk job background.

---

## Komponen yang Perlu Diketahui

| Komponen | Lokasi | Fungsi |
|----------|--------|--------|
| Taksaka.Server | Folder deploy server | Aplikasi utama (API + engine) |
| Folder `plugins/` | Di samping `Taksaka.Server.dll` | File DLL worker |
| SQL Server database `Taksaka` | Server database | Penyimpanan (schema belum ada) |
| Taksaka.Web | Server web terpisah atau static files | Konsol operator (UI) |

---

## Cara Menginstal Taksaka di Server Baru

### Persyaratan

- Windows Server dengan **.NET 8** (ASP.NET Core Runtime)
- SQL Server (database kosong `Taksaka`)
- Akun layanan Windows (bukan Administrator)

### Langkah instalasi

1. **Buat folder** `C:\Apps\Taksaka` dengan subfolder `plugins` dan `logs`.

2. **Publish aplikasi** (dari mesin build):

   ```powershell
   dotnet publish src\taksaka.backend\Taksaka.Server\Taksaka.Server.csproj -c Release -o C:\Apps\Taksaka
   ```

3. **Salin konfigurasi produksi** — buat `appsettings.Production.json`:

   ```json
   {
     "ConnectionStrings": {
       "Taksaka": "Server=SQLSERVER;Database=Taksaka;User Id=taksaka_svc;Password=***;Encrypt=True"
     },
     "PluginLoader": {
       "PluginsPath": "C:\\Apps\\Taksaka\\plugins"
     }
   }
   ```

4. **Pastikan DLL worker** ada di `C:\Apps\Taksaka\plugins\`.

5. **Daftarkan sebagai layanan Windows** (contoh NSSM):

   ```powershell
   nssm install Taksaka "C:\Program Files\dotnet\dotnet.exe" "C:\Apps\Taksaka\Taksaka.Server.dll"
   nssm set Taksaka AppDirectory C:\Apps\Taksaka
   nssm set Taksaka AppEnvironmentExtra ASPNETCORE_ENVIRONMENT=Production
   nssm start Taksaka
   ```

6. **Uji**:

   ```powershell
   Invoke-RestMethod http://localhost:5000/health
   ```

Detail lengkap: [01-deployment-guide.md](01-deployment-guide.md)

---

## Cara Menambah Worker Baru

Worker adalah file **DLL** yang berisi logika job (proyeksi data, integrasi, email, dll.).

### Langkah

1. Tim development menyerahkan file DLL (mis. `Taksaka.Workers.Satusehat.dll`).
2. **Stop** layanan Taksaka.
3. **Salin** DLL ke `C:\Apps\Taksaka\plugins\` (langsung di root folder, **bukan** subfolder).
4. Jika ada DLL dependensi, salin juga ke folder yang sama.
5. **Start** layanan Taksaka.
6. **Buka log** — cari baris:

   ```
   Discovered plugin assembly: C:\Apps\Taksaka\plugins\Taksaka.Workers.Satusehat.dll
   ```

### Catatan penting

- Worker **belum dieksekusi** oleh engine versi saat ini — hanya terdeteksi di log.
- Setelah fitur dispatch aktif, langkah deploy tetap sama: salin DLL → restart.

Panduan developer: [06-plugin-development-guide.md](06-plugin-development-guide.md)

---

## Cara Mengaktifkan / Menonaktifkan Worker

### Menonaktifkan (cara saat ini)

1. Stop layanan Taksaka.
2. Hapus atau rename file DLL worker di `plugins/` (mis. pindahkan ke `plugins_disabled\`).
3. Start layanan.
4. Pastikan log **tidak** lagi menampilkan DLL tersebut.

### Mengaktifkan kembali

1. Kembalikan DLL ke `plugins/`.
2. Restart layanan.

### Toggle tanpa restart

**Belum tersedia.** Rencana future: flag enable/disable per worker di database atau konfigurasi.

---

## Cara Menjadwalkan Job

**Belum tersedia** di implementasi saat ini.

- Tidak ada UI Scheduler yang fungsional (`/scheduler` menampilkan "coming soon").
- `IScheduler` belum membuat job.

### Yang bisa dilakukan sekarang

- Koordinasi dengan tim development untuk penjadwalan sementara di luar Taksaka (mis. Windows Task Scheduler memanggil API — **API create job juga belum ada**).

### Setelah fitur aktif (rencana)

1. Buka konsol operator → **Scheduler**.
2. Tambah jadwal (cron / interval).
3. Pilih worker target dan payload default.
4. Simpan dan verifikasi job muncul di antrean.

---

## Cara Memonitor Antrean Job

**Belum tersedia** — antrean tidak disimpan.

### Sementara

| Cara | Keterangan |
|------|------------|
| Log aplikasi | Satu-satunya sumber observasi |
| `GET /api/health` | Status platform (selalu Healthy saat ini) |
| Konsol operator → Queue | Placeholder |

### Setelah fitur aktif

1. Buka `http://{server-console}/queue`
2. Pantau jumlah job per prioritas (Critical, High, Normal, Low, Background)
3. Perhatikan job tertua dan estimasi waktu tunggu

---

## Cara Menangani Job yang Gagal

**Belum tersedia** — tidak ada job yang dieksekusi; retry dan dead letter masih stub.

### Prosedur rencana (setelah implementasi)

1. Buka konsol → **Queue** atau **Alerts**
2. Filter status `Failed` atau `DeadLetter`
3. Baca pesan error dan `ExecutionHistory`
4. Perbaiki penyebab (data, integrasi eksternal, konfigurasi)
5. Klik **Retry** untuk retry otomatis atau **Replay** untuk job baru dengan payload sama

### Saat ini

- Eskalasi ke tim development dengan cuplikan log Serilog
- Jangan hapus file di `plugins/` kecuali DLL bermasalah

---

## Cara Upgrade Versi Tanpa Kehilangan Data

### Data yang perlu dilindungi

| Aset | Risiko saat upgrade |
|------|---------------------|
| Database `Taksaka` | Aman jika di-backup (belum ada tabel) |
| `appsettings.Production.json` | Jangan ditimpa publish |
| `plugins/*.dll` | Backup folder sebelum ganti |

### Prosedur

1. **Backup** folder `C:\Apps\Taksaka` ke `C:\Apps\Taksaka_backup_YYYYMMDD`
2. **Backup** database SQL (full backup) — wajib ketika schema sudah ada
3. **Stop** layanan
4. Deploy file baru **kecuali** `appsettings.Production.json` (gabungkan perubahan manual)
5. Salin plugin DLL baru jika ada
6. **(Future)** Jalankan skrip migrasi database dari tim dev
7. **Start** layanan
8. Uji `/health` dan `/api/health`
9. Pantau log 30 menit

Rollback: restore folder backup + database (lihat [08-operations-runbook.md](08-operations-runbook.md)).

---

## Cara Backup Database Taksaka

### Sekarang (database kosong / tanpa tabel)

Backup tetap disarankan agar prosedur siap:

```sql
BACKUP DATABASE [Taksaka]
TO DISK = N'D:\Backup\Taksaka_Full.bak'
WITH INIT, COMPRESSION;
```

Jadwalkan via SQL Server Agent — harian, retensi sesuai kebijakan RS (mis. 14 hari).

### Verifikasi backup

Bulanan: restore ke instance test `Taksaka_RESTORE_TEST` dan pastikan login layanan bisa connect.

---

## Cara Restore Database Taksaka

1. Stop layanan Taksaka.
2. Restore di SSMS atau:

   ```sql
   RESTORE DATABASE [Taksaka]
   FROM DISK = N'D:\Backup\Taksaka_Full.bak'
   WITH REPLACE;
   ```

3. Pastikan user `taksaka_svc` masih memiliki akses.
4. Start layanan.
5. Uji health endpoint.

---

## Cara Migrasi Konfigurasi

### Dari server lama ke server baru

1. Export `appsettings.Production.json` dari server lama (redaksi password di dokumen).
2. Salin folder `plugins/` lengkap.
3. Install Taksaka di server baru (lihat instalasi di atas).
4. Terapkan connection string baru jika SQL Server pindah.
5. Update DNS / reverse proxy ke server baru.
6. Uji end-to-end sebelum mematikan server lama.

### Override via environment variable (tanpa edit file)

```powershell
nssm set Taksaka AppEnvironmentExtra ASPNETCORE_ENVIRONMENT=Production
nssm set Taksaka AppEnvironmentExtra ConnectionStrings__Taksaka=Server=...
nssm set Taksaka AppEnvironmentExtra PluginLoader__PluginsPath=C:\Apps\Taksaka\plugins
```

Gunakan `__` (double underscore) untuk nested key.

### CORS untuk konsol operator produksi

Saat ini kode hanya mengizinkan `http://localhost:5173`. Untuk produksi, tim development harus memperbarui `ServerServiceCollectionExtensions.cs` dengan URL konsol RS — **belum bisa diubah hanya lewat appsettings**.

---

## Konsol Operator (Taksaka.Web)

### Menjalankan (development)

```powershell
cd src\taksaka.frontend\Taksaka.Web
npm install
npm run dev
```

Buka browser: `http://localhost:5173`

### Deploy produksi

```powershell
$env:VITE_API_BASE_URL = "https://taksaka-api.rsud.local"
npm run build
```

Salin isi folder `dist/` ke IIS. Pastikan CORS di backend mengizinkan origin konsol.

Menu navigasi: Dashboard, Queue, Workers, Scheduler, Alerts, Health, Settings — **semua halaman masih placeholder** kecuali koneksi SignalR ke backend.

---

## Checklist Harian Singkat (EDP)

- [ ] Layanan Taksaka **Running**
- [ ] `/health` merespons OK
- [ ] Tidak ada error berulang di log
- [ ] File di `plugins/` sesuai daftar worker aktif

---

## Kapan Harus Hubungi Developer

| Situasi | Tindakan |
|---------|----------|
| Service tidak start setelah deploy | Developer + cuplikan log |
| DLL worker baru tidak muncul di log | Cek path; eskalasi jika path benar |
| Perlu penjadwalan / retry / dead letter | Fitur belum live — planning dengan dev |
| Perlu ubah CORS / autentikasi | Perubahan kode |
| Error SQL setelah migrasi schema | DBA + developer |

---

## Dokumen Terkait

| Dokumen | Isi |
|---------|-----|
| [01-deployment-guide.md](01-deployment-guide.md) | Deploy teknis lengkap |
| [03-operator-manual.md](03-operator-manual.md) | Manual operator (Inggris) |
| [08-operations-runbook.md](08-operations-runbook.md) | Runbook harian/mingguan |
| [04-troubleshooting.md](04-troubleshooting.md) | Pemecahan masalah |

---

## Glosarium Singkat

| Istilah | Arti |
|---------|------|
| Job | Satu unit pekerjaan background |
| Worker | Plugin DLL yang menjalankan satu jenis job |
| Queue | Antrean job menunggu eksekusi |
| Dead Letter | Job gagal permanen setelah retry habis |
| Plugin | File DLL di folder `plugins/` |
