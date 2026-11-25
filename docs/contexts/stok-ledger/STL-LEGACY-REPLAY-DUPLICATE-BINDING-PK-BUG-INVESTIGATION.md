# BUG-INVESTIGATION

## Context

Issue: `ISS-STL-001` (`STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-BUG-ISSUE.md`)
Problem Summary: Permintaan `POST /api/tindakan/create` untuk tindakan yang memiliki BOM barang gagal di database operasional lokasi akibat rentetan kegagalan pada lapisan sinkronisasi dan pencatatan Stock Ledger v2 (dimulai dari pelanggaran indeks unik `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId`, diikuti pelanggaran primary key `PK_BILRG_StokLegacyBinding`), serta ditemukan sejumlah kerentanan serupa yang akan memicu kegagalan di lingkungan production.

## Current State

1. Saat pengguna menyimpan tindakan poliklinik (`POST /api/tindakan/create`) untuk tarif `TRJ0000037` (`nilaiTarifId = 01M6G0G33FMX7YKKFBKN30YXHH`, `fb_charge_brg = 0`) yang memiliki 3 barang BOM (`ALK000000164`, `ALK000000107`, `ALK000000002`), backend menjalankan pembuatan tindakan, tagihan, jurnal, lalu memanggil pemroses BOM (`TindakanBomProcessor`) di dalam satu transaksi.
2. Pemroses BOM menjalankan freshness gate (`EnsureFreshnessForScopesCommand`) untuk menyinkronkan riwayat `tb_buku` ke tabel Stock Ledger v2 (`BILRG_StokBatch`, `BILRG_StokLokasi`, `BILRG_StokMutasi`, `BILRG_StokLegacyBinding`, `BILRG_StokLegacyScope`) melalui `HydrateScopeFromLegacyCommand` dan `CatchUpScopeFromLegacyCommand`.
3. Pada percobaan pertama di lokasi, proses replay gagal karena `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` menolak baris kedua dari transaksi penjualan lama `(DU0011211A, 5, STL0AD2XH65G)`. Indeks di database lokasi masih menggunakan definisi lama tanpa filter `WHERE`.
4. Setelah indeks unik tersebut diperbarui di database lokasi, percobaan kedua gagal dengan HTTP 500: `Violation of PRIMARY KEY constraint 'PK_BILRG_StokLegacyBinding'. Cannot insert duplicate key in object 'dbo.BILRG_StokLegacyBinding'. The duplicate key value is (SBD0AD36KXMT).`
5. Akibat eksepsi tersebut, seluruh transaksi `POST /api/tindakan/create` ter-rollback dan tindakan medis tidak dapat disimpan.

## Problem Analysis

Investigasi menyeluruh pada kode sumber `b09-bilreg-api` (baik pada branch `master` maupun branch fitur `feat/trk-tindakan-bom-potong-stok`) menunjukkan bahwa error `PK_BILRG_StokLegacyBinding` hanyalah gejala permukaan pertama. Terdapat tujuh kelompok masalah struktural yang memiliki pola serupa dan akan muncul beruntun di lingkungan production jika hanya ditambal pada satu titik.

### 1. Tabrakan ID berbasis waktu (`NunaId`) secara sistemik pada loop cepat dan operasi multi-item

Fungsi `NunaId.New(prefix)` (12 karakter) dan `NunaId.NewLegacyCompact(prefix)` (10 karakter) membangkitkan ID berbasis stempel waktu. Ketika dipanggil berulang kali di dalam loop memori atau loop database lokal yang cepat (dalam jendela milidetik yang sama), fungsi tersebut menghasilkan ID yang identik. Titik-titik yang terdampak meliputi:

- Replay jurnal legacy (`LegacyScopeJournalReplayer.Apply`): di dalam loop `foreach (var journal in journals)`, pembangkitan `BindingId` (`SBD`), `StokMutasiId` (`STM`), dan `StokLokasiId` (`STL` saat `IncreaseLokasi` menambah kombinasi layanan atau tanggal kedaluwarsa baru) tidak memiliki penjagaan keunikan sama sekali. Jika hanya `SBD` yang diperbaiki, baris yang sama akan langsung gagal pada `PK_BILRG_StokMutasi` (`STM`) atau `PK_BILRG_StokLokasi` (`STL`). Pembangkitan `StokBatchId` (`STB`) lintas scope di `EnsureFreshnessForScopesHandler` juga memiliki risiko yang sama.
- Ekspansi BOM tindakan (`TindakanBomProcessor.Process`): di dalam loop memori `foreach (var calc in calculatedItems)` (tanpa jeda I/O sama sekali), kode memanggil `NunaId.New("TBG")` untuk setiap barang BOM dan `NunaId.New("PND")` untuk setiap barang BOM yang stoknya kurang. Untuk tarif `TRJ0000037` yang memiliki 3 barang BOM, ketiga iterasi berjalan dalam hitungan mikrodetik sehingga pasti menghasilkan `TindakanBarangId` (`TBG...`) dan `PendingId` (`PND...`) yang kembar, lalu gagal pada `PK_BILRG_TindakanBarang` dan `PK_BILRG_TindakanStokPending` saat disimpan ke database.
- Pemakaian barang multi-item (`PakaiBrgInternalService.CreateFromTindakan` dan `PakaiBrgCreateCommand`): setiap barang BOM memanggil `PostInternalConsumptionConsequenceCommand` secara berurutan di dalam loop. Walaupun handler tersebut memiliki `HashSet` lokal (`usedBukuIds`, `usedMutasiIds`, `usedBindingIds`), koleksi tersebut dibuat ulang dari kosong pada setiap item barang. ID terakhir pada barang pertama dapat bertabrakan dengan ID pertama pada barang kedua.
- Konfirmasi penerimaan barang / Delivery Order multi-item (`DeliveryConfirmCommand` dan `DeliveryDirectConfirmCommand` yang memanggil `PostGoodsReceiptConsequenceCommand` di dalam loop): handler `PostGoodsReceiptConsequenceHandler` membangkitkan `STB`, `STL`, `STM`, `SBD`, `BK`, dan `ST` tanpa deduplikasi lintas baris item DO.
- Pengeluaran dispensing apotek (`PostDispenseIssueConsequenceHandler`), retur penjualan (`PostSalesReturnConsequenceHandler`), dan mutasi transfer stok (`PostStockTransferConsequenceHandler`): pada `PostDispenseIssueConsequenceHandler`, alokasi FEFO yang terpecah ke lebih dari satu batch/ED di dalam loop tidak memiliki deduplikasi untuk `BK`, `STM`, maupun `SBD`. Pada `PostStockTransferConsequenceHandler`, pembangkitan `legacyStokInId` (`ST`) di dalam loop alokasi tidak memiliki deduplikasi sehingga berisiko menabrak primary key `tb_stok`.

### 2. Risiko bottleneck performa dan timeout transaksi jika deduplikasi menggunakan `Thread.Sleep`

Pada beberapa handler yang sudah ada (seperti `PostSaleIssueConsequenceHandler` dan `PostInternalConsumptionConsequenceHandler`), penghindaran duplikasi ID dilakukan dengan `Thread.Sleep(5)` di dalam loop (bahkan terdapat pemanggilan `Thread.Sleep(5)` tanpa syarat sebelum membuat `StockLegacyBindingModel`).

Jika pendekatan `Thread.Sleep(5)` diterapkan pada `LegacyScopeJournalReplayer.Apply`, dampaknya di database production akan sangat berat:

- Barang habis pakai berputaran tinggi (seperti `ALK000000002` — spuit, sarung tangan, atau kassa) yang berasal dari satu nomor DO gudang atau satu kode stok awal dapat memiliki ratusan hingga ribuan baris mutasi historis di `tb_buku` lintas seluruh unit layanan rumah sakit.
- Menjalankan `Thread.Sleep(5)` untuk `STM` dan `SBD` pada 1.000 baris `tb_buku` akan menahan thread selama 5 hingga 10 detik per scope DO, ditambah ribuan eksekusi `INSERT` satu per satu pada koneksi terpisah di dalam `TransactionScope` permintaan `POST /api/tindakan/create`.
- Untuk tindakan dengan 3 barang BOM yang masing-masing memiliki beberapa DO aktif, waktu tunggu kumulatif dapat melampaui batas timeout HTTP maupun `TransactionScope`, serta menahan kunci transaksi pada tabel tindakan dan billing.

### 3. Batasan indeks unik `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` dan anomali barang bonus pada DO

Saat ini `BILRG_StokMutasi.sql` menerapkan indeks unik pada `(TrsReffId, MovementKind, StokLokasiId)` dengan filter `WHERE MovementKind IN (1, 2, 3, 8, 9, 10)` (`1 = DO`, `2 = MT_OUT`, `3 = MT_IN`, `8 = RJ`, `9 = RU`, `10 = RT`). Asumsi bahwa keenam jenis mutasi tersebut hanya pernah memiliki maksimal satu baris per `(TrsReffId, StokLokasiId)` tidak sesuai dengan perilaku data production:

- Pada `BILRG_StokLokasi`, keunikan lokasi ditentukan oleh `(StokBatchId, LayananId, TglEd)` tanpa menyertakan `NoBatch` maupun `Hpp`.
- Pada penerimaan barang (`DeliveryConfirmCommand`, `MovementKind = 1 / DO`), satu dokumen DO sering memuat dua baris untuk barang dan tanggal kedaluwarsa yang sama: satu baris pembelian reguler dan satu baris barang bonus (`IsBonus = true`), atau dua nomor batch pabrik berbeda dengan tanggal kedaluwarsa yang sama. Keduanya memetakan ke `StokLokasiId` yang sama dengan `TrsReffId = DeliveryOrderId` yang sama.
- Ketika kasus tersebut terjadi:
  1. Saat konfirmasi DO baru melalui `PostGoodsReceiptConsequenceHandler`, pengecekan `_mutasiRepo.Exists(request.TrsReffId, MovementKindEnum.GoodsReceipt, existingLokasi.StokLokasiId)` pada baris kedua (baris bonus) akan bernilai `true`. Sistem salah menganggap baris bonus sebagai pengiriman ulang idempoten (`Outcome = Idempotent`), sehingga **stok barang bonus tidak ditambahkan sama sekali ke `BILRG_StokBatch`, `BILRG_StokLokasi`, `tb_stok`, maupun `tb_buku`** (kehilangan stok secara diam-diam).
  2. Saat me-replay data `tb_buku` legacy melalui `LegacyScopeJournalReplayer` yang sudah telanjur memiliki dua baris `DO` (`1`), dua baris `MT_OUT` (`2`), dua baris `MT_IN` (`3`), atau dua baris retur (`8, 9, 10`) untuk dokumen dan lokasi stok yang sama, proses replay akan tetap meledak pada indeks unik `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId`.

### 4. Ketiadaan batas transaksi atomik pada Hydrate dan Catch-Up yang memicu korupsi saldo atau status `Inconsistent` permanen

Di dalam `LegacyScopeJournalReplayer.Apply`, setiap iterasi jurnal langsung mengeksekusi `_mutasiRepo.Insert(mutasi)` dan `_bindingRepo.Insert(binding)` ke database, sedangkan penyimpanan saldo `BILRG_StokBatch` dan `BILRG_StokLokasi` (`_batchRepo.SaveChanges`) serta watermark `BILRG_StokLegacyScope` (`_scopeRepo.SaveChanges`) baru dipanggil di luar loop setelah seluruh jurnal selesai diproses:

- Baik `HydrateScopeFromLegacyHandler` maupun `CatchUpScopeFromLegacyHandler` tidak membuka `TransactionScope` sendiri.
- Pada banyak alur pemanggil (seperti `SettlePendingTindakanStockHandler`, `PostSaleIssueConsequenceHandler`, `PostInternalConsumptionConsequenceHandler`, `PostStockTransferConsequenceHandler`, dan `PostGoodsReceiptConsequenceHandler`), freshness gate dipanggil **sebelum** `_uow.Commit` membuka `TransHelper.NewScope()`.
- Jika terjadi kegagalan pada baris jurnal ke-$k$ (misalnya karena duplikasi kunci, timeout, atau koneksi terputus), baris jurnal $1$ sampai $k-1$ sudah ter-commit permanen di `BILRG_StokMutasi` dan `BILRG_StokLegacyBinding`, sementara `BILRG_StokBatch` dan `BILRG_StokLokasi` belum tersimpan.
- Pada panggilan berikutnya untuk scope tersebut:
  - Jika `BILRG_StokBatch` belum sempat terbuat, `LegacyScopeJournalReplayer` mendeteksi adanya binding tanpa batch dan **mengunci scope tersebut secara permanen ke status `Inconsistent` (`bindings without batch`)**.
  - Jika `BILRG_StokBatch` sudah ada sebelumnya (saat catch-up), baris $1$ sampai $k-1$ akan dilewati (`if (_bindingRepo.FindByLegacyBukuId(...).HasValue) continue;`) padahal perubahan saldonya belum pernah disimpan ke `BILRG_StokBatch` dan `BILRG_StokLokasi`. Hal ini menyebabkan **saldo stok v2 menjadi tidak sinkron secara permanen dengan `tb_stok`**.

### 5. Ketidakcocokan aturan idempotensi multi-item dan pengurangan `tb_stok` multi-baris

Terdapat tiga ketidaksesuaian logika ketika satu dokumen transaksi memuat beberapa baris atau ketika `tb_stok` memiliki beberapa baris untuk kombinasi DO dan ED yang sama:

- Pengecekan idempotensi pada `PostSaleIssueConsequenceHandler` dan `PostDispenseIssueConsequenceHandler` memanggil `_mutasiRepo.ListByTrsReffId(request.TrsReffId)` tanpa memfilter per item atau nomor urut baris. Jika satu nomor transaksi penjualan/dispensing digunakan untuk memposting lebih dari satu barang, barang kedua akan menemukan mutasi milik barang pertama dan melempar eksepsi `Partial sale-issue idempotency` atau `Partial dispense-issue idempotency`.
- Pada `PostInternalConsumptionConsequenceHandler`, idempotensi difilter per `(TrsReffId, BrgId)`. Jika satu tarif memiliki dua baris BOM dengan kode barang (`BrgId`) yang sama, baris kedua akan dianggap idempoten dengan baris pertama (jika kuantitasnya sama, pemotongan stok baris kedua dilewati secara diam-diam; jika kuantitasnya berbeda, sistem melempar eksepsi `Partial internal-consumption idempotency`).
- Pada `PostStockTransferConsequenceHandler` dan `PostDispenseIssueConsequenceHandler`, metode `ResolveSourceLegacyStokId` mengharuskan `matches.Count == 1` pada `tb_stok` dengan syarat `b.QtySisa >= qtyNeeded`. Padahal setiap kali terjadi mutasi masuk (`MT_IN`), retur, atau pembatalan ke suatu lokasi untuk DO dan ED yang sama, `LegacyStockWriterPort` selalu melakukan `INSERT` baris baru di `tb_stok` mentre `BILRG_StokLokasi` menggabungkannya dalam satu baris saldo. Akibatnya, lokasi yang menerima dua kali transfer dari DO dan ED yang sama akan memiliki 2 baris di `tb_stok`. Saat lokasi tersebut melakukan transfer keluar atau dispensing, `ResolveSourceLegacyStokId` akan gagal dengan `Expected exactly one legacy stok for deplete ...; found 2` (atau `found 0` jika kuantitas gabungan cukup tetapi tidak ada satu baris tunggal di `tb_stok` yang mencukupi).

### 6. Anomali riwayat `tb_buku` masa lalu memblokir pencatatan tindakan klinis

Di database production yang sudah beroperasi lama, data historis `tb_buku` sering memiliki ketidaksempurnaan masa lalu:

- Resolusi waktu `fd_tgl_mutasi` dan `fs_jam_mutasi` di `tb_buku` umumnya hanya sampai tingkat detik (atau tanggal saja bila jam kosong). Ketika mutasi masuk dan mutasi keluar terjadi pada detik yang sama, pengurutan `.OrderBy(x => x.TglMutasi).ThenBy(x => x.LegacyBukuId)` di `LegacyStockReadPort.ListJournals` tidak menjamin mutasi masuk (`QtyIn > 0`) diproses lebih dulu daripada mutasi keluar (`QtyOut > 0`). Jika mutasi keluar terurut lebih dulu, `DecreaseLokasi` gagal karena saldo lokasi belum ada atau menjadi negatif, lalu menandai scope sebagai `Inconsistent`.
- Ketika satu saja scope DO dari barang BOM berstatus `Inconsistent` (bahkan jika anomali historis `tb_buku` tersebut terjadi di unit layanan lain berbulan-bulan lalu pada nomor DO yang sama), `TindakanBomProcessor` saat ini melempar `InvalidOperationException("Freshness gate gagal...")` yang menggagalkan penyimpanan tindakan klinis (`POST /api/tindakan/create`). Hal ini bertentangan dengan tujuan bisnis antrean `BILRG_TindakanStokPending`, yaitu memastikan pelayanan dan pencatatan tindakan klinis pasien tidak terhenti oleh kendala stok.

### 7. Guard `IF NOT EXISTS` pada skrip DDL SQL mengabaikan pembaruan definisi indeks pada database lokasi

Skrip DDL di `Bilreg.SqlDb` hanya memeriksa keberadaan nama indeks (`IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = ...)`). Pada database lokasi yang sudah pernah menjalankan versi awal skrip, perubahan definisi indeks (seperti penambahan klausa `WHERE`) atau penambahan kolom tidak teraplikasi secara otomatis sehingga menimbulkan ketidaksesuaian skema antara kode aplikasi dan database lokasi.

## Affected Components

- Modul dan layanan aplikasi (`b09-bilreg-api`):
  - `Bilreg.Application.InventoryContext.StockLedgerFeature.LegacyScopeJournalReplayer`
  - `Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases.HydrateScopeFromLegacyCommand`
  - `Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases.CatchUpScopeFromLegacyCommand`
  - `Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases.EnsureFreshnessForScopesCommand`
  - `Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases.PostGoodsReceiptConsequenceCommand`
  - `Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases.PostStockTransferConsequenceCommand`
  - `Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases.PostSaleIssueConsequenceCommand`
  - `Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases.PostSaleVoidConsequenceCommand`
  - `Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases.PostSalesReturnConsequenceCommand`
  - `Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases.PostInternalConsumptionConsequenceCommand`
  - `Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases.PostInternalConsumptionVoidConsequenceCommand`
  - `Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases.PostDispenseIssueConsequenceCommand`
  - `Bilreg.Application.ChargeContext.TindakanFeature.TindakanBomProcessor` (branch `feat/trk-tindakan-bom-potong-stok`)
  - `Bilreg.Application.InventoryContext.PakaiBrgFeature.PakaiBrgInternalService` (branch `feat/trk-tindakan-bom-potong-stok`)
  - `Bilreg.Application.InventoryContext.PakaiBrgFeature.SettlePendingTindakanStockService` (branch `feat/trk-tindakan-bom-potong-stok`)
- Model domain (`b09-bilreg-api`):
  - `StockBatchModel`, `LocationStockBalanceModel`, `StockMovementModel`, `StockLegacyBindingModel`
- Infrastruktur dan akses data (`b09-bilreg-api`):
  - `LegacyStockReadPort`, `LegacyStockWriterPort`, `StockConsequenceUnitOfWork`, `StockBatchRepo`, `TindakanBarangDal`, `TindakanStokPendingDal`
- Objek database (`Bilreg.SqlDb` dan database lokasi):
  - `BILRG_StokBatch`, `BILRG_StokLokasi`, `BILRG_StokMutasi`, `BILRG_StokLegacyScope`, `BILRG_StokLegacyBinding`
  - `BILRG_TindakanBarang`, `BILRG_TindakanStokPending`
  - `tb_buku`, `tb_stok`, `TA_TARIF`, `TA_TARIF7`

## Impact Assessment

- Dampak bisnis dan klinis:
  - Pencatatan tindakan medis dan tagihan pasien di poliklinik/unit layanan terhenti (`HTTP 500`) setiap kali tarif tindakan memiliki BOM barang yang belum tersinkronisasi atau terdampak tabrakan ID/indeks pada Stock Ledger v2.
  - Pada penerimaan barang (DO) yang memiliki baris reguler dan baris bonus dengan kode barang dan tanggal kedaluwarsa yang sama, stok barang bonus berisiko hilang secara diam-diam karena terdeteksi sebagai duplikat idempoten.
- Dampak operasional:
  - Database production di lokasi memiliki volume riwayat `tb_buku` yang jauh lebih besar dan bervariasi dibandingkan database development, sehingga replay sinkron saat transaksi berjalan berisiko tinggi memicu timeout, penguncian tabel, atau status `Inconsistent` permanen yang tidak dapat dipulihkan oleh pengguna tanpa intervensi database manual.
- Dampak teknis:
  - Ketiadaan transaksi atomik di dalam `HydrateScopeFromLegacy` dan `CatchUpScopeFromLegacy` dapat meninggalkan data mutasi dan binding parsial tanpa pembaruan saldo `BILRG_StokBatch` dan `BILRG_StokLokasi`, yang merusak integritas saldo Stock Ledger v2 terhadap `tb_stok`.

## Assumptions

- `A-01`: Tabel legacy `tb_buku` dan `tb_stok` di lingkungan lokasi masih aktif digunakan berdampingan (mode koeksistensi) dengan tabel Stock Ledger v2.
- `A-02`: Panjang kolom primary key pada tabel Stock Ledger v2 (`VARCHAR(12)` untuk `STB`, `STL`, `STM`, `SBD`), tabel legacy (`VARCHAR(10)` untuk `tb_buku.fs_kd_trs` dan `tb_stok.fs_kd_trs`), serta tabel BOM tindakan (`VARCHAR(26)` untuk `TBG` dan `PND`) harus tetap dipertahankan agar tidak merusak kontrak skema yang sudah terpasang.
- `A-03`: Pencatatan tindakan klinis (`POST /api/tindakan/create` dan `POST /api/tindakan/save`) merupakan alur kritis pelayanan pasien yang tidak boleh gagal hanya karena kegagalan pemotongan stok atau ketidakkonsistenan riwayat stok masa lalu.

## Open Questions

Tidak ada pertanyaan bisnis terbuka yang memblokir keputusan investigasi. Seluruh temuan didukung oleh bukti langsung dari kode sumber dan data operasional lokasi.

## Recommended Decision

Lakukan perbaikan menyeluruh dan terkoordinasi pada arsitektur Stock Ledger v2 dan pemroses BOM tindakan yang mencakup enam arah koreksi berikut:

1. Standarisasi mekanisme pembangkitan ID bebas tabrakan dan bebas `Thread.Sleep` untuk seluruh entitas Stock Ledger v2, mutasi legacy (`BK`, `ST`, `PK`), dan entitas BOM tindakan (`TBG`, `PND`), baik di dalam satu loop, lintas item dalam satu permintaan, maupun lintas permintaan konkuren.
2. Penyelarasan aturan keunikan mutasi (`BILRG_StokMutasi`) dan aturan idempotensi di seluruh command konsekuensi stok agar mendukung dokumen multi-baris (termasuk barang bonus pada DO, multi-item pada pemakaian barang/penjualan/dispensing, serta replay multi-baris dari `tb_buku`) tanpa kehilangan stok dan tanpa penolakan indeks unik.
3. Penegakan batas transaksi atomik dan operasi penyimpanan terkelompok pada proses `HydrateScopeFromLegacy` dan `CatchUpScopeFromLegacy`, disertai pengurutan deterministik jurnal legacy (mendahulukan mutasi masuk sebelum mutasi keluar pada stempel waktu yang sama) agar tidak pernah meninggalkan binding parsial tanpa batch/saldo lokasi.
4. Penyeragaman logika pengurangan saldo `tb_stok` multi-baris pada seluruh command pengeluaran/transfer stok (`PostStockTransferConsequenceCommand` dan `PostDispenseIssueConsequenceCommand`) agar dapat memotong lintas beberapa baris `tb_stok` pada DO dan tanggal kedaluwarsa yang sama sebagaimana command penjualan dan pemakaian internal.
5. Isolasi kegagalan sinkronisasi stok pada pemroses BOM tindakan (`TindakanBomProcessor`) agar ketidaksempurnaan riwayat stok masa lalu (`Inconsistent`) pada barang BOM tidak membatalkan pencatatan tindakan klinis, melainkan mengalihkan kebutuhan barang tersebut ke antrean stok tertunda (`BILRG_TindakanStokPending`).
6. Penyediaan skrip migrasi/provisioning database lokasi yang bersifat idempoten dan memperbarui definisi indeks yang berubah (bukan hanya memeriksa `IF NOT EXISTS` berdasarkan nama indeks).

## Decision

Menyetujui pendekatan koreksi komprehensif atas ketujuh temuan investigasi (mencakup pembangkitan ID bebas tabrakan tanpa penundaan thread, penyesuaian aturan keunikan dan idempotensi mutasi multi-baris, atomisitas dan determinisme replay legacy, pengurangan `tb_stok` multi-baris pada transfer/dispensing, ketahanan pemroses BOM tindakan terhadap scope tidak konsisten melalui antrean pending, serta pembaruan skrip provisioning database lokasi) untuk diteruskan ke tahap `ARCHITECTURE`.

## Decision Rationale

- Memperbaiki `LegacyScopeJournalReplayer` saja (misalnya hanya menambahkan deduplikasi pada `SBD`) terbukti tidak cukup karena baris kode yang sama akan segera gagal pada `STM`, `STL`, atau pada `TBG` dan `PND` di `TindakanBomProcessor` yang memiliki 3 barang BOM.
- Menggunakan `Thread.Sleep(5)` untuk menghindari duplikasi ID tidak layak dipakai pada replay `tb_buku` di production karena satu DO barang habis pakai dapat memiliki ribuan baris riwayat, yang akan memicu timeout transaksi dan mengunci tabel operasional.
- Mengabaikan atomisitas `HydrateScopeFromLegacy` / `CatchUpScopeFromLegacy` dan mengabaikan kasus multi-baris pada indeks `BILRG_StokMutasi` maupun `PostGoodsReceiptConsequenceHandler` berisiko menyebabkan korupsi saldo stok v2 (`bindings without batch` atau hilangnya stok barang bonus DO secara diam-diam).
- Membiarkan kegagalan freshness gate membatalkan transaksi `POST /api/tindakan/create` melanggar prinsip kelangsungan pelayanan klinis yang menjadi alasan utama dibentuknya antrean `BILRG_TindakanStokPending`.

## Architecture Applicability

### Decision

ARCHITECTURE-REQUIRED

### Rationale

Koreksi yang dibutuhkan menyentuh kontrak pembangkitan identitas lintas komponen, aturan keunikan skema/indeks pada `BILRG_StokMutasi`, kontrak idempotensi pada use case Stock Ledger v2, batas transaksi atomik pada mekanisme koeksistensi legacy (`Hydrate` dan `CatchUp`), serta kebijakan penanganan kegagalan (*fault-tolerance policy*) antara `TindakanBomProcessor` dan `StockLedgerFeature`. Perubahan lintas batas teknis dan persistensi ini memerlukan definisi arsitektur target yang eksplisit oleh `ica-architect` sebelum masuk ke tahap perencanaan dan implementasi.
