# ISSUE

## Metadata

ID: ISSUE-LAB-TEST-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Penyelarasan Definisi Pemeriksaan Laboratorium dan Pemesanan terhadap Shared Tariff (Many-to-One) dan Keterangan Billing Spesifik (OC-LAB-TEST v1.1)

## Source

Reported By: User
Reported Date: 2026-10-16

## Description

Permintaan penyelarasan dan peningkatan kapabilitas Definisi Pemeriksaan Laboratorium (`LAB-TEST`) dan Pemesanan Pemeriksaan Laboratorium (`LAB-ORDER`) pada backend `b09-bilreg-api` berdasarkan pembaruan spesifikasi kanonikal [OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md) (Versi 1.1, Approved 2026-10-15).

Berdasarkan hasil asesmen keselarasan implementasi saat ini, ditemukan dua kesenjangan bisnis (*business gaps*) utama yang perlu diakomodasi:
1. **Dukungan Tarif Bersama (*Shared Tariff Many-to-One*)**: Spesifikasi bisnis [OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md) Versi 1.1 mengizinkan beberapa definisi pemeriksaan laboratorium aktif berbagi satu tarif tindakan resmi yang sama di Tata Rekening (`TRK-TARIF`) untuk pemeriksaan dengan kesetaraan kelas biaya administrasi. Namun, implementasi saat ini memaksakan relasi 1-to-1 (*single active definition per TarifId*) via invariant pengecekan konflik, dan proses intake pemesanan laboratorium hanya mengandalkan `TarifId`, sehingga sistem tidak dapat meresolusi tes mana yang dipesan secara deterministik pada skema tarif bersama.
2. **Keterangan Tagihan Spesifik Pemeriksaan (*Consolidated Billing Line Description*)**: Spesifikasi bisnis mensyaratkan saat pembebanan tagihan ke Tata Rekening (`TRK-BILLING`), sistem mencantumkan nama tarif resmi disertai keterangan nama spesifik pemeriksaan laboratorium yang dipesan. Saat ini pada pembentukan transaksi tagihan, kolom keterangan masih diisi string kosong `""`.

## Desired Outcome

Mengacu pada spesifikasi bisnis [OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md) Versi 1.1:

1. **Fleksibilitas Pemetaan Tarif Bersama (Many-to-One)**:
   - Sistem mengizinkan beberapa definisi pemeriksaan laboratorium aktif terikat pada tepat satu tarif tindakan resmi yang sah di Tata Rekening (`TRK-TARIF`), tanpa ditolak oleh validasi konflik tarif ganda (AC-03).
2. **Resolusi Pemesanan Laboratorium yang Deterministik**:
   - Alur penerimaan order laboratorium (`OC-LAB-ORDER-LAB`) mampu menerima identitas pemeriksaan spesifik (`TestDefinitionId`) secara eksplisit atau berpasangan dengan `TarifId`, sehingga sistem dapat menguraikan kebutuhan wadah flebotomi, jenis spesimen biologis, dan kerangka hasil analitik secara presisi (AC-05).
3. **Pencantuman Keterangan Nama Tes pada Rincian Tagihan**:
   - Pembebanan tagihan tindakan laboratorium ke Tata Rekening (`TRK-BILLING`) mencantumkan nama tarif resmi disertai keterangan nama spesifik pemeriksaan laboratorium yang dipesan (`labOrderItem.LabTestName`), dan nilai biaya diperhitungkan per pemeriksaan yang dipesan (§7 #1).
4. **Kekebalan Riwayat Transaksi (*Non-Retroactivity*)**:
   - Penyesuaian pemetaan tarif bersama tidak memengaruhi integritas data order dan lembar hasil yang sudah terbit sebelumnya (§7 #5).

## Current Situation

1. **Pencegahan Relasi Tarif Ganda pada Master Definisi Tes**:
   - Pada `Bilreg.Application.LabContext.LabTestDefinitionFeature.LabTestDefinitionSupport.EnsureNoActiveTarifConflict`, sistem melempar exception `LAB_TEST_DEFINITION_TARIF_CONFLICT: Another active LabTestDefinition already exists for this TarifId` jika ditemukan lebih dari satu definisi tes aktif yang mereferensikan `TarifId` yang sama.
2. **Ambiguitas Resolusi Tes saat Pemesanan Order**:
   - Pada `Bilreg.Application.LabContext.LabOrderFeature.LabOrderTarifItemInput`, model masukan baris pemeriksaan hanya memuat `TarifId` dan `TarifName` (tanpa `TestDefinitionId`).
   - Pada `Bilreg.Infrastructure.LabContext.LabOrderFeature.LabTestResolutionService.ResolveByTarifItems`, resolusi dilakukan via `_definitionRepo.LoadActiveByTarifId(input.TarifId)`. Jika satu tarif digunakan bersama oleh beberapa pemeriksaan aktif, metode ini tidak dapat menentukan definisi tes mana yang dimaksud oleh klinisi.
3. **Keterangan Nama Tes Kosong pada Tagihan Kasir**:
   - Pada `Bilreg.Domain.PaymentContext.TrsBillFeature.TrsBillFactory.CreateFromLabOrderItem`, pembentukan `TrsBillKetType` mengisi parameter keterangan dengan string kosong: `new TrsBillKetType(tarif.TarifName, "", tarif.TarifId, 1, "")`, sehingga nama spesifik pemeriksaan laboratorium tidak muncul pada rincian tagihan kasir/pasien.

## Evidence

1. Dokumen Outcome Kanonikal:
   - [OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md) (Versi 1.1, Approved 2026-10-15) - Bagian 5.1 #2, Bagian 7 #1 & #2, Bagian 8 EX-LTD-02, Bagian 9 AC-03 & AC-05.
   - [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md) (Versi 1.0) - Bagian 5.2.C.
2. Berkas Kode Sumber Terkait:
   - `src/bilreg/Bilreg.Application/LabContext/LabTestDefinitionFeature/LabTestDefinitionSupport.cs` (lines 36-49)
   - `src/bilreg/Bilreg.Infrastructure/LabContext/LabTestDefinitionFeature/LabTestDefinitionDal.cs` (lines 181-198)
   - `src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/LabOrderTarifItemInput.cs`
   - `src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabTestResolutionService.cs` (lines 25-45, 82-98)
   - `src/bilreg/Bilreg.Domain/PaymentContext/TrsBillFeature/TrsBillFactory.cs` (line 134)

## Notes

- Kategori perubahan: **CHANGE-REQUEST** (Penyelarasan kapabilitas terhadap evolusi model bisnis OC-LAB-TEST v1.1).
- Dokumen hilir berikutnya: **FEASIBILITY-ASSESSMENT** yang akan disusun oleh peran Analis (`ica-analyst`).
