# BUG-INVESTIGATION

## 1. Investigation Information

Investigation ID: INV-BILREG-API-SWAGGER-CONTRACT-001  
Associated Issue: [BILREG-API-SWAGGER-AND-CONTRACT-DEFECTS-BUG-ISSUE.md](file:///d:/KERJAAN/4.MyHospitalWeb/b21-myhosweb-system/Issues/BILREG-API-SWAGGER-AND-CONTRACT-DEFECTS-BUG-ISSUE.md) (`ISSUE-BILREG-API-SWAGGER-CONTRACT-001`)  
Status: READY FOR PLANNING  
Created At: 2026-11-05 09:25:00 +07:00  
Updated At: 2026-11-05 09:25:00 +07:00  

---

## 2. Problem Statement

Selama proses audit dan eksekusi test runner pada codebase API `b09-bilreg-api`, teridentifikasi serangkaian kegagalan sistemik yang mencakup:
1. Endpoint dokumentasi OpenAPI (`/openapi/v1.json`) mengalami exception HTTP 500 saat diakses, melumpuhkan dokumentasi interaktif Swagger/Scalar dan menggagalkan test ekspor spesifikasi.
2. Endpoint otentikasi login pada pengujian regresi JWT mengembalikan HTTP 404 akibat perubahan routing controller.
3. Kontrak endpoint pada konteks Tata Rekening menolak request valid dengan HTTP 400 karena ketidaksesuaian anotasi model binding `[Required]`, serta perlindungan otorisasi yang tidak aktif.
4. Model domain agregat antrian (`AntrianMapModel`) tidak menginisialisasi slot saat dibuat dari entitas jadwal, menghasilkan total 0 slot.
5. Inkoherensi data fixture waktu pada pengujian IGD yang melanggar invariant domain `CheckOut < CheckIn`.

**Modul Terdampak**:
- `Bilreg.Api` (Presentation layer: controller routing, kontrak request DTO, Swagger generation)
- `Bilreg.Domain` (Admisi Antrian: agregat seeding)
- `Bilreg.Test` (Integration test, authentication regression, IGD handler tests)

---

## 3. Current Behavior

1. **Swagger / OpenAPI Generation**:
   Pemanggilan `GET /openapi/v1.json` melempar `Swashbuckle.AspNetCore.SwaggerGen.SwaggerGeneratorException` dengan pesan:
   `Ambiguous HTTP method for action - Bilreg.Api.Controllers.ChargeContext.TindakanController.CreateTindakan (Bilreg.Api). Actions require an explicit HttpMethod binding for Swagger/OpenAPI 3.0`.
2. **Endpoint Otentikasi**:
   Pemanggilan `POST /login` merespons HTTP 404 Not Found karena aksi berada di path `/api/User/login`.
3. **Kontrak Tata Rekening**:
   Pemanggilan `POST /api/tatarekening/{regId}/adjust` tanpa payload `userId` mengembalikan HTTP 400 Bad Request, dengan error validasi `The UserId field is required.`, meskipun token JWT membawa identitas user. Controller `TataRekeningController` juga dapat diakses tanpa header Authorization pada method tertutup karena `[Authorize]` dikomentari.
4. **Antrian Map**:
   Pemanggilan `AntrianMapModel.CreateFromJadwal(jadwal, tgl)` menghasilkan model dengan `TotalSlotCount == 0`, menyebabkan pencarian slot nomor urut melempar `InvalidOperationException: Sequence contains no matching element`.
5. **IGD CheckIn/CheckOut**:
   Eksekusi handler void, transfer bed, dan discharge pada IGD melempar `ArgumentException: CheckOutDateTime (03/05/2025 10:15:30) tidak boleh lebih awal dari CheckInDateTime (01/01/2026 08:00:00)`.

---

## 4. Expected Behavior

1. Endpoint `/openapi/v1.json` berhasil merender seluruh spesifikasi OpenAPI 3.0 dengan HTTP 200 tanpa error ambiguitas.
2. Endpoint otentikasi menerima request login baik melalui `/api/user/login` maupun `/login`.
3. Kontrak Tata Rekening mengekstraksi `userId` dari klaim JWT apabila tidak disertakan dalam body JSON, dan tidak gagal validasi model binding 400.
4. `AntrianMapModel.CreateFromJadwal()` otomatis melakukan pemanggilan seeding slot sesuai konfigurasi pola dan kapasitas `MaxPasien`.
5. Data pengujian IGD menggunakan timestamp clock provider yang koheren (CheckOut >= CheckIn).

---

## 5. Investigation Findings

### Finding 1: Method Alias Tanpa Anotasi pada `TindakanController.cs`
Pada file `src/bilreg/Bilreg.Api/Controllers/ChargeContext/TindakanController.cs`:
- Baris 31: `public Task<IActionResult> CreateTindakan(TdkCreateTindakanCmd cmd) => Create(cmd);`
- Baris 60: `public Task<IActionResult> UpdatePetugasMedis([FromBody] TindakanUpdatePetugasRequest req) => UpdatePetugas(req);`
- Baris 71: `public Task<IActionResult> GetTindakan(string id) => Get(id);`
- Baris 91: `public Task<IActionResult> ListTindakanJual(string regId) => ListTdkJual(regId);`

Dalam arsitektur ASP.NET Core, setiap method `public` pada kelas turunan `Controller`/`ControllerBase` yang tidak dianotasi `[NonAction]` secara default diperlakukan sebagai Action endpoint. Karena method-method di atas tidak memiliki atribut verb (`[HttpGet]`, `[HttpPost]`), Swashbuckle gagal menentukan metode HTTP dan membatalkan pembuatan skema OpenAPI.

### Finding 2: Perubahan Prefix Route `UserController.cs`
Pada commit `e1d548e30232da4deb7e1a6db2bb293e7cfb4174`:
```diff
+ [Route("api/[controller]")]
+ [ApiController]
  public class UserController : Controller
```
Aksi `Login` memiliki anotasi `[Route("login")]`. Akibat penambahan prefix controller, URL efektif bergeser dari `/login` menjadi `/api/User/login`. `JwtAuthenticationRegressionTest` yang mengeksekusi `POST /login` tidak disesuaikan atau tidak disediakan fallback route.

### Finding 3: Inkonsistensi Anotasi Validasi `TataRekeningRequests.cs`
Pada file `src/bilreg/Bilreg.Api/Controllers/PaymentContext/TataRekeningFeature/Contracts/TataRekeningRequests.cs`:
```csharp
public record FinancialVerificationRequest(
    [Required] FinancialVerificationAction Action,
    DateTime? VerifiedAt = null, [Required, MinLength(1)] string UserId = null);

public record FinancialAdjustmentRequest(
    [Required] FinancialAdjustmentInputDto Adjustment,
    DateTime? AppliedAt = null, [Required, MinLength(1)] string UserId = null);
```
Parameter `UserId = null` diberi anotasi `[Required]`. Kompilator C# mengeluarkan warning `CS8625: Cannot convert null literal to non-nullable reference type`. Pada saat runtime, deserializer JSON menyetel nilai `UserId` menjadi `null` bila field tidak ada di JSON, sehingga validator model ASP.NET Core memicu 400 Bad Request. Selain itu, pada `TataRekeningController.cs` baris 15, atribut `[Authorize]` sengaja dikomentari.

### Finding 4: Inisialisasi Slot Kosong pada `AntrianMapModel.cs`
Pada file `src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianMapModel.cs`:
```csharp
public static AntrianMapModel CreateFromJadwal(JadwalPraktekType jadwal, DateOnly tgl)
{
    var newKey = NunaId.New("ANM");
    var result = new AntrianMapModel(
        antrianMapId: newKey,
        jadwalId    : jadwal.JadwalPraktekId,
        dokter      : jadwal.Dokter,
        layanan     : jadwal.Layanan,
        tglJadwal   : tgl,
        jamJadwal   : jadwal.JamMulai,
        jamPraktek  : jadwal.JamMulai,
        pattern     : jadwal.AntrianPattern,
        maxPasien   : jadwal.MaxPasien,
        listMap: []
    );
    // Method SeedingMap() TIDAK DIPANGGIL sebelum return!
    return result;
}
```
Ketika unit test `AntrianMapModelTest` memanggil `model.ListMap.Single(...)`, koleksi kosong sehingga melempar exception LINQ.

### Finding 5: Ketidakselarasan Waktu Uji `TestTglJamProvider`
Pada `Bilreg.Test/Shared/TestTglJamProvider.cs`:
- Nilai waktu `Now` dipatok ke `2025-05-03 10:15:30`.
Pada `IgdVisitVoidHandlerTest.cs` dan sejenisnya:
- Fixture data visit di-create dengan `new DateTime(2026, 1, 1, 8, 0, 0)`.
Ketika method handler memanggil pembatalan/transfer dengan timestamp provider, aturan domain membandingkan `CheckOut (2025)` dengan `CheckIn (2026)` dan menolak transaksi.

---

## 6. Root Cause Candidates

### RC-01: Method Action Ambigius pada `TindakanController.cs`
- **Status**: CONFIRMED
- **Evidence**: Stack trace `Swashbuckle.AspNetCore.SwaggerGen.SwaggerGeneratorException` pada `ApotekOpenApiExportTest` secara eksplisit menyebut `Bilreg.Api.Controllers.ChargeContext.TindakanController.CreateTindakan`. Kode sumber mengonfirmasi 4 method public tanpa HTTP binding dan tanpa `[NonAction]`.

### RC-02: Perubahan Base Route `UserController` Mematahkan Kontrak `/login`
- **Status**: CONFIRMED
- **Evidence**: Riwayat git commit `e1d548e3` menambahkan `[Route("api/[controller]")]`. Uji `JwtAuthenticationRegressionTest.LoginAndGetTokenAsync` mengeksekusi `POST /login` dan memperoleh HTTP 404.

### RC-03: Anotasi `[Required]` Bersifat Mutually Exclusive dengan Nilai Default Null
- **Status**: CONFIRMED
- **Evidence**: Compiler warning CS8625 pada baris 12 dan 33 `TataRekeningRequests.cs` serta kegagalan 7 test case di `TataRekeningApiIntegrationTest` dengan status 400 Bad Request.

### RC-04: Hilangnya Pemanggilan `SeedingMap()` pada Factory Method `AntrianMapModel`
- **Status**: CONFIRMED
- **Evidence**: Baris 49 `AntrianMapModel.cs` mengoper `listMap: []` ke konstruktor tanpa memanggil method `SeedingMap()` sebelum mengembalikan `result`. 17 unit test seeding gagal membuktikan slot kosong.

### RC-05: Regresi Timestamp Mock Pengujian IGD
- **Status**: CONFIRMED
- **Evidence**: Test fixture IGD memakai tahun 2026 sedangkan `TestTglJamProvider.Instance` memakai tahun 2025, memicu error validasi domain tanggal.

---

## 7. Missing Evidence

Tidak ada bukti yang tertunda (*All evidence closed*). Seluruh akar masalah telah dikonfirmasi langsung melalui baris kode dan eksekusi test runner.

---

## 8. Fix Options

### FO-01: Remediasi `TindakanController.cs`
- **Opsi A**: Berikan atribut `[NonAction]` pada `CreateTindakan`, `UpdatePetugasMedis`, `GetTindakan`, dan `ListTindakanJual`.
- **Opsi B**: Hapus method alias tersebut dan arahkan langsung ke method utama (`Create`, `UpdatePetugas`, `Get`, `ListTdkJual`).
- **Analisis**: Opsi A mempertahankan kompatibilitas jika ada pemanggil internal berbasis class instance, sedangkan Opsi B lebih bersih jika method tidak digunakan di luar controller. Rekomendasi: Gunakan `[NonAction]` untuk keamanan instan tanpa breaking changes internal.

### FO-02: Remediasi Route Otentikasi `UserController.cs`
- **Opsi A**: Tambahkan route eksplisit `[HttpPost("/login")]` di atas aksi `Login` di samping `[HttpPost("login")]`.
- **Opsi B**: Ubah seluruh test dan klien untuk memanggil `/api/user/login`.
- **Analisis**: Opsi A memberikan backwards compatibility untuk klien web, gateway, serta unit test tanpa resiko regresi.

### FO-03: Remediasi Kontrak `TataRekeningRequests.cs`
- **Opsi A**: Ubah `string? UserId = null` tanpa `[Required]`, dan pada controller lakukan fallback: `var uid = !string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : (User.Identity?.Name ?? "admin");`. Aktifkan kembali `[Authorize]` pada `TataRekeningController`.
- **Analisis**: Opsi A sesuai dengan pola controller lain (seperti `TindakanController`) dan mengizinkan token claims mengisi identitas user.

### FO-04: Remediasi Seeding `AntrianMapModel.cs`
- **Opsi**: Pada `AntrianMapModel.CreateFromJadwal()` dan `CreateFromEffective()`, panggil `result.SeedingMap();` sebelum `return result;`.

### FO-05: Remediasi Fixture Tanggal IGD Test
- **Opsi**: Buat instance `TestTglJamProvider` lokal pada konstruktor test IGD dengan tanggal yang sesuai (`new DateTime(2026, 1, 1, 12, 0, 0)`), melampaui waktu CheckIn `2026-01-01 08:00:00`.

---

## 9. Recommended Fix & Decisions

1. **ChargeContext.TindakanController**: Pasang atribut `[NonAction]` pada baris 31, 60, 71, dan 91.
2. **UserController**: Tambahkan atribut `[HttpPost("/login")]` pada aksi `Login`.
3. **PaymentContext (Tata Rekening)**: Hapus `[Required]` dari properti `UserId` pada DTO request kontrak Tata Rekening, dukung fallback identitas dari JWT, dan aktifkan kembali `[Authorize]` pada `TataRekeningController`.
4. **AdmisiContext (Antrian)**: Tambahkan `result.SeedingMap();` pada factory method `AntrianMapModel.CreateFromJadwal` dan `CreateFromEffective`.
5. **IgdContext (Test Suite)**: Sesuaikan timestamp `TestTglJamProvider` pada kelas uji IGD agar tidak mendahului tanggal check-in visit.
6. **TrsBilling2DtoTest**: Perbaiki string format pembanding dari `yyyy-mm-dd` menjadi `yyyy-MM-dd`.

Artifact ini siap dikonsumsi oleh Architect / Developer untuk pembuatan `IMPLEMENTATION-PLAN` dan eksekusi perbaikan.
