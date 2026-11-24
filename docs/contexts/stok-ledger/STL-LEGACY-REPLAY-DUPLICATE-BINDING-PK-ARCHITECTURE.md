---
Title: Arsitektur Koreksi Keandalan Replay Legacy, Pembangkitan ID Monotonik, dan Toleransi Kegagalan Pemotongan Stok BOM Tindakan
Code: STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-11-19
---

# 1. Overview

Dokumen arsitektur ini mendefinisikan realisasi teknis untuk memperbaiki kegagalan pembuatan tindakan ber-BOM barang (`POST /api/tindakan/create`) akibat tabrakan primary key dan indeks unik pada Stock Ledger v2, sekaligus menutup seluruh celah produksi sejenis yang ditemukan pada proses sinkronisasi legacy (`Hydrate` dan `CatchUp`), command konsekuensi stok, serta pemroses BOM tindakan.

Referensi artefak asal:

- Originating BUG ISSUE: `ISS-STL-001` ([STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-BUG-ISSUE.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/stok-ledger/STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-BUG-ISSUE.md))
- Arsitektur kapabilitas terkait: [stok-ledger-architecture.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/stok-ledger/stok-ledger-architecture.md)

# 2. Architectural Basis

## Business Context

Berdasarkan `ISS-STL-001` dan domain Stock Ledger ([stok-ledger-domain.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/stok-ledger/stok-ledger-domain.md)):

- Pencatatan tindakan klinis pasien yang memiliki BOM barang harus memotong stok unit layanan secara otomatis bila saldo tersedia, atau memasukkan kekurangan ke antrean stok tertunda (`BILRG_TindakanStokPending`) tanpa menghentikan pelayanan klinis.
- Selama masa koeksistensi dengan tabel legacy (`tb_stok` dan `tb_buku`), Stock Ledger v2 wajib menyinkronkan riwayat mutasi legacy secara akurat dan menjaga kesetaraan saldo antara tabel v2 dan tabel legacy.

## Analysis Input

Berdasarkan [STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-BUG-INVESTIGATION.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/stok-ledger/STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-BUG-INVESTIGATION.md), terdapat enam keputusan investigasi yang disetujui untuk direalisasikan:

1. Pembangkitan ID bebas tabrakan dan bebas `Thread.Sleep` untuk seluruh entitas Stock Ledger v2 (`STB`, `STL`, `STM`, `SBD`), mutasi legacy (`BK`, `ST`, `PK`), serta entitas BOM tindakan (`TBG`, `PND`).
2. Penyelarasan aturan indeks pada `BILRG_StokMutasi` dan aturan idempotensi pada command konsekuensi stok agar mendukung dokumen multi-baris (termasuk barang bonus pada DO, multi-item pada pemakaian/penjualan/dispensing, dan replay multi-baris dari `tb_buku`).
3. Penegakan batas transaksi atomik pada `HydrateScopeFromLegacy` dan `CatchUpScopeFromLegacy` beserta pengurutan deterministik jurnal legacy (`QtyIn > 0` sebelum `QtyOut > 0` pada stempel waktu yang sama).
4. Penyeragaman pengurangan saldo `tb_stok` multi-baris pada `PostStockTransferConsequenceCommand` dan `PostDispenseIssueConsequenceCommand`.
5. Isolasi kegagalan freshness gate pada `TindakanBomProcessor` agar scope yang berstatus `Inconsistent` dialihkan ke antrean `BILRG_TindakanStokPending` dan tidak membatalkan transaksi tindakan klinis.
6. Penyediaan skrip migrasi skema/indeks yang idempoten dan memperbarui definisi indeks lama pada database lokasi.

```text
ISSUE (ISS-STL-001)
        +
BUG-INVESTIGATION (STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-BUG-INVESTIGATION.md)
        ↓
ARCHITECTURE (STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-ARCHITECTURE.md)
```

# 3. Scope

Included:

- Pembangkit ID monotonik bebas `Thread.Sleep` (`MonotonicNunaId`) pada lapisan Domain dan penerapannya di seluruh model serta use case `StockLedgerFeature`, `PakaiBrgFeature`, dan `TindakanBomProcessor`.
- Perubahan definisi indeks `BILRG_StokMutasi` dari unique index menjadi non-unique index pada `(TrsReffId, MovementKind, StokLokasiId)` beserta migrasi otomatis atas indeks lama `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId`.
- Pengurutan deterministik pembacaan `tb_buku` pada `LegacyStockReadPort.ListJournals` serta pembungkusan transaksi atomik dan pemulihan baseline pada `HydrateScopeFromLegacyHandler`, `CatchUpScopeFromLegacyHandler`, dan `LegacyScopeJournalReplayer`.
- Dukungan pemotongan multi-baris `tb_stok` pada `PostStockTransferConsequenceHandler` dan `PostDispenseIssueConsequenceHandler`.
- Penyesuaian aturan idempotensi multi-baris/baris-bonus pada `PostGoodsReceiptConsequenceHandler`, `PostSaleIssueConsequenceHandler`, `PostDispenseIssueConsequenceHandler`, dan `PostInternalConsumptionConsequenceHandler`.
- Kebijakan *fallback-to-pending* pada `TindakanBomProcessor` ketika freshness gate melaporkan `AbortedInconsistent` atau gagal menyinkronkan scope legacy, serta penggabungan baris BOM yang memiliki `BrgId` kembar dalam satu tindakan.

Excluded:

- Perubahan kontrak payload atau struktur respons HTTP pada endpoint `POST /api/tindakan/create` maupun `POST /api/tindakan/save`.
- Perubahan aplikasi frontend (`c012_myhospital_web`).
- Penghentian mode koeksistensi (`tb_stok` dan `tb_buku` tetap dipertahankan).

# 4. Technical Decisions

## TD-01 — Pembangkit ID Monotonik Base36 Bebas `Thread.Sleep` (`MonotonicNunaId`)

Untuk menghilangkan tabrakan primary key sekaligus menghindari penundaan `Thread.Sleep(5)` pada loop replay ribuan baris:

- Dibuat komponen `MonotonicNunaId` di lapisan `Bilreg.Domain` yang membungkus `NunaId.New(prefix)` (prefix 3 karakter + 9 karakter Base36 = 12 karakter) dan `NunaId.NewLegacyCompact(prefix)` (prefix 2 karakter + 8 karakter Base36 = 10 karakter).
- `MonotonicNunaId` menyimpan nilai numerik Base36 terakhir per prefix di memori proses dengan sinkronisasi thread-safe (`lock`).
- Setiap kali ID diminta:
  1. Ambil kandidat awal dari `NunaId.New(prefix)` atau `NunaId.NewLegacyCompact(prefix)`, lalu konversi bagian suffix Base36 menjadi bilangan bulat 64-bit (`long`).
  2. Jika pemanggil menyertakan `minExclusiveId` (misalnya `watermarkToBeat` untuk `BK` pada `tb_buku`), konversi suffix Base36 dari `minExclusiveId` menjadi `minVal`.
  3. Hitung `nextVal = Math.Max(candidateVal, Math.Max(lastValPerPrefix, minVal) + 1)`.
  4. Simpan `lastValPerPrefix = nextVal` dan format kembali `nextVal` menjadi string Base36 huruf besar dengan lebar tetap (9 karakter untuk `New`, 8 karakter untuk `NewLegacyCompact`, diawali `'0'`).
- Karena karakter Base36 (`0-9` lalu `A-Z`) dengan lebar tetap memiliki urutan ASCII yang sama persis dengan urutan numeriknya, setiap ID baru dijamin unik dan naik secara leksikografis (`StringComparer.Ordinal`) dalam waktu $O(1)$ tanpa `Thread.Sleep`.
- Seluruh pabrik domain (`StockBatchModel.Create`, `LocationStockBalanceModel.Create`, `StockMovementModel.CreateInbound/CreateOutbound`, `StockLegacyBindingModel.CreateMutasiBuku/CreateLokasiStok`, `PakaiBrgModel.Create`), seluruh handler `Post*ConsequenceCommand`, `PakaiBrgInternalService`, `PakaiBrgCreateCommand`, dan `TindakanBomProcessor` (`TBG`, `PND`) wajib menggunakan `MonotonicNunaId`. Seluruh pemanggilan `Thread.Sleep(5)` pada handler Stock Ledger dihapus.

## TD-02 — Penggantian Unique Index `BILRG_StokMutasi` dan Aturan Idempotensi Multi-Baris

Untuk mendukung beberapa baris mutasi dengan `(TrsReffId, MovementKind, StokLokasiId)` yang sama (baik dari replay `tb_buku`, barang bonus pada DO yang sama, maupun pemotongan beberapa baris `tb_stok` pada lokasi yang sama):

1. Indeks pada `BILRG_StokMutasi`:
   - Indeks unik `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` dihapus dan diganti dengan indeks non-unik `IX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` pada kolom `(TrsReffId, MovementKind, StokLokasiId)`.
   - Keunikan 1-ke-1 antara baris `tb_buku` dan `BILRG_StokMutasi` tetap dijaga secara ketat oleh indeks unik yang sudah ada pada `BILRG_StokLegacyBinding`, yaitu `UX_BILRG_StokLegacyBinding_LegacyBukuId` dan `UX_BILRG_StokLegacyBinding_StokMutasiId`.
2. Idempotensi pada `PostGoodsReceiptConsequenceHandler` (`UC-STL-001`):
   - Pada `PostGoodsReceiptConsequenceCommand`, ditambahkan parameter opsional `bool AllowAdditiveLine = false` (default `false` agar panggilan tunggal tetap idempoten terhadap retry).
   - Ketika `AllowAdditiveLine == false`, pengecekan idempotensi memeriksa apakah sudah ada mutasi `GoodsReceipt` pada `(TrsReffId, StokLokasiId)` dengan `QtyIn == request.Qty && Hpp == request.Hpp`.
   - Ketika sebuah DO di `DeliveryConfirmCommand` atau `DeliveryDirectConfirmCommand` memiliki lebih dari satu baris item dengan `(BrgId, TglEd)` yang sama (misalnya baris pembelian reguler dan baris bonus), baris kedua dan seterusnya pada kombinasi `(BrgId, TglEd)` yang sama dikirim dengan `AllowAdditiveLine: true` sehingga stok barang bonus tetap ditambahkan ke `BILRG_StokBatch`, `BILRG_StokLokasi`, `BILRG_StokMutasi`, `tb_stok`, dan `tb_buku`.
3. Idempotensi pada `PostSaleIssueConsequenceHandler` (`UC-STL-003`), `PostDispenseIssueConsequenceHandler`, dan `PostInternalConsumptionConsequenceHandler` (`UC-STL-006`):
   - Pengecekan mutasi terdahulu (`priorMutasi`) pada `PostSaleIssueConsequenceHandler` dan `PostDispenseIssueConsequenceHandler` difilter berdasarkan `x.BrgId == request.BrgId` (sebagaimana `PostInternalConsumptionConsequenceHandler`) sehingga dokumen multi-item yang berbagi satu `TrsReffId` tidak gagal dengan `Partial sale-issue/dispense-issue idempotency` ketika memproses item kedua yang berbeda `BrgId`.
   - Pada `TindakanBomProcessor` dan `PakaiBrgInternalService`, apabila satu daftar item memuat lebih dari satu baris dengan `BrgId` yang sama, kuantitas pemotongan langsung (`QtyImmediateCut`) dikonsolidasikan per `BrgId` saat memanggil `PostInternalConsumptionConsequenceCommand` sehingga tidak terdeteksi sebagai duplikat idempoten.

## TD-03 — Atomisitas Transaksi dan Determinisme Urutan Replay Legacy (`Hydrate` dan `CatchUp`)

Untuk mencegah kondisi `bindings without batch` dan korupsi saldo parsial:

1. Pengurutan deterministik di `LegacyStockReadPort.ListJournals`:
   - Baris `tb_buku` diurutkan dengan `.OrderBy(x => x.TglMutasi).ThenBy(x => x.QtyIn > 0 ? 0 : 1).ThenBy(x => x.LegacyBukuId, StringComparer.Ordinal)`.
   - Dengan aturan ini, pada stempel waktu `TglMutasi` yang sama (resolusi detik/tanggal di data legacy), mutasi masuk (`QtyIn > 0`) selalu diproses sebelum mutasi keluar (`QtyOut > 0`).
2. Batas transaksi atomik pada `HydrateScopeFromLegacyHandler` dan `CatchUpScopeFromLegacyHandler`:
   - Seluruh eksekusi apply dan penyimpanan (`_mutasiRepo.Insert`, `_bindingRepo.Insert`, `_batchRepo.SaveChanges`, dan `_scopeRepo.SaveChanges`) di dalam `HydrateScopeFromLegacyHandler` dan `CatchUpScopeFromLegacyHandler` dibungkus dalam `using var trans = TransHelper.NewScope();` dan diakhiri `trans.Complete();`.
   - Di dalam `LegacyScopeJournalReplayer.Apply`, mutasi dan binding dikumpulkan terlebih dahulu di memori (`List<StockMovementModel>` dan `List<StockLegacyBindingModel>`) dan hanya ditulis ke database setelah seluruh jurnal dalam scope tersebut valid (atau jika `IsInconsistent`, hanya status `Inconsistent` pada `BILRG_StokLegacyScope` yang disimpan tanpa menulis mutasi/binding parsial).
   - Apabila pada database sudah telanjur terdapat baris `BILRG_StokLegacyBinding` yatim dari kegagalan masa lalu sementara `BILRG_StokBatch` belum ada (`batch is null`), `LegacyScopeJournalReplayer` tetap menerapkan pembentukan batch dan saldo lokasi dari jurnal awal tanpa menggandakan baris mutasi/binding yang sudah tercatat.

## TD-04 — Pengurangan `tb_stok` Multi-Baris pada Transfer dan Dispensing

Untuk menyamakan perilaku dengan `PostSaleIssueConsequenceHandler` dan `PostInternalConsumptionConsequenceHandler`:

- Metode `ResolveSourceLegacyStokId` yang mewajibkan `matches.Count == 1` pada `PostStockTransferConsequenceHandler` dan `PostDispenseIssueConsequenceHandler` diganti dengan algoritma pemotongan multi-baris (`legacyBalances` yang memiliki `LayananId == sourceLayananId && TglEd == line.TglEd && QtySisa > 0`, diurutkan berdasarkan `LegacyStokId` secara ordinal).
- Selama `legacyBalances.Sum(b => b.QtySisa) >= line.QtyAllocated`, kebutuhan kuantitas dipecah (`sliceQty = Math.Min(remaining, match.QtySisa)`) melintasi baris-baris `tb_stok` yang tersedia, dan masing-masing slice membentuk operasi `LegacyStokDepleteWriteOperation`, `LegacyOutboundBukuWriteOperation`, `StockMovementModel`, serta `StockLegacyBindingModel` secara konsisten.

## TD-05 — Isolasi Kegagalan Sinkronisasi Stok pada `TindakanBomProcessor` (*Fallback-to-Pending*)

Untuk menjamin bahwa pembuatan tindakan klinis (`POST /api/tindakan/create` dan `POST /api/tindakan/save`) tidak pernah gagal akibat ketidakkonsistenan riwayat stok masa lalu:

- Pada `TindakanBomProcessor.Process`, freshness gate dijalankan per `BrgId` (atau per scope `(BrgId, BrgMasukReffId)`).
- Jika `EnsureFreshnessForScopesCommand` untuk suatu `BrgId` mengembalikan `EnsureFreshnessOutcomeEnum.AbortedInconsistent` (atau melempar eksepsi domain/validasi stok legacy), `TindakanBomProcessor` **tidak melempar eksepsi** yang membatalkan transaksi tindakan.
- Sebaliknya, barang tersebut diperlakukan memiliki saldo siap potong `availableStockByBrg[brgId] = 0m` pada saat tindakan dibuat, sehingga seluruh kuantitas yang dibutuhkan (`QtyKecilRequested`) otomatis masuk ke antrean `BILRG_TindakanStokPending` (`QtyImmediateCut = 0`, `QtyPending = QtyKecilRequested`) dan tindakan klinis tetap berhasil disimpan.

## TD-06 — Skrip Provisioning dan Migrasi Indeks Idempoten

- Pada `BILRG_StokMutasi.sql` dan `StockLedgerV2SchemaFixture.cs`, skrip DDL diperbarui agar memeriksa apakah `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` masih ada di `sys.indexes`; jika ada, indeks unik tersebut di-`DROP` dan diganti dengan indeks non-unik `IX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId`.
- Pada `StockLedgerV2SchemaFixture.EnsureSchema()`, migrasi indeks `BILRG_StokMutasi` tetap dieksekusi meskipun tabel `BILRG_StokMutasi` sudah ada sebelumnya di database uji/lokasi.

# 5. Component Responsibilities

| Component | Responsibility |
|---|---|
| `MonotonicNunaId` (`Bilreg.Domain`) | Membangkitkan ID 12-karakter (`New`) dan 10-karakter (`NewLegacyCompact`) berbasis Base36 yang dijamin unik, monotonik naik secara ordinal, dan bebas `Thread.Sleep` di seluruh thread dalam proses. |
| `StockBatchModel`, `LocationStockBalanceModel`, `StockMovementModel`, `StockLegacyBindingModel`, `PakaiBrgModel` (`Bilreg.Domain`) | Menggunakan `MonotonicNunaId` saat membentuk identitas agregat dan entitas anak baru. |
| `LegacyStockReadPort` (`Bilreg.Infrastructure`) | Membaca `tb_buku` dan `tb_stok` per scope `(BrgId, BrgMasukReffId)` dengan urutan deterministik (`TglMutasi`, `QtyIn > 0` lebih dulu dari `QtyOut > 0`, lalu `LegacyBukuId`). |
| `LegacyScopeJournalReplayer` (`Bilreg.Application`) | Memutar ulang jurnal legacy di memori terlebih dahulu dan hanya mem-persist batch, lokasi, mutasi, serta binding ketika replay scope valid, atau memulihkan batch baseline bila terdapat binding yatim dari kegagalan sebelumnya. |
| `HydrateScopeFromLegacyHandler` dan `CatchUpScopeFromLegacyHandler` (`Bilreg.Application`) | Menjalankan replay scope di dalam batas transaksi atomik (`TransHelper.NewScope()`). |
| `Post*ConsequenceHandler` (`Bilreg.Application`) | Menjalankan alokasi stok, pemotongan `tb_stok` multi-baris, pemeriksaan idempotensi multi-item/baris-bonus, dan pembangkitan ID melalui `MonotonicNunaId` tanpa `Thread.Sleep`. |
| `TindakanBomProcessor` (`Bilreg.Application`) | Mengekspansi BOM tarif menjadi pemotongan stok langsung (`PakaiBrgInternalService`) dan antrean pending (`BILRG_TindakanStokPending`) menggunakan `MonotonicNunaId`, serta mengalihkan barang ke antrean pending bila freshness gate gagal/inconsistent. |
| `BILRG_StokMutasi.sql` dan `StockLedgerV2SchemaFixture` (`Bilreg.SqlDb` / `Bilreg.Test`) | Menjaga skema `BILRG_StokMutasi` menggunakan indeks non-unik `IX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` dan menghapus indeks unik lama secara idempoten. |

# 6. Integration Design

| Source | Target | Purpose |
|---|---|---|
| `TindakanCreateHandler` / `TindakanSaveHandler` | `TindakanBomProcessor` | Memproses kebutuhan barang BOM setelah tindakan utama terbentuk di dalam transaksi tindakan. |
| `TindakanBomProcessor` | `EnsureFreshnessForScopesCommand` | Menyinkronkan scope `(BrgId, BrgMasukReffId)` per barang BOM; jika gagal/inconsistent, dialihkan ke `BILRG_TindakanStokPending`. |
| `TindakanBomProcessor` | `PakaiBrgInternalService` | Membuat dokumen pemakaian barang (`tb_trs_pakai`) dengan item yang sudah dikonsolidasikan per `BrgId` untuk kuantitas yang tersedia. |
| `PakaiBrgInternalService` / `DeliveryConfirmHandler` / `StockPharmacyAdapter` | `Post*ConsequenceCommand` | Mencatat konsekuensi mutasi stok secara dual-write ke tabel Stock Ledger v2 dan tabel legacy. |
| `EnsureFreshnessForScopesHandler` | `HydrateScopeFromLegacyCommand` / `CatchUpScopeFromLegacyCommand` | Menjalankan sinkronisasi awal atau susulan dari `tb_buku` dalam transaksi atomik per scope. |

# 7. Data Ownership

| Data | Owner |
|---|---|
| `BILRG_StokBatch`, `BILRG_StokLokasi`, `BILRG_StokMutasi`, `BILRG_StokLegacyScope`, `BILRG_StokLegacyBinding` | `InventoryContext.StockLedgerFeature` |
| `tb_stok`, `tb_buku` (selama masa koeksistensi) | `InventoryContext.StockLedgerFeature` (melalui `ILegacyStockReadPort` dan `ILegacyStockWriterPort`) |
| `tb_trs_pakai`, `tb_trs_pakai2`, `BILRG_TindakanStokPending` | `InventoryContext.PakaiBrgFeature` |
| `TA_TRS_TINDAKAN`, `BILRG_TindakanBarang` | `ChargeContext.TindakanFeature` |

# 8. Database Design

## New Tables

| Table | Purpose |
|---|---|
| Tidak ada tabel baru | Seluruh 5 tabel Stock Ledger v2 dan 2 tabel BOM tindakan (`BILRG_TindakanBarang`, `BILRG_TindakanStokPending`) sudah didefinisikan. |

## Modified Tables

| Table | Change |
|---|---|
| `BILRG_StokMutasi` | Menghapus indeks unik `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` (jika ada) dan menggantinya dengan indeks non-unik `IX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` pada `(TrsReffId, MovementKind, StokLokasiId)`. |

## Relationships

- Satu `BILRG_StokMutasi` berkorespondensi paling banyak dengan satu baris `tb_buku` yang diikat melalui `BILRG_StokLegacyBinding` (`UX_BILRG_StokLegacyBinding_LegacyBukuId` dan `UX_BILRG_StokLegacyBinding_StokMutasiId`).
- Satu dokumen sumber (`TrsReffId`) pada suatu jenis mutasi (`MovementKind`) dan lokasi (`StokLokasiId`) dapat memiliki satu atau lebih baris `BILRG_StokMutasi` (misalnya pemotongan lintas beberapa baris `tb_stok` pada batch/ED yang sama, atau penerimaan reguler + bonus pada DO yang sama).

## Migration Considerations

- Skrip `BILRG_StokMutasi.sql` harus mengecek keberadaan `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` di `sys.indexes` dan melakukan `DROP INDEX` sebelum membuat `IX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId`.
- Jika pada database lokasi terdapat scope yang sudah telanjur masuk ke `AlignmentStatus = 3` (`Inconsistent`) akibat kegagalan parsial sebelumnya, status scope tersebut dapat di-reset ke `0` (`NotAligned`) setelah deployment agar di-hydrate ulang secara utuh dan atomik.

# 9. Cross-Cutting Concerns

- Concurrency dan Performa: `MonotonicNunaId` menggunakan sinkronisasi memori dalam proses (`lock`) tanpa `Thread.Sleep`, sehingga pembangkitan ribuan ID selama replay `tb_buku` selesai dalam waktu kurang dari 1 milidetik dan tetap aman terhadap pemanggilan paralel lintas thread.
- Ketahanan Operasional (*Fault Tolerance*): Kegagalan sinkronisasi riwayat stok legacy (`AbortedInconsistent`) dicatat melalui logger peringatan (`ILogger.LogWarning`) dan dialihkan ke antrean `BILRG_TindakanStokPending` tanpa menggagalkan transaksi tindakan klinis.
- Atomisitas Persistensi: `HydrateScopeFromLegacy` dan `CatchUpScopeFromLegacy` hanya menulis ke database setelah validasi replay satu scope selesai di memori dan membungkus seluruh penulisan dalam `TransHelper.NewScope()`.

# 10. Implementation Constraints

- Panjang string ID wajib mematuhi batas kolom SQL Server: 12 karakter untuk `STB`, `STL`, `STM`, `SBD` (`VARCHAR(12)`), 10 karakter untuk `BK`, `ST`, `PK` (`VARCHAR(10)`), dan maksimal 26 karakter untuk `TBG`, `PND` (`VARCHAR(26)`).
- Dilarang menggunakan `Thread.Sleep` di dalam loop pembangkitan ID atau replay jurnal.
- Wajib menggunakan Dapper dan SQL eksplisit sesuai standar `b09-bilreg-api`.
- Tidak boleh mengubah kontrak JSON endpoint publik `POST /api/tindakan/create` dan `POST /api/tindakan/save`.

# 11. Acceptance Conditions

1. Pembangkitan 1.000+ ID berturut-turut melalui `MonotonicNunaId.New` dan `MonotonicNunaId.NewLegacyCompact` untuk prefix yang sama menghasilkan seluruh ID unik, berurutan naik secara ordinal (`StringComparer.Ordinal`), dan selesai tanpa `Thread.Sleep`.
2. Replay `tb_buku` melalui `HydrateScopeFromLegacy` dan `CatchUpScopeFromLegacy` dengan banyak baris pada milidetik yang sama, beberapa baris pada `(TrsReffId, MovementKind, StokLokasiId)` yang sama (termasuk `DO`, `MT_OUT`, `MT_IN`, `DU`, `PK`, `RJ/RU/RT`), serta mutasi masuk dan keluar pada detik `TglMutasi` yang sama berhasil diselesaikan secara atomik tanpa pelanggaran primary key maupun unique index.
3. Kegagalan di tengah proses replay scope tidak pernah meninggalkan baris `BILRG_StokMutasi` atau `BILRG_StokLegacyBinding` tanpa `BILRG_StokBatch` dan `BILRG_StokLokasi`.
4. `PostStockTransferConsequenceCommand` dan `PostDispenseIssueConsequenceCommand` berhasil memotong stok ketika saldo di `tb_stok` terpecah ke dalam dua atau lebih baris untuk kombinasi `(BrgId, BrgMasukReffId, LayananId, TglEd)` yang sama.
5. Konfirmasi DO (`DeliveryConfirmCommand` / `DeliveryDirectConfirmCommand`) yang memiliki baris reguler dan baris bonus pada `(BrgId, TglEd)` yang sama mencatat kedua baris tersebut ke stok tanpa kehilangan kuantitas bonus.
6. Permintaan `POST /api/tindakan/create` dengan tarif ber-BOM 3 barang (seperti `TRJ0000037`) berhasil menyimpan tindakan, memotong stok atau mencatat ke `BILRG_TindakanStokPending` tanpa tabrakan `PK_BILRG_TindakanBarang` maupun `PK_BILRG_TindakanStokPending`, dan tetap berhasil menyimpan tindakan (dengan mengalihkan barang ke antrean pending) apabila salah satu barang BOM memiliki riwayat legacy yang `Inconsistent`.
