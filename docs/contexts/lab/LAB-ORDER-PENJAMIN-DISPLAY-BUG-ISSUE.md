# ISSUE

## Metadata

ID: ISSUE-LAB-ORDER-PENJAMIN-DISPLAY-BUG-001
Type: BUG
Status: RESOLVED
Title: Tampilan Penjamin / Cara Bayar Order Lab Ter-hardcode sebagai "BPJS Kesehatan" untuk Pasien Non-Walk-In

## Source

Reported By: User via Visual Inspection
Reported Date: 2026-11-07
Related Feature: OC-LAB-ORDER-LAB
Severity: MEDIUM

## Description

Pada panel rincian pesanan laboratorium (`LabOrderDetailPanel.vue` bagian Identitas Pasien), informasi **Penjamin / Cara Bayar** selalu menampilkan nilai literal `"BPJS Kesehatan"` untuk seluruh pasien rumah sakit (Rawat Jalan, Rawat Inap, dan IGD). Padahal pasien tersebut memiliki ragam penjamin/cara bayar yang berbeda di pendaftaran rumah sakit, seperti Umum/Tunai (Bayar Sendiri), Asuransi Swasta, maupun Perusahaan Kerjasama (IKS).

## Desired Outcome

Panel rincian pesanan laboratorium menampilkan Penjamin / Cara Bayar yang sebenarnya sesuai data penjamin pada registrasi aktif pasien (`ta_registrasi` $\rightarrow$ `ta_tipe_jaminan`). Untuk Pasien Luar Walk-In (`orderSource == 4`), sistem secara tepat menampilkan indikasi pembayaran mandiri/umum atau keterangan penjamin yang sah, bukan mengasumsikan seluruh pasien rumah sakit sebagai peserta BPJS Kesehatan.

## Current Situation

Pada komponen UI [`LabOrderDetailPanel.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/workspace/LabOrderDetailPanel.vue#L738-L742), tampilan penjamin dievaluasi menggunakan ekspresi:
```html
<span class="font-semibold text-purple-700">{{
  order.orderSource === 4 ? 'Umum / Tunai' : 'BPJS Kesehatan'
}}</span>
```
Karena seluruh order internal RS memiliki `orderSource` bernilai `1` (Rawat Jalan), `2` (Rawat Inap), atau `3` (IGD), maka semua pesanan internal selalu jatuh ke cabang fallback string literal `'BPJS Kesehatan'`. Selain itu, response DTO backend [`LabOrderGetResponse`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderGetQuery.cs#L15) dan schema frontend [`labOrderDetailSchema`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/types/lab-order.schema.ts#L78) belum memuat field nama penjamin dari registrasi pasien.

## Evidence

- **User Screenshot Evidence**: [uploaded_media_1794027809972.png](file:///C:/Users/We/.gemini/antigravity-cli/brain/71277c7f-4451-4526-bdf9-361ca0b0e3b1/.user_uploaded/uploaded_media_1794027809972.png) memperlihatkan pasien dengan No. RM 00-00-35-43 memiliki `Penjamin / Cara Bayar: BPJS Kesehatan`.
- **UI Source Code Reference**: [`LabOrderDetailPanel.vue:740`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/workspace/LabOrderDetailPanel.vue#L740).
- **Backend DTO Reference**: [`LabOrderGetQuery.cs:15-60`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderGetQuery.cs#L15-L60).

## Notes

Masalah ini merupakan defek presentasi dan integrasi kontrak query, yang tidak mengubah persistensi pesanan lab pada basis data fisik melainkan memerlukan pemenuhan resolusi penjamin pada endpoint detail order dan konsumsi di antarmuka frontend.
