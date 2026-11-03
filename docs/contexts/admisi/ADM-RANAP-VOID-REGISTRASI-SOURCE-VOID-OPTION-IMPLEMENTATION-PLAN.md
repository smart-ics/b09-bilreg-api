---
Title: Implementation Plan - Opsi Pembatalan Otomatis Dokumen Sumber Saat Pembatalan Registrasi Rawat Inap
Code: PLAN-ADM-RANAP-VOID-REG-SOURCE-001
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-03T11:46:00+07:00
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan opsi kotak centang (*checkbox*) konfirmasi pada dialog pembatalan registrasi rawat inap (*void admisi*) untuk membatalkan dokumen sumber pendaftaran (Reservasi Kamar atau Permintaan Opname) secara otomatis dan atomik bersamaan dengan pembatalan registrasi, dengan status *default checked* dan visualisasi yang jelas bagi pengguna.

Referenced artifacts:

- ISSUE: [ADM-RANAP-VOID-REGISTRASI-SOURCE-VOID-OPTION-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-VOID-REGISTRASI-SOURCE-VOID-OPTION-ISSUE.md) (`ISSUE-ADM-RANAP-VOID-REG-SOURCE-001`)
- FEASIBILITY-ASSESSMENT: [ADM-RANAP-VOID-REGISTRASI-SOURCE-VOID-OPTION-FEASIBILITY-ASSESSMENT.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-VOID-REGISTRASI-SOURCE-VOID-OPTION-FEASIBILITY-ASSESSMENT.md) (`FA-ADM-RANAP-VOID-REG-SOURCE-001`, Status: READY-FOR-PLANNING)
- DOMAIN: [admisi-ranap-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/admisi-ranap-domain.md)

Architecture Applicability: ARCHITECTURE-NOT-REQUIRED

No architectural target-state artifact was required. Implementation relies on existing technical structure. Approved feasibility decisions are authoritative for the change. The current codebase is the source of current technical truth.

---

# 2. Planning Scope

Perbaikan mencakup dua repositori dalam *workspace*:

1. **Backend Core Persistence & Coordinated Cancellation (`b09-bilreg-api`)**:
   - `ICoordinatedCancellationRepo` & `CoordinatedCancellationRepo.cs`: Menambahkan metode `VoidSource` untuk memperbarui status dokumen sumber (`BILRG_AdmOpnameRequest` menjadi `2 / Cancelled` atau `BILRG_AdmReservation` menjadi `3 / Cancelled`) beserta informasi pembatal (`VodUser`, `VodDate`, dan keterangan alasan pembatalan).
   - `AdmCoordinatedCancelCmd.cs`:
     - Menambahkan properti `bool VoidSource = false` pada command.
     - Mengondisikan pemanggilan `_repo.VoidSource(...)` jika `VoidSource == true`, atau `_repo.RestoreSource(...)` jika `VoidSource == false`.
     - Mengikutsertakan nilai `VoidSource` dalam kalkulasi `Fingerprint` idempotensi request.
   - `AdmissionController.cs`: Mengekspos properti `VoidSource` pada DTO `AdmCoordinatedCancelBody` dan meneruskannya ke command.
   - Pengujian Otomatis:
     - Unit test di `AdmCoordinatedCancelHandlerTest.cs` menguji skenario `VoidSource == true`, `VoidSource == false`, dan *Direct Admission* (tanpa sumber).
     - Integration test di `AdmissionCancellationApiIntegrationTest.cs` menguji eksekusi API dengan parameter `voidSource`.

2. **Frontend UI & Action Host Module (`c012_myhospital_web`)**:
   - `admisiRanap.ts`: Memperbarui skema Zod `cancelAdmissionInputSchema` untuk menyertakan `voidSource: z.boolean().default(true)`.
   - `CoordinatedCancelDialog.vue`:
     - Menambahkan kontrol kotak centang (*checkbox*) konfirmasi pembatalan dokumen asal dengan nilai awal tercentang (*default checked*).
     - Menampilkan kotak centang hanya jika registrasi memiliki dokumen asal (`hasSource == true`), dan menyembunyikannya pada *Direct Admission*.
     - Menerapkan tipografi tebal dan aksen warna peringatan yang jelas dan kontras agar pengguna langsung memahami konsekuensi tindakan.
     - Memperbarui payload event `confirm` menjadi `{ reason: string, voidSource: boolean }`.
   - `useJourneyActionHost.ts` & `AdmisiRanapConsole.vue`:
     - Menyediakan informasi konteks keberadaan sumber (`hasSource` dan label sumber) pada `cancelAdmissionState`.
     - Meneruskan nilai `voidSource` dari konfirmasi dialog ke mutasi `cancelAdmissionMutation`.
   - Pengujian Otomatis:
     - Unit test di `useJourneyActionHost.spec.ts` dan test komponen dialog memverifikasi kemunculan checkbox, status default tercentang, dan pengiriman payload `voidSource`.

---

# 3. Dependencies

- External Dependencies:
  - Database SQL Server `HOSPITAL_PKL` dengan tabel `BILRG_AdmOpnameRequest`, `BILRG_AdmReservation`, `BILRG_AdmAdmission`, dan `ta_registrasi`.
  - Runtime .NET 8 SDK (`b09-bilreg-api`) dan Node.js / pnpm (`c012_myhospital_web`).

- Slice Dependencies:
  - `P1-S01`: Tidak memiliki ketergantungan (titik awal persistensi backend).
  - `P1-S02`: Bergantung pada `P1-S01` (controller & API integration test membutuhkan implementasi use case).
  - `P2-S03`: Bergantung pada `P1-S02` (skema dan service frontend diselaraskan dengan kontrak endpoint backend).
  - `P2-S04`: Bergantung pada `P2-S03` (dialog UI dan composable memanfaatkan skema dan service yang telah diperbarui).

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---------|---------|---------|---------|
| P1 | IMPLEMENTED | GO | 2/2 |
| P2 | IMPLEMENTED | GO | 2/2 |

---

# 5. Phases

## P1 - Backend Coordinated Cancellation with Void Source Option (`b09-bilreg-api`)

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P1-S01

Title: Implementasi VoidSource pada ICoordinatedCancellationRepo dan AdmCoordinatedCancelCmd

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menambahkan kapabilitas pembatalan dokumen asal (Reservasi atau Permintaan Opname) pada repository persistensi dan use case handler `AdmCoordinatedCancelCmd` secara atomik berdasarkan nilai parameter `VoidSource`.

Depends On: None  

Repository: b09-bilreg-api  

Completion Criteria:  
1. Berkas [CoordinatedCancellationPersistence.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/CoordinatedCancellationPersistence.cs) diperbarui:
   - Interface `ICoordinatedCancellationRepo` memiliki metode baru:  
     `int VoidSource(CoordinatedCancellationState state, string reason, string userId, DateTime now);`
2. Berkas [CoordinatedCancellationRepo.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/AdmissionFeature/CoordinatedCancellationRepo.cs) diperbarui:
   - Mengimplementasikan `VoidSource`:
     - Jika `state.SourceKind == "OpnameRequestModel"`: memutakhirkan `BILRG_AdmOpnameRequest` menjadi `OpnameRequestStatus = 2` (`Cancelled`), `VodUser = @userId`, `VodDate = @now`, `UpdUser = @userId`, `UpdDate = @now`, dan mencatat alasan pembatalan jika kolom tersedia.
     - Jika `state.SourceKind == "ReservationModel"`: memutakhirkan `BILRG_AdmReservation` menjadi `ReservationStatus = 3` (`Cancelled`), `VodUser = @userId`, `VodDate = @now`, `UpdUser = @userId`, `UpdDate = @now`, dan mencatat alasan pembatalan jika kolom tersedia.
   - Pengecekan koherensi pada `LockState` (`coherent`) mengakomodasi status dokumen asal yang berstatus dibatalkan (`Cancelled`).
3. Berkas [AdmCoordinatedCancelCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs) diperbarui:
   - `AdmCoordinatedCancelCmd` menerima `bool VoidSource = false`.
   - Pada blok eksekusi dokumen asal:
     - Jika `state.SourceId is not null && normalized.VoidSource`: panggil `EnsureSource(_repo.VoidSource(state, normalized.Reason, normalized.UserId, occurredAt))`.
     - Jika `state.SourceId is not null && !normalized.VoidSource`: panggil `EnsureSource(_repo.RestoreSource(state, normalized.UserId, occurredAt))` (perilaku eksisting).
   - Fungsi `Fingerprint` menyertakan `normalized.VoidSource` dalam hash.
4. Pengujian unit di [AdmCoordinatedCancelHandlerTest.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/AdmCoordinatedCancelHandlerTest.cs) ditambahkan:
   - Skenario ketika `VoidSource == true` dan `state.SourceId != null`: memverifikasi `_repo.VoidSource` dipanggil 1 kali dan `_repo.RestoreSource` tidak pernah dipanggil (`Times.Never`).
   - Skenario ketika `VoidSource == false` dan `state.SourceId != null`: memverifikasi `_repo.RestoreSource` dipanggil 1 kali dan `_repo.VoidSource` tidak pernah dipanggil (`Times.Never`).
   - Skenario ketika `state.SourceId == null` (*Direct Admission*): pembatalan tetap berhasil tanpa memanggil `VoidSource` maupun `RestoreSource`.
5. Rangkaian pengujian unit lulus via perintah:  
   `dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~AdmCoordinatedCancelHandlerTest"`

Notes:  
Seluruh langkah mutasi pembatalan dokumen sumber berada di bawah `TransHelper.NewScope()`, menjamin transaksi tetap ACID.

Implementation Notes:  
- Menambahkan metode `int VoidSource(CoordinatedCancellationState state, string reason, string userId, DateTime now);` ke dalam interface `ICoordinatedCancellationRepo`.
- Mengimplementasikan `VoidSource` di `CoordinatedCancellationRepo.cs` untuk memperbarui status sumber menjadi Cancelled (`OpnameRequestStatus = 2` atau `ReservationStatus = 3`) disertai penulisan metadata audit `VodUser`, `VodDate`, `UpdUser`, `UpdDate`.
- Memperbarui evaluasi koherensi pada `CoordinatedCancellationRepo.LockState` agar status sumber yang telah dibatalkan (`OpnameRequestStatus == 2` atau `ReservationStatus == 3`) diakui sebagai kondisi koheren.
- Menambahkan properti `bool VoidSource = false` pada `AdmCoordinatedCancelCmd` dan mengondisikan pemanggilan `_repo.VoidSource` jika `VoidSource == true`, atau `_repo.RestoreSource` jika `false`.
- Memperbarui kalkulasi `Fingerprint` untuk menyertakan `normalized.VoidSource` dan audit log untuk mencatat aksi `"VOID"` atau `"RESTORE"` secara sesuai.
- Menambahkan 3 skenario pengujian unit baru pada `AdmCoordinatedCancelHandlerTest.cs` (`WhenVoidSourceTrueAndSourceExists_CallsVoidSourceAndNeverRestoreSource`, `WhenVoidSourceFalseAndSourceExists_CallsRestoreSourceAndNeverVoidSource`, `WhenDirectAdmissionWithoutSource_CancellationSucceedsWithoutVoidOrRestoreSource`), semuanya lulus (11/11 tests pass).

Changed Files:  
- `b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/CoordinatedCancellationPersistence.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/AdmissionFeature/CoordinatedCancellationRepo.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/AdmCoordinatedCancelHandlerTest.cs`

---

### P1-S02

Title: Ekspos Parameter VoidSource pada API Controller dan Pengujian Integrasi

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Mengekspos properti `VoidSource` pada request DTO `AdmCoordinatedCancelBody` di `AdmissionController.cs`, meneruskannya ke MediatR command, dan memverifikasi integrasi API melalui pengujian otomatis.

Depends On: P1-S01  

Repository: b09-bilreg-api  

Completion Criteria:  
1. Berkas [AdmissionController.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/AdmisiRanapContext/AdmissionController.cs) diperbarui:
   - Record `AdmCoordinatedCancelBody` menerima properti opsional `bool? VoidSource = false`.
   - Metode `Cancel` meneruskan `body.VoidSource ?? false` ke instance `AdmCoordinatedCancelCmd`.
2. Pengujian integrasi di [AdmissionCancellationApiIntegrationTest.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/Api/AdmissionCancellationApiIntegrationTest.cs) ditambahkan / diperbarui:
   - Memverifikasi pengiriman HTTP request pembatalan dengan body `{ "reason": "Batal rawat", "voidSource": true, ... }` berhasil mengembalikan HTTP 200 OK.
   - Memverifikasi pengiriman tanpa field `voidSource` tetap sukses (default `false` / backward compatible).
3. Seluruh pengujian di `Bilreg.Test` untuk cancellation lulus via perintah:  
   `dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~AdmCoordinatedCancelHandlerTest|FullyQualifiedName~AdmissionCancellationApiIntegrationTest"`

Notes:  
Parameter `voidSource` bersifat opsional di tingkat controller dengan nilai fallback `false` demi menjaga kompatibilitas dengan pemanggil API eksisting.

Implementation Notes:  
- Menambahkan properti `bool? VoidSource = false` pada record DTO `AdmCoordinatedCancelBody` di `AdmissionController.cs`.
- Meneruskan `body.VoidSource ?? false` pada instansiasi `AdmCoordinatedCancelCmd` di action method `AdmissionController.Cancel`.
- Menambahkan setup mocking `_repo.VoidSource` pada `AdmissionCancellationApiHarness.SetupSuccess` di `AdmissionCancellationWebApplicationFactory.cs`.
- Memperbarui helper `Fingerprint` pada `AdmissionCancellationApiIntegrationTest.cs` agar selaras dengan skema fingerprint berparameter `VoidSource` yang ditambahkan pada use case handler.
- Menambahkan dua skenario integration test baru pada `AdmissionCancellationApiIntegrationTest.cs`:
  - `Cancel_WithVoidSourceTrue_SucceedsAndCallsVoidSource`: memverifikasi pengiriman payload HTTP dengan `voidSource: true` memanggil `_repo.VoidSource` (1x) dan tidak memanggil `RestoreSource`.
  - `Cancel_WithOmittedVoidSource_SucceedsAndCallsRestoreSource`: memverifikasi pengiriman payload HTTP tanpa field `voidSource` berhasil dan secara default memanggil `_repo.RestoreSource` (backward-compatible).
- Seluruh 22 tes unit dan integrasi cancellation di `Bilreg.Test` berhasil dijalankan dan lulus 100%.

Changed Files:  
- `b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/AdmisiRanapContext/AdmissionController.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/Api/AdmissionCancellationWebApplicationFactory.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/Api/AdmissionCancellationApiIntegrationTest.cs`

---

## P2 - Frontend Dialog & Action Host Integration (`c012_myhospital_web`)

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P2-S03

Title: Sinkronisasi Skema Zod cancelAdmissionInputSchema dan Query Service

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Memperbarui kontrak skema input pembatalan admisi pada frontend (`admisiRanap.ts`) agar menyertakan parameter `voidSource` dan memastikan tipe TypeScript tersinkronisasi.

Depends On: P1-S02  

Repository: c012_myhospital_web  

Completion Criteria:  
1. Berkas [admisiRanap.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/types/admisiRanap.ts) diperbarui:
   - Skema `cancelAdmissionInputSchema` menyertakan `voidSource: z.boolean().default(true)`.
   - Tipe inferensi `CancelAdmissionInput` secara otomatis mencakup properti `voidSource?: boolean`.
2. Verifikasi pemanggilan mutasi pada [AdmisiRanapService.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/queries/AdmisiRanapService.ts) mendukung pengiriman payload `voidSource`.
3. Seluruh unit test terkait type dan query lulus via perintah:  
   `pnpm --dir d:\project_aktif\project_MyHospitalWeb\c012_myhospital_web test src/modules/Admisi/types/__tests__ --run` (atau suite test terkait).

Notes:  
Skema Zod menggunakan `.default(true)` sehingga jika frontend tidak mengirimkan secara eksplisit, payload default adalah `true` sesuai kebutuhan bisnis.

Implementation Notes:  
- Memperbarui skema `cancelAdmissionInputSchema` di `c012_myhospital_web/src/modules/Admisi/types/admisiRanap.ts` dengan menambahkan `voidSource: z.boolean().default(true)`.
- Mengubah tipe `CancelAdmissionInput` menjadi `z.input<typeof cancelAdmissionInputSchema>` sehingga properti `voidSource?: boolean` bersifat opsional saat input mutasi dipersiapkan, namun saat diparsing melalui Zod oleh `createPostMutationFn`, otomatis diisi default `true`.
- Memverifikasi implementasi `useCancelAdmission` pada `AdmisiRanapService.ts` yang menggunakan `cancelAdmissionInputSchema` dan `CancelAdmissionInput` telah mendukung pengiriman payload `voidSource`.
- Menambahkan unit test baru di `c012_myhospital_web/src/modules/Admisi/types/__tests__/admisiRanap.spec.ts` untuk memverifikasi:
  - Pemanggilan `.parse()` tanpa `voidSource` menghasilkan default `voidSource: true`.
  - Pemanggilan `.parse()` dengan `voidSource: false` menghasilkan `voidSource: false`.
  - Pemanggilan `.parse()` dengan `voidSource: true` menghasilkan `voidSource: true`.
  - Validasi penolakan tipe invalid untuk `voidSource`.
  - Validasi parsing `cancelAdmissionResponseSchema`.
- Seluruh unit test types (51 tests di `admisiRanap.spec.ts` dan 164 tests di suite types) serta query service (`AdmisiRanapService.spec.ts`) dan composables (`useJourneyActionHost.spec.ts`) lulus 100%. Type checking `pnpm run type-check` (`vue-tsc`) juga lulus tanpa error.

Changed Files:  
- `c012_myhospital_web/src/modules/Admisi/types/admisiRanap.ts`
- `c012_myhospital_web/src/modules/Admisi/types/__tests__/admisiRanap.spec.ts`

---

### P2-S04

Title: Penambahan Checkbox Batal Dokumen Sumber pada CoordinatedCancelDialog dan useJourneyActionHost

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menambahkan kontrol kotak centang konfirmasi pembatalan dokumen asal pada `CoordinatedCancelDialog.vue` dengan styling kontras/jelas, status default tercentang, hanya muncul jika pasien memiliki dokumen sumber, dan meneruskan nilainya saat konfirmasi pembatalan.

Depends On: P2-S03  

Repository: c012_myhospital_web  

Completion Criteria:  
1. Berkas [CoordinatedCancelDialog.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/CoordinatedCancelDialog.vue) diperbarui:
   - Menerima props opsional baru:
     - `hasSource?: boolean`
     - `sourceKindLabel?: string` (contoh: "Reservasi" atau "Permintaan Opname")
     - `defaultVoidSource?: boolean` (default: `true`)
   - Ketika `hasSource` bernilai `true`, dialog menampilkan kotak centang (*Checkbox*) dan label konfirmasi:
     - State `voidSource` diinisialisasi dengan nilai `props.defaultVoidSource ?? true`.
     - Ditampilkan dengan tipografi tebal dan aksen warna peringatan yang kontras dan jelas (misal teks berbobot `font-medium`, aksen warna teks/border bernuansa perhatian/peringatan) agar pengguna langsung mengetahui bahwa dokumen sumber akan ikut dibatalkan.
     - Teks deskripsi di bawah checkbox menegaskan: *"Centang untuk membatalkan [sourceKindLabel] ini secara otomatis. Jika tidak dicentang, dokumen asal tetap dipertahankan."*
   - Ketika `hasSource` bernilai `false` (pendaftaran langsung), kontrol kotak centang disembunyikan (*hidden*).
   - Payload event `confirm` memuat `{ reason: string, voidSource: boolean }`.
2. Berkas [useJourneyActionHost.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useJourneyActionHost.ts) diperbarui:
   - `cancelAdmissionState` menyimpan informasi `hasSource: boolean` dan `sourceKindLabel: string` yang diekstrak dari status selected journey (apakah memiliki `opnameRequestId` atau `reservationId`).
   - `onConfirmCancelAdmission(payload: { reason: string, voidSource?: boolean })` meneruskan `voidSource: payload.voidSource ?? true` ke `cancelAdmissionMutation.mutateAsync`.
3. Berkas [AdmisiRanapConsole.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/views/AdmisiRanapConsole.vue) diperbarui:
   - Meneruskan prop `:has-source="cancelAdmissionState.hasSource"` dan `:source-kind-label="cancelAdmissionState.sourceKindLabel"` ke `CoordinatedCancelDialog` untuk pembatalan registrasi.
4. Pengujian unit di [useJourneyActionHost.spec.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/__tests__/useJourneyActionHost.spec.ts) ditambahkan / diperbarui:
   - Memverifikasi saat `onConfirmCancelAdmission` dipanggil dengan `voidSource: true`, mutasi API dipanggil dengan `voidSource: true`.
   - Memverifikasi saat dipanggil dengan `voidSource: false`, mutasi API dipanggil dengan `voidSource: false`.
5. Seluruh pengujian unit berjalan sukses via perintah:  
   `pnpm --dir d:\project_aktif\project_MyHospitalWeb\c012_myhospital_web test src/modules/Admisi/composables/__tests__/useJourneyActionHost.spec.ts --run`

Notes:  
Kotak centang harus tercentang secara default saat dialog terbuka, dan styling penanda peringatan dibuat kontras agar petugas tidak melewatkan opsi tersebut.

Implementation Notes:  
- Memperbarui `CoordinatedCancelDialog.vue`:
  - Menambahkan props `hasSource?: boolean`, `sourceKindLabel?: string`, dan `defaultVoidSource?: boolean` (default `true`).
  - Menambahkan alert container bergaya kontras tinggi (amber border/background) dengan `Checkbox` Reka UI saat `hasSource: true`. Label berbobot tebal (`font-semibold`) dan deskripsi eksplisit sesuai kebutuhan bisnis.
  - Menginisialisasi dan me-reset `voidSource` state ke `defaultVoidSource ?? true` saat dialog dibuka (`watch(props.open)`).
  - Mengirim payload `{ reason, voidSource }` pada event `confirm`.
  - Menyembunyikan opsi checkbox saat `hasSource: false`.
- Memperbarui `useJourneyActionHost.ts`:
  - Menambahkan field `hasSource: boolean` dan `sourceKindLabel: string` pada state `cancelAdmissionState`.
  - Menerima opsi `selectedJourneyDetail` dan mengekstrak jenis dokumen sumber dari `detail.systemAudit` (`opnameRequestId` -> 'Permintaan Opname', `reservationId` -> 'Reservasi') maupun fallback dari `intent`.
  - Meneruskan `voidSource: payload.voidSource ?? true` pada payload pemanggilan `cancelAdmissionMutation.mutateAsync`.
- Memperbarui `AdmisiRanapConsole.vue`:
  - Meneruskan `selectedJourneyDetail: journeyDetailQuery.data` ke composable `useJourneyActionHost`.
  - Meneruskan `:has-source="cancelAdmissionState.hasSource"` dan `:source-kind-label="cancelAdmissionState.sourceKindLabel"` ke `CoordinatedCancelDialog` untuk pembatalan registrasi.
- Memperbarui dan melengkapi pengujian unit:
  - Memperbarui `useJourneyActionHost.spec.ts` dengan penambahan skenario:
    - Verifikasi pemanggilan mutasi dengan `voidSource: true` (eksplisit) dan `voidSource: false`.
    - Ekstraksi sumber `opnameRequestId` ('Permintaan Opname') dan `reservationId` ('Reservasi') dari `selectedJourneyDetail`.
    - Verifikasi fallback eksplisit pada intent serta reset state pada penutupan dan konfirmasi dialog.
  - Membuat unit test suite baru `CoordinatedCancelDialog.spec.ts` (6 skenario): render judul/konsekuensi/aktor, sembunyikan checkbox saat `hasSource: false`, tampilkan opsi checkbox + peringatan saat `hasSource: true`, emit `confirm` dengan `voidSource: true` default, emit `confirm` dengan `voidSource: false` saat uncheck, dan reset state saat dialog terbuka kembali.
- Seluruh 33 unit test di kedua berkas test lulus 100%, dan pemeriksaan tipe TypeScript via `pnpm run type-check` (`vue-tsc`) lulus tanpa error.

Changed Files:  
- `c012_myhospital_web/src/modules/Admisi/components/ranap/CoordinatedCancelDialog.vue`
- `c012_myhospital_web/src/modules/Admisi/composables/useJourneyActionHost.ts`
- `c012_myhospital_web/src/modules/Admisi/views/AdmisiRanapConsole.vue`
- `c012_myhospital_web/src/modules/Admisi/composables/__tests__/useJourneyActionHost.spec.ts`
- `c012_myhospital_web/src/modules/Admisi/components/ranap/__tests__/CoordinatedCancelDialog.spec.ts`

---

# 6. Change Log

- 2026-11-03: Pembuatan awal dokumen rencana implementasi (`PLAN-ADM-RANAP-VOID-REG-SOURCE-001`) berdasarkan Feasibility Assessment `FA-ADM-RANAP-VOID-REG-SOURCE-001` (Status: READY-FOR-PLANNING, ARCHITECTURE-NOT-REQUIRED). Execution Approval diberikan (`APPROVED`).
