# ISSUE

## Metadata

ID: ISSUE-WAITING-LIST-BED-USAGE-CONSOLIDATION-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Penyatuan Fitur WaitingList ke BedUsageContext dan Migrasi Penggunaan dari Admisi Ranap

## Source

Reported By: User
Reported Date: 2026-11-09

## Description

Saat ini di backend (`b09-bilreg-api`) terdapat duplikasi pengelolaan antrean rawat inap (*waiting list*) di dua bounded context yang berbeda:
1. `AdmisiRanapContext.WaitingListFeature` (diakses melalui rute `/api/admisi-ranap/waiting-list`).
2. `BedUsageContext.WaitingListFeature` (diakses melalui rute `/api/bed-usage/waiting-list`).

Kondisi saat ini:
- Modul frontend Admisi Ranap (`c012_myhospital_web`) masih sepenuhnya bergantung pada rute dan kontrak data `AdmisiRanapContext.WaitingListFeature`.
- Modul frontend Rawat Inap / Cockpit Bangsal (`wardCockpitApi.ts`) telah menggunakan rute `BedUsageContext.WaitingListFeature`.
- Di backend, controller `api/admisi-ranap/waiting-list` sebagian hanya bertindak sebagai adapter delegasi ke `BedUsageContext`, namun logika pembuatan, perbaruan, query data, dan orchestrator registrasi admisi masih memegang model serta repository internal `AdmisiRanapContext`.

Pengguna meminta agar arsitektur Waiting List di backend disatukan menjadi satu-satunya context di bawah `BedUsageContext`, seluruh implementasi Waiting List di `AdmisiRanapContext` dihilangkan (dihapus), dan seluruh pemanggilan dari frontend Admisi Ranap dialihkan ke endpoint `api/bed-usage/waiting-list` dengan jaminan tidak ada regresi atau error pada alur Admisi Ranap yang sudah berjalan.

## Desired Outcome

1. Backend `b09-bilreg-api` hanya memiliki satu bounded context yang mengelola Waiting List, yaitu `BedUsageContext.WaitingListFeature`.
2. Seluruh artefak kode Waiting List di dalam `AdmisiRanapContext` (Domain Model, Application UseCases/Repo Interface, Infrastructure DAL/Repo/DTO, dan Unit Tests) dibersihkan / dihapus setelah migrasi selesai.
3. Seluruh alur kerja pada modul frontend Admisi Ranap (`c012_myhospital_web`) yang membutuhkan Waiting List (pembuatan waiting list saat admisi, perbaruan prioritas/kamar, monitoring worklist daftar tunggu, dan penanganan penolakan/batal) dapat beroperasi secara normal tanpa error melalui endpoint `api/bed-usage/waiting-list`.
4. Alur operasional registrasi admisi rawat inap (termasuk otomasi registrasi dari reservasi/opname request, orkestrasi pembatalan admisi, dan pemantauan timeline journey pasien) tetap berjalan lancar dan terintegrasi mulus dengan context `BedUsageContext`.
5. Fungsionalitas rawat inap / cockpit bangsal yang sudah menggunakan `BedUsageContext.WaitingListFeature` tidak mengalami gangguan (zero-regression).

## Current Situation

1. Di backend `b09-bilreg-api`:
   - Terdapat dua pasang Domain Model, UseCase, DAL, DTO, dan Repository untuk Waiting List: satu di `AdmisiRanapContext` dan satu di `BedUsageContext`.
   - Endpoint `GET /api/bed-usage/waiting-list/by-reg/{regId}` saat ini mengembalikan list/array (`IEnumerable<WaitingListGetResponse>`), sedangkan frontend Admisi Ranap mengharapkan object tunggal atau null (`WaitingListDetail | null`).
   - Endpoint pencarian/list antrean dengan filter bangsal dan status (`GET /api/bed-usage/waiting-list`) belum tersedia di controller BedUsage, melainkan baru ada endpoint `worklist` khusus per bangsal aktif.
   - Pendaftaran rawat inap otomatis (`AdmissionRegistrationOrchestrator`) dan pembatalan terkoordinasi (`AdmCoordinatedCancelCmd`) masih mengikat dependensi ke `AdmisiRanapContext.WaitingListFeature.IWaitingListRepo`.
2. Di frontend `c012_myhospital_web`:
   - File service [`AdmisiRanapService.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/queries/AdmisiRanapService.ts) mengarahkan seluruh query dan mutasi waiting list ke string endpoint `admisi-ranap/waiting-list`.
   - Schema validasi zod di [`admisiRanap.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/types/admisiRanap.ts) (`createWaitingListInputSchema`, `waitingListDetailSchema`, `waitingListListResponseSchema`) dikonfigurasi mengikuti struktur data dari `AdmisiRanapContext`.

## Evidence

1. **Definisi Route Endpoint Frontend**:
   - File: [`c012_myhospital_web/src/modules/Admisi/queries/AdmisiRanapService.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/queries/AdmisiRanapService.ts) baris 115:
     `WAITING_LIST: 'admisi-ranap/waiting-list'`
2. **Backward-Compatible Adapter di Backend**:
   - File: [`b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/AdmisiRanapContext/WaitingListController.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/AdmisiRanapContext/WaitingListController.cs) baris 11-15:
     Tercatat dokumentasi bahwa rute `/api/admisi-ranap/waiting-list` adalah *backward-compatible adapter* dan pemanggil diarahkan menggunakan `/api/bed-usage/waiting-list`.
3. **Ketergantungan Orchestrator Admisi**:
   - File: [`b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs) baris 28-29:
     Mengimpor `Bilreg.Application.AdmisiRanapContext.WaitingListFeature` dan `Bilreg.Domain.AdmisiRanapContext.WaitingListFeature`.

## Notes

- Permintaan diklasifikasikan sebagai **CHANGE-REQUEST**.
- Analisis kelayakan pemenuhan kontrak, penyesuaian payload, dan rencana mitigasi regresi akan dirumuskan pada tahap FEASIBILITY-ASSESSMENT dan ARCHITECTURE UPDATE berikutnya oleh peran Analyst / Architect.
