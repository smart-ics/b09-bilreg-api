---
Title: Feasibility Assessment - Opsi Pembatalan Otomatis Dokumen Sumber Saat Pembatalan Registrasi Rawat Inap
Code: FA-ADM-RANAP-VOID-REG-SOURCE-001
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-03T10:48:00+07:00
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan terhadap usulan perubahan (*CHANGE-REQUEST*) pada alur pembatalan registrasi rawat inap (*void admisi*) untuk menambahkan opsi kotak centang (*checkbox*) konfirmasi pembatalan otomatis dokumen sumber pendaftaran (Reservasi Kamar atau Permintaan Opname).

Referenced artifacts:

- ISSUE: [ADM-RANAP-VOID-REGISTRASI-SOURCE-VOID-OPTION-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-VOID-REGISTRASI-SOURCE-VOID-OPTION-ISSUE.md) (`ISSUE-ADM-RANAP-VOID-REG-SOURCE-001`)
- DOMAIN: [admisi-ranap-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/admisi-ranap-domain.md)

## Objective

Mengevaluasi kelayakan fungsional, teknis, dan pengalaman pengguna (*UX*) dalam menyediakan kontrol konfirmasi pembatalan dokumen asal (Reservasi atau Permintaan Opname) pada dialog pembatalan registrasi rawat inap; mengidentifikasi kesenjangan (*gaps*), pertanyaan terbuka (*open questions*), asumsi, risiko integritas data, serta memberikan rekomendasi pendekatan realisasi sebelum difinalisasi oleh Arsitek.

---

# 2. Current State

Fakta kondisi sistem saat ini berdasarkan penelusuran artefak dan basis kode sumber:

## Existing Behavior

1. **Frontend Dialog Pembatalan Registrasi**:
   - Komponen dialog [CoordinatedCancelDialog.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/CoordinatedCancelDialog.vue) digunakan bersama untuk pembatalan registrasi, pembatalan permintaan opname, dan pembatalan reservasi melalui [AdmisiRanapConsole.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/views/AdmisiRanapConsole.vue).
   - Dialog saat ini hanya memiliki bidang masukan alasan (*Textarea* `reason`) dengan validasi wajib diisi (1–500 karakter).
   - Tidak ada elemen kontrol pemilihan (*checkbox*) atau indikator status dokumen sumber (apakah pasien berasal dari Reservasi, Permintaan Opname, atau Pendaftaran Langsung).
   - Orkestrator antarmuka [useJourneyActionHost.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useJourneyActionHost.ts) mengirimkan payload `CancelAdmissionInput` ke API endpoint `POST /api/admisi-ranap/admission/{regId}/cancel`:
     - `reason` (string)
     - `userId` (string)
     - `expectedAdmissionStatus` (number)
     - `expectedUpdatedAt` (string / null)
     - `requestId` (string UUID)

2. **Backend Coordinated Cancellation**:
   - Handler use case [AdmCoordinatedCancelCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs) menerima perintah `AdmCoordinatedCancelCmd`.
   - Melalui `_repo.LockState(regId)`, sistem mendeteksi keberadaan `state.SourceId` dan `state.SourceKind` (`"OpnameRequestModel"` atau `"ReservationModel"`).
   - Pada baris 92 [AdmCoordinatedCancelCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs#L92), sistem saat ini **selalu** mengeksekusi pemulihan dokumen asal:
     `if (state.SourceId is not null) EnsureSource(_repo.RestoreSource(state, normalized.UserId, occurredAt));`
   - Pada persistensi [CoordinatedCancellationRepo.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/AdmissionFeature/CoordinatedCancellationRepo.cs#L74-L79):
     - Jika `OpnameRequestModel`: `OpnameRequestStatus` dikembalikan ke `0` (`Requested`) dan `FulfilledRegId = '-'`.
     - Jika `ReservationModel`: `ReservationStatus` dikembalikan ke `1` (`Maintained`) dan `RealizedRegId = '-'`.
   - Tidak ada parameter kendali dari pemanggil untuk membatalkan (*void*) dokumen sumber tersebut secara bersamaan.

3. **Status Dokumen Sumber (Domain Model)**:
   - Status Permintaan Opname ([OpnameRequestStatusEnum.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiRanapContext/OpnameRequestFeature/OpnameRequestStatusEnum.cs)):
     - `0` = `Requested`
     - `1` = `Fulfilled`
     - `2` = `Cancelled`
   - Status Reservasi Kamar ([ReservationStatusEnum.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiRanapContext/ReservationFeature/ReservationStatusEnum.cs)):
     - `0` = `Reserved`
     - `1` = `Maintained`
     - `2` = `Realized`
     - `3` = `Cancelled`

## Existing Constraints

1. **Integritas Transaksi Atomik (ACID Scope)**: Seluruh mutasi pembatalan registrasi ranap (tabel `ta_registrasi`, `BILRG_RegAktif`, `ta_reg_history_dokter`, `BILRG_BedWaitingList`, `BILRG_AdmAdmission`, dan dokumen sumber) berjalan di dalam satu `TransactionScope` (`TransHelper.NewScope()`). Perubahan status dokumen sumber harus tetap berada dalam transaksi yang sama untuk mencegah data tidak konsisten.
2. **Backward Compatibility & Pendaftaran Langsung (Direct Admission)**: Tidak semua registrasi rawat inap memiliki dokumen sumber (misalnya admisi darurat/langsung tanpa reservasi atau tanpa opname request). Alur baru harus tetap aman dan valid jika pasien tidak memiliki dokumen sumber (`state.SourceId is null`).
3. **Idempotensi & Ledger**: Eksekusi pembatalan dilindungi oleh *idempotency ledger* (`BILRG_AdmCoordinatedCancellationRequest`) dengan *fingerprint*. Penambahan parameter perintah harus diikutsertakan dalam pembentukan *fingerprint* agar variasi perintah yang berbeda tidak dianggap replay identik.
4. **Visibilitas UX**: Karena default kotak centang adalah tercentang (*destructive by default* terhadap dokumen sumber), komponen UI harus memberikan sinyal visual (warna aksen, penegasan teks, dan label peringatan) yang kontras agar petugas admisi tidak secara tidak sengaja membatalkan dokumen sumber yang seharusnya ingin mereka proses ulang.

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| GAP-001 | CRITICAL | Kontrak API dan Command Backend (`AdmCoordinatedCancelCmd`, `AdmCoordinatedCancelBody`, `CancelAdmissionInput`) belum memiliki parameter penentu pembatalan dokumen sumber (misal `VoidSource` / `CascadeVoidSource: bool`). |
| GAP-002 | CRITICAL | Backend persistensi `ICoordinatedCancellationRepo` hanya menyediakan metode `RestoreSource`, belum memiliki metode untuk membatalkan dokumen sumber (`VoidSource` / `CancelSource`) menjadi status `Cancelled` (`OpnameRequestStatus = 2`, `ReservationStatus = 3`). |
| GAP-003 | MAJOR | Komponen dialog frontend `CoordinatedCancelDialog.vue` belum mendukung kontrol kotak centang (*checkbox*), status *default checked*, dan styling penanda peringatan yang kontras. |
| GAP-004 | MAJOR | `useJourneyActionHost.ts` dan pemetaan aksi journey (`mapJourneyAllowedAction.ts`) belum meneruskan informasi konteks dokumen sumber (jenis & nomor dokumen sumber) ke dialog pembatalan registrasi untuk ditampilkan secara informatif kepada pengguna. |
| GAP-005 | MINOR | `AuditSnapshots` dan pencatatan riwayat audit pada `AdmCoordinatedCancelCmd` saat pembatalan sumber perlu memastikan status audit dokumen sumber tercatat secara akurat apakah dipulihkan (`RESTORE`) atau dibatalkan (`VOID`). |

---

# 4. Open Questions

| ID | Question | Impact |
|---|---|---|
| OQ-001 | Bagaimana teks label dan penegasan visual kotak centang ditampilkan jika registrasi **tidak memiliki** dokumen sumber (misal *Direct Admission*)? Apakah kotak centang disembunyikan (*hidden*) atau dinonaktifkan? | Mempengaruhi fleksibilitas desain antarmuka `CoordinatedCancelDialog.vue` agar tidak membingungkan pengguna ketika pasien tidak memiliki reservasi/opname request. |
| OQ-002 | Apakah alasan pembatalan (`Reason`) yang dimasukkan pengguna pada dialog pembatalan registrasi juga harus dicatat sebagai alasan pembatalan pada dokumen sumber (reservasi / opname request)? | Menentukan apakah kolom keterangan/alasan pembatalan pada tabel `BILRG_AdmOpnameRequest` dan `BILRG_AdmReservation` perlu diisi dengan teks `Reason` tersebut. |
| OQ-003 | Apakah nilai bawaan (*default*) `VoidSource = true` berlaku juga pada pemanggilan API langsung jika parameter opsional tidak dikirimkan, atau apakah wajib eksplisit? | Menentukan perilaku *backward compatibility* pada level API controller dan skenario unit testing backend. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | Membatalkan dokumen sumber berarti mengubah status `BILRG_AdmOpnameRequest` menjadi `2` (`Cancelled`) atau `BILRG_AdmReservation` menjadi `3` (`Cancelled`), dengan mencatat petugas pembatal (`VodUser`) dan waktu pembatalan (`VodDate`). |
| ASM-002 | Jika kotak centang tidak dicentang (`VoidSource == false`), sistem mempertahankan perilaku eksisting yaitu melakukan `RestoreSource` (mengembalikan opname request ke `Requested` atau reservasi ke `Maintained`) agar dapat diproses ulang di kemudian hari. |
| ASM-003 | Jika pasien terdaftar langsung (*Direct Admission*) tanpa opname request maupun reservasi, flag `VoidSource` diabaikan oleh backend tanpa menimbulkan kesalahan validasi. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Pengguna tidak menyadari bahwa kotak centang tercentang secara default, sehingga dokumen reservasi/opname pasien ikut terbatalkan padahal masih ingin digunakan untuk pendaftaran ulang. | Dokumen reservasi/opname harus dibuat ulang secara manual oleh dokter pengirim atau petugas reservasi. | Memberikan warna aksen peringatan (misal teks/badge kuning/merah bernada hati-hati), deskripsi dampak yang eksplisit di bawah checkbox, serta memastikan label menyebutkan nomor ID dokumen yang terdampak. |
| RISK-002 | Kegagalan pembatalan dokumen sumber di database menyebabkan registrasi terbatalkan sebagian (*partial cancellation*). | Inkonsistensi status sistem antara registrasi ranap dan dokumen asal. | Menjalankan seluruh pembaruan status (admisi, registrasi, kamar/waiting list, dan dokumen sumber) dalam transaksi atomik database (`TransHelper.NewScope()`). |
| RISK-003 | Bentrokan konkurensi saat dokumen sumber secara bersamaan diubah oleh petugas lain. | Mutasi pembatalan menolak atau menghasilkan status konflik. | Mempertahankan mekanisme *optimistic locking* / *updlock* yang sudah ada pada `LockState` di `CoordinatedCancellationRepo`. |

---

# 7. Recommendations

## Option A: Flag Boolean Terpadu pada Coordinated Cancel Command (Rekomendasi Utama)

Menambahkan parameter opsional boolean `VoidSource` (default `true` di level UI) pada payload mutasi `POST /api/admisi-ranap/admission/{regId}/cancel`.

- Backend:
  - `AdmCoordinatedCancelCmd` menambahkan properti `bool VoidSource = false`.
  - Jika `state.SourceId is not null`:
    - Jika `normalized.VoidSource == true` -> panggil `_repo.VoidSource(state, normalized.Reason, normalized.UserId, occurredAt)`.
    - Jika `normalized.VoidSource == false` -> panggil `_repo.RestoreSource(state, normalized.UserId, occurredAt)` (perilaku eksisting).
- Frontend:
  - `CoordinatedCancelDialog.vue` menambahkan prop opsional `showVoidSourceCheckbox: boolean`, `sourceLabel: string`, dan `defaultVoidSource: boolean = true`.
  - Checkbox ditampilkan dengan gaya visual tegas (misal teks tebal berlatar/bergaris tepi aksen peringatan).
  - Mengembalikan objek konfirmasi `{ reason: string, voidSource: boolean }`.

### Advantages

- Atomik dan konsisten: seluruh orkestrasi pembatalan selesai dalam satu kali *round-trip* API di bawah satu transaksi database.
- Idempotensi terlindungi: status `VoidSource` ikut dihitung dalam *fingerprint* ledger pembatalan terkoordinasi.
- Menghindari *race condition* yang rentan jika frontend harus memanggil dua endpoint terpisah (batal registrasi kemudian batal reservasi secara beruntun).

### Disadvantages

- Memerlukan pembaruan kontrak DTO backend dan skema Zod frontend pada satu titik endpoint.

## Option B: Dua Langkah Mutasi Berurutan di Frontend (Sequential Client-Side Orchestration)

Frontend memanggil endpoint pembatalan registrasi eksisting, lalu jika sukses dan checkbox tercentang, frontend memanggil endpoint pembatalan reservasi / opname request secara terpisah (`useCancelReservation` / `useCancelOpnameRequest`).

### Advantages

- Tidak perlu mengubah kontrak parameter pada endpoint pembatalan registrasi backend.

### Disadvantages

- **Non-atomik**: jika pemanggilan kedua gagal (misal gangguan koneksi), registrasi telah batal tetapi dokumen sumber sudah terlanjur berstatus pulih (*restored*), meninggalkan status data gantung.
- Inkonsistensi UX dan lambat karena membutuhkan dua kali *round-trip* jaringan berturut-turut.

---

# 8. Gap Closure

Resolusi terhadap kesenjangan dan pertanyaan terbuka:

## GAP-001 & GAP-002: Kontrak Parameter & Persistensi Pembatalan Dokumen Sumber

### Decision

Mengadopsi **Option A**. Menambahkan parameter boolean `VoidSource` pada `AdmCoordinatedCancelCmd`, `AdmCoordinatedCancelBody`, dan skema frontend `cancelAdmissionInputSchema`. Menambahkan metode `VoidSource` pada `ICoordinatedCancellationRepo` untuk memperbarui status `BILRG_AdmOpnameRequest` menjadi `Cancelled` (`2`) atau `BILRG_AdmReservation` menjadi `Cancelled` (`3`) secara atomik.

### Rationale

Pendekatan terpadu (*single coordinated transaction*) adalah satu-satunya cara menjamin integritas data (ACID) dan mencegah kondisi gantung (*split-brain state*) di rumah sakit.

### Impact

Pembaruan pada DTO/Command backend `AdmCoordinatedCancelCmd.cs`, API controller, repository `CoordinatedCancellationRepo.cs`, skema Zod `admisiRanap.ts`, dan service API frontend.

---

## GAP-003 & GAP-004: Penyesuaian Antarmuka Dialog & Konteks Dokumen Sumber

### Decision

Memperbarui `CoordinatedCancelDialog.vue` untuk mendukung tampilan kotak centang konfirmasi pembatalan dokumen asal jika registrasi memiliki dokumen sumber (`sourceKind` dan `sourceId` tersedia):
1. Kotak centang berstatus awal tercentang (`checked = true`).
2. Label dan visualisasi kotak centang menggunakan styling yang kontras (font tebal dengan teks penjelasan yang jelas mengenai konsekuensi pembatalan dokumen sumber).
3. Jika registrasi tidak memiliki dokumen sumber (pendaftaran langsung), kotak centang tidak ditampilkan agar antarmuka tetap bersih.

### Rationale

Memenuhi kebutuhan pengguna secara presisi dengan tetap menjaga keselamatan operasional melalui petunjuk visual yang jelas (*preventing unintended destructive actions*).

### Impact

Pembaruan pada komponen `CoordinatedCancelDialog.vue`, `AdmisiRanapConsole.vue`, dan composable `useJourneyActionHost.ts`.

---

## OQ-001: Tampilan jika Registrasi Tanpa Dokumen Sumber

### Decision

CLOSED. Jika registrasi adalah pendaftaran langsung (*Direct Admission* tanpa `opnameRequestId` maupun `reservationId`), kotak centang pembatalan dokumen sumber disembunyikan (*v-if="hasSource"*).

### Rationale

Tidak relevan menanyakan pembatalan reservasi/opname kepada pengguna jika pasien memang tidak pernah memiliki reservasi atau permintaan opname.

---

## OQ-002: Alasan Pembatalan pada Dokumen Sumber

### Decision

CLOSED. Alasan pembatalan (`Reason`) yang dimasukkan pengguna pada dialog disimpan sebagai keterangan pembatalan pada entitas dokumen sumber yang dibatalkan, dan dicatat pada audit log.

### Rationale

Menjaga jejak audit (*auditability*) yang konsisten mengapa reservasi atau opname request tersebut dibatalkan.

---

## OQ-003: Default Nilai Parameter di Level Backend

### Decision

CLOSED. Pada backend, nilai default `VoidSource` adalah `false` jika tidak disertakan dalam payload untuk menjaga *backward compatibility*. Namun frontend selalu mengirimkan nilai eksplisit (`true` atau `false`) sesuai pilihan pengguna di dialog.

### Rationale

Menjamin keamanan pemanggil API pihak ketiga atau pengujian otomatis eksisting yang belum mengirimkan flag baru.

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-NOT-REQUIRED**

## Rationale

Struktur arsitektur pembatalan terkoordinasi (`CoordinatedCancellation`) telah mapan dan memiliki batas tanggung jawab (*boundary*) yang jelas:
1. Orkestrasi pembatalan terkoordinasi telah memiliki boundary `ICoordinatedCancellationRepo` dan use case handler `AdmCoordinatedCancelCmd`.
2. Perubahan hanya menambahkan satu parameter penentu arah aksi (`VoidSource` vs `RestoreSource`) di dalam boundary use case yang sama, tanpa mengubah komponen arsitektur baru, tanpa dependensi sistem eksternal baru, dan tanpa memindahkan kepemilikan data.
3. Seluruh keputusan kelayakan pada dokumen ini bersifat definitif dan dapat langsung diwujudkan ke dalam *Target Architecture Delta* atau langsung ke *Implementation Plan*.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated

## Status

**READY-FOR-PLANNING**

## Notes

Seluruh kesenjangan teknis dan pertanyaan terbuka telah ditutup dengan keputusan yang jelas. Sesuai tata kelola Knowledge-Centric SDLC, status kelayakan dipertahankan `NOT-READY` oleh peran Analis (`ica-analyst`). Otoritas pemberian gate `READY-FOR-PLANNING` berada di bawah peran Arsitek (`ica-architect`).

---

# 11. References

Referenced artifacts:

- ISSUE: [ADM-RANAP-VOID-REGISTRASI-SOURCE-VOID-OPTION-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-VOID-REGISTRASI-SOURCE-VOID-OPTION-ISSUE.md)
- DOMAIN: [admisi-ranap-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/admisi-ranap-domain.md)

Referenced codebase locations:

- Backend Command: [AdmCoordinatedCancelCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs)
- Backend Persistence Interface: [CoordinatedCancellationPersistence.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/CoordinatedCancellationPersistence.cs)
- Backend Persistence Impl: [CoordinatedCancellationRepo.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/AdmissionFeature/CoordinatedCancellationRepo.cs)
- Backend Controller: [AdmissionController.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/AdmisiRanapContext/AdmissionController.cs)
- Frontend Dialog: [CoordinatedCancelDialog.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/CoordinatedCancelDialog.vue)
- Frontend View Host: [AdmisiRanapConsole.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/views/AdmisiRanapConsole.vue)
- Frontend Composable: [useJourneyActionHost.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useJourneyActionHost.ts)
- Frontend Zod Schema: [admisiRanap.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/types/admisiRanap.ts)
