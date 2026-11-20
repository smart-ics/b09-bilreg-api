# ISSUE

## Metadata

ID: ISSUE-LAB-ORDER-PATIENT-BIRTHDATE-BUG-001
Type: BUG
Status: OPEN
Title: Ketidaksesuaian Tanggal Lahir pada Panel Identitas Pasien Layar Laboratorium Setelah Simpan Transaksi Order Lab

## Source

Reported By: User via Issue Intake
Reported Date: 2026-11-19
Related Feature: OC-LAB-ORDER-LAB
Severity: MEDIUM

## Description

Pada layar laboratorium (modul Laboratory), setelah pengguna memilih pasien, menginput tindakan LAB, dan menyimpan transaksi order lab, isian **Tanggal Lahir** pada panel **Identitas Pasien** (`LabOrderDetailPanel.vue`) tidak sesuai dengan tanggal lahir sebenarnya yang tercatat pada data sosial pasien (`PAS-DATSOS`).

### Langkah Reproduksi (Steps to Reproduce):
1. Buka antarmuka pembuatan order laboratorium internal.
2. Cari dan pilih salah satu pasien kunjungan aktif.
3. Input tindakan pemeriksaan laboratorium (tambah item tes lab).
4. Klik tombol simpan transaksi (*"Simpan & Terbitkan Order Lab"*).
5. Periksa isian Tanggal Lahir pada kartu/panel Identitas Pasien di detail order laboratorium yang terbentuk.

## Desired Outcome

Tanggal lahir yang ditampilkan pada kartu/panel Identitas Pasien di layar laboratorium konsisten dan akurat sesuai data tanggal lahir sebenarnya dari data sosial pasien (`PAS-DATSOS` / master pasien) yang terdaftar pada kunjungan aktif terkait.

## Current Situation

Pada saat transaksi order laboratorium disimpan melalui kanvas pemesanan (`LabOrderCreateCanvas.vue`), nilai tanggal lahir pasien tidak terpetakan dari tanggal lahir data sosial pasien, melainkan jatuh pada fallback tanggal registrasi kunjungan (`regDate`) atau nilai default (`1990-01-01`). Akibatnya, data order yang tersimpan di sistem dan ditampilkan pada panel Identitas Pasien (`LabOrderDetailPanel.vue`) memuat tanggal lahir yang keliru (tidak sesuai tanggal lahir sebenarnya pasien).

## Evidence

- **User Bug Report**:
  - Langkah Reproduksi:
    1. Pilih Pasien
    2. Input tindakan LAB
    3. Save transaksi
    4. Cek isian Identitas Pasien
  - Expected Result: Tanggal lahir sesuai isian data sosial
  - Actual Result: Tanggal lahir tidak sesuai isian data sosial
- **UI Source Code Reference**:
  - [`LabOrderCreateCanvas.vue:372-379`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue#L372-L379): Nilai `birthDate` dan `birthDateYmd` pada payload pembuatan order internal jatuh pada fallback `selectedPatient.value.regDate` atau `'1990-01-01'`:
    ```typescript
    birthDate:
      ((selectedPatient.value as unknown as Record<string, unknown>).birthDate as string) ||
      selectedPatient.value.regDate ||
      '1990-01-01',
    birthDateYmd:
      ((selectedPatient.value as unknown as Record<string, unknown>).birthDate as string) ||
      selectedPatient.value.regDate ||
      '1990-01-01',
    ```
  - [`useLabMasterLookup.ts:36-55`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/composables/useLabMasterLookup.ts#L36-L55): Skema lookup `labMasterRegItemSchema` dari endpoint registrasi aktif hanya memetakan `regDate` dan belum memuat field tanggal lahir pasien.
  - [`LabOrderDetailPanel.vue:250-253, 961-964`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/workspace/LabOrderDetailPanel.vue#L250-L253): Panel Identitas Pasien menampilkan `props.order.birthDate` yang telah tersimpan dari payload tersebut.
- **Backend Reference**:
  - [`RegRegAktifListQuery.cs:7-9`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegRegAktifListQuery.cs#L7-L9): DTO `RegRegAktifListResponse` belum menyertakan informasi tanggal lahir pasien dari entitas registrasi aktif.
  - [`LabOrderCreateFromEmrCmd.cs:154-160`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs#L154-L160): Pembuatan snapshot order laboratorium menerima `request.BirthDateYmd` dari payload klien tanpa merekonsiliasi dengan data tanggal lahir pasien pada entitas registrasi (`reg.Pasien.TglLahir`).

## Notes

Masalah ini tergolong cacat integrasi data identitas pasien antara alur registrasi/kunjungan aktif dan formulir pemesanan laboratorium internal. Sesuai batasan peran Issue Intake (`ica-issuer`), artefak ini bersifat solusi-netral dan tidak mendefinisikan perubahan arsitektur atau implementasi langsung. Dokumen ini siap diteruskan ke tahap Bug Investigation.
