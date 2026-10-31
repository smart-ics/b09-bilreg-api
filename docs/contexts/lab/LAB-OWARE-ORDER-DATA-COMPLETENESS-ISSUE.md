# ISSUE

## Metadata

ID: LAB-OWARE-ORDER-DATA-COMPLETENESS-ISSUE
Type: CHANGE-REQUEST
Status: OPEN
Title: Pengayaan Kelengkapan Data Order Laboratorium dari Bilreg untuk Kebutuhan Integrasi LIS

## Source

Reported By: User
Reported Date: 2026-10-30

## Description

Saat ini order laboratorium yang dikirim dari Bilreg berhasil diterima dan disimpan di OWARE. Namun, beberapa data kelengkapan order yang dibutuhkan oleh sistem LIS (Laboratory Information System) eksternal belum tersedia pada payload respons `GET api/LabContext/LabOrderFeature/{orderId}` dari Bilreg.

Data yang dibutuhkan LIS namun belum tersedia pada payload respons antara lain mencakup:
1. Data Registrasi & Kunjungan: Klasifikasi jenis rawat/kunjungan (Rawat Jalan / Rawat Inap / IGD), jenis jaminan/asuransi, dan nomor kepesertaan jaminan.
2. Data Demografi Pasien: NIK / nomor identitas, alamat lengkap, tempat lahir, dan nomor telepon/HP pasien.
3. Data Ruang Perawatan (untuk pasien Rawat Inap): Nomor/nama bed, kamar, dan ruangan/bangsal perawatan.

Ketiadaan data ini menyebabkan middleware OWARE tidak dapat meneruskan atribut-atribut demografis dan administratif pasien secara lengkap ke vendor-vendor LIS (seperti Sysmex, HCLAB, Softmedix, Prolims, Wynacom, dll.).

## Desired Outcome

1. Payload respons detail order lab dari Bilreg (`GET api/LabContext/LabOrderFeature/{orderId}`) memuat data kelengkapan pasien, registrasi/penjamin, dan ruang rawat sesuai yang dibutuhkan LIS.
2. Builder order di OWARE (`OrderLabBilregBuilder`) dapat memetakan data tersebut ke atribut `OrderLabModel` (`JenisInap`, `Nik`, `Address`, `PhoneNo`, `BirthPlace`, `TipeJaminanName`, `PesertaJaminanId`, `BedName`, `KamarName`, `BangsalName`).
3. Adapter LIS di OWARE dapat mengirimkan data pasien dan order laboratorium ke sistem LIS pihak ketiga secara akurat dan lengkap.

## Current Situation

1. Endpoint Bilreg `GET api/LabContext/LabOrderFeature/{orderId}` mengembalikan `LabOrderGetResponse` yang saat ini baru memuat informasi order dasar, dokter/layanan perujuk, catatan klinis, daftar pemeriksaan, serta data pasien terbatas (`PatientId`, `PatientName`, `BirthDate`, `Gender`, `AgeAtOrder`).
2. Atribut seperti `Nik`, `Alamat`, `PhoneNo`, `BirthPlace`, `JenisInap` / jenis kunjungan, data jaminan, serta informasi bed/kamar/bangsal belum disertakan dalam payload respons Bilreg.
3. Pada OWARE (`OrderLabBilregBuilder.cs`), atribut-atribut terkait registrasi (`JenisInap`, `TipeJaminanId`), pasien (`Nik`, `Address`, `PhoneNo`, `BirthPlace`), dan rawat inap (`BedId`, `BedName`, `KamarName`, `BangsalName`) saat ini masih diisi dengan string kosong (`string.Empty`).
4. Dampaknya, pengiriman ke mesin/sistem LIS eksternal menggunakan nilai fallback (misal jenis pasien default "OP" / rawat jalan, NIK default "-", alamat kosong, dan bed default "0000").

## Evidence

- Kode pemetaan OWARE: [OrderLabBilregBuilder.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Workers/Creators/OrderLabBilregBuilder.cs#L60-L88)
- DTO penerima di OWARE: [BilregLabOrderResponseDto.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Contracts/DTOs/BilregLabOrderResponseDto.cs#L1-L49)
- Endpoint dan Handler di Bilreg: [LabOrderGetQuery.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderGetQuery.cs#L15-L59) dan [LabOrderController.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/LabContext/LabOrderFeature/LabOrderController.cs#L77-L82)
- Implementasi adapter LIS di OWARE yang mengonsumsi data tersebut: [OrderLabTarifSysmexService.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/ServicesLabSysmex/OrderLabTarifSysmexService.cs#L76-L125), [OrderLabTarifHclabService.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/ServicesLabHclab/OrderLabTarifHclabService.cs#L79-L125), [OrderLabTarifSoftmedixService.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/ServicesLabSoftmedix/OrderLabTarifSoftmedixService.cs#L57-L100)

## Notes

- Kebutuhan ini merupakan CHANGE-REQUEST lintas-service antara `b09-bilreg-api` (penyedia data order) dan `a046_oware_ordermiddlewareapi` (middleware jembatan ke LIS).
- Analisis ketersediaan field di domain/repository Bilreg (`RegModel`, `PasienModel`, `BedUsageContext`), strategi penambahan field pada DTO (backward-compatibility), dan pemetaan spesifik per vendor LIS akan dianalisis lebih lanjut pada tahap Discovery & Feasibility Assessment oleh role `ica-analyst`.
