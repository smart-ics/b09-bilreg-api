---
Title: Test Package - Verifikasi Perbaikan Prefix Ruangan pada Cetakan Struk Registrasi dan Endpoint Detail Registrasi (GET /api/reg/{id})
Code: ANTRIAN-POLI-PREFIX-BUG-001
Artifact: TEST-PACKAGE
Version: 1.0
LastUpdated: 2026-11-14
Status: READY-FOR-EXECUTION
TestExecution: [ANTRIAN-POLI-PREFIX-BUG-001-TEST-EXECUTION.md](ANTRIAN-POLI-PREFIX-BUG-001-TEST-EXECUTION.md)
ParentTestPackage: [ANTRIAN-POLI-PREFIX-TEST-PACKAGE.md](ANTRIAN-POLI-PREFIX-TEST-PACKAGE.md)
---

# 1. Tujuan Pengujian

Memastikan perbaikan cacat `DEF-001` (`ISSUE-BILREG-ANTRIAN-POLI-PREFIX-BUG-001`) berjalan dengan benar, yaitu:
1. Endpoint detail registrasi `GET /api/reg/{id}` (`RegGetQuery` / `RegGetResponse`) mengembalikan properti aditif `queueLabel` dan `prefixAntrian` dari sesi antrian dokter.
2. Bukti cetak struk registrasi rawat jalan (`antrian_registrasi.html` via antarmuka pendaftaran petugas) mencetak nomor antrian berprefix ringkas (contoh: `C7` pada ruangan berprefix `C`), bukan nomor polos `7`.
3. Perilaku fallback pada ruangan tanpa prefix tetap menampilkan angka polos tanpa galat.

Referensi:
- IMPLEMENTATION-PLAN: `ANTRIAN-POLI-PREFIX-BUG-001-IMPLEMENTATION-PLAN.md` (Status: COMPLETED)
- BUG-INVESTIGATION: `ANTRIAN-POLI-PREFIX-BUG-001-BUG-INVESTIGATION.md`
- ISSUE: `ANTRIAN-POLI-PREFIX-BUG-001-ISSUE.md`
- Parent Test Package: `ANTRIAN-POLI-PREFIX-TEST-PACKAGE.md` (TC-01 Retest)

---

# 2. Prasyarat Lingkungan

| No | Prasyarat | Keterangan |
|---|---|---|
| P1 | Layanan `Bilreg.Api` berjalan dari worktree | Menjalankan kode terbaru dari worktree `b09-bilreg-api-worktree-antrian-poli-prefix` pada port `5189`. |
| P2 | Aplikasi web petugas `c012_myhospital_web` berjalan dari worktree | Menjalankan kode terbaru dari worktree `c012_myhospital_web-worktree-antrian-poli-prefix` (port `5173`). |
| P3 | Data Registrasi Uji | Registrasi pasien pada jadwal dokter dengan ruangan berprefix (misal: Poli Penyakit Dalam ruangan prefix `C`, contoh: `RG01378282` atau registrasi walk-in baru). |
| P4 | Token Akses / Akun Petugas | Akun petugas pendaftaran/admisi atau Bearer token untuk verifikasi API langsung. |

---

# 3. Kasus Uji

## TC-BUG-01 — Verifikasi Endpoint GET /api/reg/{id} Memuat queueLabel dan prefixAntrian

**Tujuan**: Memastikan query detail registrasi menyertakan properti label antrian berprefix.

**Prasyarat**: P1, P3, P4.

**Langkah**:
1. Lakukan pemanggilan HTTP GET ke endpoint detail registrasi:
   `GET http://localhost:5189/api/reg/RG01378282` (atau ID registrasi uji aktif).
2. Periksa struktur dan nilai payload respons JSON pada bagian `data`.

**Hasil yang Diharapkan**:
- Respons HTTP status 200 OK.
- Nilai `data.noAntrian` tetap bertipe angka: `7`.
- Properti aditif baru terisi:
  - `"queueLabel": "C7"` (kombinasi prefix ruangan `C` dan nomor urut `7`).
  - `"prefixAntrian": "C"`.

---

## TC-BUG-02 — Verifikasi Cetakan Struk Registrasi Menampilkan Format Berprefix (Format C7)

**Tujuan**: Memastikan nomor antrian pada struk pendaftaran tercetak dengan kode huruf ruangan.

**Prasyarat**: P1, P2, P3.

**Langkah**:
1. Buka aplikasi web petugas ([c012_myhospital_web](http://localhost:5173)), masuk ke menu **Admisi** > **Pendaftaran Rawat Jalan**.
2. Buka data registrasi pasien pada Poli Penyakit Dalam (ruangan prefix `C`).
3. Klik tombol cetak bukti pendaftaran (**Cetak Struk**) untuk memunculkan dialog/pratinjau cetak `antrian_registrasi.html`.
4. Amati tampilan nomor antrian pada struk.

**Hasil yang Diharapkan**:
- Nomor antrian tercetak berformat ringkas: **`C7`** (atau `C<noUrut>`).
- Tidak tercetak angka polos tanpa prefix (`7`).
- Tidak tercetak format 4 digit admisi (`C0007`).

---

## TC-BUG-03 — Verifikasi Fallback pada Ruangan Tanpa Prefix

**Tujuan**: Memastikan pendaftaran/registrasi pada ruangan tanpa prefix tidak menghasilkan prefix aneh atau error.

**Prasyarat**: Pasien terdaftar pada jadwal dokter di ruangan tanpa prefix (prefix kosong / `-`).

**Langkah**:
1. Panggil `GET /api/reg/{id}` untuk registrasi tersebut.
2. Lakukan cetak struk registrasi pada aplikasi web.

**Hasil yang Diharapkan**:
- Respons API memuat `noAntrian: <n>`, `queueLabel: "<n>"`, `prefixAntrian: ""`.
- Nomor antrian pada cetakan struk tercetak polos: `<n>` (misal: `1`, `2`) tanpa ada karakter asing.
