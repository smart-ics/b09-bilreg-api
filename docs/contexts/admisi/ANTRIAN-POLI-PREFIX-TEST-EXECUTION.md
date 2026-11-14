---
Title: Test Execution - Penerapan Prefix Ruangan pada Sesi Antrian Poli/Dokter, Display Antrian, dan Worklist Rawat Jalan
Code: ANTRIAN-POLI-PREFIX
Feature: FEAT-ANTRIAN-POLI-PREFIX
ImplementationPlanStatus: COMPLETED
Tester: Human Tester (Guided by ica-tester)
ExecutionDate: 2026-11-14
Artifact: TEST-EXECUTION
GateStatus: TEST PASSED
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
| Passed | 14 |
| Failed | 0 |
| Blocked | 1 |
| Gate Status | **TEST PASSED** |

---

# 2. Test Results

## TC-12 — Validasi: Prefix Tidak Valid
- **Status**: PASS
- **Notes**: Pengisian prefix yang melampaui kapasitas skema ditolak secara aman oleh layer database (`SQL Error [2628]`) tanpa merusak integritas data maupun kestabilan layanan.

## TC-01 — Pendaftaran Walk-In dengan Ruangan Berprefix 1 Huruf (Happy Path)
- **Status**: PASS
- **Notes**: Retest post BUG-001 & sanitasi DDL/data berhasil. Respons `GET /api/reg/RG01378282` terverifikasi mengembalikan `noAntrian: 7`, `queueLabel: "C7"`, dan `prefixAntrian: "C"`.

## TC-03 — Pendaftaran Walk-In pada Ruangan Tanpa Prefix
- **Status**: PASS
- **Notes**: Pendaftaran walk-in pada jadwal/ruangan tanpa prefix berhasil menghasilkan nomor urut polos tanpa huruf dan tanpa spasi (`queueLabel: "<n>"`, `prefixAntrian: ""`).

## TC-02 — Pendaftaran Walk-In dengan Ruangan Berprefix 2 Huruf
- **Status**: BLOCKED
- **Reason**: Kolom fisik pada tabel master warisan (*legacy*) `HiDok_Ruang.PrefixAntrian` berukuran `VARCHAR(1)`. Upaya pengisian prefix 2 huruf (misal: `'OB'`) ditolak oleh SQL Server dengan galat `SQL Error [2628]: String or binary data would be truncated`. Fitur prefix 2 huruf memerlukan *Change Request* terpisah untuk alter tabel master `HiDok_Ruang`.

## TC-11 — Backfill Satu Kali pada Sesi yang Awalnya Kosong
- **Status**: PASS
- **Notes**: Sesi antrian yang awalnya terbentuk tanpa ruangan/prefix secara otomatis mengadopsi prefix ruangan saat ruangan disetel, dan nomor pendaftaran selanjutnya langsung menghasilkan label berprefix (`C41`, `C42`).

## TC-04 — Reservasi (Booking) Mendapat Label Berprefix
- **Status**: PASS
- **Notes**: Reservasi (booking) baru pada sesi Poli Penyakit Dalam jam 08:00 (`01M7BZTRC31NFP26NZATXC8XJY`) terverifikasi menghasilkan nomor berprefix ringkas: `noAntrian: 42`, `queueLabel: "C42"`, dan `prefixAntrian: "C"`. Selain itu, uji reservasi pada sesi sore jam 16:00 (tanpa ruangan) secara tepat menghasilkan nomor polos `queueLabel: "1"`, `prefixAntrian: ""`.

## TC-05 — Ubah Kunjungan ke Jadwal Berprefix Lain
- **Status**: PASS
- **Notes**: Pemindahan kunjungan pasien `RG01378282` dari Poli Penyakit Dalam (prefix `'C'`) ke Poli Urologi (Dokter `11081`, jadwal tanpa prefix) berhasil menghasilkan nomor antrian baru tanpa menyisakan prefix lama: `noAntrian: 33`, `queueLabel: "33"`, dan `prefixAntrian: ""`.

## TC-06 — Detail Antrian (Query) Menyertakan Label
- **Status**: PASS
- **Notes**: Pemanggilan query antrian `GET /api/Antrian/{antrianId}` terverifikasi memuat properti `noAntrian` (angka), `queueLabel` (teks berprefix, mis. `"C7"`), dan `prefixAntrian` (`"C"`).

## TC-07 — Monitor Klinik: Tampilan dan Suara Pemanggilan
- **Status**: PASS
- **Notes**: Kartu antrian pada layar monitor poli (`display-web`) menampilkan format nomor antrian ringkas (`C7` / `C41`) dan suara audio pemanggilan melafalkan huruf prefix serta angka dengan jelas dan wajar.

## TC-08 — Struk Tiket Anjungan Mandiri (Kiosk)
- **Status**: PASS
- **Defect**: DEF-003 (Resolved in commit `6084c0c`)
- **Notes**: Retest pendaftaran mandiri di `kiosk-web` terverifikasi menampilkan nomor antrian berprefix ringkas (`C<n>`) pada layar sukses registrasi dan cetakan karcis anjungan mandiri.

## TC-09 — Banner Pasien & Daftar Periksa Dokter (Web Petugas)
- **Status**: PASS
- **Notes**: Modul Outpatient (daftar periksa dokter dan banner informasi pasien `PatientInfoBanner.vue`) terverifikasi menampilkan format nomor antrian ringkas berprefix (`C7` / `C41`) selaras dengan tiket fisik pasien.

## TC-10 — Aturan Bisnis: Snapshot Tidak Ditimpa (Immutability)
- **Status**: PASS
- **Notes**: Sesi antrian hari berjalan mengunci snapshot prefix (`TryUpdatePrefix` mengembalikan `false` jika `QueuePrefixSnapshot` sudah terisi). Integritas tiket pasien yang sudah terbit di hari berjalan terlindungi dari perubahan master jadwal dokter di tengah hari.

## TC-13 — Regresi: Antrian Loket Admisi Tetap 4 Digit
- **Status**: PASS
- **Notes**: Antrian loket admisi (front office) terverifikasi tetap mempertahankan format standar 4 digit (contoh: `A0001`), tidak terpengaruh oleh aturan pemformatan ringkas antrian poliklinik.

## TC-14 — Regresi: IGD Tanpa Prefix
- **Status**: PASS
- **Notes**: Pendaftaran rawat darurat (IGD) terverifikasi menghasilkan nomor antrian numerik polos tanpa prefix (`prefixAntrian: ""` dan `queueLabel: "<angka>"`), membuktikan alur IGD tidak terganggu oleh fitur prefix poli.

## TC-15 — Kompatibilitas API Lama (Zero Breaking Change)
- **Status**: PASS
- **Notes**: Seluruh endpoint pendaftaran, booking, ubah kunjungan, dan detail antrian terbukti tetap menyertakan properti warisan `noAntrian` bertipe angka murni tanpa perubahan tipe atau pemutusan kontrak data lama.

---

# 3. Defects

## DEF-001
- **Related Test Case**: TC-01
- **Severity**: MAJOR
- **Status**: RESOLVED
- **Resolution**: Kontrak aditif `QueueLabel` dan `PrefixAntrian` pada `RegGetResponse` serta pemetaan data cetak pada `LegacyRegistrationWorkspace.vue` telah diimplementasikan via `ANTRIAN-POLI-PREFIX-BUG-001`.

## DEF-002
- **Related Test Case**: TC-01 (Retest)
- **Severity**: MAJOR
- **Status**: RESOLVED
- **Resolution**: DDL kolom `BILRG_Antrian.QueuePrefixSnapshot` ditingkatkan menjadi `VARCHAR(5)` NOT NULL, constraint default dipasang ulang, spasi peninggalan `CHAR(1)` disanitasi, dan snapshot sesi Poli Penyakit Dalam terisi `'C'`.

## DEF-003
- **Related Test Case**: TC-08
- **Severity**: MAJOR
- **Status**: RESOLVED
- **Resolution**: Komponen `RegistrationSuccessStep.vue` dan `RegistrationReprintStep.vue` pada `kiosk-web` telah diperbaiki untuk mengikat `queueLabel || noAntrian`. Skema `registrationPrintDataSchema` dan `regGetResponseSchema` juga telah ditambahkan properti aditif `queueLabel` (commit `6084c0c`). Retest diverifikasi berhasil oleh tester.

---

# 4. Final Verdict

Gate **TEST PASSED** diberikan untuk fitur `FEAT-ANTRIAN-POLI-PREFIX`.
Seluruh 14 skenario fungsional dan regresi dinyatakan **PASS**. Satu skenario (TC-02) berstatus **BLOCKED** karena limitasi kolom tabel master warisan `HiDok_Ruang.PrefixAntrian` (`VARCHAR(1)`), yang akan diajukan melalui *Change Request* pemeliharaan skema DDL terpisah jika rumah sakit memerlukan prefix 2 huruf di masa mendatang.
