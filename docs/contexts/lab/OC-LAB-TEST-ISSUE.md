# ISSUE

## Metadata

ID: ISSUE-LAB-TEST-001
Type: CHANGE-REQUEST
Status: CLOSED
Title: Penyelarasan Definisi Pemeriksaan Laboratorium dan Pemesanan terhadap Kardinalitas Tarif 1:1 (OC-LAB-TEST v1.2)

## Source

Reported By: User / Product Owner
Reported Date: 2026-10-16

## Description

Permintaan peninjauan keselarasan kapabilitas Definisi Pemeriksaan Laboratorium (`LAB-TEST`) dan Pemesanan Pemeriksaan Laboratorium (`LAB-ORDER`) pada backend `b09-bilreg-api` terhadap spesifikasi kanonikal [OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md).

Sebelumnya diidentifikasi kesenjangan terkait skema tarif bersama (*shared tariff Many-to-One*) pada dokumen outcome Versi 1.1. Namun, melalui keputusan resmi Product Owner pada 2026-10-16, dukungan tarif bersama ditiadakan dan aturan bisnis dikembalikan ke model relasi ketat 1:1 (*One-to-One*).

## Desired Outcome

Mengacu pada spesifikasi bisnis [OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md) Versi 1.2:

1. **Pemetaan Tarif Relasi 1:1 yang Konsisten**:
   - Satu `TarifId` aktif hanya boleh terikat pada tepat satu `LabTestDefinition` yang aktif.
   - Pendaftaran atau pengaktifan tes baru yang menggunakan `TarifId` yang sudah terikat pada tes aktif lain wajib ditolak (konflik 1:1).
2. **Resolusi Pemesanan Laboratorium yang Deterministik**:
   - Alur penerimaan order laboratorium (`OC-LAB-ORDER-LAB`) meresolusi `TarifId` menjadi spesifikasi tes, kebutuhan wadah flebotomi, dan kerangka hasil secara deterministik.
3. **Kekebalan Riwayat Transaksi (*Non-Retroactivity*)**:
   - Snapshot data tes dan komponen analitik yang tersimpan saat order dibuat tidak terpengaruh oleh perubahan master di masa depan.

## Current Situation

1. Sistem eksisting di `b09-bilreg-api` telah menerapkan:
   - Invariant pengecekan konflik 1:1 (`EnsureNoActiveTarifConflict`) pada `LabTestDefinitionSupport`.
   - Resolusi deterministik berbasis `TarifId` pada `LabTestResolutionService`.
   - Snapshotting data pra-analitik dan komponen pada `LabOrderItemModel` dan `LabOrderItemComponentModel`.
2. Seluruh 156 unit test lulus pengujian 100%.

## Resolution & Notes

- **Keputusan Product Owner (2026-10-16)**: Dukungan 1 tarif untuk banyak definisi tes (*shared tariff*) resmi dihilangkan dan model relasi dikembalikan ke 1:1.
- **Dampak Implementasi**: Seluruh basis kode eksisting di `b09-bilreg-api` sudah selaras sempurna dengan aturan 1:1. Tidak ada perubahan kode (*zero code changes*).
- **Pembaruan Artefak**:
  - [OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md) diperbarui ke Versi 1.2.
  - [OC-LAB-TEST-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-TEST-FEATURE.md) diperbarui ke Versi 1.2.
  - [OC-LAB-TEST-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-TEST-FEASIBILITY-ASSESSMENT.md) Versi 1.1 menutup seluruh gap (`GAP-001` s/d `GAP-004` CLOSED) dengan kesimpulan `ARCHITECTURE-NOT-REQUIRED`.
- Status issue ini: **CLOSED**.
