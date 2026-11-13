---
Title: Bilreg.Api Swagger Ambiguity, Controller Contracts, and Domain Seeding Remediation Plan
Code: BILREG-API-SWAGGER-AND-CONTRACT-DEFECTS
Artifact: IMPLEMENTATION-PLAN
Version: 1.0.0
LastUpdated: 2026-11-05
Status: NOT-STARTED
Execution Approval: APPROVED
---

# 1. Objective

Remediasi sekumpulan defek presentation layer, kontrak routing, domain slot seeding, dan suite pengujian pada repositori `b09-bilreg-api`.

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- BUG: [BILREG-API-SWAGGER-AND-CONTRACT-DEFECTS-BUG-ISSUE.md](file:///d:/KERJAAN/4.MyHospitalWeb/b21-myhosweb-system/Issues/BILREG-API-SWAGGER-AND-CONTRACT-DEFECTS-BUG-ISSUE.md)
- BUG-INVESTIGATION: [BILREG-API-SWAGGER-AND-CONTRACT-DEFECTS-BUG-INVESTIGATION.md](file:///d:/KERJAAN/4.MyHospitalWeb/b21-myhosweb-system/Issues/BILREG-API-SWAGGER-AND-CONTRACT-DEFECTS-BUG-INVESTIGATION.md)

Architecture Applicability: ARCHITECTURE-NOT-REQUIRED
No architectural target-state artifact was required. Implementation relies on existing technical structure. Approved investigation decisions are authoritative for the change. The current codebase is the source of current technical truth.

---

# 2. Planning Scope

Perbaikan terkonsentrasi pada `src/bilreg` di repositori `b09-bilreg-api`:
1. `Bilreg.Api/Controllers/ChargeContext/TindakanController.cs`: penambahan atribut `[NonAction]`.
2. `Bilreg.Api/Controllers/UserController.cs`: pemulihan route root `/login`.
3. `Bilreg.Api/Controllers/PaymentContext/TataRekeningFeature/`: perbaikan DTO `TataRekeningRequests.cs` dan pengaktifan kembali `[Authorize]` pada `TataRekeningController.cs`.
4. `Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianMapModel.cs`: pemanggilan `SeedingMap()` pada pembuatan model agregat dari jadwal.
5. `Bilreg.Test/IgdContext/`: koreksi timestamp mock `TestTglJamProvider` pada pengujian handler IGD.
6. `Bilreg.Test/PaymentContext/TrsBillingFeature/TrsBilling2DtoTest.cs`: koreksi format string bulan `yyyy-MM-dd`.

---

# 3. Dependencies

- `Depends On` antar slice dideklarasikan secara spesifik.
- Slices pada Phase 1 (P1-S01, P1-S02, P1-S03) dan Phase 2 (P2-S04, P2-S05, P2-S06) saling independen dan dapat dieksekusi secara paralel.

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - API Presentation & Routing Fixes | NOT-STARTED | NOT-REVIEWED | 0/3 |
| P2 - Domain Logic & Test Harness Fixes | NOT-STARTED | NOT-REVIEWED | 0/3 |

---

# 5. Phases

## P1 - API Presentation & Routing Fixes

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P1-S01
Title: Fix Swagger/OpenAPI Action Ambiguity in TindakanController.cs  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Berikan atribut `[NonAction]` pada method alias `CreateTindakan`, `UpdatePetugasMedis`, `GetTindakan`, dan `ListTindakanJual` di `TindakanController.cs` agar Swashbuckle tidak mendeteksinya sebagai action tanpa HTTP verb.  
Depends On: None  
Repository: `D:\KERJAAN\4.MyHospitalWeb\b09-bilreg-api`  
Completion Criteria:
- Method alias diberi atribut `[NonAction]`.
- Pengujian `ApotekOpenApiExportTest.OpenApi_Snapshot_contains_apotek_paths_with_security_on_mutations` lolos tanpa HTTP 500.

### P1-S02
Title: Restore Legacy /login Route in UserController.cs  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Pasang atribut `[HttpPost("/login")]` di atas aksi `Login` pada `UserController.cs` sehingga endpoint login dapat diakses melalui path `/login` dan `/api/User/login`.  
Depends On: None  
Repository: `D:\KERJAAN\4.MyHospitalWeb\b09-bilreg-api`  
Completion Criteria:
- Route `/login` aktif melayani request POST otentikasi.
- Seluruh 3 test case di `JwtAuthenticationRegressionTest` lolos (Status 200).

### P1-S03
Title: Remediate Tata Rekening Request Contracts & Restore Controller Authorization  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Hapus atribut `[Required]` dari parameter `UserId` berkondisi default `null` pada `FinancialVerificationRequest`, `FinancialAdjustmentRequest`, `FinalizeFinancialResponsibilityRequest`, dan `SettlementInitiationRequest`. Aktifkan kembali anotasi `[Authorize]` pada `TataRekeningController.cs`.  
Depends On: None  
Repository: `D:\KERJAAN\4.MyHospitalWeb\b09-bilreg-api`  
Completion Criteria:
- Warning CS8625 pada `TataRekeningRequests.cs` hilang.
- Request adjust/verify tanpa explicit `userId` tidak lagi 400.
- `TataRekeningApiIntegrationTest` skenario auth dan adjust lolos.

---

## P2 - Domain Logic & Test Harness Fixes

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P2-S04
Title: Fix AntrianMapModel.CreateFromJadwal Missing Slot Seeding  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Panggil `result.SeedingMap();` pada factory method `CreateFromJadwal()` dan `CreateFromEffective()` di `AntrianMapModel.cs` sebelum mengembalikan instance agregat.  
Depends On: None  
Repository: `D:\KERJAAN\4.MyHospitalWeb\b09-bilreg-api`  
Completion Criteria:
- Pemanggilan `CreateFromJadwal` menghasilkan `TotalSlotCount` sesuai `MaxPasien`.
- Seluruh 17 test case pada `AntrianMapModelTest.SeedingTests` dan `SeedingAutoTests` lolos.

### P2-S05
Title: Fix IGD Test Fixture Clock Coherence  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Sesuaikan `TestTglJamProvider` yang digunakan pada `IgdVisitVoidHandlerTest`, `IgdTransferBedHandlerTest`, dan `IgdVisitDischargeHandlerTest` agar memiliki waktu >= waktu check-in data uji visit (misal `2026-01-01 10:00:00`).  
Depends On: None  
Repository: `D:\KERJAAN\4.MyHospitalWeb\b09-bilreg-api`  
Completion Criteria:
- Handler test IGD lolos tanpa melempar `ArgumentException: CheckOutDateTime tidak boleh lebih awal dari CheckInDateTime`.

### P2-S06
Title: Fix Date Format Typo in TrsBilling2DtoTest.cs  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Ubah pemformatan tanggal pada assertion baris 99 `TrsBilling2DtoTest.cs` dari `yyyy-mm-dd` menjadi `yyyy-MM-dd`.  
Depends On: None  
Repository: `D:\KERJAAN\4.MyHospitalWeb\b09-bilreg-api`  
Completion Criteria:
- `TrsBilling2DtoTest.FromModelFinalization_MapsPaymentKasirAndNilai` lolos evaluasi assertion tanggal.

---

# 6. Change Log

- 2026-11-05: Initial plan creation and approval for execution.
