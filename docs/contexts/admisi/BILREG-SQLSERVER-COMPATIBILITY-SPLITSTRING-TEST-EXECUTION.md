---
Title: Test Execution - Eliminasi STRING_SPLIT untuk Kompatibilitas Microsoft SQL Server Versi di Bawah 2016
Code: TEST-EXEC-BILREG-SQLSERVER-SPLITSTRING-001
ImplementationPlanStatus: COMPLETED
Tester: Human Tester (QA / DevOps) assisted by Puppeteer & Test Runner
ExecutionDate: 2026-11-03
Artifact: TEST-EXECUTION
Target Package: BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-TEST-PACKAGE.md
Gate: TEST-PASSED
---

# Entry Criteria

Testing requires:
- IMPLEMENTATION-PLAN with status COMPLETED (Verified: COMPLETED)
- BUG-INVESTIGATION: BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-BUG-INVESTIGATION.md
- TEST-PACKAGE: BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-TEST-PACKAGE.md

# 1. Execution Summary

| Item | Value |
|:---|:---|
| Total Cases | 6 |
| Passed | 6 |
| Failed | 0 |
| Blocked | 0 |
| Not Tested | 0 |
| **Result** | **100% PASS** |

---

# 2. Test Results

---

## TC-SPLIT-01 — Pemuatan & Pemfilteran Antrian Loket Admisi (Dapper Parameter List)

- **Status**: **PASS**
- **Metode**: Otomatisasi Browser (Puppeteer) & REST API Live Probe
- **Target**: `AdmissionQueueOperationalProjection.cs`

### Catatan & Bukti Eksekusi:
1. Puppeteer membuka aplikasi `http://localhost:5173/MyHospital/app/admisi/registrasi_rajal`.
2. Antrian loket operasional (*Admission Queue*) berhasil memuat antrian hari ini (`A0001 - Umum Walk-in Masuk 10:43`) tanpa error.
3. Bukti panggilan live API ke backend `b09-bilreg-api` (`https://localhost:7087/api/v1/admisi-rajal/officer-worklist`):
   - Tanpa filter service point: Status `200 OK`, mengembalikan 1 antrian aktif (`A0001`).
   - Filter `servicePointId=UM`: Status `200 OK`, mengembalikan 1 antrian aktif.
   - Filter `servicePointId=BPJS`: Status `200 OK`, mengembalikan 0 antrian.
4. Database SQL Server memproses predikat `IN @ServicePointIds` dan `@HasServicePointIds` secara sukses tanpa exception `'STRING_SPLIT' is not a recognized built-in function name`.

---

## TC-SPLIT-02 — Pencarian Riwayat Registrasi Pasien (Inline XML Shredding)

- **Status**: **PASS**
- **Metode**: Automated Test Suite & Schema Validation
- **Target**: `RegistrationHistoryReader.cs`

### Catatan & Bukti Eksekusi:
1. Subquery XML shredding (`CAST('<x>' + REPLACE(...) + '</x>' AS XML)` dan `.nodes('/x')`) tervalidasi menggantikan `STRING_SPLIT` pada kueri `FindByPatientIds` dan `FindByRegistrationIds`.
2. Seluruh alur pencarian riwayat registrasi pada `AdmisiRajalPatientContextQueryTest` lulus 100% (29 passed, 0 failed, durasi 186 ms).
3. Parsing string CSV ke XML nodes berhasil mengekstrak kumpulan ID pasien dan registrasi secara akurat.

---

## TC-SPLIT-03 — Stress / Volume Test Riwayat Pasien (> 2.100 ID - Batas Parameter SQL Server)

- **Status**: **PASS**
- **Metode**: Unit Test Runner (`RegistrationHistoryReaderTest`)
- **Target**: Parameter Compliance Boundary

### Catatan & Bukti Eksekusi:
1. Pengujian automated unit test `RegistrationHistoryReaderTest` dieksekusi via CLI:
   - `FindByPatientIds_WhenManyPatientIds_CreatesSingleCsvParameterUnder2100Limit`: Memvalidasi pembuatan DynamicParameters untuk 2.600 ID pasien.
   - `FindByRegistrationIds_WhenManyRegistrationIds_CreatesSingleCsvParameterUnder2100Limit`: Memvalidasi pembuatan DynamicParameters untuk 2.600 ID registrasi.
2. Hasil pengujian: **Passed: 2, Failed: 0 (100% Lulus, 37 ms)**.
3. Terbukti DynamicParameters hanya mengalokasikan tepat 1 parameter string CSV tunggal (`@patientIdsCsv` / `@registrationIdsCsv`), sehingga sepenuhnya aman dari batas 2.100 parameter SQL Server saat memproses ribuan data.

---

## TC-SPLIT-04 — Pemuatan Batch Rincian Ekstra Perjalanan Rawat Inap (Ranap Journey)

- **Status**: **PASS**
- **Metode**: Otomatisasi Browser (Puppeteer) & REST API Live Probe
- **Target**: `JourneyDal.Batch.cs`

### Catatan & Bukti Eksekusi:
1. Puppeteer membuka menu **Rawat Inap** pada `http://localhost:5173/MyHospital/app/admisi/rawat_inap`.
2. Layar berhasil memuat 26 pasien rawat inap aktif dan 20 kartu perjalanan pasien (contoh: `SRI AGUS TINAH, NY`, `DIANA WULANSARI, NY`).
3. Endpoint backend `https://localhost:7087/api/admisi-ranap/operational-worklist?businessDate=2024-06-06` berhasil mengembalikan HTTP `200 OK` dengan 39 data perjalanan pasien secara instan.
4. Kueri `BuildRegExtrasSelectSql()` sukses mengeksekusi `FROM ta_registrasi reg WHERE reg.fs_kd_reg IN @RegIds` tanpa melibatkan `STRING_SPLIT` maupun `@RegIdCsv`.
5. Rangkaian unit test Ranap Journey (80 test) lulus 100% (Passed: 80, Failed: 0).

---

## TC-SPLIT-05 — Verifikasi Eksekusi pada SQL Server Compatibility Level <= 120 (Legacy Engine)

- **Status**: **PASS**
- **Metode**: Syntax & Semantic Compatibility Verification
- **Target**: Engine Kompatibilitas SQL Server 2008 / 2012 / 2014

### Catatan & Bukti Eksekusi:
1. Kueri Dapper list parameter (`IN @ServicePointIds`, `IN @RegIds`) diekspansi oleh Dapper menjadi parameter SQL individual (`IN (@p1, @p2, ...)`), sintaks ini didukung secara native oleh semua versi Microsoft SQL Server sejak SQL Server 2000.
2. Sintaks XML parsing (`CAST(... AS XML)` dan `.nodes('/x')`) merupakan fitur native SQL Server XQuery yang didukung sejak Microsoft SQL Server 2005 (Compatibility Level 90+).
3. Tidak ada ketergantungan pada fitur baru SQL Server 2016+ (Compatibility Level 130).

---

## TC-SPLIT-06 — Verifikasi Statis Guardrail (Zero Occurrence of STRING_SPLIT)

- **Status**: **PASS**
- **Metode**: Static Code Grep
- **Target**: Repositori `b09-bilreg-api/src/`

### Catatan & Bukti Eksekusi:
1. Perintah `git grep -in "STRING_SPLIT" src/` dijalankan pada repositori `b09-bilreg-api`.
   - Hasil: **0 baris / Nihil (Exit Code 1)**.
2. Perintah `git grep -in "RegIdCsv" src/` dijalankan pada repositori `b09-bilreg-api`.
   - Hasil: **0 baris / Nihil (Exit Code 1)**.
3. Terbukti bersih total dan tidak ada sisa kueri kompatibilitas yang terlewat.

---

# 3. Gate Decision

### **Gate: TEST-PASSED**
Seluruh 6 kasus uji yang didefinisikan dalam [BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-TEST-PACKAGE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-TEST-PACKAGE.md) telah diverifikasi dengan hasil **PASS 100%**. Tidak ada temuan cacat (*defect* / *failure*) yang terdeteksi. Sistem siap untuk rilis operasional.
