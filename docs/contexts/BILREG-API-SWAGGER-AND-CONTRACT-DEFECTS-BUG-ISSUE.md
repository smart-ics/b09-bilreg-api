# ISSUE

## Metadata

ID: ISSUE-BILREG-API-SWAGGER-CONTRACT-001
Type: BUG
Status: Open
Title: Kegagalan OpenAPI/Swagger Generation (HTTP 500), Regresi Route Login (HTTP 404), dan Defek Kontrak Controller pada Bilreg.Api

## Source

Reported By: Codebase Audit & Test Runner Verification (`b09-bilreg-api`)
Reported Date: 2026-11-05

## Description

Pada repositori backend API `b09-bilreg-api`, pengujian fungsionalitas dan pemanggilan dokumen OpenAPI (`/openapi/v1.json`) mengalami kegagalan fatal (HTTP 500). Setelah dilakukan penelusuran build dan eksekusi test runner, ditemukan beberapa anomali dan defek kontrak pada controller, routing, dan domain seeding:

1. **Swagger / OpenAPI Generation Crash (HTTP 500)**:
   Pada `Bilreg.Api.Controllers.ChargeContext.TindakanController`, terdapat beberapa method `public` alias (`CreateTindakan`, `UpdatePetugasMedis`, `GetTindakan`, `ListTindakanJual`) yang tidak memiliki atribut HTTP verb eksplisit (`[HttpPost]`, `[HttpGet]`) maupun attribute `[NonAction]`. Akibatnya Swashbuckle mendeteksi ambiguitas aksi dan melempar `SwaggerGeneratorException`, membuat seluruh dokumentasi Swagger/Scalar tidak dapat diakses dan test integrasi API gagal.

2. **Regresi Route Login (HTTP 404)**:
   Pada `Bilreg.Api.Controllers.UserController`, penambahan attribute controller-level `[Route("api/[controller]")]` mengubah path endpoint otentikasi dari `/login` menjadi `/api/User/login`. Klien API lama maupun tes regresi otentikasi (`JwtAuthenticationRegressionTest`) yang mengakses `POST /login` mengalami HTTP 404 Not Found.

3. **Defek Kontrak Request Tata Rekening (`UserId = null` dengan `[Required]`)**:
   Pada `Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature.Contracts.TataRekeningRequests.cs`, beberapa record contract (`FinancialVerificationRequest`, `FinancialAdjustmentRequest`, `FinalizeFinancialResponsibilityRequest`, `SettlementInitiationRequest`) dideklarasikan dengan parameter `[Required, MinLength(1)] string UserId = null`. Penetapan nilai default `null` pada properti bertanda `[Required]` menyebabkan ModelState validation ASP.NET Core otomatis menolak payload yang tidak menyertakan `userId` secara eksplisit dengan HTTP 400 Bad Request, mengabaikan resolusi user dari JWT claim. Selain itu, anotasi `[Authorize]` pada `TataRekeningController` dinonaktifkan (`//[Authorize]`).

4. **Defek Domain Seeding Antrian**:
   Pada `Bilreg.Domain.AdmisiContext.AntrianFeature.AntrianMapModel.cs`, method pabrik `CreateFromJadwal()` menginisialisasi objek model dengan list slot kosong (`listMap: []`) namun tidak memanggil `result.SeedingMap()`, sehingga map antrian yang baru dibuat selalu memiliki 0 slot antrian.

5. **Inkoherensi Timestamp Provider Pengujian IGD**:
   Pada unit test IGD (`IgdVisitVoidHandlerTest`, `IgdTransferBedHandlerTest`, `IgdVisitDischargeHandlerTest`), data uji visit di-setup dengan CheckIn `01/01/2026`, namun `TestTglJamProvider.Instance` di-hardcode ke `03/05/2025`. Hal ini memicu pelanggaran invariant domain: `CheckOutDateTime tidak boleh lebih awal dari CheckInDateTime`.

6. **Defek Typo Pemformatan Tanggal Unit Test**:
   Pada `Bilreg.Test.PaymentContext.TrsBillingFeature.TrsBilling2DtoTest`, assertion membandingkan tanggal dengan format `yyyy-mm-dd` (menit) alih-alih `yyyy-MM-dd` (bulan).

## Desired Outcome

1. **Stabilitas OpenAPI/Swagger**: Endpoint `/openapi/v1.json` dan antarmuka dokumentasi Scalar/Swagger dapat di-generate dengan sukses (HTTP 200) tanpa exception ambiguitas aksi.
2. **Kompatibilitas Endpoint Otentikasi**: Endpoint login dapat diakses secara stabil baik melalui route standar `/api/user/login` maupun route legacy `/login`.
3. **Validasi Kontrak Bersih**: Request contract pada Tata Rekening menerima userId opsional dari payload atau mengisi secara transparan dari konteks klaim pengguna login (`User.Identity.Name`) tanpa gagal validasi 400. `TataRekeningController` terlindungi kembali dengan atribut otorisasi yang tepat.
4. **Keberhasilan Seeding Antrian**: Objek `AntrianMapModel` yang dibuat melalui `CreateFromJadwal()` otomatis memiliki slot antrian yang ter-seeding sesuai kapasitas `MaxPasien` dan aturan pola antrian (`Pattern`).
5. **Integritas Suite Pengujian**: Seluruh test unit dan integrasi domain yang tidak bergantung pada database eksternal dapat dieksekusi dengan status PASS secara deterministik.

## Current Situation

- Endpoint `/openapi/v1.json` merespons HTTP 500 (`Swashbuckle.AspNetCore.SwaggerGen.SwaggerGeneratorException`).
- `JwtAuthenticationRegressionTest` gagal karena pemanggilan `POST /login` menghasilkan HTTP 404.
- Pengujian `TataRekeningApiIntegrationTest` gagal pada 7 skenario (menghasilkan HTTP 400 Bad Request dan HTTP 200 saat unauthenticated).
- Pengujian `AntrianMapModelTest` gagal pada 17 test case karena jumlah slot ter-seeding adalah 0.
- Pengujian handler IGD gagal melempar `ArgumentException` terkait tanggal checkout mendahului checkin.
- Sebanyak 206 test case pada `Bilreg.Test` berstatus FAIL saat eksekusi `dotnet test`.

## Evidence

1. **Log Exception Swagger Generator**:
   ```text
   Swashbuckle.AspNetCore.SwaggerGen.SwaggerGeneratorException: Ambiguous HTTP method for action - Bilreg.Api.Controllers.ChargeContext.TindakanController.CreateTindakan (Bilreg.Api). Actions require an explicit HttpMethod binding for Swagger/OpenAPI 3.0
      at Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenerator.GenerateOperations(IEnumerable`1 apiDescriptions, SchemaRepository schemaRepository)
      at Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenerator.GeneratePaths(IEnumerable`1 apiDescriptions, SchemaRepository schemaRepository)
      at Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenerator.GetSwaggerDocumentWithoutFilters(String documentName, String host, String basePath)
      at Swashbuckle.AspNetCore.Swagger.SwaggerMiddleware.Invoke(HttpContext httpContext, ISwaggerProvider swaggerProvider)
   ```
2. **Method Tanpa HTTP Binding pada `TindakanController.cs`**:
   - `Bilreg.Api/Controllers/ChargeContext/TindakanController.cs`: baris 31, 60, 71, 91.
3. **Kegagalan Assertion Seeding Slot Antrian**:
   ```text
   Bilreg.Test.AdmisiContext.AntrianFeature.AntrianMapModelTest+SeedingTests.CreateFromJadwal_ShouldSeedCyclically_WhenMaxPasienIs10
   Expected model.TotalSlotCount to be 10, but found 0 (difference of -10).
   ```
4. **Kegagalan Validasi Invariant Waktu IGD**:
   ```text
   Bilreg.Test.IgdContext.IgdVisitFeature.IgdVisitVoidHandlerTest.Handle_Clean_Voids_AndCascadesBedRelease
   System.ArgumentException : CheckOutDateTime (03/05/2025 10:15:30) tidak boleh lebih awal dari CheckInDateTime (01/01/2026 08:00:00).
   ```

## Notes

- Perbaikan pada `TindakanController.cs` hanya memerlukan penambahan atribut `[NonAction]` pada method alias atau penataan akses modifier.
- Perbaikan kontrak `TataRekeningRequests.cs` harus memastikan bahwa `UserId` dapat diturunkan dari JWT claims tanpa memaksakan input redundant dari payload request.
- Masalah DAL/SQL Server test (~130 test) memerlukan penyediaan koneksi database lokal yang dikonfigurasi lewat environment variable `BILREG_AQ_IT_SERVER` dan `BILREG_AQ_IT_DATABASE` dan berada di luar cakupan defek kode logika ini.
