---
Title: Review Report P1-S02 — Migrasi Indeks BILRG_StokMutasi ke Indeks Non-Unik dan Pembaruan Schema Fixture
Code: STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK
Artifact: REVIEW-REPORT
Slice: P1-S02
ReviewIteration: 1
Verdict: GO
Date: 2026-11-20
---

# Review Summary — P1-S02

- **Worktree**: `E:\PROJECT\ICS\PROJECT-ACTIVE\MHW\b09-bilreg-api-worktree-stl-s02`
- **Branch**: `fix/stl-p1-s02`
- **Reviewed Commit**: `66073cf21b22cb1106be0413c180fb2fe79790cb`
- **Verdict**: `GO`

# Completion Criteria Verification

1. **`BILRG_StokMutasi.sql`**: Memeriksa `sys.indexes` untuk `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId`, melakukan `DROP INDEX` jika ada, dan membuat indeks non-unik `IX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` pada `(TrsReffId, MovementKind, StokLokasiId)` jika belum ada (`CREATE TABLE` dijaga `IF OBJECT_ID('BILRG_StokMutasi', 'U') IS NULL`). — **PASS**
2. **`BILRG_StockLedgerV2_Rollback.sql`**: Menghapus `IX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` dan `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` jika ada. — **PASS**
3. **`StockLedgerV2SchemaFixture.EnsureSchema()`**: Menjalankan `ExecuteScript(conn, "BILRG_StokMutasi.sql")` secara langsung agar migrasi indeks tetap diterapkan walaupun tabel `BILRG_StokMutasi` sudah ada. — **PASS**
4. **`StokMutasiDalTest.cs`**: Mengganti test lama dengan `[Theory]` untuk `GoodsReceipt` (1), `TransferOut` (2), `TransferIn` (3), `SaleIssueDu` (5), dan `SalesReturnRu` (9) guna memverifikasi penyisipan dua baris dengan `(TrsReffId, MovementKind, StokLokasiId)` yang sama berhasil dan terbaca kembali lewat `ListByTrsReffId`. — **PASS**

# Independent Test Execution

Command (with temporary `global.json` SDK `8.0.417` removed after run):
```powershell
dotnet test src\bilreg\Bilreg.Test\Bilreg.Test.csproj --no-restore --filter "FullyQualifiedName~StokMutasiDalTest" --blame-hang-timeout 30s -p:UseSharedCompilation=false /nodeReuse:false
```

Result:
- `Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6, Duration: 2 s - Bilreg.Test.dll (net8.0)`
