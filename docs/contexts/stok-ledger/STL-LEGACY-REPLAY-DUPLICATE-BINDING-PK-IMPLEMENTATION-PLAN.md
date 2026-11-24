---
Title: Rencana Implementasi Koreksi Keandalan Replay Legacy, Pembangkitan ID Monotonik, dan Toleransi Kegagalan Pemotongan Stok BOM Tindakan
Code: STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-19
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Merealisasikan koreksi menyeluruh atas kegagalan pembuatan tindakan ber-BOM barang (`ISS-STL-001`) dan kerentanan produksi pada Stock Ledger v2 sesuai arsitektur target yang telah disetujui.

Planning Mode: FEATURE-PLANNING

Referenced artifacts:

- ISSUE (BUG): `ISS-STL-001` ([STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-BUG-ISSUE.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/stok-ledger/STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-BUG-ISSUE.md))
- BUG-INVESTIGATION: [STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-BUG-INVESTIGATION.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/stok-ledger/STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-BUG-INVESTIGATION.md)
- ARCHITECTURE: [STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-ARCHITECTURE.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/stok-ledger/STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-ARCHITECTURE.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Rencana implementasi ini mencakup perbaikan pada repositori backend `b09-bilreg-api` (mencakup komponen Stock Ledger v2 dan Delivery Order serta pemroses BOM tindakan `TindakanBomProcessor` dan `PakaiBrgInternalService` pada branch `feat/trk-tindakan-bom-potong-stok`):

1. Fase 1 (`P1-S01`, `P1-S02`) — Pondasi pembangkitan ID monotonik Base36 bebas `Thread.Sleep` (`MonotonicNunaId`) pada model domain, serta migrasi indeks `BILRG_StokMutasi` dari unique index menjadi non-unique index.
2. Fase 2 (`P2-S03`, `P2-S04`) — Atomisitas dan determinisme urutan replay jurnal legacy (`LegacyStockReadPort`, `LegacyScopeJournalReplayer`, `HydrateScopeFromLegacyCommand`, `CatchUpScopeFromLegacyCommand`), serta penyeragaman seluruh handler `Post*ConsequenceCommand` dan `DeliveryConfirmCommand` / `DeliveryDirectConfirmCommand` (bebas `Thread.Sleep`, pemotongan `tb_stok` multi-baris pada transfer/dispensing, idempotensi multi-item, dan pelestarian stok barang bonus DO).
3. Fase 3 (`P3-S05`) — Pengamanan `TindakanBomProcessor` dan `PakaiBrgInternalService` menggunakan `MonotonicNunaId` untuk `TBG`, `PND`, dan `PK`, konsolidasi baris BOM dengan `BrgId` sama, serta kebijakan *fallback-to-pending* (`BILRG_TindakanStokPending`) ketika freshness gate tidak konsisten atau gagal.

---

# 3. Dependencies

- Seluruh pekerjaan dilaksanakan di dalam repositori `b09-bilreg-api` (pada branch kerja yang memuat fitur pemotongan stok BOM tindakan `feat/trk-tindakan-bom-potong-stok` agar kelima slice dapat diverifikasi secara utuh).
- Aturan ketergantungan slice:
  - `Depends On` menyatakan prasyarat implementasi antar-slice.
  - Ketergantungan merujuk pada Slice ID di dalam dokumen ini.
  - Ketergantungan terpenuhi ketika slice yang dirujuk berstatus implementasi `IMPLEMENTED` dan output kodenya dapat diverifikasi di repositori.
  - `P1-S01` dan `P1-S02` tidak saling bergantung dan dapat dikerjakan secara paralel.
  - `P2-S03` dan `P2-S04` bergantung pada `P1-S01` dan `P1-S02`.
  - `P3-S05` bergantung pada `P1-S01`, `P2-S03`, dan `P2-S04`.

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 | IMPLEMENTED | GO | 2/2 |
| P2 | IMPLEMENTED | GO | 2/2 |
| P3 | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Foundation: Monotonic ID Generator and Database Index Migration

Implementation Status: IMPLEMENTED
Review Status: GO

### P1-S01

Title: Implementasi `MonotonicNunaId` Bebas `Thread.Sleep` dan Adopsi pada Model Domain Stock Ledger serta Pakai Barang

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Menyediakan pembangkit ID Base36 monotonik thread-safe (`MonotonicNunaId`) di `Bilreg.Domain` yang menjamin keunikan dan urutan leksikografis naik (`StringComparer.Ordinal`) tanpa `Thread.Sleep` untuk format 12-karakter (`New`) maupun 10-karakter (`NewLegacyCompact`, termasuk dukungan parameter opsional `minExclusiveId`), serta menerapkannya pada `StockBatchModel`, `LocationStockBalanceModel`, `StockMovementModel`, `StockLegacyBindingModel`, dan `PakaiBrgModel`.

Depends On: None

Repository: `b09-bilreg-api`

Completion Criteria:

1. Kelas `MonotonicNunaId` tersedia di `Bilreg.Domain` dengan metode `New(string prefix, string? minExclusiveId = null)` (12 karakter: 3 karakter prefix + 9 karakter Base36) dan `NewLegacyCompact(string prefix, string? minExclusiveId = null)` (10 karakter: 2 karakter prefix + 8 karakter Base36).
2. `StockBatchModel.Create`, `LocationStockBalanceModel.Create`, `StockMovementModel.CreateInbound`, `StockMovementModel.CreateOutbound`, `StockLegacyBindingModel.CreateMutasiBuku`, `StockLegacyBindingModel.CreateLokasiStok`, dan `PakaiBrgModel.Create` menggunakan `MonotonicNunaId`.
3. Unit test memverifikasi bahwa pemanggilan 1.000+ kali berturut-turut dalam satu loop memori untuk prefix yang sama (`SBD`, `STM`, `STL`, `STB`, `TBG`, `PND`, `BK`, `ST`, `PK`) menghasilkan 100% ID unik, panjang karakter tepat (12 atau 10), berurutan naik secara `StringComparer.Ordinal`, mampu melampaui `minExclusiveId` dalam satu panggilan, dan selesai tanpa `Thread.Sleep`.

Notes:

- Realisasi `TD-01` pada `STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-ARCHITECTURE.md`.

---

### P1-S02

Title: Migrasi Indeks `BILRG_StokMutasi` ke Indeks Non-Unik dan Pembaruan Schema Fixture

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Mengubah skrip DDL `BILRG_StokMutasi.sql`, `BILRG_StockLedgerV2_Rollback.sql`, `StockLedgerV2SchemaFixture.cs`, dan pengujian `StokMutasiDalTest.cs` agar menghapus indeks unik `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` (baik yang tanpa filter maupun dengan filter) dan menggantinya dengan indeks non-unik `IX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` secara idempoten.

Depends On: None

Repository: `b09-bilreg-api`

Completion Criteria:

1. `src/bilreg/Bilreg.SqlDb/InventoryContext/StockLedgerFeature/BILRG_StokMutasi.sql` memeriksa keberadaan `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` di `sys.indexes` dan melakukan `DROP INDEX`, lalu membuat indeks non-unik `IX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` pada `(TrsReffId, MovementKind, StokLokasiId)` jika belum ada.
2. `BILRG_StockLedgerV2_Rollback.sql` menghapus `IX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` maupun `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` jika ada.
3. `StockLedgerV2SchemaFixture.EnsureSchema()` memastikan migrasi indeks `BILRG_StokMutasi` tetap dijalankan walaupun tabel `BILRG_StokMutasi` sudah ada di database uji.
4. `StokMutasiDalTest` diperbarui untuk memverifikasi bahwa penyisipan dua baris mutasi dengan `(TrsReffId, MovementKind, StokLokasiId)` yang sama untuk berbagai `MovementKind` (termasuk `GoodsReceipt = 1`, `TransferOut = 2`, `TransferIn = 3`, `SaleIssueDu = 5`, `SalesReturnRu = 9`) berhasil disimpan dan dapat dibaca kembali melalui `ListByTrsReffId`.

Notes:

- Realisasi `TD-02` (bagian indeks database) dan `TD-06` pada `STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-ARCHITECTURE.md`.

---

## P2 - Core Stock Ledger Hardening: Atomic Deterministic Replay and Consequence Handlers

Implementation Status: IMPLEMENTED
Review Status: GO

### P2-S03

Title: Pengurutan Deterministik `LegacyStockReadPort` dan Batas Transaksi Atomik pada `LegacyScopeJournalReplayer`, `Hydrate`, serta `CatchUp`

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Memastikan pembacaan jurnal legacy dari `tb_buku` selalu mengurutkan mutasi masuk (`QtyIn > 0`) sebelum mutasi keluar (`QtyOut > 0`) pada `TglMutasi` yang sama, serta memastikan `LegacyScopeJournalReplayer`, `HydrateScopeFromLegacyHandler`, dan `CatchUpScopeFromLegacyHandler` memvalidasi seluruh baris scope di memori sebelum menulis ke database di dalam satu batas transaksi atomik (`TransHelper.NewScope()`).

Depends On: P1-S01, P1-S02

Repository: `b09-bilreg-api`

Completion Criteria:

1. `LegacyStockReadPort.ListJournals` mengurutkan hasil dengan `.OrderBy(x => x.TglMutasi).ThenBy(x => x.QtyIn > 0 ? 0 : 1).ThenBy(x => x.LegacyBukuId, StringComparer.Ordinal)`.
2. `LegacyScopeJournalReplayer.Apply` mengumpulkan `StockMovementModel` dan `StockLegacyBindingModel` yang baru di dalam koleksi memori selama iterasi jurnal, dan hanya menyisipkannya ke `_mutasiRepo` serta `_bindingRepo` ketika seluruh iterasi jurnal dalam scope tersebut berhasil divalidasi (tidak `IsInconsistent`).
3. Apabila pada awal `LegacyScopeJournalReplayer.Apply` ditemukan baris `journal` yang sudah memiliki binding di `_bindingRepo` sementara `batch` belum ada di `_batchRepo` (kondisi pemulihan dari binding yatim sebelumnya), replayer tetap membentuk/memperbarui `batch` dan `lokasi` di memori dari jurnal tersebut tanpa menyisipkan ulang mutasi/binding yang sudah ada, sehingga tidak lagi gagal dengan `bindings without batch`.
4. `HydrateScopeFromLegacyHandler` dan `CatchUpScopeFromLegacyHandler` membungkus penulisan hasil replay (`_batchRepo.SaveChanges`, penyisipan mutasi/binding, dan `_scopeRepo.SaveChanges`) di dalam `using var trans = TransHelper.NewScope(); ... trans.Complete();`.
5. Unit test dan integration test pada `HydrateScopeFromLegacyHandlerTest`, `CatchUpScopeFromLegacyHandlerTest`, dan `LegacyStockReadPortTest` lulus dan mencakup skenario: (a) replay banyak baris beruntun tanpa tabrakan `PK_BILRG_StokLegacyBinding` / `PK_BILRG_StokMutasi` / `PK_BILRG_StokLokasi`, (b) mutasi masuk dan keluar pada detik `TglMutasi` yang sama di mana `LegacyBukuId` keluar lebih kecil daripada masuk, (c) multi-baris `DO`, `MT_OUT`, `MT_IN`, dan `DU` pada `(TrsReffId, StokLokasiId)` yang sama, serta (d) kegagalan pada jurnal ke-$k$ tidak meninggalkan mutasi atau binding parsial di database.

Notes:

- Realisasi `TD-03` pada `STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-ARCHITECTURE.md`.

---

### P2-S04

Title: Penyeragaman Handler Konsekuensi Stock Ledger (Bebas `Thread.Sleep`, Depleting `tb_stok` Multi-Baris, Idempotensi Multi-Item, dan Baris Bonus DO)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Menghapus seluruh `Thread.Sleep(5)` pada seluruh handler `Post*ConsequenceCommand` dengan memanfaatkan `MonotonicNunaId`, mendukung pemotongan `tb_stok` multi-baris pada `PostStockTransferConsequenceHandler` dan `PostDispenseIssueConsequenceHandler`, memperbaiki filter idempotensi per `BrgId` pada penjualan dan dispensing multi-item, serta mencegah hilangnya stok barang bonus pada `PostGoodsReceiptConsequenceHandler` dan `DeliveryConfirmCommand` / `DeliveryDirectConfirmCommand`.

Depends On: P1-S01, P1-S02

Repository: `b09-bilreg-api`

Completion Criteria:

1. Seluruh pemanggilan `Thread.Sleep(5)` di `PostSaleIssueConsequenceCommand.cs`, `PostSaleVoidConsequenceCommand.cs`, `PostInternalConsumptionConsequenceCommand.cs`, `PostInternalConsumptionVoidConsequenceCommand.cs`, dan `PostStockTransferConsequenceCommand.cs` dihapus; pembangkitan `BK` dan `ST` menggunakan `MonotonicNunaId.NewLegacyCompact("BK", watermarkToBeat)` dan `MonotonicNunaId.NewLegacyCompact("ST")`.
2. `PostStockTransferConsequenceHandler` dan `PostDispenseIssueConsequenceHandler` mengganti `ResolveSourceLegacyStokId` (yang sebelumnya mewajibkan `matches.Count == 1`) dengan pemotongan lintas beberapa baris `tb_stok` (`legacyBalances.Sum(b => b.QtySisa) >= line.QtyAllocated`) yang diurutkan berdasarkan `LegacyStokId` secara ordinal.
3. `PostSaleIssueConsequenceHandler` dan `PostDispenseIssueConsequenceHandler` memfilter pemeriksaan mutasi idempoten berdasarkan `x.BrgId == request.BrgId` sehingga transaksi multi-item dengan satu `TrsReffId` dapat memproses barang kedua dan seterusnya tanpa gagal `Partial idempotency`.
4. `PostGoodsReceiptConsequenceCommand` mendukung parameter opsional `bool AllowAdditiveLine = false`; ketika `DeliveryConfirmCommand` atau `DeliveryDirectConfirmCommand` memproses lebih dari satu baris item DO untuk kombinasi `(BrgId, TglEd)` yang sama (seperti baris reguler ditambah baris bonus), baris kedua dan seterusnya dikirim dengan `AllowAdditiveLine: true` sehingga stok barang bonus tetap tercatat penuh.
5. Seluruh unit test pada `PostGoodsReceiptConsequenceHandlerTest`, `PostStockTransferConsequenceHandlerTest`, `PostDispenseIssueConsequenceHandlerTest`, `PostSaleIssueConsequenceHandlerTest`, `PostSaleVoidConsequenceHandlerTest`, `PostSalesReturnConsequenceHandlerTest`, `PostInternalConsumptionConsequenceHandlerTest`, dan `PostInternalConsumptionVoidConsequenceHandlerTests` lulus termasuk test baru untuk kasus multi-baris `tb_stok`, multi-item `TrsReffId`, dan baris bonus DO.

Notes:

- Realisasi `TD-01`, `TD-02`, dan `TD-04` pada `STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-ARCHITECTURE.md`.

---

## P3 - Tindakan BOM Fault Tolerance and Multi-Item BOM Safety

Implementation Status: IMPLEMENTED
Review Status: GO

### P3-S05

Title: Pengamanan ID Monotonik, Konsolidasi Barang Kembar, dan Kebijakan *Fallback-to-Pending* pada `TindakanBomProcessor` serta `PakaiBrgInternalService`

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Memastikan `TindakanBomProcessor` dan `PakaiBrgInternalService` menggunakan `MonotonicNunaId` untuk `TBG`, `PND`, dan `PK`, mengonsolidasikan permintaan pemakaian langsung jika terdapat `BrgId` kembar dalam satu BOM, serta mengalihkan barang BOM ke antrean `BILRG_TindakanStokPending` (tanpa melempar eksepsi yang menggagalkan penyimpanan tindakan klinis) apabila freshness gate untuk barang tersebut mengembalikan `AbortedInconsistent` atau gagal menyinkronkan riwayat legacy.

Depends On: P1-S01, P2-S03, P2-S04

Repository: `b09-bilreg-api`

Completion Criteria:

1. `TindakanBomProcessor.Process` menggunakan `MonotonicNunaId.New("TBG")` untuk `TindakanBarangId` dan `MonotonicNunaId.New("PND")` untuk `PendingId`, serta `PakaiBrgInternalService.CreateFromTindakan` (dan `PakaiBrgCreateCommand`) menggunakan `MonotonicNunaId.NewLegacyCompact("PK")` untuk `PakaiBrgId`.
2. Ketika membangun `immediateCutItems` untuk dikirim ke `PakaiBrgInternalService.CreateFromTindakan`, `TindakanBomProcessor` mengelompokkan dan menjumlahkan `QtyImmediateCut` per `BrgId` (`GroupBy(BrgId)`) sehingga tarif yang memiliki dua baris BOM dengan `BrgId` sama tetap memotong total stok yang benar dalam satu dokumen `PK`.
3. Pada tahap freshness gate di `TindakanBomProcessor.Process`, pengecekan dilakukan per `BrgId`; jika `EnsureFreshnessForScopesCommand` untuk suatu `BrgId` menghasilkan `EnsureFreshnessOutcomeEnum.AbortedInconsistent` (atau melempar eksepsi sinkronisasi legacy), `TindakanBomProcessor` mencatat log peringatan (`ILogger.LogWarning`), menetapkan saldo tersedia barang tersebut menjadi `0m` (`QtyImmediateCut = 0`, `QtyPending = QtyKecilRequested`), dan melanjutkan penyimpanan tindakan beserta pencatatan ke `BILRG_TindakanStokPending` tanpa membatalkan transaksi `POST /api/tindakan/create` atau `POST /api/tindakan/save`.
4. Unit test pada `TindakanBomProcessorTest`, `PakaiBrgInternalServiceTests`, `TdkCreateTindakanHandlerTest`, dan `TdkSaveTindakanHandlerTest` lulus, termasuk pengujian khusus untuk: (a) tarif dengan 3 barang BOM menghasilkan `TindakanBarangId` (`TBG`) dan `PendingId` (`PND`) yang semuanya unik, (b) tarif dengan 2 baris BOM ber-`BrgId` sama, dan (c) barang BOM yang freshness gate-nya `AbortedInconsistent` otomatis masuk ke `BILRG_TindakanStokPending` dan `TdkCreateTindakanCmd` tetap sukses.

Notes:

- Realisasi `TD-01`, `TD-02`, dan `TD-05` pada `STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-ARCHITECTURE.md`.

---

# 6. Change Log

- `1.0` (2026-11-19): Rencana implementasi awal dibuat dari `STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-ARCHITECTURE.md` v1.0 dan disetujui untuk eksekusi (`Execution Approval: APPROVED`).
