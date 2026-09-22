---
Title: Pasien Digital Sign — MyHospital/Bilreg
Code: PDS-MH
Artifact: DOMAIN
Version: 1.0
LastUpdated: 2026-09-21
---

# 1. Business Overview

MyHospital (Bilreg BFF) mendukung alur tanda tangan digital pasien yang dilayani
oleh platform PenaEl atas nama HiDok. Domain ini memegang pengetahuan bisnis
tentang **permintaan tanda tangan digital (Digital Sign Request)** yang diajukan
oleh unit pelayanan untuk seorang pasien, pada satu registrasi (RegId), untuk
satu dokumen (DokumenId), beserta identitas penandatangan (Patient Signer) yang
sudah diprovisioning di PenaEl.

Cakupan resolusi identitas penandatangan telah terimplementasi dan didokumentasi
di `docs/contexts/pasien-digital-sign/pasien-digital-sign-resolve.md`. Dokumen
ini menambahkan pengetahuan tentang **catatan permanen permintaan tanda tangan
digital** yang harus disimpan MyHospital agar `signingRequestId` (diterbitkan
PenaEl saat `allocate`) dapat diambil kembali kapan pun untuk melanjutkan alur
(`upload`, `create`, dan pengambilan dokumen yang sudah ditandatangani).

Alur eksekusi PenaEl (`allocate → upload → create`), PIN, approve/reject, dan
webhook tetap dimiliki HiDok/PenaEl dan dirujuk dari dokumen HiDok
(`HiDokBackEnd/docs/contexts/pasien-digital-sign/`).

# 2. Ubiquitous Language

| Istilah | Definisi |
|---|---|
| **Digital Sign / Tanda Tangan Digital** | Penandatanganan elektronik dokumen pasien melalui platform PenaEl/HiDok. |
| **Digital Sign Request / Permintaan Tanda Tangan Digital** | Permohonan tanda tangan untuk satu pasien pada satu registrasi dan satu dokumen. |
| **SigningRequestId** | Identitas unik permintaan yang diterbitkan PenaEl saat alokasi (`allocate`); **tidak pernah di-generate oleh MyHospital**. |
| **Patient Signer** | Pasien yang terdaftar sebagai penandatangan di PenaEl (hasil resolve: `UserrId` + `SignerId`). |
| **PatientSignerId / SignerId** | Identitas penandatangan pasien di PenaEl; diresolusi dari NoMR via HiDok. |
| **RegId** | Kode registrasi MyHospital (`ta_registrasi.fs_kd_reg`) yang menjadi konteks layanan pasien. |
| **DokumenId** | Identitas dokumen MyHospital yang diminta untuk ditandatangani (mis. dokumen persetujuan tindakan). |
| **ExternalDocumentId** | Identitas dokumen dari perspektif sisi dokumen/eksternal (PenaEl/HIS) bila dipertukarkan. |
| **HisReference** | Referensi korelasi HIS yang dikirim bersama permintaan ke PenaEl. |
| **TglJamRequest** | Tanggal dan jam permintaan tanda tangan digital diajukan dan dicatat. |
| **Status Digital Sign** | Fase terkini permintaan: Requested, Approved, Rejected, Expired, Signed. |
| **UserrId** | Identitas Userr (pasien) di MyHospital; penghubung ke identitas PenaEl. |

# 3. Domain Capabilities

Domain capabilities adalah tanggung jawab layanan yang disediakan domain ini;
bukan user-facing feature.

- **Patient Signer Identity** — menyediakan identitas pasien sebagai penandatangan
  PenaEl (resolve `${HospitalId} + NoMR` → `UserrId + SignerId`), termasuk
  provision on-demand oleh HiDok.
- **Digital Sign Request Recording** — menyediakan pencatatan permanen setiap
  permintaan tanda tangan digital berikut konteksnya (SigningRequestId, RegId,
  DokumenId, TglJamRequest) dan data pendukungnya.
- **Digital Sign Request Inquiry** — menyediakan pengambilan kembali catatan
  permintaan berdasarkan `SigningRequestId` (dan konteks pendukung) untuk
  melanjutkan alur digital sign.

# 4. Actors & Roles

| Actor | Peran dalam domain |
|---|---|
| **Petugas Unit Pelayanan (Klinik/HIS Frontend)** | Mengajukan permintaan tanda tangan digital untuk pasien dan dokumen pada suatu registrasi. |
| **Sistem MyHospital (Bilreg BFF)** | Mencatat permintaan secara permanen dan menjaga konsistensi identitas penandatangan. |
| **Platform PenaEl / HiDok** | Menerbitkan `SigningRequestId`, memproses dokumen, dan melaporkan status permintaan (eksternal). |

# 5. Domain Objects

## DigitalSignRequestRecord

Catatan permanen permintaan tanda tangan digital di MyHospital.

| Field | Tipe | Wajib | Keterangan |
|---|---|---|---|
| `SigningRequestId` | identifier | Ya | Diterbitkan PenaEl saat `allocate`; unik di MyHospital. |
| `RegId` | identifier | Ya | Kode registrasi MyHospital konteks layanan (`ta_registrasi.fs_kd_reg`). |
| `DokumenId` | identifier | Ya | Dokumen MyHospital yang diminta ditandatangani. |
| `TglJamRequest` | datetime | Ya | Tanggal-jam permintaan diajukan/dicatat. |
| `NoMR` | string | Ya | Nomor rekam medis pasien (dasar resolve Patient Signer). |
| `UserrId` | identifier | Ya | Identitas Userr pasien di MyHospital (dari resolve). |
| `PatientSignerId` | identifier | Ya | Identitas penandatangan di PenaEl (dari resolve). |
| `HisReference` | string | Tidak | Referensi korelasi HIS (bila dikirim ke PenaEl). |
| `ExternalDocumentId` | string | Tidak | Identitas dokumen eksternal bila dipertukarkan. |
| `Status` | enum | Ya | Fase terkini permintaan (lihat §8). |
| `SignedAt` | datetime | Tidak | Saat permintaan dilaporkan Signed (bila tersedia). |
| `RequestedBy` | identifier | Tidak | Identitas petugas/Userr yang mengajukan permintaan. |
| `CreatedAt` | datetime audit | Ya | Saat catatan dibuat di MyHospital. |

Catatan: definisi kolom, tipe penyimpanan, dan indeks adalah keputusan
ARCHITECTURE/implementasi, bukan bagian dari DOMAIN.

## PatientSigner

Identitas pasien sebagai penandatangan di PenaEl (kepemilikan resolve — lihat
`pasien-digital-sign-resolve.md`): `UserrId`, `SignerId`, `NoMR`, `RSID`.

# 6. Aggregates

- **DigitalSignRequestRecord** adalah aggregate root untuk catatan permintaan.
  Satu `DigitalSignRequestRecord` mewakili satu permintaan (satu pasien + satu
  registrasi + satu dokumen) dan memegang siklus hidup `Status`-nya.

# 7. Business Rules

- **BR-1 — Sumber SigningRequestId**: `SigningRequestId` selalu diterima dari
  PenaEl (response `allocate`); MyHospital tidak pernah membuat nilai ini.
- **BR-2 — Keunikan**: `SigningRequestId` unik; catatan duplikat untuk nilai yang
  sama tidak diperbolehkan.
- **BR-3 — Konteks wajib**: Catatan hanya dapat dibuat bila memiliki `RegId`,
  `DokumenId`, dan `TglJamRequest` yang valid; ketiganya adalah konteks wajib
  permintaan.
- **BR-4 — Penandatangan wajib ada**: Catatan hanya dapat dibuat untuk pasien
  yang sudah ter-resolve sebagai Patient Signer (`PatientSignerId` terisi).
- **BR-5 — Satu permintaan per konteks**: Untuk satu kombinasi
  (`RegId`, `DokumenId`) hanya ada satu permintaan aktif pada satu waktu.
- **BR-6 — Pencatatan waktu**: `TglJamRequest` direkam oleh sistem pada saat
  pengajuan (bukan diisi bebas oleh pengguna).

# 8. State Machines & Lifecycles

## DigitalSignRequestRecord

```text
Requested ──▶ Approved ──▶ Signed
    │            │
    │            └──▶ Rejected
    └──▶ Expired
```

| Transisi | Asal → Tujuan | Syarat |
|---|---|---|
| Permintaan dicatat | — → Requested | BR-1 s.d. BR-6 terpenuhi. |
| Disetujui | Requested → Approved | Platform melaporkan persetujuan. |
| Ditolak | Approved → Rejected | Platform melaporkan penolakan. |
| Kedaluwarsa | Requested → Expired | Masa berlaku permintaan habis di platform. |
| Ditandatangani | Approved → Signed | Platform melaporkan dokumen ditandatangani. |

Sinkronisasi status aktual dari platform (via webhook/polling) adalah keputusan
bisnis yang masih terbuka (§10) dan tidak menentukan state machine di atas.

# 9. Domain Events

- **DigitalSignRequestRecorded** — catatan permintaan tanda tangan digital
  berhasil dibuat (membawa `SigningRequestId`, `RegId`, `DokumenId`,
  `TglJamRequest`, `PatientSignerId`).
- **DigitalSignRequestApproved** — permintaan dilaporkan disetujui.
- **DigitalSignRequestRejected** — permintaan dilaporkan ditolak.
- **DigitalSignRequestExpired** — permintaan dilaporkan kedaluwarsa.
- **DigitalSignRequestSigned** — permintaan dilaporkan ditandatangani.

# 10. Related Features

- **Digital Sign Request Record** (`pasien-digital-sign-request-record-feature.md`)
  — feature yang mengorkestrasi pencatatan permanen permintaan tanda tangan
  digital di MyHospital dan konsumsi domain ini.

Open questions yang memengaruhi DOMAIN ini:

- **OQ-1 (Sumber status)**: Apakah MyHospital akan menerima status permintaan
  (Approved/Rejected/Expired/Signed) melalui webhook PenaEl/HiDok, polling, atau
  tidak melacak status sama sekali (hanya catatan awal)? Keputusan ini mengubah
  kelengkapan state machine dan event.
- **OQ-2 (Arti DokumenId)**: Apakah `DokumenId` adalah identifier dokumen internal
  MyHospital (mis. persetujuan tindakan dari modul tertentu) dan bagaimana relasinya
  dengan `ExternalDocumentId` yang dipertukarkan ke PenaEl?
- **OQ-3 (Zona waktu)**: Zona waktu yang dipakai untuk `TglJamRequest` (UTC server
  vs WIB) dan format penyajiannya.