---
Title: Review Report P2-S03 — Pengurutan Deterministik LegacyStockReadPort dan Batas Transaksi Atomik pada LegacyScopeJournalReplayer, Hydrate, serta CatchUp
Code: STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK
Artifact: REVIEW-REPORT
Slice: P2-S03
ReviewIteration: 1
Verdict: GO
Date: 2026-11-20
---

# Review Summary — P2-S03

- **Worktree**: `E:\PROJECT\ICS\PROJECT-ACTIVE\MHW\b09-bilreg-api-worktree-stl-s03`
- **Branch**: `fix/stl-p2-s03`
- **Reviewed Commit**: `aa1188ab680d6c4a794a86d2fa3af90633f39f15`
- **Verdict**: `GO`

# Completion Criteria Verification

1. **`LegacyStockReadPort.ListJournals`**: Mengurutkan hasil dengan `.OrderBy(x => x.TglMutasi).ThenBy(x => x.QtyIn > 0 ? 0 : 1).ThenBy(x => x.LegacyBukuId, StringComparer.Ordinal)`. — **PASS**
2. **`LegacyScopeJournalReplayer.Apply` (Buffering Memori)**: Mengumpulkan `pendingMutasi` dan `pendingBindings` selama iterasi jurnal, dan hanya menyisipkan ke `_mutasiRepo` dan `_bindingRepo` setelah seluruh iterasi jurnal dalam scope tervalidasi tanpa kegagalan. — **PASS**
3. **`LegacyScopeJournalReplayer.Apply` (Orphan Binding Recovery)**: Ketika jurnal sudah memiliki binding di `_bindingRepo` sementara `batch` belum ada di `_batchRepo`, replayer membentuk/memperbarui `batch` dan `lokasi` di memori (`rebuildingMissingBatch = true`) tanpa menyisipkan ulang mutasi/binding yang sudah ada. — **PASS**
4. **Batas Transaksi Atomik**: `HydrateScopeFromLegacyHandler` dan `CatchUpScopeFromLegacyHandler` membungkus replay dan persistensi dalam `using var trans = TransHelper.NewScope(); ... trans.Complete();`. — **PASS**
5. **Unit & Integration Tests**: `HydrateScopeFromLegacyHandlerTest`, `CatchUpScopeFromLegacyHandlerTest`, dan `LegacyStockReadPortTest` mencakup seluruh skenario (banyak baris beruntun, masuk-keluar pada detik sama, multi-baris `DO`/`MT_OUT`/`MT_IN`/`DU`, kegagalan jurnal ke-$k$ tanpa sisa parsial, serta pemulihan orphan binding). — **PASS**

# Independent Test Execution

Command (with temporary `global.json` SDK `8.0.417` removed after run):
```powershell
dotnet test src\bilreg\Bilreg.Test\Bilreg.Test.csproj --filter "FullyQualifiedName~HydrateScopeFromLegacyHandlerTest|FullyQualifiedName~CatchUpScopeFromLegacyHandlerTest|FullyQualifiedName~LegacyStockReadPortTest" --blame-hang-timeout 30s -p:UseSharedCompilation=false /nodeReuse:false
```

Result:
- `Passed! - Failed: 0, Passed: 35, Skipped: 0, Total: 35, Duration: 1 s - Bilreg.Test.dll (net8.0)`
