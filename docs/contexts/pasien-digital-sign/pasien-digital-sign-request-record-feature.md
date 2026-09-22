---
Title: Digital Sign Request Record — Pencatatan Permintaan Tanda Tangan Digital
Code: PDS-MH-F01
Artifact: FEATURE
Version: 1.0
LastUpdated: 2026-09-21
---

# 1. Purpose

MyHospital (Bilreg BFF) harus menyimpan **catatan permanen** setiap permintaan
tanda tangan digital pasien, sehingga `signingRequestId` yang diterbitkan PenaEl
saat `allocate` tidak hilang dan dapat diambil kembali kapan pun. Tanpa catatan
ini, unit pelayanan tidak dapat melanjutkan alur (`upload`, `create`) maupun
mengambil dokumen yang sudah ditandatangani karena `signingRequestId` hanya
diketahui pada saat response `allocate`.

Feature ini melengkapi capability **resolve** yang sudah berjalan
(`pasien-digital-sign-resolve.md`) dengan **pencatatan request** di sisi
MyHospital.

# 2. Business Outcome

Setiap permintaan tanda tangan digital pasien yang diajukan unit pelayanan
tercatat permanen di MyHospital bersama konteksnya (RegId, DokumenId,
TglJamRequest) dan data pendukung digital sign. Unit pelayanan dapat mengambil
kembali `signingRequestId` kapan pun untuk melanjutkan alur atau mengambil
dokumen yang sudah ditandatangani, tanpa harus menyimpan ulang id di sisi
frontend.

# 3. Participating Domains

| Domain | Responsibility dalam feature ini |
|---|---|
| **Pasien Digital Sign (PDS-MH)** | Mencatat `DigitalSignRequestRecord`, menjaga keunikan `SigningRequestId`, dan memelihara siklus hidup status permintaan. |
| **Pasien (PasienContext)** | Menyediakan identitas pasien (`NoMR`, `UserrId`) sebagai acuan resolve Patient Signer. |
| **Admisi / Registrasi** | Menyediakan `RegId` (`ta_registrasi.fs_kd_reg`) sebagai konteks layanan pasien. |
| **Dokumen (domain pemilik dokumen, referensi)** | Menyediakan `DokumenId` untuk dokumen yang diminta ditandatangani. |

Catatan: perilaku domain lain dirujuk, tidak didefinisikan ulang di sini.

# 4. Trigger

Unit pelayanan telah menyelesaikan alur alokasi PenaEl dan memperoleh
`signingRequestId` (alur `allocate → upload → create`). MyHospital menerima fakta
tersebut dan diminta mencatatnya secara permanen bersama konteks permintaan.

Asumsi (perlu konfirmasi — lihat OQ-4): unit pelayanan/frontend menyerahkan
`signingRequestId` berikut konteksnya ke BFF MyHospital; BFF sendiri tidak
memanggil `allocate`.

# 5. Preconditions

- Pasien sudah ter-resolve sebagai Patient Signer (`PatientSignerId` tersedia) —
  lihat capability `Patient Signer Identity`.
- `SigningRequestId` valid dan diterima dari PenaEl (tidak pernah dibuat oleh
  MyHospital).
- Konteks wajib tersedia: `RegId`, `DokumenId`, `TglJamRequest`.
- `RegId` dan `DokumenId` dikenal oleh MyHospital.

# 6. Operational Flow

1. **Pengajuan** — Petugas unit pelayanan mengajukan tanda tangan digital untuk
   seorang pasien pada satu registrasi dan satu dokumen.
2. **Resolve penandatangan** — MyHospital memperoleh `PatientSignerId` melalui
   capability resolve (identitas pasien sudah tersedia).
3. **Alokasi di PenaEl** — Permintaan dialokasikan di PenaEl dan
   `SigningRequestId` diterbitkan (alur PenaEl, di luar repo ini).
4. **Pencatatan** — MyHospital mencatat `DigitalSignRequestRecord` dengan
   `SigningRequestId` serta konteks wajib dan data pendukung (domain PDS-MH,
   aturan BR-1 s.d. BR-6).
5. **Konfirmasi ke unit pelayanan** — Unit pelayanan menerima konfirmasi bahwa
   permintaan tercatat dan `signingRequestId` dapat dipakai untuk melanjutkan
   alur PenaEl.
6. **(Opsional, menunggu OQ-1)** — Status permintaan diperbarui oleh MyHospital
   ketika platform melaporkan Approved/Rejected/Expired/Signed.

# 7. Domain Orchestration

- **PDS-MH** memegang tanggung jawab pencatatan: menentukan apakah catatan baru
  valid (keunikan `SigningRequestId`, satu permintaan aktif per kombinasi
  `RegId + DokumenId`) dan memelihara status permintaan.
- **PasienContext** menyediakan `NoMR`/`UserrId`; **Admisi/Registrasi**
  menyediakan `RegId`; **domain dokumen** menyediakan `DokumenId`. Ketiganya
  adalah sumber referensi yang divalidasi sebelum catatan dibuat.
- Koordinasi hanya memastikan data konteks diterima dan diverifikasi; seluruh
  aturan domain dijalankan oleh masing-masing domain pemilik.

# 8. Constraints

- `SigningRequestId` tidak pernah di-generate oleh MyHospital (BR-1).
- Catatan duplikat untuk `SigningRequestId` yang sama ditolak (BR-2).
- Catatan wajib memiliki `RegId`, `DokumenId`, dan `TglJamRequest` (BR-3).
- Catatan hanya dibuat untuk pasien yang sudah ter-resolve (BR-4).
- Hanya satu permintaan aktif per kombinasi `(RegId, DokumenId)` pada satu waktu
  (BR-5).
- `TglJamRequest` direkam sistem, bukan input bebas pengguna (BR-6).

# 9. Exceptions

| Skenario | Hasil yang diharapkan |
|---|---|
| `SigningRequestId` duplikat | Pencatatan ditolak; unit pelayanan diberi tahu bahwa permintaan telah tercatat. |
| Pasien belum ter-resolve | Pencatatan ditolak sampai pasien di-resolve sebagai Patient Signer. |
| `RegId` tidak dikenal | Pencatatan ditolak dengan alasan konteks registrasi tidak valid. |
| `DokumenId` tidak dikenal | Pencatatan ditolak dengan alasan dokumen tidak valid. |
| Konteks wajib tidak lengkap | Pencatatan ditolak; field wajib diidentifikasi. |
| Laporan status platform belum tersedia | Catatan tetap tersimpan pada status Requested; pembaruan status ditunda. |

# 10. Acceptance Criteria

1. Setiap permintaan yang diajukan tersimpan sebagai `DigitalSignRequestRecord` dengan `SigningRequestId`, `RegId`, `DokumenId`, dan `TglJamRequest`.
2. Field pendukung tercatat sesuai definisi domain PDS-MH (`NoMR`, `UserrId`, `PatientSignerId`, dan field opsional bila tersedia).
3. `SigningRequestId` yang sama tidak dapat dicatat dua kali.
4. Hanya satu permintaan aktif per kombinasi `(RegId, DokumenId)` pada satu waktu; permintaan baru untuk kombinasi yang sama ditolak selama permintaan aktif belum selesai.
5. `SigningRequestId` dapat diambil kembali melalui capability inquiry kapan pun (berdasarkan konteks yang tersedia).
6. Pengajuan dengan pasien belum ter-resolve ditolak dengan pesan yang jelas.
7. Pengajuan dengan `RegId` atau `DokumenId` tidak dikenal ditolak dengan pesan yang jelas.
8. `TglJamRequest` direkam oleh sistem pada saat pengajuan, bukan input pengguna.

# 11. Open Questions

- **OQ-4 (Pemanggil allocate)**: Apakah BFF MyHospital yang memanggil `allocate`
  PenaEl, atau frontend yang memanggil lalu menyerahkan `signingRequestId` ke BFF
  untuk dicatat? Berpengaruh pada trigger dan langkah 3–4 alur.
- **OQ-5 (Endpoint/entry point)**: Apakah pencatatan dilakukan lewat endpoint BFF
  baru (mis. `POST /api/digital-sign/requests`) yang dipanggil frontend, atau
  disisipkan dalam alur existing?
- **OQ-1 (Sumber status)**: mewarisi open question DOMAIN PDS-MH — apakah
  MyHospital melacak status (webhook/polling) atau hanya mencatat awal.
- **OQ-2/OQ-3 (DokumenId & zona waktu)**: mewarisi open question DOMAIN PDS-MH.