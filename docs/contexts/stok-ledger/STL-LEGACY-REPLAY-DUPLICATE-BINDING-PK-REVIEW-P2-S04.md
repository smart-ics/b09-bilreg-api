---
Title: Review Report P2-S04 — Penyeragaman Handler Konsekuensi Stock Ledger (Bebas Thread.Sleep, Depleting tb_stok Multi-Baris, Idempotensi Multi-Item, dan Baris Bonus DO)
Code: STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK
Artifact: REVIEW-REPORT
Slice: P2-S04
ReviewIteration: 1
Verdict: GO
Date: 2026-11-20
---

# Review Summary — P2-S04

- **Worktree**: `E:\PROJECT\ICS\PROJECT-ACTIVE\MHW\b09-bilreg-api-worktree-stl-s04`
- **Branch**: `fix/stl-p2-s04`
- **Reviewed Commit**: `62e07e7dc266185b8e9f06d9dba10adefdb27917`
- **Verdict**: `GO`

# Completion Criteria Verification

1. **Penghapusan `Thread.Sleep(5)`**: Seluruh pemanggilan `Thread.Sleep(5)` pada `PostSaleIssueConsequenceCommand.cs`, `PostSaleVoidConsequenceCommand.cs`, `PostInternalConsumptionConsequenceCommand.cs`, `PostInternalConsumptionVoidConsequenceCommand.cs`, dan `PostStockTransferConsequenceCommand.cs` telah dihapus (terverifikasi 0 kemunculan `Thread.Sleep` pada `StockLedgerFeature/UseCases/`) dan diganti dengan `MonotonicNunaId.NewLegacyCompact("BK", watermarkToBeat)` serta `MonotonicNunaId.NewLegacyCompact("ST")`. — **PASS**
2. **Pemotongan `tb_stok` Multi-Baris**: `PostStockTransferConsequenceHandler` dan `PostDispenseIssueConsequenceHandler` mengganti `ResolveSourceLegacyStokId` dengan pemotongan lintas beberapa baris `tb_stok` (`legacyBalances.Sum(b => b.QtySisa) >= line.QtyAllocated`) yang diurutkan berdasarkan `LegacyStokId` secara ordinal. — **PASS**
3. **Idempotensi Multi-Item**: `PostSaleIssueConsequenceHandler` dan `PostDispenseIssueConsequenceHandler` memfilter pemeriksaan mutasi idempoten berdasarkan `x.BrgId == request.BrgId`. — **PASS**
4. **Baris Bonus DO**: `PostGoodsReceiptConsequenceCommand` mendukung `bool AllowAdditiveLine = false`; `DeliveryConfirmCommand` dan `DeliveryDirectConfirmCommand` melacak `(BrgId, TglEd)` dan mengirim `AllowAdditiveLine: true` untuk baris kedua dan seterusnya. — **PASS**
5. **Unit Test**: Seluruh 8 kelas `Post*ConsequenceHandlerTest` beserta `DeliveryConfirmCommandTest` dan `DeliveryDirectConfirmCommandTest` lulus (116 test, 0 gagal). — **PASS**

# Independent Test Execution

Command (with temporary `global.json` SDK `8.0.417` removed after run):
```powershell
dotnet test src\bilreg\Bilreg.Test\Bilreg.Test.csproj --filter "FullyQualifiedName~PostGoodsReceiptConsequenceHandlerTest|FullyQualifiedName~PostStockTransferConsequenceHandlerTest|FullyQualifiedName~PostDispenseIssueConsequenceHandlerTest|FullyQualifiedName~PostSaleIssueConsequenceHandlerTest|FullyQualifiedName~PostSaleVoidConsequenceHandlerTest|FullyQualifiedName~PostSalesReturnConsequenceHandlerTest|FullyQualifiedName~PostInternalConsumptionConsequenceHandlerTest|FullyQualifiedName~PostInternalConsumptionVoidConsequenceHandlerTests|FullyQualifiedName~DeliveryConfirmCommandTest|FullyQualifiedName~DeliveryDirectConfirmCommandTest" --blame-hang-timeout 30s -p:UseSharedCompilation=false /nodeReuse:false
```

Result:
- `Passed! - Failed: 0, Passed: 116, Skipped: 0, Total: 116, Duration: 7 s - Bilreg.Test.dll (net8.0)`
