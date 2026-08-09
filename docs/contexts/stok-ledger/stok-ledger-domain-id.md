# Domain Stock Ledger

**Status artefak:** Spesifikasi bisnis kanonik — pendamping semantik Bahasa Indonesia

**Bounded context:** Stock Ledger

**Cakupan versi:** Model bisnis target untuk konsekuensi persediaan rumah sakit: asal penerimaan, saldo lokasi, mutasi, pengeluaran FEFO/FIFO, dan pembatalan melalui jurnal balik; termasuk koeksistensi dengan Legacy Stock Record selama operasi paralel

**Sumber canonical English:** [stok-ledger-domain.md](./stok-ledger-domain.md)

**Bukti terkait (non-normatif):** referensi generasi stok legacy `docs/stok-ledger/clbGenStokX1.cls`

## 1. Gambaran Umum Bisnis

### 1.1 Tujuan dan nilai

Stock Ledger memiliki konsekuensi persediaan yang dapat dipertanggungjawabkan dari transaksi bisnis yang mengubah jumlah atau lokasi stok. Domain ini tidak memiliki alasan bisnis mengapa barang diterima, dijual, dipindahkan, dipakai, dimusnahkan, disesuaikan, atau di-repack. Konteks bisnis asal yang memutuskan dan mengotorisasi fakta tersebut; Stock Ledger mencatat akibat stoknya dengan jejak Receipt Source yang berkelanjutan.

Bisnis harus memastikan bahwa:

- setiap jumlah stok yang dapat dipertanggungjawabkan tetap dapat ditelusuri ke Receipt Source-nya;
- Remaining Quantity tidak pernah menjadi negatif;
- pengeluaran di dalam satu Stock Location mengikuti FEFO bila Expiration Date ada, FIFO menurut urutan penerimaan bila Expiration Date tidak ada, dan dapat diganti dengan pemilihan Expiration Date eksplisit pada Source Stock Consequence;
- saldo yang mencapai nol tetap menjadi bagian representasi yang dapat dipertanggungjawabkan;
- Stock Movement yang sudah selesai tidak dihapus; void dicatat sebagai jurnal balik;
- riwayat mutasi dan saldo saat ini dapat direkonsiliasi untuk satu Item dan satu Receipt Source; dan
- selama Coexistence Period, Legacy Stock Record tetap menjadi kewenangan data yang tersimpan, sementara Stock Ledger memelihara Stock Ledger Representation yang lebih kaya dan harus tetap dapat direkonsiliasi dengannya.

### 1.2 Cakupan

Konteks ini mencakup:

1. pengakuan stok masuk dari penerimaan barang;
2. pembentukan dan pemeliharaan Stock Batch serta Location Stock Balance;
3. pencatatan Stock Movement;
4. Stock Transfer antar Stock Location dengan Receipt Source yang sama;
5. pengeluaran FEFO/FIFO untuk penjualan dan pemakaian lain yang diotorisasi, termasuk pemilihan Expiration Date eksplisit;
6. konsekuensi Retur Beli dan Retur Jual;
7. konsekuensi pemakaian internal dan pemusnahan;
8. penyesuaian stok naik dan turun;
9. konsumsi bahan dan pengakuan hasil pada Repack;
10. Stock Reversal melalui jurnal balik;
11. retensi Location Stock Balance yang habis;
12. rekonsiliasi per Item dan Receipt Source (seluruh rumah sakit dan per Stock Location); serta
13. koeksistensi dengan Legacy Stock Record selama operasi paralel.

**Di luar cakupan versi ini (ditunda):**

- konsekuensi stok Reserved Order; dan
- konsekuensi stok Serah Obat / Medication Handover (`DS`).

Kapabilitas itu dapat muncul di sistem target kemudian, tetapi tidak didefinisikan di dokumen ini.

### 1.3 Batas bisnis

Stock Ledger memiliki Stock Batch, Location Stock Balance, Stock Movement, hasil Outbound Allocation (FEFO/FIFO dan pemilihan Expiration Date eksplisit), kesinambungan Unit Valuation dalam satu Receipt Source, hasil rekonsiliasi representasinya, dan konsekuensi Stock Reversal.

Domain ini mengandalkan konteks lain tanpa mengambil alih kewenangannya:

- Purchasing atau penerimaan barang memiliki fakta komersial penerimaan;
- Apotek atau konteks pemenuhan lain memiliki fakta penjualan dan penyerahan bisnis;
- Stock Transfer memiliki maksud operasional pemindahan antar lokasi;
- pemakaian internal / suplai unit memiliki otorisasi konsumsi;
- otoritas pemusnahan memiliki otorisasi pemusnahan atau penghapusan;
- Stock Opname atau pengendalian persediaan memiliki otorisasi hitung fisik dan penyesuaian;
- Repack memiliki maksud transformasi kemasan atau bentuk;
- Product Catalog memiliki identitas Item dan satuan; serta
- otoritas fasilitas atau organisasi memiliki identitas Stock Location.

Stock Ledger tidak memutuskan apakah penerimaan, penjualan, transfer, retur, pemakaian, pemusnahan, penyesuaian, atau repack harus terjadi. Domain ini menerima Source Stock Consequence yang sudah diotorisasi lalu mencatat akibat persediaannya.

### 1.4 Kewenangan informasi selama koeksistensi

| Fakta | Pemilik yang berwenang |
|---|---|
| Identitas Item | Product Catalog |
| Identitas Stock Location | Otoritas fasilitas / organisasi |
| Transaksi bisnis sumber | Bounded context asal |
| Identitas Receipt Source | Otoritas penerimaan asal |
| Jumlah stok dan jurnal yang tersimpan selama Coexistence Period | Legacy Stock Record (`tb_stok` dan `tb_buku`) |
| Stock Ledger Representation target (termasuk saldo habis) | Stock Ledger |
| Alokasi keluar (FEFO/FIFO / Expiration Date eksplisit) yang dilakukan Stock Ledger | Stock Ledger |
| Rekonsiliasi Stock Ledger Representation | Stock Ledger |

```text
Model bisnis target
= Stock Ledger

Kewenangan data tersimpan selama Coexistence Period
= Legacy Stock Record (tb_stok + tb_buku)
```

Keberhasilan membentuk atau mencatat Stock Ledger Representation tidak memindahkan kewenangan data tersimpan dari Legacy Stock Record selama koeksistensi masih berlangsung.

### 1.5 Model bisnis utama

```text
Source Stock Consequence yang diotorisasi
  -> Stock Movement
       -> Stock Batch dibentuk atau diperbarui
            -> Location Stock Balance bertambah atau berkurang
                 -> Rekonsiliasi tersedia per Item + Receipt Source
```

Hubungan asal-usul:

```text
Item + Receipt Source
  -> satu Stock Batch
       -> Location Stock Balance di berbagai Stock Location
            -> Stock Movement yang menjaga atau menghabiskan jumlah batch
```

## 2. Ubiquitous Language

| Inggris | Indonesia | Definisi |
|---|---|---|
| Stock Ledger | Buku besar stok | Bounded context yang mencatat konsekuensi persediaan yang dapat dipertanggungjawabkan dengan jejak Receipt Source di seluruh Stock Location. |
| Item | Barang | Identitas barang katalog yang jumlah stoknya dilacak. |
| Stock Location | Lokasi stok | Tempat fisik atau logis tempat stok disimpan (misalnya gudang, apotek, atau unit klinis). |
| Receipt Source | Sumber penerimaan | Identitas tahan lama untuk satu kali masuknya barang ke sistem stok rumah sakit, biasanya identitas dokumen penerimaan. |
| Stock Batch | Batch stok | Stok yang dapat dipertanggungjawabkan dari satu Item dan satu Receipt Source di seluruh rumah sakit. |
| Location Stock Balance | Saldo stok lokasi | Sisa jumlah satu Stock Batch di satu Stock Location untuk satu Expiration Date (termasuk Expiration Date yang tidak ada). |
| Depleted Balance | Saldo habis | Location Stock Balance dengan Remaining Quantity nol yang tetap dipertahankan untuk akuntabilitas. |
| Remaining Quantity | Jumlah sisa | Jumlah yang masih tersedia dalam Stock Batch atau Location Stock Balance. |
| Initial Quantity | Jumlah awal | Jumlah yang diakui saat Location Stock Balance pertama kali terbentuk oleh mutasi masuk di lokasi itu. |
| Expiration Date | Tanggal kedaluwarsa | Tanggal kedaluwarsa yang melekat pada Location Stock Balance. Boleh tidak ada jika rumah sakit klien tidak mencatat kedaluwarsa. |
| Unit Valuation | Nilai satuan | Nilai stok per unit persediaan untuk jumlah yang berbagi Receipt Source yang sama. |
| Stock Movement | Mutasi stok | Fakta jumlah masuk atau keluar yang tidak diubah lagi untuk satu Location Stock Balance dari satu Source Stock Consequence. |
| Source Stock Consequence | Konsekuensi stok sumber | Efek persediaan yang diotorisasi setelah transaksi bisnis sumber selesai. |
| Source Transaction Reference | Referensi transaksi sumber | Identitas transaksi bisnis asal yang bertanggung jawab atas suatu Stock Movement. |
| Movement Kind | Jenis mutasi | Klasifikasi bisnis Stock Movement (penerimaan, transfer keluar/masuk, pengeluaran jual, retur, pemakaian, pemusnahan, penyesuaian, repack, atau pembalikan). |
| Outbound Allocation | Alokasi keluar | Pemilihan Location Stock Balance di satu Stock Location untuk memenuhi jumlah keluar menurut Explicit Expiry Selection, FEFO, atau FIFO. |
| Explicit Expiry Selection | Pemilihan kedaluwarsa eksplisit | Aturan keluar di mana Source Stock Consequence menyebut Expiration Date tertentu; hanya saldo dengan Expiration Date itu yang layak. |
| FEFO Allocation | Alokasi FEFO | Pemilihan First-Expire-First-Out: di antara saldo layak yang punya Expiration Date, Expiration Date yang lebih dekat (lebih awal) dipakai dulu; jika sama, diurutkan menurut urutan penerimaan. |
| FIFO Allocation | Alokasi FIFO | Pemilihan First-In-First-Out menurut urutan penerimaan (Receipt Source / waktu masuk), dipakai bila saldo layak tidak punya Expiration Date. |
| Stock Transfer | Mutasi antar lokasi | Sepasang mutasi keluar dan masuk yang memindahkan jumlah antar Stock Location tanpa mengubah Receipt Source. |
| Goods Receipt Consequence | Konsekuensi penerimaan barang | Pengakuan masuk barang yang diterima ke suatu Stock Location. |
| Purchase Return Consequence | Konsekuensi retur beli | Pengeluaran retur barang yang sebelumnya diterima kepada pemasok atau otoritas setara. |
| Sale Issue Consequence | Konsekuensi pengeluaran jual | Pengeluaran stok untuk penjualan obat atau barang. |
| Sales Return Consequence | Konsekuensi retur jual | Pemulihan masuk jumlah yang sebelumnya dikeluarkan karena penjualan, berdasarkan retur yang diotorisasi. |
| Internal Consumption Consequence | Konsekuensi pemakaian internal | Pengeluaran stok untuk pemakaian internal yang bukan penjualan pasien. |
| Destruction Consequence | Konsekuensi pemusnahan | Pengeluaran stok untuk dimusnahkan atau dihapusbukukan. |
| Stock Adjustment Consequence | Konsekuensi penyesuaian stok | Kenaikan atau penurunan jumlah tercatat yang diotorisasi agar selaras dengan keputusan pengendalian persediaan. |
| Repack Consequence | Konsekuensi repack | Konsumsi jumlah Item sumber dan pengakuan jumlah Item hasil dalam satu otorisasi repack. |
| Stock Reversal | Pembalikan stok | Konsekuensi jurnal balik yang menetralkan Stock Movement sebelumnya tanpa menghapusnya. |
| Reconciliation Scope | Lingkup rekonsiliasi | Kumpulan fakta yang dinilai bersama, terutama satu Item dan satu Receipt Source. |
| Stock Ledger Representation | Representasi Stock Ledger | Pandangan Stock Ledger atas batch, saldo lokasi, dan mutasi, termasuk saldo habis. |
| Legacy Stock Record | Catatan stok legacy | Jurnal dan saldo stok operasional yang tersimpan (`tb_buku` dan `tb_stok`) dan tetap menjadi kewenangan data selama koeksistensi. |
| Coexistence Period | Periode koeksistensi | Periode ketika Legacy Stock Record dan Stock Ledger Representation berjalan paralel. |
| Inventory Conservation | Konservasi persediaan | Dalam satu Item dan Receipt Source, jumlah masuk yang diakui sama dengan jumlah sisa ditambah pengeluaran final dan akibat penyesuaian bersih yang dapat dipertanggungjawabkan; transfer lokasi tidak mengubah jumlah batch di tingkat rumah sakit. |

## 3. Kapabilitas Bisnis

### 3.1 Goods Receipt Recognition

**Indonesia:** Pengakuan penerimaan barang

Mengakui jumlah masuk di suatu Stock Location, membentuk atau menambah Stock Batch dan Location Stock Balance, serta mencatat Unit Valuation dari fakta penerimaan yang diotorisasi.

### 3.2 Location Balance Maintenance

**Indonesia:** Pemeliharaan saldo lokasi

Memelihara Remaining Quantity per Stock Batch dan Stock Location, termasuk retensi Depleted Balance.

### 3.3 Stock Movement Recording

**Indonesia:** Pencatatan mutasi stok

Mencatat Stock Movement masuk dan keluar yang tidak diubah lagi, terhubung ke Source Transaction Reference dan Movement Kind.

### 3.4 Stock Transfer

**Indonesia:** Mutasi antar lokasi

Memindahkan jumlah antar Stock Location dengan mempertahankan Item, Receipt Source, dan Unit Valuation, serta jumlah keluar dan masuk yang sama.

### 3.5 Outbound Allocation

**Indonesia:** Alokasi pengeluaran

Memenuhi kebutuhan keluar di satu Stock Location dengan memakai Location Stock Balance menurut Explicit Expiry Selection, FEFO, atau FIFO, yang dapat mencakup lebih dari satu Stock Batch dan Expiration Date.

### 3.6 Sale and Return Consequences

**Indonesia:** Konsekuensi jual dan retur jual

Menerapkan Sale Issue dan Sales Return tanpa memiliki dokumen komersial penjualan.

### 3.7 Purchase Return Consequences

**Indonesia:** Konsekuensi retur beli

Menerapkan pengeluaran Retur Beli terhadap jumlah Receipt Source yang berlaku.

### 3.8 Consumption and Destruction Consequences

**Indonesia:** Konsekuensi pemakaian dan pemusnahan

Menerapkan pengeluaran Internal Consumption dan Destruction di Stock Location yang diotorisasi.

### 3.9 Stock Adjustment Consequences

**Indonesia:** Konsekuensi penyesuaian stok

Menerapkan kenaikan atau penurunan jumlah yang diotorisasi, dengan Unit Valuation yang dapat dipertanggungjawabkan untuk kenaikan.

### 3.10 Repack Consequences

**Indonesia:** Konsekuensi repack

Mengonsumsi jumlah sumber dan mengakui jumlah hasil dalam satu otorisasi repack sambil menjaga akuntabilitas kedua sisi.

### 3.11 Stock Reversal

**Indonesia:** Pembalikan stok

Menetralkan Stock Movement sebelumnya melalui jurnal balik yang mempertahankan mutasi asli dan memulihkan jumlah yang dikonservasi bila pembalikan masih sah secara bisnis.

### 3.12 Stock Reconciliation

**Indonesia:** Rekonsiliasi stok

Menilai konsistensi mutasi dan saldo dalam suatu Reconciliation Scope di tingkat rumah sakit dan per lokasi.

### 3.13 Legacy Coexistence Alignment

**Indonesia:** Penyelarasan koeksistensi legacy

Menjaga agar Stock Ledger Representation tetap dapat direkonsiliasi dengan Legacy Stock Record selama Legacy Stock Record tetap menjadi kewenangan data tersimpan.

## 4. Aktor & Peran

Stock Ledger tidak memiliki alur kerja langsung dengan pengguna akhir. Orang berinteraksi dengan kegiatan bisnis asal; Stock Ledger menerima Source Stock Consequence yang dihasilkan.

| Peran | Keterlibatan bisnis |
|---|---|
| Goods Receipt Officer | Mengotorisasi fakta penerimaan yang menghasilkan Goods Receipt Consequence. |
| Purchasing / Returns Officer | Mengotorisasi retur beli yang menghasilkan Purchase Return Consequence. |
| Pharmacy Staff | Mengotorisasi fakta jual dan retur jual yang menghasilkan Sale Issue atau Sales Return Consequence. |
| Stock Transfer Officer | Mengotorisasi pemindahan antar Stock Location. |
| Clinical / Unit Supply Officer | Mengotorisasi Internal Consumption. |
| Destruction Authorizer | Mengotorisasi Destruction Consequence. |
| Inventory Controller | Mengotorisasi Stock Adjustment dan koreksi berbasis hitung fisik. |
| Repack Officer | Mengotorisasi Repack Consequence. |
| Stock Ledger Steward | Meninjau selisih rekonsiliasi dan ketidakkonsistenan koeksistensi yang belum selesai; tidak menciptakan alasan bisnis sumber. |

Izin membuat transaksi sumber milik peran dan konteks asal tersebut. Stock Ledger boleh menolak konsekuensi yang melanggar kebijakan persediaan (misalnya jumlah layak tidak cukup) tanpa menyetujui keputusan bisnis sumber itu sendiri.

## 5. Domain Objects

### 5.1 Stock Batch

Mewakili seluruh jumlah yang dapat dipertanggungjawabkan untuk satu Item dan satu Receipt Source di seluruh rumah sakit.

Objek ini mempertahankan Remaining Quantity tingkat rumah sakit, Unit Valuation, dan kumpulan Location Stock Balance untuk asal tersebut.

### 5.2 Location Stock Balance

Mewakili Remaining Quantity satu Stock Batch di satu Stock Location untuk satu Expiration Date (termasuk Expiration Date yang tidak ada).

Oleh karena itu, Item, Receipt Source, dan Stock Location yang sama dapat memiliki lebih dari satu Location Stock Balance jika Expiration Date-nya berbeda. Setiap saldo boleh habis menjadi nol dan harus tetap dipertahankan. Riwayat mutasi menjelaskan bagaimana saldo terbentuk dan dipakai.

### 5.3 Stock Movement

Mewakili satu fakta jumlah masuk atau keluar yang tidak diubah lagi terhadap satu Location Stock Balance.

Objek ini mempertahankan Movement Kind, Source Transaction Reference, arah jumlah, Unit Valuation, dan waktu bisnis efektif dari efek stok.

### 5.4 Source Stock Consequence

Mewakili instruksi persediaan yang diotorisasi dari konteks asal setelah transaksi bisnisnya selesai.

Objek ini mengidentifikasi Item, jumlah, Stock Location, Receipt Source bila sudah diketahui, Expiration Date bila dipilih secara eksplisit atau disertakan pada pengakuan masuk, Movement Kind, Source Transaction Reference, dan waktu bisnis efektif.

### 5.5 Outbound Allocation

Mewakili cara satu kebutuhan keluar di satu Stock Location dipenuhi dari satu atau lebih Location Stock Balance menurut Explicit Expiry Selection, FEFO, atau FIFO.

### 5.6 Stock Transfer

Mewakili pasangan mutasi keluar dan masuk yang memindahkan lokasi tanpa mengubah Receipt Source atau jumlah Stock Batch tingkat rumah sakit.

### 5.7 Stock Reversal

Mewakili hubungan jurnal balik antara Stock Movement asli dan mutasi penyeimbangnya.

### 5.8 Stock Reconciliation

Mewakili satu penilaian konsistensi konservasi dan saldo untuk suatu Reconciliation Scope.

### 5.9 Legacy Stock Record

Mewakili kewenangan stok tersimpan yang sedang beroperasi selama koeksistensi. Objek ini dimiliki sistem stok legacy dan tidak dirancang ulang oleh dokumen domain ini.

## 6. Aggregates

### 6.1 Stock Batch Aggregate

**Aggregate Root:** `Stock Batch`

Batas konsistensi: satu Item dan satu Receipt Source.

Aggregate ini memiliki:

- Remaining Quantity tingkat rumah sakit untuk batch tersebut;
- Location Stock Balance batch itu di seluruh Stock Location dan Expiration Date;
- Stock Movement yang memengaruhi saldo tersebut untuk batch itu; serta
- status habis setiap Location Stock Balance.

Aggregate menjaga agar tetap konsisten:

- Inventory Conservation untuk batch;
- Unit Valuation untuk jumlah batch;
- Remaining Quantity tidak negatif; serta
- retensi Depleted Balance.

Satu Source Stock Consequence yang membutuhkan Outbound Allocation lintas beberapa Receipt Source mengoordinasikan beberapa Stock Batch aggregate. Setiap aggregate tetap konsisten untuk Receipt Source-nya sendiri.

Pembagian teknis penulisan untuk aggregate yang besar adalah urusan arsitektur dan tidak mengubah batas konsistensi bisnis ini.

### 6.2 Stock Reconciliation Aggregate

**Aggregate Root:** `Stock Reconciliation`

Batas konsistensi: satu penilaian Reconciliation Scope.

Aggregate ini memiliki total perhitungan, selisih, hasil, dan waktu penilaian efektif. Aggregate ini tidak menulis ulang diam-diam Stock Movement atau saldo yang sudah selesai.

## 7. Aturan Bisnis

### Asal-usul dan konservasi

- **BR-STL-001** — Setiap Stock Movement wajib berasal dari tepat satu Source Stock Consequence yang diotorisasi atau dari Stock Reversal atas mutasi sebelumnya.
- **BR-STL-002** — Stock Ledger tidak boleh menciptakan alasan bisnis untuk penerimaan, penjualan, transfer, retur, pemakaian, pemusnahan, penyesuaian, atau repack.
- **BR-STL-003** — Setiap Stock Movement wajib menyimpan Source Transaction Reference.
- **BR-STL-004** — Pengulangan Source Stock Consequence yang sama tidak boleh menggandakan jumlah persediaan.
- **BR-STL-005** — Setiap jumlah yang dapat dipertanggungjawabkan wajib memiliki tepat satu Receipt Source.
- **BR-STL-006** — Receipt Source wajib tetap tidak berubah melalui transfer, pengeluaran, retur, dan pembalikan, kecuali koreksi secara khusus menangani asal yang salah.
- **BR-STL-007** — Stock Transfer tidak boleh menciptakan Receipt Source baru.
- **BR-STL-008** — Jumlah dari Receipt Source berbeda wajib tetap dapat dipertanggungjawabkan secara terpisah meskipun Item dan Stock Location-nya sama.
- **BR-STL-009** — Dalam satu Item dan Receipt Source, transfer lokasi tidak boleh mengubah Remaining Quantity tingkat rumah sakit.
- **BR-STL-010** — Remaining Quantity tidak boleh menjadi negatif.

### Saldo dan valuasi

- **BR-STL-011** — Location Stock Balance yang Remaining Quantity-nya menjadi nol wajib tetap dipertahankan sebagai Depleted Balance.
- **BR-STL-012** — Depleted Balance tidak boleh dihapus diam-diam atau dipakai ulang sebagai identitas saldo yang berbeda.
- **BR-STL-013** — Unit Valuation wajib menyatakan nilai per unit persediaan, bukan nilai total baris.
- **BR-STL-014** — Unit Valuation untuk suatu Receipt Source tidak boleh diubah secara terpisah dari konsekuensi jumlah yang diotorisasi untuk asal tersebut.
- **BR-STL-015** — Receipt Source baru dengan Unit Valuation berbeda wajib membentuk Stock Batch terpisah.

### Mutasi dan pembalikan

- **BR-STL-016** — Stock Movement yang sudah selesai wajib bersifat tidak diubah lagi.
- **BR-STL-017** — Stock Movement wajib mencatat jumlah positif dan menyatakan arah sebagai masuk atau keluar.
- **BR-STL-018** — Stock Movement keluar wajib mengidentifikasi Location Stock Balance dan Receipt Source yang dipakai.
- **BR-STL-019** — Void atas konsekuensi sebelumnya wajib dicatat sebagai jurnal balik Stock Reversal; mutasi asli tidak boleh dihapus.
- **BR-STL-020** — Stock Reversal wajib merujuk mutasi atau Source Transaction Reference yang dibalik dan hanya boleh memulihkan jumlah yang dikonservasi jika pembalikan masih sah secara bisnis.
- **BR-STL-021** — Stock Transfer wajib mencatat jumlah keluar dan masuk yang sama untuk provenance yang dipindahkan.

### Alokasi keluar (FEFO / FIFO / kedaluwarsa eksplisit)

- **BR-STL-022** — Konsumsi keluar wajib hanya terjadi dalam Item dan Stock Location yang diminta transaksi sumber.
- **BR-STL-023** — Jika Source Stock Consequence memberi Explicit Expiry Selection, hanya Location Stock Balance dengan Expiration Date itu yang wajib layak.
- **BR-STL-024** — Jika tidak ada Explicit Expiry Selection dan saldo layak memiliki Expiration Date, alokasi wajib memakai FEFO: Expiration Date yang lebih dekat (lebih awal) dulu; Expiration Date yang sama kemudian mengikuti urutan penerimaan (waktu masuk / urutan Receipt Source).
- **BR-STL-025** — Jika tidak ada Explicit Expiry Selection dan saldo layak tidak memiliki Expiration Date, alokasi wajib memakai FIFO menurut urutan penerimaan (waktu masuk / urutan Receipt Source).
- **BR-STL-026** — Satu kebutuhan keluar boleh memakai beberapa Location Stock Balance lintas Expiration Date dan Stock Batch.
- **BR-STL-027** — Jumlah layak yang tidak mencukupi wajib menghasilkan penolakan atau jumlah tidak terpenuhi yang eksplisit; tidak boleh menghasilkan stok negatif.
- **BR-STL-028** — Location Stock Balance wajib berbeda untuk setiap kombinasi Stock Batch, Stock Location, dan Expiration Date (termasuk Expiration Date yang tidak ada).
- **BR-STL-029** — Transfer dan pengakuan masuk wajib mempertahankan Expiration Date dari jumlah yang dipindahkan atau diterima pada Location Stock Balance hasilnya.

### Rekonsiliasi dan koeksistensi

- **BR-STL-030** — Reconciliation Scope utama wajib satu Item dan satu Receipt Source di seluruh Stock Location.
- **BR-STL-031** — Rekonsiliasi juga wajib dapat dilakukan per Item, Receipt Source, dan Stock Location.
- **BR-STL-032** — Rekonsiliasi wajib mencakup Depleted Balance dan seluruh mutasi yang dapat dipertanggungjawabkan untuk lingkup tersebut.
- **BR-STL-033** — Selama Coexistence Period, Legacy Stock Record wajib tetap menjadi kewenangan data tersimpan untuk jumlah stok.
- **BR-STL-034** — Selama koeksistensi, Stock Ledger Representation wajib tetap dapat direkonsiliasi dengan Legacy Stock Record yang berlaku untuk lingkup yang sudah diselaraskan.
- **BR-STL-035** — Selisih rekonsiliasi wajib dicatat secara eksplisit dan tidak boleh diselesaikan dengan menulis ulang diam-diam mutasi yang sudah selesai.

### Kapabilitas yang ditunda

- **BR-STL-036** — Konsekuensi stok Reserved Order dan Serah Obat / Medication Handover berada di luar kapabilitas yang didefinisikan versi ini.

## 8. State Machines & Lifecycles

### 8.1 Lifecycle Location Stock Balance

```text
Established (Remaining Quantity > 0)
  -> Active (jumlah bertambah atau berkurang melalui mutasi)
  -> Depleted (Remaining Quantity = 0, tetap dipertahankan)
  -> Active (jika mutasi masuk atau pembalikan kemudian memulihkan jumlah)
```

Depleted adalah keadaan bisnis dari saldo yang sama, bukan penghapusan.

### 8.2 Lifecycle Stock Movement

```text
Recorded (immutable)
  -> optionally Counteracted by Stock Reversal (asli tetap terlihat)
```

Tidak ada mutasi draf yang dapat diedit di dalam Stock Ledger. Penyusunan draf milik transaksi bisnis asal sebelum Source Stock Consequence diterbitkan.

### 8.3 Lifecycle Stock Batch

```text
Established on first inbound recognition for Item + Receipt Source
  -> Active while hospital-wide Remaining Quantity > 0
  -> Fully Depleted while hospital-wide Remaining Quantity = 0 (batch retained)
  -> Active again if accountable inbound or reversal restores quantity
```

### 8.4 Lifecycle penyelarasan koeksistensi (per Item + Receipt Source)

```text
Not Aligned
  -> Aligned (Stock Ledger Representation dibentuk terhadap Legacy Stock Record)
  -> Stale (legacy berubah setelah penyelarasan terakhir)
  -> Aligned (setelah pengejaran ulang)
  -> Inconsistent (selisih belum selesai; konsekuensi stok native untuk lingkup itu diblokir sampai diselesaikan)
```

## 9. Domain Events

| Event | Arti bisnis |
|---|---|
| Stock Batch Established | Provenance stok Item + Receipt Source baru diakui. |
| Location Stock Balance Established | Batch pertama kali memiliki jumlah di suatu Stock Location. |
| Location Stock Balance Increased | Remaining Quantity di suatu lokasi bertambah. |
| Location Stock Balance Decreased | Remaining Quantity di suatu lokasi berkurang. |
| Location Stock Balance Depleted | Remaining Quantity di suatu lokasi mencapai nol dan tetap dipertahankan. |
| Stock Movement Recorded | Mutasi masuk atau keluar yang tidak diubah lagi telah dicatat. |
| Stock Transferred | Sepasang mutasi transfer selesai untuk suatu provenance. |
| Stock Issued | Kebutuhan keluar dipenuhi melalui Outbound Allocation (FEFO, FIFO, atau Explicit Expiry Selection). |
| Stock Reversed | Jurnal balik Stock Reversal menetralkan mutasi sebelumnya. |
| Stock Reconciliation Completed | Penilaian rekonsiliasi selesai dengan suatu hasil. |
| Stock Scope Aligned | Reconciliation Scope menjadi selaras dengan Legacy Stock Record. |
| Stock Scope Marked Inconsistent | Selisih koeksistensi atau konservasi yang belum selesai diidentifikasi. |

## 10. Workflow Bisnis

Workflow berikut menggambarkan alur data dan konsekuensi, bukan prosedur antarmuka operator.

### 10.1 Inbound Goods Receipt Consequence

```text
Originating Goods Receipt completed
  -> Source Stock Consequence accepted
  -> Stock Movement inbound recorded
  -> Stock Batch established or increased
  -> Location Stock Balance established or increased
```

### 10.2 Stock Transfer Consequence

```text
Originating transfer authorized
  -> Source Stock Consequence accepted
  -> Outbound Allocation or explicit provenance selection at source location (as applicable)
  -> Stock Movement outbound at source location
  -> Stock Movement inbound at destination location
  -> Location balances updated; hospital-wide batch quantity unchanged
```

### 10.3 Outbound Sale Issue Consequence

```text
Originating sale authorized
  -> Source Stock Consequence accepted for Item + Stock Location + quantity
       (+ optional Explicit Expiry Selection)
  -> Outbound Allocation (Explicit Expiry Selection, else FEFO, else FIFO)
  -> one or more outbound Stock Movements (one per consumed Location Stock Balance)
  -> affected Stock Batches and balances decreased
```

### 10.4 Return, Consumption, Destruction, Adjustment, Repack

```text
Originating transaction authorized
  -> Source Stock Consequence accepted
  -> applicable inbound and/or outbound Stock Movements recorded
  -> Stock Batch and Location Stock Balance updated under conservation and valuation rules
```

### 10.5 Stock Reversal (void)

```text
Originating void authorized for a prior Source Transaction Reference
  -> reverse-journal Source Stock Consequence accepted
  -> counteracting Stock Movement(s) recorded against the original movement(s)
  -> balances restored when reversal is valid
  -> original movement(s) remain visible
```

### 10.6 Coexistence native consequence (parallel period)

```text
Originating transaction will be recorded through Stock Ledger
  -> ensure affected Item + Receipt Source scope is Aligned and not Stale/Inconsistent against Legacy Stock Record
  -> apply Stock Ledger consequence rules
  -> persist effect so Legacy Stock Record and Stock Ledger Representation both reflect the outcome
```

### 10.7 Coexistence legacy-originated change

```text
Legacy Stock Record changes from a legacy-originated transaction
  -> Stock Ledger Representation catch-up for the affected scope
  -> alignment restored or Inconsistent marked
```

### 10.8 Reconciliation

```text
Choose Reconciliation Scope (Item + Receipt Source, optionally + Stock Location)
  -> compare movement conservation with Location Stock Balances (including depleted)
  -> during coexistence, compare with Legacy Stock Record
  -> emit reconciliation outcome
```
