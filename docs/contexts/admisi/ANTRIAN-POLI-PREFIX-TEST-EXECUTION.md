---
Title: Test Execution - Penerapan Prefix Ruangan pada Sesi Antrian Poli/Dokter, Display Antrian, dan Worklist Rawat Jalan
Code: ANTRIAN-POLI-PREFIX
Feature: FEAT-ANTRIAN-POLI-PREFIX
ImplementationPlanStatus: COMPLETED
Tester: Human Tester (Guided by ica-tester)
ExecutionDate: 2026-11-13
Artifact: TEST-EXECUTION
TestPackage: [ANTRIAN-POLI-PREFIX-TEST-PACKAGE.md](ANTRIAN-POLI-PREFIX-TEST-PACKAGE.md)
---

# Entry Criteria

Testing requires:
- IMPLEMENTATION-PLAN with status COMPLETED (Status: COMPLETED)
- FEATURE / ISSUE: `ANTRIAN-POLI-PREFIX-ISSUE.md`
- ARCHITECTURE: `ANTRIAN-POLI-PREFIX-ARCHITECTURE.md`
- TEST-PACKAGE: `ANTRIAN-POLI-PREFIX-TEST-PACKAGE.md`

---

# 1. Execution Summary

| Item | Value |
|---|---|
| Total Cases | 15 |
| Passed | 0 |
| Failed | 1 |
| Blocked | 0 |
| Not Tested | 14 |

---

# 2. Test Results

## TC-12 — Validasi: Prefix Tidak Valid
- **Status**: NOT TESTED
- **Notes**: -

## TC-01 — Pendaftaran Walk-In dengan Ruangan Berprefix 1 Huruf (Happy Path)
- **Status**: FAIL

### Actual Result
Pendaftaran poli penyakit dalam pada ruangan dengan prefix `C` menghasilkan nomor antrian tanpa prefix pada cetakan struk registrasi. Endpoint `GET /api/reg/RG01378282` hanya mengembalikan `"noAntrian": 7` tanpa properti `queueLabel` atau `prefixAntrian`.

### Expected Result
Nomor antrian pada bukti cetak/registrasi poli menampilkan label berprefix ringkas (contoh: `C7`) sesuai prefix ruangan jadwal dokter/poli tanpa leading zero.

### Evidence
- Screenshot cetakan: `Screenshot 2026-11-13 134635.png` (nomor tercetak polos tanpa prefix `C`).
- Response `GET http://localhost:5189/api/reg/RG01378282`:
  ```json
  {
    "status": "success",
    "code": "200",
    "data": {
      "regId": "RG01378282",
      "regDate": "2024-06-06",
      "layanan": {
        "layananId": "2RJ03",
        "layananName": "POLI PENY DALAM"
      },
      "noAntrian": 7,
      "sjpNo": "-"
    }
  }
  ```

---

## TC-03 — Pendaftaran Walk-In pada Ruangan Tanpa Prefix
- **Status**: NOT TESTED
- **Notes**: -

## TC-11 — Backfill Satu Kali pada Sesi yang Awalnya Kosong
- **Status**: NOT TESTED
- **Notes**: -

## TC-02 — Pendaftaran Walk-In dengan Ruangan Berprefix 2 Huruf
- **Status**: NOT TESTED
- **Notes**: -

## TC-04 — Reservasi (Booking) Mendapat Label Berprefix
- **Status**: NOT TESTED
- **Notes**: -

## TC-05 — Ubah Kunjungan ke Jadwal Berprefix Lain
- **Status**: NOT TESTED
- **Notes**: -

## TC-06 — Detail Antrian (Query) Menyertakan Label
- **Status**: NOT TESTED
- **Notes**: -

## TC-07 — Monitor Klinik: Tampilan dan Suara Pemanggilan
- **Status**: NOT TESTED
- **Notes**: -

## TC-08 — Struk Tiket Anjungan Mandiri (Kiosk)
- **Status**: NOT TESTED
- **Notes**: -

## TC-09 — Banner Pasien & Daftar Periksa Dokter (Web Petugas)
- **Status**: NOT TESTED
- **Notes**: -

## TC-10 — Aturan Bisnis: Snapshot Tidak Ditimpa (Immutability)
- **Status**: NOT TESTED
- **Notes**: -

## TC-13 — Regresi: Antrian Loket Admisi Tetap 4 Digit
- **Status**: NOT TESTED
- **Notes**: -

## TC-14 — Regresi: IGD Tanpa Prefix
- **Status**: NOT TESTED
- **Notes**: -

## TC-15 — Kompatibilitas API Lama (Zero Breaking Change)
- **Status**: NOT TESTED
- **Notes**: -

---

# 3. Defects

## DEF-001
- **Related Test Case**: TC-01
- **Severity**: MAJOR
- **Actual Result**:
  Cetakan bukti registrasi poli dan query detail registrasi `GET /api/reg/{id}` tidak menampilkan/memuat label antrian berprefix (`queueLabel`), sehingga nomor antrian yang tercetak tetap berupa angka polos (contoh: `7` bukan `C7`).
- **Expected Result**:
  Bukti registrasi dan cetakan struk antrian menampilkan format ringkas berprefix (`C7`), dan query detail registrasi `GET /api/reg/{id}` menyediakan properti label antrian berprefix.
- **Evidence**:
  - Screenshot: `Screenshot 2026-11-13 134635.png`
  - JSON response: `GET /api/reg/RG01378282` mengembalikan `"noAntrian": 7` tanpa `queueLabel` / `prefixAntrian`.
  - Investigasi teknis:
    1. Backend: `RegGetQuery.cs` / `RegGetResponse` di `Bilreg.Application.AdmisiContext.RegFeature.UseCases` belum memiliki properti aditif `QueueLabel` dan `PrefixAntrian` (hanya `NoAntrian: int`).
    2. Frontend: `LegacyRegistrationWorkspace.vue` pada `preparePrintItems()` masih memetakan `noAntrian: reg.noAntrian` tanpa memperhitungkan `queueLabel`.

---

# 4. Recommendations / Next Step

1. Hasil **FAIL** pada TC-01 dan temuan **DEF-001** telah dicatat dalam `TEST-EXECUTION`.
2. Sesuai tata kelola Knowledge-Centric SDLC:
   - Tester tidak memperbaiki kode secara langsung dan tidak membuat artefak `ISSUE` secara sepihak.
   - Temuan **DEF-001** diserahkan ke role **Issuer** (`ica-issuer` / `ics-issue-creation`) untuk dibuatkan formal `ISSUE` (tipe `BUG`), yang selanjutnya masuk ke tahap `BUG-INVESTIGATION` oleh **Analyst** (`ica-analyst`).
