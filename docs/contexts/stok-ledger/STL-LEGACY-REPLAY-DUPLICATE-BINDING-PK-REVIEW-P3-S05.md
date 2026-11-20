---
Title: Review Report P3-S05 — Pengamanan ID Monotonik, Konsolidasi Barang Kembar, dan Kebijakan Fallback-to-Pending pada TindakanBomProcessor serta PakaiBrgInternalService
Code: STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK
Artifact: REVIEW-REPORT
Slice: P3-S05
ReviewIteration: 1
Verdict: GO
Date: 2026-11-20
---

# Review Summary — P3-S05

- **Worktree**: `E:\PROJECT\ICS\PROJECT-ACTIVE\MHW\b09-bilreg-api-worktree-stl-s05`
- **Branch**: `fix/stl-p3-s05`
- **Reviewed Commit**: `a253419ab8509eb61940e048a7aca038395041f1`
- **Verdict**: `GO`

# Completion Criteria Verification

1. **`MonotonicNunaId` pada `TindakanBomProcessor` & `PakaiBrg`**: `TindakanBomProcessor.Process` menggunakan `MonotonicNunaId.New("TBG")` dan `MonotonicNunaId.New("PND")`; `PakaiBrgInternalService.CreateFromTindakan` dan `PakaiBrgCreateCommand` menggunakan `MonotonicNunaId.NewLegacyCompact("PK")`. — **PASS**
2. **Konsolidasi `BrgId` Kembar**: `immediateCutItems` dikelompokkan dan dijumlahkan berdasarkan `BrgId` (`GroupBy(x => x.Resolved.Bom.BrgId, StringComparer.OrdinalIgnoreCase)`). — **PASS**
3. **Freshness Gate *Fallback-to-Pending***: Freshness gate dijalankan per `BrgId`; ketika `EnsureFreshnessForScopesCommand` mengembalikan `AbortedInconsistent` atau melempar eksepsi, `TindakanBomProcessor` mencatat `LogWarning`, menetapkan saldo tersedia menjadi `0m`, dan meneruskan penyimpanan tindakan serta pencatatan `BILRG_TindakanStokPending`. — **PASS**
4. **Unit Test**: `TindakanBomProcessorTest`, `PakaiBrgInternalServiceTests`, `TdkCreateTindakanHandlerTest`, dan `TdkSaveTindakanHandlerTest` lulus (67 test, 0 gagal), termasuk skenario 3 barang BOM (`TBG`/`PND` unik), 2 baris BOM dengan `BrgId` sama, dan fallback ke `BILRG_TindakanStokPending` saat freshness gate `AbortedInconsistent`. — **PASS**

# Independent Test Execution

Command (with temporary `global.json` SDK `8.0.417` removed after run):
```powershell
dotnet test src\bilreg\Bilreg.Test\Bilreg.Test.csproj --filter "FullyQualifiedName~TindakanBomProcessorTest|FullyQualifiedName~PakaiBrgInternalServiceTests|FullyQualifiedName~TdkCreateTindakanHandlerTest|FullyQualifiedName~TdkSaveTindakanHandlerTest" --blame-hang-timeout 30s -p:UseSharedCompilation=false /nodeReuse:false
```

Result:
- `Passed! - Failed: 0, Passed: 67, Skipped: 0, Total: 67, Duration: 396 ms - Bilreg.Test.dll (net8.0)`
