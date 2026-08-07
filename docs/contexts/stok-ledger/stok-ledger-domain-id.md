# Domain Stock Ledger

**Status artefak:** Pendamping semantik Bahasa Indonesia

**Bounded context:** Stock Ledger

**Cakupan versi:** Modernisasi bertahap pencatatan persediaan legacy sambil menjaga kompatibilitas dengan transaksi stok yang ada

**Sumber canonical English:** [stok-ledger-domain.md](./stok-ledger-domain.md)

---

## 1. Gambaran Umum Bisnis

### 1.1 Tujuan dan nilai

Stock Ledger menetapkan model bisnis target untuk pergerakan jumlah persediaan, asal-usul stok, Stock Layer, jumlah sisa, dan nilai satuan di seluruh Stock Location.

Domain ini memastikan setiap jumlah persediaan yang dapat dipertanggungjawabkan dapat ditelusuri ke Receipt Source asalnya sepanjang penerimaan, transfer, pemakaian, retur, koreksi, dan konsekuensi persediaan lainnya.

Stock Ledger bekerja di belakang transaksi bisnis yang dimiliki bounded context lain. Konteks tersebut menentukan mengapa persediaan harus bergerak. Stock Ledger menentukan dan mencatat konsekuensi persediaan yang dapat dipertanggungjawabkan.

Selama Coexistence Period, Legacy Stock Record tetap menjadi kebenaran stok yang tersimpan dan berwenang. Stock Ledger memelihara Stock Ledger Representation yang lebih kaya, yang harus tetap dapat direkonsiliasi dengan dan diselaraskan ke kewenangan legacy itu selama kedua sistem terus memproses transaksi stok.

Domain ini harus memastikan bahwa:

* setiap jumlah stok mempertahankan Item, Receipt Source, Stock Location, Expiration Date bila berlaku, dan Unit Valuation;
* pergerakan antar lokasi mempertahankan Receipt Source asli;
* pemakaian stok mengikuti FIFO di dalam Stock Location yang diminta transaksi sumber, kecuali transaksi itu secara eksplisit mengidentifikasi Expiration Date;
* pemakaian FIFO tetap dapat ditelusuri ke Stock Layer yang dipakai;
* Stock Layer yang habis tetap menjadi bagian dari Stock Ledger Representation meskipun Legacy Stock Record tidak dapat mempertahankannya;
* ledger mutasi dan posisi stok saat ini dapat direkonsiliasi dalam lingkup terbatas;
* satu transaksi bisnis sumber tidak menghasilkan konsekuensi stok ganda;
* koreksi dan pembalikan mempertahankan fakta yang sudah tercatat;
* reservasi stok direpresentasikan sebagai transfer yang dapat dipertanggungjawabkan ke Virtual Stock Location;
* asal-usul stok legacy dapat direkonstruksi secara bertahap tanpa migrasi historis penuh; dan
* fakta hasil rekonstruksi tetap dapat dibedakan dari fakta yang dicatat secara native oleh Stock Ledger baru;
* perubahan stok yang berasal dari legacy boleh terus terjadi setelah rekonstruksi dan wajib dimasukkan melalui Legacy Synchronization;
* Remaining Quantity negatif dilarang tanpa pengecualian; dan
* Stock Ledger tidak menambah persetujuan di luar otoritas yang sudah ditetapkan oleh transaksi sumber.

### 1.2 Cakupan

Konteks ini mencakup:

1. pengakuan penerimaan stok yang dapat dipertanggungjawabkan;
2. pembentukan dan pemeliharaan Stock Layer;
3. pencatatan mutasi stok;
4. transfer stok antar Stock Location;
5. pemakaian stok FIFO dan pemilihan Expiration Date secara eksplisit;
6. reservasi stok melalui Virtual Stock Location;
7. retur stok dan penyesuaian yang dapat dipertanggungjawabkan;
8. pelacakan Remaining Quantity, Expiration Date, dan Unit Valuation;
9. pelestarian Stock Layer yang habis;
10. rekonsiliasi per Item dan Receipt Source;
11. Legacy Stock Reconstruction bertahap;
12. deteksi konsekuensi stok ganda atau tidak konsisten; dan
13. koeksistensi dengan pemrosesan stok legacy yang berlanjut serta Legacy Synchronization; dan
14. penerbitan hasil mutasi stok dan posisi stok yang dapat dipertanggungjawabkan.

### 1.3 Batas bisnis

Stock Ledger memiliki:

* Stock Movement;
* Stock Layer;
* Stock Position menurut Item, Receipt Source, dan Stock Location;
* alokasi FIFO dan pemilihan Expiration Date eksplisit untuk jumlah keluar;
* kontinuitas asal-usul;
* konsekuensi jumlah persediaan;
* kontinuitas Expiration Date dan Unit Valuation di dalam satu Stock Layer;
* mutasi reservasi menuju dan dari Virtual Stock Location;
* rekonsiliasi stok;
* konsekuensi koreksi dan pembalikan stok; serta
* hasil Legacy Stock Reconstruction.

Stock Ledger mengandalkan bounded context lain tanpa mengambil alih kewenangannya:

* Purchasing atau Goods Receipt memiliki fakta komersial dan operasional bahwa barang diterima;
* Pharmacy, Sales, Clinical Supply, atau konteks fulfillment lain memiliki fakta bahwa barang disediakan atau dijual;
* Stock Transfer memiliki maksud operasional dan otorisasi untuk memindahkan persediaan;
* Stock Opname memiliki kegiatan penghitungan fisik dan jumlah yang diamati;
* Return Processing memiliki alasan bisnis dan otorisasi untuk retur;
* Product Catalog memiliki identitas Item dan definisi satuan;
* Organizational atau Facility Management memiliki identitas Stock Location; dan
* Finance atau Accounting memiliki konsekuensi akuntansi keuangan di luar Unit Valuation Stock Ledger.

Stock Ledger tidak menentukan apakah penjualan, penerimaan, transfer, reservasi, pembuangan, retur, atau penyesuaian harus terjadi. Domain ini hanya mencatat konsekuensi stok setelah menerima fakta bisnis sumber yang dapat dipertanggungjawabkan atau permintaan yang diotorisasi. Setiap persetujuan yang diperlukan untuk aktivitas tersebut milik transaksi asal dan sudah selesai sebelum pemrosesan Stock Ledger dimulai.

Selama Coexistence Period, Stock Ledger tidak memperoleh kepemilikan eksklusif atas suatu Item + Receipt Source hanya karena lingkup itu diproses secara native atau berhasil direkonstruksi. Transaksi stok yang berasal dari legacy boleh terus memengaruhi lingkup yang sama.

### 1.4 Kewenangan informasi

| Fakta bisnis | Pemilik yang berwenang |
| --- | --- |
| Identitas Item | Product Catalog |
| Identitas Stock Location | Otoritas Facility atau Organizational |
| Penerimaan barang komersial | Purchasing atau Goods Receipt |
| Identitas transaksi sumber | Bounded context asal |
| Identitas Receipt Source | Otoritas penerimaan asal |
| Transfer yang diminta | Stock Transfer |
| Maksud reservasi | Konteks transaksi asal |
| Keputusan pembuangan, kerusakan, atau kehilangan | Konteks transaksi asal |
| Pemenuhan penjualan atau penyediaan | Konteks fulfillment asal |
| Hasil penghitungan fisik | Stock Opname |
| Penyesuaian stok yang diotorisasi | Otoritas persediaan yang bertanggung jawab |
| Fakta jumlah dan mutasi stok yang tersimpan selama Coexistence Period | Legacy Stock Authority |
| Asal-usul Stock Layer dan representasi lapisan habis yang dipertahankan | Stock Ledger |
| Alokasi FIFO dan yang dibatasi kedaluwarsa yang dilakukan Stock Ledger | Stock Ledger |
| Hasil rekonsiliasi Stock Ledger | Stock Ledger |
| Entri akuntansi keuangan | Finance atau Accounting |

Stock Ledger tidak boleh menyimpulkan bahwa suatu transaksi bisnis valid hanya karena pergerakan persediaan secara teknis memungkinkan.

Pembedaan kewenangan dinyatakan secara eksplisit:

```text
Target business model
= Stock Ledger

Persisted stock source of truth during coexistence
= Legacy Stock Record
```

Rekonstruksi yang berhasil tidak mengubah hubungan kewenangan ini.

### 1.5 Model bisnis utama

```text
Source Business Fact
  -> Stock Consequence Request
       -> Stock Movement
            -> Stock Layer established, increased, decreased, or transferred
                 -> Stock Position updated
                      -> Reconciliation available
```

Hubungan asal-usul utama adalah:

```text
Item
  + Receipt Source
      -> one or more Stock Layers
           -> distributed across physical or Virtual Stock Locations
                -> reserved, consumed, or moved while retaining the same provenance
```

Stock Layer boleh mencapai Remaining Quantity nol, tetapi tetap menjadi bagian yang dapat dipertanggungjawabkan dari riwayat Stock Position.

### 1.6 Karakter sebagai domain pendukung

Stock Ledger adalah supporting bounded context. Domain ini mungkin tidak memiliki workflow langsung yang dihadapi pengguna untuk transaksi biasa.

Perilaku bisnisnya terutama dipicu oleh fakta dan permintaan dari bounded context lain. Tidak adanya interaksi pengguna langsung tidak mengurangi tanggung jawabnya atas mutasi persediaan, asal-usul, saldo, dan rekonsiliasi.

### 1.7 Kewenangan koeksistensi

Coexistence Period adalah kondisi bisnis peralihan di mana kemampuan pemrosesan stok legacy dan yang baru beroperasi secara bersamaan.

Selama periode ini:

- Legacy Stock Record tetap menjadi sumber kebenaran untuk fakta jumlah dan mutasi stok yang tersimpan;
- Stock Ledger boleh memproses transaksi baru dengan aturan domainnya sambil mempertahankan konsekuensi stok yang kompatibel dengan legacy;
- transaksi yang berasal dari legacy boleh terus terjadi setelah suatu Item + Receipt Source direkonstruksi;
- Legacy Stock Reconstruction menetapkan baseline awal Stock Ledger, bukan pemindahan kewenangan;
- Legacy Synchronization memasukkan perubahan legacy berikutnya ke dalam Stock Ledger Representation; dan
- perbedaan yang tidak dapat dijelaskan antara Legacy Stock Record dan Stock Ledger Representation membuat lingkup yang terdampak tidak konsisten sampai direkonsiliasi.

Cutover akhir di masa depan boleh mengubah kewenangan runtime, tetapi cutover tersebut berada di luar keputusan domain saat ini.

---

## 2. Ubiquitous Language

| Inggris | Indonesia | Definisi |
| --- | --- | --- |
| Stock Ledger | Buku Besar Stok | Bounded context yang menetapkan model target untuk mutasi persediaan, asal-usul, Stock Layer, alokasi, dan rekonsiliasi. Selama Coexistence Period, representasinya tetap diselaraskan ke Legacy Stock Authority. |
| Item | Item | Produk yang dikelola sebagai persediaan dan memiliki identitas unik. |
| Stock Location | Lokasi Stok | Lokasi bisnis tempat persediaan disimpan, misalnya gudang, apotek, bangsal, klinik, atau unit gawat darurat. |
| Virtual Stock Location | Lokasi Stok Virtual | Stock Location logis yang memisahkan persediaan menurut ketersediaan bisnis, misalnya stok cadangan, tanpa mengubah fasilitas fisik atau asal-usulnya. |
| Receipt Source | Sumber Penerimaan | Identitas yang dapat dipertanggungjawabkan dari peristiwa atau dokumen yang membuat persediaan pertama kali masuk ke organisasi. Pada model legacy saat ini, identitas ini direpresentasikan oleh `KodeDO`. |
| Stock Provenance | Asal-usul Stok | Jejak asal persediaan dari Receipt Source-nya sepanjang seluruh transfer, pemakaian, retur, dan koreksi berikutnya. |
| Stock Layer | Lapisan Stok | Jumlah yang dapat dipertanggungjawabkan dari satu Item yang terkait dengan satu Receipt Source, satu Stock Location, satu Unit Valuation, dan satu mutasi pembentuk lapisan. |
| Layer-Forming Movement | Mutasi Pembentuk Lapisan | Stock Movement yang membentuk Stock Layer di suatu Stock Location. |
| Initial Quantity | Jumlah Awal | Jumlah yang dipegang Stock Layer ketika lapisan itu dibentuk. |
| Remaining Quantity | Jumlah Sisa | Jumlah yang saat ini tersedia atau dapat dipertanggungjawabkan di dalam suatu Stock Layer. |
| Depleted Stock Layer | Lapisan Stok Habis | Stock Layer yang Remaining Quantity-nya nol. Lapisan ini tetap bagian dari Stock Ledger Representation dan tidak dibuang. |
| Unit Valuation | Nilai Satuan | Nilai persediaan per unit yang dipertahankan oleh Stock Layer. |
| Expiration Date | Tanggal Kedaluwarsa | Tanggal setelahnya Stock Layer tidak lagi layak untuk pemakaian biasa. Tanggal ini dipertahankan sepanjang transfer, reservasi, retur, dan pemakaian. |
| Explicit Expiry Selection | Pemilihan Kedaluwarsa Eksplisit | Instruksi dari transaksi sumber yang membatasi Stock Layer yang layak ke Expiration Date tertentu sebelum urutan FIFO diterapkan. |
| Stock Position | Posisi Stok | Representasi Stock Ledger terkini atas Stock Layer untuk lingkup Item, Receipt Source, dan Stock Location yang ditentukan. |
| Stock Movement | Mutasi Stok | Fakta bisnis yang tidak dapat diubah bahwa jumlah persediaan masuk, keluar, pindah antar lokasi, dikembalikan, atau disesuaikan. |
| Stock Movement Line | Baris Mutasi Stok | Satu konsekuensi jumlah di dalam Stock Movement, terkait dengan Item, Receipt Source, Stock Location, arah, jumlah, dan Unit Valuation. |
| Source Business Fact | Fakta Bisnis Sumber | Fakta yang berwenang dari bounded context lain yang memberi alasan bisnis bagi konsekuensi stok. |
| Source Transaction Reference | Referensi Transaksi Sumber | Identitas stabil yang menghubungkan Stock Movement dengan transaksi bisnis yang menyebabkannya. |
| Stock Receipt | Penerimaan Stok | Stock Movement yang mengakui persediaan masuk ke pengendalian stok yang dapat dipertanggungjawabkan dari sumber eksternal. |
| Stock Transfer | Transfer Stok | Pergerakan persediaan keluar dan masuk yang terkoordinasi antara dua Stock Location dengan tetap mempertahankan Item, Receipt Source, dan Unit Valuation. |
| Stock Consumption | Pemakaian Stok | Stock Movement keluar yang disebabkan oleh penjualan, dispensing, penggunaan, kerusakan, kedaluwarsa, atau disposisi akhir lain yang dapat dipertanggungjawabkan. |
| FIFO | FIFO | Kebijakan yang memakai Stock Layer yang layak menurut urutan yang berlaku dari yang tertua ke yang terbaru. |
| FIFO Allocation | Alokasi FIFO | Pembagian yang dapat dipertanggungjawabkan dari satu jumlah keluar ke satu atau lebih Stock Layer yang layak. |
| Stock Reservation | Reservasi Stok | Pemisahan logis persediaan dengan mentransfernya dari Stock Location biasa ke Virtual Stock Location yang ditentukan. |
| Reservation Release | Pelepasan Reservasi | Transfer persediaan yang sebelumnya direservasi dari Virtual Stock Location kembali ke Stock Location biasa yang berlaku. |
| Stock Return | Retur Stok | Mutasi yang mengembalikan persediaan yang sebelumnya dipindahkan atau dipakai ke Receipt Source dan Stock Layer asalnya bila dapat ditelusuri. Transaksi retur tetap menjadi Source Transaction Reference. |
| Stock Adjustment | Penyesuaian Stok | Koreksi jumlah persediaan yang diotorisasi akibat selisih atau keputusan bisnis yang dapat dipertanggungjawabkan. |
| Stock Correction | Koreksi Stok | Fakta baru yang dapat dipertanggungjawabkan yang mengoreksi Stock Movement sebelumnya tanpa menghapus fakta asli. |
| Stock Reversal | Pembalikan Stok | Stock Movement yang menetralkan Stock Movement sebelumnya sambil mempertahankan kedua fakta. |
| Stock Reconciliation | Rekonsiliasi Stok | Penilaian bahwa Stock Movement dan Stock Position cocok dalam Reconciliation Scope yang ditentukan. |
| Reconciliation Scope | Lingkup Rekonsiliasi | Kumpulan terbatas fakta persediaan yang dinilai bersama. Lingkup utama adalah satu Item dan satu Receipt Source di seluruh Stock Location. |
| Reconciliation Difference | Selisih Rekonsiliasi | Perbedaan jumlah atau nilai yang ditemukan selama Stock Reconciliation. |
| Native Stock Fact | Fakta Stok Native | Stock Layer atau Stock Movement yang dicatat langsung menurut aturan Stock Ledger baru. |
| Legacy-Synchronized Stock Fact | Fakta Stok Hasil Penyelarasan Legacy | Stock Movement, atau Stock Layer yang dibentuk oleh mutasi itu, yang dicatat di Stock Ledger dari transaksi yang berasal dari legacy setelah baseline Stock Ledger yang berlaku ditetapkan. |
| Legacy Stock Fact | Fakta Stok Legacy | Fakta stok yang berasal dari model persediaan legacy. |
| Legacy Stock Reconstruction | Rekonstruksi Stok Legacy | Rekonstruksi yang dapat dipertanggungjawabkan atas Stock Layer yang hilang dari riwayat mutasi legacy yang tersedia. |
| Reconstructed Stock Layer | Lapisan Stok Hasil Rekonstruksi | Stock Layer yang dibentuk dari Legacy Stock Reconstruction, bukan dari Stock Receipt native. |
| Reconstruction Scope | Lingkup Rekonstruksi | Item dan Receipt Source yang seluruh asal-usul legacy-nya direkonstruksi di semua Stock Location. |
| Reconstruction Trigger | Pemicu Rekonstruksi | Aktivitas pemrosesan stok pertama yang memenuhi syarat dan membutuhkan Item serta Receipt Source yang belum direkonstruksi. |
| Reconstruction Status | Status Rekonstruksi | Keadaan yang dapat dipertanggungjawabkan yang menunjukkan apakah Reconstruction Scope belum direkonstruksi, sedang direkonstruksi, sudah direkonstruksi, atau ditemukan tidak konsisten. |
| Provenance Continuity | Kontinuitas Asal-usul | Aturan bahwa Receipt Source dan Unit Valuation tetap dapat ditelusuri sepanjang transfer dan pemakaian. |
| Inventory Conservation | Konservasi Persediaan | Aturan bahwa jumlah yang masuk ke suatu lingkup asal-usul sama dengan jumlah yang tersisa ditambah hasil keluar dan penyesuaian yang dapat dipertanggungjawabkan. |
| Stock Consequence | Konsekuensi Stok | Efek jumlah persediaan yang dihasilkan dari Source Business Fact yang diotorisasi. |
| Duplicate Stock Consequence | Konsekuensi Stok Ganda | Lebih dari satu Stock Movement yang dapat dipertanggungjawabkan yang dibuat untuk tanggung jawab transaksi sumber yang sama. |
| Coexistence Period | Periode Koeksistensi | Periode peralihan di mana kemampuan pemrosesan stok legacy dan yang baru beroperasi secara bersamaan sementara Legacy Stock Record tetap menjadi kebenaran stok tersimpan yang berwenang. |
| Legacy Stock Authority | Kewenangan Stok Legacy | Kewenangan bisnis Legacy Stock Record selama Coexistence Period. |
| Legacy Stock Record | Catatan Stok Legacy | Representasi stok terkini dan riwayat mutasi legacy yang tetap berwenang selama koeksistensi. Pada sistem legacy saat ini, ini direpresentasikan oleh `tb_stok` dan `tb_buku`. |
| Stock Ledger Representation | Representasi Stock Ledger | Pandangan Stock Ledger yang lebih kaya atas mutasi stok, asal-usul, Stock Layer, lapisan habis, dan fakta rekonsiliasi. |
| Legacy Synchronization | Penyelarasan Legacy | Pemasukan yang dapat dipertanggungjawabkan atas perubahan stok legacy yang tercatat setelah rekonstruksi awal ke dalam Stock Ledger Representation. |
| Synchronization Position | Posisi Penyelarasan | Titik yang diketahui sampai di mana fakta stok legacy yang berlaku sudah tercermin di Stock Ledger Representation. Ini menyatakan kesegaran data, bukan mekanisme penyimpanan teknis. |

---

## 3. Kapabilitas Bisnis

### 3.1 Stock Receipt Recognition

**Indonesia:** Pengakuan Penerimaan Stok

Mengakui persediaan yang masuk ke organisasi secara dapat dipertanggungjawabkan dan menetapkan Receipt Source, Initial Quantity, Unit Valuation, serta Stock Layer pertamanya.

### 3.2 Stock Provenance Management

**Indonesia:** Pengelolaan Asal-usul Stok

Mempertahankan hubungan antara jumlah persediaan dan Receipt Source-nya di setiap Stock Location dan mutasi.

### 3.3 Stock Layer Management

**Indonesia:** Pengelolaan Lapisan Stok

Membentuk dan memelihara Stock Layer, termasuk lapisan yang Remaining Quantity-nya sudah nol.

### 3.4 Stock Movement Recording

**Indonesia:** Pencatatan Mutasi Stok

Mencatat konsekuensi masuk, keluar, transfer, retur, penyesuaian, koreksi, dan pembalikan yang tidak dapat diubah.

### 3.5 Expiry-Constrained FIFO Consumption

**Indonesia:** Pemakaian FIFO dengan Batasan Kedaluwarsa

Mengalokasikan permintaan persediaan keluar di dalam Stock Location yang diminta transaksi sumber. Menerapkan Explicit Expiry Selection bila diberikan; jika tidak, menerapkan FIFO, sambil mempertahankan jumlah tepat yang dipakai dari setiap lapisan.

### 3.6 Stock Reservation Management

**Indonesia:** Pengelolaan Reservasi Stok

Merreservasi dan melepas persediaan melalui transfer yang dapat dipertanggungjawabkan antara Stock Location biasa dan Virtual Stock Location sambil mempertahankan asal-usul.

### 3.7 Stock Transfer Coordination

**Indonesia:** Koordinasi Transfer Stok

Mengoordinasikan pergerakan keluar dan masuk antar Stock Location sambil mempertahankan Receipt Source dan Unit Valuation.

### 3.8 Stock Position Management

**Indonesia:** Pengelolaan Posisi Stok

Memelihara Stock Ledger Representation atas Remaining Quantity menurut Item, Receipt Source, Stock Location, dan Stock Layer.

### 3.9 Stock Reconciliation

**Indonesia:** Rekonsiliasi Stok

Memvalidasi bahwa Stock Movement dan Stock Position tetap konsisten secara kuantitatif untuk satu Item dan Receipt Source di seluruh Stock Location.

### 3.10 Stock Correction and Reversal

**Indonesia:** Koreksi dan Pembalikan Stok

Mengoreksi konsekuensi persediaan melalui fakta baru yang dapat dipertanggungjawabkan tanpa menghapus atau menulis ulang diam-diam Stock Movement yang sudah selesai.

### 3.11 Legacy Stock Reconstruction

**Indonesia:** Rekonstruksi Stok Legacy

Merekonstruksi secara bertahap Stock Layer yang sebelumnya dihapus atau tidak tersedia ketika suatu Item dan Receipt Source pertama kali membutuhkan pemrosesan di bawah Stock Ledger baru.

### 3.12 Cross-Context Stock Consequence Coordination

**Indonesia:** Koordinasi Konsekuensi Stok Lintas Konteks

Menerima fakta sumber yang dapat dipertanggungjawabkan dari bounded context lain dan menerbitkan hasil stok yang dapat dipertanggungjawabkan tanpa mengambil kepemilikan atas transaksi bisnis asal.

### 3.13 Legacy Coexistence and Synchronization

**Indonesia:** Koeksistensi dan Penyelarasan Legacy

Mendukung kelanjutan pemrosesan stok legacy sambil memelihara Stock Ledger Representation yang lebih kaya dan diselaraskan ke Legacy Stock Authority selama Coexistence Period.

---

## 4. Aktor & Peran

Stock Ledger terutama dipicu oleh bounded context lain. Peran manusia terlibat ketika otorisasi bisnis atau keputusan pengecualian diperlukan.

### 4.1 Inventory Officer

Memulai atau mengonfirmasi aktivitas persediaan yang diizinkan dalam kewenangannya dan menyelidiki selisih stok.

### 4.2 Receiving Officer

Mengonfirmasi informasi penerimaan fisik yang dibutuhkan proses Goods Receipt yang bertanggung jawab. Receiving Officer tidak secara mandiri mendefinisikan asal-usul Stock Ledger di luar penerimaan yang dapat dipertanggungjawabkan.

### 4.3 Stock Transfer Officer

Melaksanakan pemindahan yang diotorisasi antar Stock Location dan tetap bertanggung jawab atas bukti transfer.

### 4.4 Stock Opname Officer

Melakukan penghitungan stok fisik dan menyerahkan jumlah yang diamati kepada proses Stock Opname yang bertanggung jawab.

### 4.5 Inventory Controller

Menelaah hasil rekonsiliasi dan menyelidiki selisih. Persetujuan penyesuaian, pembuangan, kerusakan, kehilangan, pemusnahan, koreksi, atau pembalikan tetap dimiliki konteks transaksi asal.

### 4.6 System Administrator

Boleh mendukung operasi teknis tetapi tidak memiliki keputusan jumlah stok, asal-usul, valuasi, rekonsiliasi, atau koreksi.

---

## 5. Domain Objects

### 5.1 Stock Movement

Merepresentasikan konsekuensi jumlah persediaan yang dapat dipertanggungjawabkan di dalam model Stock Ledger.

Stock Movement mengidentifikasi:

* Source Transaction Reference-nya;
* tipe mutasi bisnisnya;
* waktu bisnis efektifnya;
* sumber penanggung jawabnya;
* Stock Movement Line-nya; dan
* hubungan apa pun ke mutasi yang mengoreksi atau membalik.

Stock Movement yang sudah selesai bersifat immutable. Koreksi kemudian membentuk Stock Movement lain.

### 5.2 Stock Movement Line

Merepresentasikan satu efek persediaan masuk atau keluar.

Setiap baris mengidentifikasi:

* Item;
* Receipt Source;
* Stock Location;
* arah mutasi;
* jumlah;
* Unit Valuation; dan
* Stock Layer yang terdampak bila berlaku.

Transfer direpresentasikan oleh baris keluar dan masuk yang terkoordinasi.

### 5.3 Stock Layer

Merepresentasikan satu lapisan jumlah persediaan yang dapat dipertanggungjawabkan di satu Stock Location.

Stock Layer mempertahankan:

* Item;
* Receipt Source;
* Stock Location;
* mutasi pembentuk lapisan;
* Initial Quantity;
* Remaining Quantity;
* Unit Valuation;
* Expiration Date bila berlaku;
* Effective Receipt Time;
* identitas Stock Layer yang dipakai sebagai kunci urutan deterministik terakhir;
* informasi urutan lapisan yang dibutuhkan FIFO;
* klasifikasi asal sebagai native, reconstructed, atau legacy-synchronized; dan
* status depletion.

Stock Layer tetap dipertahankan di Stock Ledger Representation ketika Remaining Quantity-nya mencapai nol.

### 5.4 Stock Position

Merepresentasikan jumlah terkini yang dapat dipertanggungjawabkan dari Stock Layer dalam lingkup yang ditentukan.

Stock Position dapat dilihat menurut:

* Item dan Stock Location;
* Item dan Receipt Source;
* Item, Receipt Source, dan Stock Location; atau
* Stock Layer individu.

Stock Position tidak menggantikan riwayat Stock Movement.

### 5.5 FIFO Allocation

Merepresentasikan bagaimana satu kebutuhan persediaan keluar dipenuhi dari satu atau lebih Stock Layer.

Objek ini mempertahankan:

* jumlah keluar yang diminta;
* Stock Location yang diminta;
* Expiration Date yang diminta secara eksplisit bila diberikan;
* Stock Layer yang dipilih;
* jumlah yang dipakai dari setiap lapisan;
* Unit Valuation dari setiap jumlah yang dipakai; dan
* jumlah yang tidak terpenuhi ketika stok yang layak tidak mencukupi.

### 5.6 Stock Transfer

Merepresentasikan konsekuensi persediaan terkoordinasi dari pemindahan jumlah antar Stock Location.

Objek ini mempertahankan yang sama:

* Item;
* Receipt Source;
* Unit Valuation;
* Expiration Date bila berlaku; dan
* jumlah yang ditransfer.

Stock Layer tujuan dibentuk dari asal-usul yang ditransfer. Ini bukan Receipt Source baru.

### 5.7 Stock Reconciliation

Merepresentasikan satu penilaian konsistensi mutasi dan posisi dalam Reconciliation Scope.

Objek ini mencatat:

* Item;
* Receipt Source;
* Stock Location yang diikutsertakan;
* total jumlah penerimaan yang diakui;
* total jumlah keluar yang dapat dipertanggungjawabkan;
* total Remaining Quantity;
* total konsekuensi penyesuaian;
* hasil rekonsiliasi;
* selisih apa pun; dan
* waktu bisnis efektif.

### 5.8 Legacy Stock Reconstruction

Merepresentasikan rekonstruksi yang dapat dipertanggungjawabkan atas Stock Layer untuk satu Item dan Receipt Source di seluruh Stock Location.

Objek ini mengidentifikasi:

* Reconstruction Scope;
* fakta legacy yang dipertimbangkan;
* lapisan hasil rekonstruksi;
* inkonsistensi yang belum terselesaikan;
* hasil rekonstruksi; dan
* pembedaan antara fakta hasil rekonstruksi dan fakta native.

Legacy Stock Reconstruction tidak menciptakan fakta sumber yang tidak tersedia.

### 5.9 Stock Correction

Merepresentasikan hubungan antara Stock Movement yang salah atau tidak lengkap dengan mutasi kemudian yang mengoreksinya.

Mutasi asli tetap terlihat sebagai fakta historis yang dapat dipertanggungjawabkan.

### 5.10 Stock Consequence Request

Merepresentasikan instruksi bisnis dari bounded context lain yang meminta konsekuensi persediaan.

Objek ini mengidentifikasi:

* tanggung jawab bisnis sumber;
* Source Transaction Reference;
* konsekuensi yang diminta;
* Item;
* jumlah;
* Stock Location yang berlaku;
* Expiration Date bila diketahui secara eksplisit;
* Receipt Source bila sudah ditentukan; dan
* waktu bisnis efektif.

Penerimaan Stock Consequence Request tidak memindahkan kepemilikan transaksi bisnis sumber ke Stock Ledger.

### 5.11 Legacy Stock Synchronization

Merepresentasikan hubungan bisnis yang menjaga lingkup Stock Ledger yang sebelumnya direkonstruksi atau dicatat secara native tetap mutakhir terhadap fakta stok kemudian yang dicatat di bawah Legacy Stock Authority.

Objek ini mengidentifikasi:

* Item dan Receipt Source yang terdampak;
* Synchronization Position;
* fakta stok legacy yang belum tercermin di Stock Ledger;
* perubahan Stock Ledger yang dihasilkan; dan
* inkonsistensi apa pun yang mencegah representasi dianggap mutakhir.

Legacy Stock Synchronization memperbarui Stock Ledger Representation tanpa memindahkan kewenangan runtime dari Legacy Stock Record selama Coexistence Period.

---

## 6. Aggregates

### 6.1 Stock Movement Aggregate

**Aggregate Root:** `Stock Movement`

Aggregate ini memiliki Stock Movement Line dan menjaga fakta berikut tetap saling konsisten:

* Source Transaction Reference;
* tipe mutasi;
* arah mutasi;
* jumlah yang terdampak;
* Stock Location yang terdampak;
* Receipt Source;
* Unit Valuation;
* pasangan transfer;
* hubungan koreksi atau pembalikan; dan
* penyelesaian mutasi.

Untuk transfer, aggregate memastikan jumlah keluar dan masuk sama serta asal-usul dipertahankan.

Stock Movement yang sudah selesai bersifat immutable.

### 6.2 Stock Position Aggregate

**Aggregate Root:** `Stock Position`

Aggregate ini merepresentasikan satu Item dan Receipt Source sebagai lingkup asal-usul utamanya.

Aggregate ini memiliki Stock Layer untuk asal-usul tersebut di seluruh Stock Location dan menjaga agar saling konsisten:

* Initial Quantity;
* Remaining Quantity;
* Stock Location;
* Unit Valuation;
* Expiration Date;
* Effective Receipt Time;
* urutan Stock Layer yang deterministik;
* urutan FIFO;
* asal native, reconstructed, atau legacy-synchronized;
* keadaan depletion; dan
* total Remaining Quantity.

Aggregate ini mempertahankan Stock Layer yang habis.

Stock Position tidak memiliki transaksi komersial sumber, Product Catalog, atau data master Stock Location.

### 6.3 Stock Reconciliation Aggregate

**Aggregate Root:** `Stock Reconciliation`

Aggregate ini menilai satu Item dan Receipt Source di seluruh Stock Location.

Aggregate ini memiliki:

* input rekonsiliasi;
* total mutasi yang dihitung;
* total Stock Position;
* selisih yang diidentifikasi;
* hasil rekonsiliasi;
* penelaah yang bertanggung jawab bila berlaku; dan
* referensi penyelesaian ketika selisih membutuhkan koreksi.

Hasil rekonsiliasi tidak mengubah diam-diam Stock Movement atau Stock Position.

### 6.4 Legacy Stock Reconstruction Aggregate

**Aggregate Root:** `Legacy Stock Reconstruction`

Aggregate ini mengoordinasikan rekonstruksi untuk satu Item dan Receipt Source.

Aggregate ini memiliki:

* Reconstruction Scope;
* Reconstruction Status;
* interpretasi asal-usul hasil rekonstruksi;
* Stock Layer hasil rekonstruksi;
* ambiguitas yang belum terselesaikan;
* hasil penyelesaian rekonstruksi; dan
* klasifikasi asal-usul.

Hanya satu rekonstruksi aktif yang boleh ada untuk Item dan Receipt Source yang sama.

### 6.5 Hubungan lintas aggregate

Stock Movement boleh membentuk, menambah, mengurangi, mentransfer, menghabiskan, atau mengoreksi Stock Layer di dalam satu atau lebih Stock Position.

Stock Reconciliation membaca Stock Movement dan Stock Position yang berlaku, tetapi tidak memiliki keduanya.

Legacy Stock Reconstruction membentuk Stock Layer yang sebelumnya hilang dan menandainya sebagai reconstructed. Selama Coexistence Period, Stock Movement native berikutnya boleh berlanjut dari baseline itu hanya setelah perubahan legacy yang berlaku diselaraskan. Stock Movement yang berasal dari legacy juga boleh terus terjadi dan wajib tercermin melalui Legacy Synchronization.

Koordinasi lintas aggregate harus mempertahankan:

* satu konsekuensi yang dapat dipertanggungjawabkan per tanggung jawab sumber;
* konsistensi Item dan Receipt Source;
* konservasi jumlah;
* kontinuitas Unit Valuation; dan
* keterlacakan koreksi.

---

## 7. Aturan Bisnis

### 7.1 Kewenangan sumber dan konsekuensi stok

* **BR-STL-001** — Setiap Stock Movement wajib berasal dari tepat satu Source Business Fact yang dapat dipertanggungjawabkan, penyesuaian yang diotorisasi, hasil rekonstruksi, koreksi, atau pembalikan.
* **BR-STL-002** — Stock Ledger tidak boleh menciptakan alasan bisnis untuk penerimaan, penjualan, dispensing, transfer, retur, atau Stock Opname.
* **BR-STL-003** — Setiap Stock Movement wajib mempertahankan Source Transaction Reference yang cukup untuk menelusurinya ke tanggung jawab bisnis asalnya.
* **BR-STL-004** — Satu tanggung jawab transaksi sumber wajib menghasilkan paling banyak satu Stock Consequence aktif yang dapat dipertanggungjawabkan dari tipe yang sama.
* **BR-STL-005** — Pengulangan tanggung jawab sumber yang sama tidak boleh menduplikasi jumlah persediaan.
* **BR-STL-006** — Stock Ledger wajib menolak atau mengidentifikasi konsekuensi yang diminta bila Item, jumlah, lokasi, atau asal-usulnya bertentangan dengan fakta sumber yang berwenang dari konteks sumber yang bertanggung jawab atau, selama koeksistensi, dengan Legacy Stock Record.

### 7.2 Receipt Source dan asal-usul

* **BR-STL-007** — Setiap jumlah persediaan yang diakui Stock Ledger wajib memiliki tepat satu Receipt Source.
* **BR-STL-008** — Receipt Source wajib tetap tidak berubah sepanjang transfer, pemakaian, retur, koreksi, dan rekonsiliasi.
* **BR-STL-009** — Stock Transfer tidak boleh menciptakan Receipt Source baru.
* **BR-STL-010** — Stock Layer tujuan yang dibentuk oleh transfer wajib mempertahankan Receipt Source dan Unit Valuation dari jumlah sumber.
* **BR-STL-011** — Jumlah dari Receipt Source berbeda wajib tetap dapat dipertanggungjawabkan secara terpisah meskipun merepresentasikan Item yang sama di Stock Location yang sama.
* **BR-STL-012** — Stock Ledger tidak boleh menggabungkan Stock Layer bila penggabungan itu menghilangkan keterlacakan Receipt Source, Unit Valuation, FIFO, atau mutasi.

### 7.3 Stock Layer

* **BR-STL-013** — Setiap Stock Layer wajib mengidentifikasi satu Item, satu Receipt Source, satu Stock Location, satu mutasi pembentuk lapisan, dan satu Unit Valuation.
* **BR-STL-014** — Initial Quantity wajib positif ketika Stock Layer dibentuk.
* **BR-STL-015** — Remaining Quantity tidak boleh melebihi Initial Quantity kecuali melalui retur atau koreksi jumlah yang dapat dipertanggungjawabkan dan diproses dari transaksi sumber yang sudah diotorisasi.
* **BR-STL-016** — Remaining Quantity tidak boleh menjadi negatif pada transaksi atau setting pelayanan apa pun.
* **BR-STL-017** — Stock Layer yang Remaining Quantity-nya menjadi nol wajib tetap menjadi bagian dari Stock Ledger Representation.
* **BR-STL-018** — Depleted Stock Layer tidak boleh dihapus diam-diam atau dipakai ulang sebagai lapisan yang berbeda.
* **BR-STL-019** — Unit Valuation wajib merepresentasikan nilai per unit persediaan, bukan nilai total lapisan.
* **BR-STL-020** — Unit Valuation wajib tetap tidak berubah untuk jumlah yang mempertahankan asal-usul yang sama dan tidak boleh dikoreksi secara terpisah dari konsekuensi jumlah serta transaksi sumber.
* **BR-STL-021** — Receipt Source baru dengan Unit Valuation berbeda wajib membentuk Stock Layer terpisah.

### 7.4 Mutasi stok

* **BR-STL-022** — Stock Movement yang sudah selesai wajib bersifat immutable.
* **BR-STL-023** — Stock Movement wajib mencatat jumlah positif dan menyatakan arah secara terpisah sebagai masuk atau keluar.
* **BR-STL-024** — Stock Movement keluar wajib mengidentifikasi Stock Layer dari mana jumlah diambil.
* **BR-STL-025** — Stock Movement masuk wajib mengidentifikasi apakah membentuk Stock Layer baru, mengembalikan Stock Layer dan Receipt Source asli, atau berasal dari transfer.
* **BR-STL-026** — Waktu bisnis efektif mutasi dan waktu pencatatan wajib tetap dapat dibedakan ketika keduanya berbeda.
* **BR-STL-027** — Setiap Stock Movement material wajib mempertahankan sumber penanggung jawab dan waktu bisnis efektif.
* **BR-STL-028** — Stock Movement yang sudah selesai tidak boleh dihapus karena jumlahnya telah dipakai sepenuhnya.

### 7.5 Pemakaian FIFO

* **BR-STL-029** — Pemakaian keluar wajib memakai FIFO di dalam Stock Location yang ditentukan transaksi asal kecuali transaksi itu memberi Explicit Expiry Selection.
* **BR-STL-030** — Kelayakan Stock Layer wajib dibatasi pada Item dan Stock Location yang diminta transaksi asal; Stock Ledger tidak boleh memilih stok dari Stock Location lain secara implisit.
* **BR-STL-031** — Ketika Expiration Date diberikan secara eksplisit, hanya Stock Layer dengan Expiration Date itu yang layak sebelum urutan FIFO diterapkan.
* **BR-STL-032** — Stock Layer yang layak wajib diurutkan terlebih dahulu menurut Effective Receipt Time, lalu menurut identitas Stock Layer ketika Effective Receipt Time sama.
* **BR-STL-033** — Satu kebutuhan keluar boleh memakai jumlah dari beberapa Stock Layer.
* **BR-STL-034** — Setiap jumlah yang dipakai dari suatu Stock Layer wajib mempertahankan Receipt Source, Expiration Date, dan Unit Valuation lapisan tersebut.
* **BR-STL-035** — Ketika satu kebutuhan keluar memakai beberapa Stock Layer, Stock Ledger wajib mencatat Stock Movement Line terpisah yang dapat dipertanggungjawabkan untuk setiap lapisan yang dipakai.
* **BR-STL-036** — Stock Ledger tidak boleh memakai lebih dari Remaining Quantity yang layak.
* **BR-STL-037** — Stok yang layak tidak mencukupi wajib menghasilkan jumlah tidak terpenuhi yang eksplisit atau penolakan; tidak boleh menghasilkan stok negatif.
* **BR-STL-038** — Depleted Stock Layer wajib dikecualikan dari alokasi berikutnya tetapi dipertahankan untuk keterlacakan dan rekonsiliasi.

### 7.6 Transfer stok

* **BR-STL-039** — Setiap Stock Transfer yang selesai wajib mengandung jumlah keluar dan masuk yang sama.
* **BR-STL-040** — Stock Transfer wajib mengidentifikasi satu Stock Location sumber dan satu Stock Location tujuan.
* **BR-STL-041** — Stock Location sumber dan tujuan tidak boleh sama untuk Stock Transfer biasa.
* **BR-STL-042** — Stock Transfer boleh memakai beberapa Stock Layer sumber dan membentuk Stock Layer tujuan yang sesuai.
* **BR-STL-043** — Setiap Stock Layer tujuan wajib tetap dapat ditelusuri ke jumlah Stock Layer sumber yang darinya dibentuk.
* **BR-STL-044** — Penyelesaian transfer wajib mempertahankan total jumlah untuk setiap Item dan Receipt Source.
* **BR-STL-045** — Selisih transfer wajib menerima hasil pengecualian, penyesuaian, kehilangan, retur, atau koreksi yang dapat dipertanggungjawabkan.

### 7.7 Konservasi persediaan dan rekonsiliasi

* **BR-STL-046** — Reconciliation Scope terutama wajib didefinisikan oleh satu Item dan satu Receipt Source di seluruh Stock Location.
* **BR-STL-047** — Rekonsiliasi wajib mengikutsertakan Depleted Stock Layer.
* **BR-STL-048** — Rekonsiliasi wajib mengikutsertakan setiap mutasi yang dapat dipertanggungjawabkan yang terkait dengan Item dan Receipt Source yang berlaku.
* **BR-STL-049** — Total jumlah yang diakui untuk satu Item dan Receipt Source wajib sama dengan total Remaining Quantity ditambah semua jumlah keluar final yang dapat dipertanggungjawabkan dan konsekuensi penyesuaian neto.
* **BR-STL-050** — Pergerakan antar Stock Location tidak boleh mengubah total jumlah dalam lingkup Item dan Receipt Source yang sama.
* **BR-STL-051** — Jumlah Remaining Quantity di semua Stock Layer dalam Reconciliation Scope wajib sama dengan posisi Stock Ledger terkini untuk lingkup itu; selama koeksistensi posisi itu harus cocok dengan Legacy Stock Record yang berlaku.
* **BR-STL-052** — Reconciliation Difference wajib dicatat secara eksplisit dan tidak boleh diselesaikan dengan menulis ulang diam-diam mutasi yang sudah selesai.
* **BR-STL-053** — Hasil rekonsiliasi wajib mengidentifikasi lingkup, waktu efektif, total yang dibandingkan, dan hasilnya.
* **BR-STL-054** — Rekonsiliasi yang berhasil tidak boleh membuktikan bahwa transaksi komersial atau operasional asal sudah benar secara keseluruhan.

Invariant jumlah utama adalah:

```text
Recognized Receipt Quantity
+ Accountable Inbound Adjustments
=
Remaining Quantity Across All Locations
+ Final Outbound Quantity
+ Accountable Outbound Adjustments
```

Transfer internal dikecualikan dari kedua sisi persamaan ini karena mempertahankan total jumlah di dalam Receipt Source.

### 7.8 Koreksi dan pembalikan

* **BR-STL-055** — Stock Movement yang sudah selesai wajib dikoreksi melalui Stock Correction atau Stock Reversal baru.
* **BR-STL-056** — Stock Correction wajib mereferensikan mutasi atau fakta sumber yang dikoreksi.
* **BR-STL-057** — Stock Reversal wajib mempertahankan Stock Movement asli dan mencatat konsekuensi jumlah yang menetralkannya.
* **BR-STL-058** — Koreksi dan pembalikan wajib mempertahankan Receipt Source dan Unit Valuation kecuali koreksi secara khusus menangani asal-usul atau valuasi yang salah.
* **BR-STL-059** — Koreksi yang mengubah asal-usul wajib mempertahankan keterlacakan ke asal-usul yang sebelumnya tercatat maupun yang dikoreksi.
* **BR-STL-060** — Koreksi tidak boleh menyebabkan Remaining Quantity menjadi negatif.
* **BR-STL-061** — Fakta yang dikoreksi dan fakta asli wajib tetap terlihat secara terpisah untuk rekonsiliasi.

### 7.9 Rekonstruksi stok legacy

* **BR-STL-062** — Legacy Stock Reconstruction wajib dipicu hanya ketika Item dan Receipt Source yang belum direkonstruksi membutuhkan pemrosesan di bawah Stock Ledger baru.
* **BR-STL-063** — Reconstruction Scope wajib mencakup Item dan Receipt Source di seluruh Stock Location.
* **BR-STL-064** — Rekonstruksi tidak boleh dibatasi hanya pada Stock Location yang memicunya.
* **BR-STL-065** — Legacy Stock Reconstruction wajib memakai fakta mutasi legacy yang tersedia dan dapat dipertanggungjawabkan, serta tidak boleh menciptakan identitas historis yang tidak tersedia.
* **BR-STL-066** — Identitas Stock Layer legacy yang sudah tidak ada tidak boleh dibuat ulang seolah identitas aslinya diketahui.
* **BR-STL-067** — Reconstructed Stock Layer wajib menerima identitas baru yang dapat dipertanggungjawabkan sambil mempertahankan Item, Receipt Source, Stock Location, Unit Valuation, dan asal-usul mutasi yang tersedia.
* **BR-STL-068** — Reconstructed Stock Layer dengan Remaining Quantity nol wajib dipertahankan.
* **BR-STL-069** — Reconstructed Stock Fact wajib tetap dapat dibedakan dari Native Stock Fact dan Legacy-Synchronized Stock Fact.
* **BR-STL-070** — Satu Item dan Receipt Source wajib memiliki paling banyak satu hasil rekonstruksi baseline yang selesai untuk dasar rekonstruksi yang sama.
* **BR-STL-071** — Pemrosesan rekonstruksi berulang tidak boleh menduplikasi Stock Layer atau jumlah.
* **BR-STL-072** — Stock Movement native tidak boleh dilanjutkan terhadap Item dan Receipt Source yang belum direkonstruksi bila hal itu menciptakan asal-usul atau rekonsiliasi yang tidak lengkap.
* **BR-STL-073** — Inkonsistensi rekonstruksi wajib dicatat dan diangkat untuk penyelesaian yang dapat dipertanggungjawabkan, bukan diseimbangkan diam-diam.
* **BR-STL-074** — Penyelesaian rekonstruksi wajib menetapkan baseline dari mana pemrosesan Stock Ledger native berikutnya dan Legacy Synchronization berlanjut.
* **BR-STL-075** — Rekonstruksi legacy tidak boleh mensyaratkan migrasi seluruh riwayat persediaan sebelum Stock Ledger baru boleh beroperasi.

### 7.10 Koeksistensi legacy

* **BR-STL-076** — Selama Coexistence Period, Legacy Stock Record wajib tetap menjadi sumber tersimpan yang berwenang untuk kebenaran jumlah dan mutasi stok.
* **BR-STL-077** — Rekonstruksi yang berhasil atau pemrosesan Stock Ledger native tidak dengan sendirinya memindahkan kewenangan runtime untuk suatu Item dan Receipt Source dari Legacy Stock Record.
* **BR-STL-078** — Transaksi stok yang berasal dari legacy boleh terus terjadi setelah suatu Item dan Receipt Source direkonstruksi atau diproses secara native.
* **BR-STL-079** — Sebelum Stock Ledger mengandalkan representasinya untuk keputusan stok berikutnya selama koeksistensi, perubahan stok legacy yang berlaku setelah Synchronization Position wajib dimasukkan, atau lingkup itu wajib dianggap belum mutakhir.
* **BR-STL-080** — Perbedaan representasi yang semata-mata disebabkan oleh ketidakmampuan Legacy Stock Record mempertahankan detail Stock Ledger, misalnya lapisan yang habis, tidak dengan sendirinya boleh diperlakukan sebagai inkonsistensi jumlah.

### 7.11 Penyelesaian dan keterlacakan

* **BR-STL-081** — Setiap jumlah stok wajib tetap dapat ditelusuri dari Receipt Source-nya ke Stock Layer saat ini atau hasil keluar final yang dapat dipertanggungjawabkan.
* **BR-STL-082** — Setiap jumlah keluar wajib mengidentifikasi jumlah Stock Layer yang dipakai.
* **BR-STL-083** — Setiap jumlah yang ditransfer wajib mengidentifikasi Stock Location sumber dan tujuan.
* **BR-STL-084** — Setiap jumlah hasil rekonstruksi wajib mengidentifikasi Reconstruction Scope dan hasil rekonstruksinya.
* **BR-STL-085** — Setiap koreksi wajib mempertahankan fakta asli dan fakta yang mengoreksi.
* **BR-STL-086** — Stock Ledger wajib mempertahankan asal-usul yang cukup untuk menjelaskan jumlah terkini dan Unit Valuation tanpa memerlukan pemutaran ulang seluruh riwayat persediaan organisasi.

### 7.12 Penyelesaian penerimaan dan retur

* **BR-STL-087** — Receipt Source yang sudah selesai tidak boleh menerima jumlah tambahan sebagai penerimaan biasa.
* **BR-STL-088** — Koreksi jumlah kemudian yang terkait dengan Receipt Source yang sudah selesai wajib dicatat sebagai koreksi atau penyesuaian, bukan sebagai penerimaan biasa tambahan.
* **BR-STL-089** — Jumlah retur yang dapat ditelusuri ke Stock Layer asalnya wajib mengembalikan Stock Layer itu dan mempertahankan Receipt Source, Expiration Date, serta Unit Valuation aslinya.
* **BR-STL-090** — Transaksi yang memicu retur wajib dipertahankan sebagai Source Transaction Reference dan tidak boleh menggantikan Receipt Source asli.
* **BR-STL-091** — Ketika Stock Layer asli tidak dapat diidentifikasi dari fakta legacy, retur wajib membentuk Stock Layer baru yang dapat dipertanggungjawabkan tanpa menciptakan identitas historis yang tidak diketahui.

### 7.13 Reservasi dan kelayakan stok

* **BR-STL-092** — Stock Reservation wajib direpresentasikan sebagai Stock Transfer dari Stock Location biasa ke Virtual Stock Location yang ditentukan.
* **BR-STL-093** — Reservation Release wajib direpresentasikan sebagai Stock Transfer dari Virtual Stock Location kembali ke Stock Location biasa yang berlaku.
* **BR-STL-094** — Reservasi dan pelepasan wajib mempertahankan Item, Receipt Source, Expiration Date, Unit Valuation, dan jumlah.
* **BR-STL-095** — Stok yang berada di Virtual Stock Location tidak boleh dipilih untuk transaksi yang meminta Stock Location lain.
* **BR-STL-096** — Transaksi sumber wajib menentukan Stock Location dari mana persediaan boleh dipilih. Stock Ledger tidak boleh memindahkan atau memakai stok dari lokasi lain secara implisit.
* **BR-STL-097** — Pembuangan, kerusakan, kehilangan, pemusnahan, dan disposisi luar biasa lain wajib berasal dari transaksi sumber yang sudah diotorisasi. Stock Ledger tidak menambah persetujuan tambahan.

### 7.14 Kardinalitas permintaan sumber

* **BR-STL-098** — Satu baris transaksi sumber biasanya wajib menghasilkan satu Stock Consequence Request untuk satu Item, jumlah yang diminta, dan Stock Location yang diminta.
* **BR-STL-099** — Baris transaksi sumber tidak diwajibkan mengidentifikasi Receipt Source atau Stock Layer kecuali secara eksplisit mengidentifikasi Expiration Date atau batasan asal-usul lain yang diizinkan.
* **BR-STL-100** — Satu Stock Consequence Request boleh menghasilkan beberapa Stock Movement Line karena alokasi FIFO, pemilihan Expiration Date eksplisit, pasangan transfer, atau beberapa Stock Layer yang terdampak.
* **BR-STL-101** — Beberapa Stock Movement Line yang dihasilkan dari satu Stock Consequence Request wajib tetap dapat ditelusuri ke baris transaksi sumber yang sama.

### 7.15 Kelengkapan rekonstruksi dan ambiguitas

* **BR-STL-102** — Rekonstruksi boleh selesai ketika Remaining Quantity dapat ditentukan untuk setiap Item dan Receipt Source meskipun identitas Stock Layer legacy asli sudah tidak diketahui.
* **BR-STL-103** — Ketika hanya total jumlah Item yang dapat ditentukan tetapi distribusinya menurut Receipt Source tidak dapat ditentukan, rekonstruksi wajib berstatus `Inconsistent` dan pemrosesan native tidak boleh dilanjutkan untuk lingkup yang terdampak.
* **BR-STL-104** — Ketika urutan mutasi legacy ambigu tetapi tidak mengubah hasil jumlah, Receipt Source, Expiration Date, atau Unit Valuation, rekonstruksi boleh memakai urutan fallback deterministik dan wajib menandai pengurutan itu sebagai hasil rekonstruksi, bukan fakta historis yang terbukti.
* **BR-STL-105** — Ketika urutan legacy yang ambigu mengubah hasil jumlah, Receipt Source, Expiration Date, atau Unit Valuation, rekonstruksi wajib berstatus `Inconsistent`.
* **BR-STL-106** — Legacy Stock Reconstruction yang berhasil wajib menetapkan baseline Stock Ledger yang seimbang untuk Item dan Receipt Source yang berlaku. Hal itu tidak memindahkan kewenangan runtime dari Legacy Stock Record selama Coexistence Period.
* **BR-STL-107** — `Legacy Stock Reconstruction Completed` adalah hasil domain internal yang dipicu selama permintaan mutasi stok pertama untuk Item dan Receipt Source yang belum direkonstruksi; transaksi transisi yang dihadapi pengguna tidak diperlukan, dan hasil itu tidak menyiratkan pemindahan kewenangan.

### 7.16 Stock Opname dan penyelarasan koeksistensi

* **BR-STL-108** — Stock Opname hanya menyediakan jumlah fisik yang diamati dan tidak menentukan Receipt Source atau Unit Valuation.
* **BR-STL-109** — Setiap selisih jumlah yang diidentifikasi dari Stock Opname wajib diselesaikan melalui transaksi penyesuaian sumber yang sudah diotorisasi sebelum Stock Ledger mencatat konsekuensinya.
* **BR-STL-110** — Legacy Stock Record boleh menghilangkan detail yang tidak dapat direpresentasikan modelnya, tetapi penghilangan itu tidak boleh menghapus fakta Stock Ledger yang lebih kaya atau ditafsirkan sebagai selisih jumlah ketika konsekuensi jumlah legacy tetap benar.
* **BR-STL-111** — Selama Coexistence Period, rekonsiliasi antara Legacy Stock Record dan Stock Ledger Representation wajib memperlakukan Legacy Stock Record sebagai sumber kebenaran yang tersimpan sambil mempertahankan detail asal-usul yang hanya ada di Stock Ledger dan tidak dapat diungkapkan representasi legacy.
* **BR-STL-112** — Legacy Stock Reconstruction menetapkan baseline awal Stock Ledger; perubahan stok legacy berikutnya wajib dimasukkan melalui Legacy Synchronization, bukan mensyaratkan rekonstruksi penuh ulang ketika baseline sebelumnya masih valid.
* **BR-STL-113** — Native Stock Fact, Reconstructed Stock Fact, dan Legacy-Synchronized Stock Fact menjelaskan asal fakta Stock Ledger dan tidak boleh ditafsirkan sebagai keadaan kewenangan selama Coexistence Period.
* **BR-STL-114** — Ketika Legacy Stock Record dan Stock Ledger Representation berbeda dalam jumlah atau fakta material lain di luar keterlambatan penyelarasan yang dapat dijelaskan atau keterbatasan representasi, lingkup yang terdampak wajib berstatus `Inconsistent` sampai direkonsiliasi.
* **BR-STL-115** — Transaksi stok yang berasal dari sistem baru boleh memakai aturan Stock Ledger untuk menentukan konsekuensinya, tetapi selama koeksistensi fakta stok yang dihasilkannya wajib tetap kompatibel dengan Legacy Stock Record yang berwenang.

---

## 8. State Machines & Lifecycles

### 8.1 Lifecycle Stock Movement

```text
Proposed
  -> Recorded
       -> Reversed
       -> Corrected
```

| State | Makna bisnis |
| --- | --- |
| Proposed | Sumber yang dapat dipertanggungjawabkan telah meminta konsekuensi stok, tetapi belum ada mutasi Stock Ledger yang dicatat. |
| Recorded | Stock Movement tercatat di Stock Ledger dan bersifat immutable di dalam ledger itu. |
| Reversed | Stock Reversal kemudian menetralkan mutasi sambil tetap mempertahankannya. |
| Corrected | Stock Correction kemudian mengubah konsekuensi bisnisnya sambil mempertahankan mutasi asli. |

`Reversed` dan `Corrected` menjelaskan disposisi kemudian dari mutasi. Keduanya tidak mengubah isi asli yang tercatat.

### 8.2 Lifecycle Stock Layer

```text
Established
  -> Active
       -> Depleted

Active or Depleted
  -> Corrected
```

| State | Makna bisnis |
| --- | --- |
| Established | Stock Layer dibentuk dengan Initial Quantity dan asal-usul. |
| Active | Remaining Quantity lebih besar dari nol. |
| Depleted | Remaining Quantity nol. Lapisan tetap dipertahankan di Stock Ledger. |
| Corrected | Koreksi kemudian yang dapat dipertanggungjawabkan mengubah konsekuensi jumlah, asal-usul, atau valuasi. |

Depleted Stock Layer tidak dihapus. Retur yang dapat ditelusuri boleh mengembalikan Remaining Quantity positif ke Stock Layer asli sambil mempertahankan Receipt Source, Expiration Date, dan Unit Valuation aslinya.

### 8.3 Lifecycle Stock Position

```text
Uninitialized
  -> Established
       -> Active
       -> Fully Depleted
       -> Inconsistent
```

| State | Makna bisnis |
| --- | --- |
| Uninitialized | Belum ada posisi Stock Ledger native atau hasil rekonstruksi untuk Item dan Receipt Source. |
| Established | Posisi asal-usul telah diakui. |
| Active | Setidaknya satu Stock Layer memiliki Remaining Quantity positif. |
| Fully Depleted | Setiap Stock Layer memiliki Remaining Quantity nol, tetapi Stock Position tetap dipertahankan. |
| Inconsistent | Selisih rekonsiliasi atau rekonstruksi membutuhkan penyelesaian yang dapat dipertanggungjawabkan. |

Penerimaan kemudian di bawah Receipt Source yang sama diizinkan hanya ketika otoritas sumber mengonfirmasi bahwa penerimaan itu termasuk dalam tanggung jawab penerimaan yang sama.

### 8.4 Lifecycle Stock Reconciliation

```text
Requested
  -> Evaluating
       -> Balanced
       -> Difference Identified
            -> Resolved
```

| State | Makna bisnis |
| --- | --- |
| Requested | Rekonsiliasi dibutuhkan untuk Item dan Receipt Source yang ditentukan. |
| Evaluating | Fakta mutasi dan posisi sedang dibandingkan. |
| Balanced | Tidak ditemukan selisih jumlah atau valuasi yang berlaku. |
| Difference Identified | Terdapat selisih yang dapat dipertanggungjawabkan. |
| Resolved | Selisih menerima koreksi, penjelasan, atau disposisi akhir yang diotorisasi. |

Rekonsiliasi boleh dilakukan lagi setelah penyelesaian. Hasil rekonsiliasi sebelumnya tetap menjadi fakta historis.

### 8.5 Lifecycle Legacy Stock Reconstruction

```text
Not Reconstructed
  -> Reconstruction Required
       -> Reconstructing
            -> Reconstructed
            -> Inconsistent
```

| State | Makna bisnis |
| --- | --- |
| Not Reconstructed | Item dan Receipt Source masih mengandalkan semata-mata representasi legacy. |
| Reconstruction Required | Aktivitas Stock Ledger baru membutuhkan baseline asal-usul yang dapat dipakai untuk ditetapkan. |
| Reconstructing | Fakta legacy yang tersedia sedang ditafsirkan dalam Reconstruction Scope lengkap. |
| Reconstructed | Baseline Stock Ledger hasil rekonstruksi yang seimbang tersedia untuk Item dan Receipt Source. Selama koeksistensi, kewenangan legacy tetap tidak berubah dan aktivitas legacy kemudian mungkin membutuhkan penyelarasan. |
| Inconsistent | Fakta legacy yang tersedia tidak dapat menghasilkan rekonstruksi yang lengkap dan seimbang tanpa penyelesaian yang dapat dipertanggungjawabkan. |

### 8.6 Lifecycle Legacy Synchronization

```text
Current
  -> Legacy Change Pending
       -> Synchronization Required
            -> Current
            -> Inconsistent
```

| State | Makna bisnis |
| --- | --- |
| Current | Stock Ledger Representation mencerminkan fakta stok legacy yang berlaku sampai Synchronization Position-nya. |
| Legacy Change Pending | Terdapat fakta stok legacy baru di luar Synchronization Position yang diketahui. |
| Synchronization Required | Stock Ledger harus memasukkan fakta itu sebelum mengandalkan representasinya untuk keputusan stok berikutnya. |
| Inconsistent | Representasi legacy dan Stock Ledger tidak dapat direkonsiliasi dari fakta yang tersedia. |

Rekonstruksi menetapkan baseline awal. Legacy Synchronization menjaga baseline itu tetap mutakhir selama koeksistensi berlanjut.

### 8.7 Lifecycle FIFO allocation

```text
Requested Quantity
  -> Allocated
       -> Fully Allocated
       -> Partially Allocated
       -> Not Allocated
```

| State | Makna bisnis |
| --- | --- |
| Requested Quantity | Konsekuensi persediaan keluar membutuhkan alokasi Stock Layer. |
| Allocated | Satu atau lebih Stock Layer yang layak telah dipilih. |
| Fully Allocated | Seluruh jumlah yang diminta terdukung. |
| Partially Allocated | Hanya sebagian jumlah yang diminta terdukung. |
| Not Allocated | Tidak ada jumlah yang layak tersedia. |

### 8.8 Lifecycle Stock Reservation

```text
Available at Ordinary Location
  -> Reserved at Virtual Location
       -> Released to Ordinary Location
       -> Consumed from Virtual Location by an explicitly permitted transaction
```

| State | Makna bisnis |
| --- | --- |
| Available at Ordinary Location | Jumlah boleh dipilih oleh transaksi yang meminta Stock Location biasa. |
| Reserved at Virtual Location | Jumlah dipisahkan secara logis dan tidak tersedia bagi transaksi yang meminta Stock Location biasa. |
| Released to Ordinary Location | Jumlah yang direservasi ditransfer kembali dan layak lagi di Stock Location biasa. |
| Consumed from Virtual Location | Transaksi sumber secara eksplisit meminta dan memakai jumlah dari Virtual Stock Location. |

Reservasi tidak mengubah Receipt Source, Expiration Date, Unit Valuation, atau kepemilikan fisik.

---

## 9. Domain Events

| Domain Event | Makna bisnis |
| --- | --- |
| Stock Consequence Requested | Sumber yang dapat dipertanggungjawabkan meminta efek jumlah persediaan. |
| Stock Receipt Recorded | Persediaan yang masuk ke organisasi diakui oleh Stock Ledger. |
| Stock Layer Established | Stock Layer baru dibentuk dengan asal-usul yang dapat dipertanggungjawabkan. |
| Stock Layer Depleted | Stock Layer mencapai Remaining Quantity nol. |
| Stock Position Established | Posisi Stock Ledger untuk suatu Item dan Receipt Source menjadi tersedia. |
| Stock Movement Recorded | Konsekuensi persediaan masuk atau keluar dicatat di Stock Ledger. |
| FIFO Allocation Completed | Jumlah keluar dialokasikan ke satu atau lebih Stock Layer. |
| FIFO Allocation Partially Completed | Hanya sebagian jumlah yang diminta terdukung oleh lapisan yang layak. |
| Stock Consumption Recorded | Jumlah persediaan dikeluarkan dari satu atau lebih Stock Layer. |
| Stock Transfer Recorded | Jumlah keluar dan masuk yang sama dicatat di dua Stock Location. |
| Stock Returned | Jumlah persediaan dikembalikan ke Stock Location yang dapat dipertanggungjawabkan. |
| Stock Adjustment Recorded | Selisih jumlah yang diotorisasi diakui. |
| Stock Movement Reversed | Mutasi baru menetralkan mutasi yang sebelumnya tercatat. |
| Stock Movement Corrected | Fakta baru yang dapat dipertanggungjawabkan mengoreksi konsekuensi mutasi sebelumnya. |
| Stock Reconciliation Requested | Lingkup Item dan Receipt Source yang ditentukan dipilih untuk validasi. |
| Stock Reconciliation Balanced | Total mutasi dan Stock Position cocok dalam lingkup. |
| Stock Reconciliation Difference Identified | Ditemukan selisih antara fakta mutasi dan posisi yang berlaku. |
| Stock Reconciliation Difference Resolved | Selisih yang diidentifikasi menerima penyelesaian yang dapat dipertanggungjawabkan. |
| Legacy Stock Reconstruction Required | Item dan Receipt Source yang belum direkonstruksi dibutuhkan untuk pemrosesan native. |
| Legacy Stock Reconstruction Started | Rekonstruksi dimulai di seluruh lingkup Item dan Receipt Source. |
| Legacy Stock Layer Reconstructed | Stock Layer dibentuk dari riwayat legacy yang tersedia. |
| Legacy Stock Reconstruction Completed | Baseline Stock Ledger hasil rekonstruksi yang seimbang menjadi tersedia tanpa mengubah kewenangan koeksistensi. |
| Legacy Stock Synchronization Required | Terdapat fakta stok legacy yang berlaku di luar Synchronization Position Stock Ledger. |
| Legacy Stock Synchronization Completed | Fakta stok legacy yang berlaku dimasukkan dan Stock Ledger Representation kembali mutakhir. |
| Legacy Stock Reconstruction Failed | Fakta legacy yang tersedia tidak dapat menghasilkan rekonstruksi yang dapat dipertanggungjawabkan. |
| Duplicate Stock Consequence Detected | Lebih dari satu konsekuensi diminta atau ditemukan untuk tanggung jawab sumber yang sama. |
| Legacy Stock Difference Identified | Ditemukan perbedaan material antara Legacy Stock Record dan Stock Ledger Representation di luar keterbatasan representasi atau keterlambatan penyelarasan yang dapat dijelaskan. |
| Stock Reserved | Jumlah persediaan ditransfer ke Virtual Stock Location yang ditentukan. |
| Stock Reservation Released | Jumlah yang direservasi ditransfer kembali ke Stock Location biasa yang berlaku. |
| Stock Disposal Recorded | Transaksi pembuangan atau pemusnahan yang sudah diotorisasi menghasilkan konsekuensi keluar final. |

---

## 10. Workflow Bisnis

### 10.1 Recognize Stock Receipt

```text
Goods Receipt Confirmed
  -> validate accountable source
  -> establish Receipt Source
  -> record Stock Receipt
  -> establish Stock Layer
  -> establish or update Stock Position
  -> publish Stock Receipt Recorded
```

**Hasil:** Persediaan yang masuk ke organisasi menjadi dapat ditelusuri menurut Item, Receipt Source, Stock Location, Initial Quantity, Remaining Quantity, dan Unit Valuation.

### 10.2 Transfer Stock Between Locations

```text
Stock Transfer Authorized
  -> identify eligible source Stock Layers
  -> allocate transfer quantity
  -> record outbound Stock Movement
  -> establish destination Stock Layers
  -> record equal inbound Stock Movement
  -> preserve Receipt Source and Unit Valuation
  -> publish Stock Transfer Recorded
```

**Hasil:** Persediaan berpindah antar Stock Location tanpa mengubah total jumlah atau asal-usul.

### 10.3 Consume Stock Using FIFO

```text
Outbound Stock Consequence Requested
  -> identify Item and requested Stock Location
  -> apply requested Expiration Date when supplied
  -> identify eligible Stock Layers only in that location and expiry group
  -> order layers by Effective Receipt Time, then Stock Layer identity
  -> allocate quantity across one or more layers
  -> reduce Remaining Quantity
  -> retain depleted layers
  -> record Stock Movement Lines per consumed layer
  -> publish Stock Consumption Recorded
```

**Hasil:** Jumlah keluar tetap dapat ditelusuri ke setiap Receipt Source dan Unit Valuation yang dipakai.

### 10.4 Record Stock Return

```text
Stock Return Authorized
  -> identify original movement, Stock Layer, and Receipt Source
  -> validate permitted return quantity
  -> retain the return transaction as Source Transaction Reference
  -> record inbound Stock Movement
  -> restore the original Stock Layer when traceable
  -> otherwise establish an accountable new Stock Layer without inventing provenance
  -> update Stock Position
  -> publish Stock Returned
```

**Hasil:** Jumlah retur dimasukkan kembali tanpa kehilangan asal-usul asli atau hubungan mutasinya.

### 10.5 Record Stock Adjustment

```text
Authorized Adjustment Transaction Received
  -> identify Item, Receipt Source, Stock Location, and affected quantity
  -> record adjustment Stock Movement
  -> update Stock Layer and Stock Position
  -> preserve reason and responsible authority
  -> publish Stock Adjustment Recorded
```

**Hasil:** Selisih jumlah dari transaksi yang sudah diotorisasi menerima konsekuensi yang dapat dipertanggungjawabkan, bukan penulisan ulang Stock Position diam-diam.

### 10.6 Correct or Reverse Stock Movement

```text
Incorrect Stock Consequence Identified
  -> retain original Stock Movement
  -> authorize correction or reversal
  -> record correcting Stock Movement
  -> update affected Stock Layers
  -> reconcile affected Item and Receipt Source
  -> publish correction or reversal event
```

**Hasil:** Fakta asli dan fakta yang mengoreksi tetap terlihat serta dapat dipertanggungjawabkan secara kuantitatif.

### 10.7 Reconcile Item and Receipt Source

```text
Reconciliation Requested
  -> select one Item and Receipt Source
  -> include all Stock Locations
  -> include active and depleted Stock Layers
  -> total accountable Stock Movements
  -> compare with Stock Position
       -> Balanced
       -> Difference Identified
  -> publish reconciliation outcome
```

**Hasil:** Konsistensi persediaan dapat divalidasi tanpa memutar ulang Item atau Receipt Source yang tidak terkait sejak awal riwayat organisasi.

### 10.8 Reconstruct Legacy Stock on First Use

```text
Stock Processing Requested
  -> detect unreconstructed Item and Receipt Source
  -> establish Reconstruction Required
  -> collect available legacy movement facts
  -> reconstruct provenance across all Stock Locations
  -> establish active and depleted Reconstructed Stock Layers
  -> reconcile Remaining Quantity per Item and Receipt Source
       -> Reconstructed baseline available
       -> Inconsistent
  -> during coexistence, preserve Legacy Stock Authority
  -> synchronize later legacy activity before relying on the Stock Ledger Representation
```

**Hasil:** Asal-usul legacy menjadi tersedia secara bertahap untuk Item dan Receipt Source yang sedang diproses, tanpa mensyaratkan migrasi lengkap riwayat persediaan bertahun-tahun dan tanpa memindahkan kewenangan runtime dari Legacy Stock Record selama koeksistensi.

### 10.9 Coordinate Cross-Context Stock Consequence

```text
Source Business Fact Published
  -> validate source responsibility and reference
  -> detect duplicate consequence
  -> determine applicable stock behavior
  -> record Stock Movement
  -> update Stock Position
  -> publish stock outcome to the source context
```

**Hasil:** Bounded context lain menerima konsekuensi persediaan yang dapat dipertanggungjawabkan sambil tetap memiliki transaksi bisnis asalnya.

### 10.10 Reserve and Release Stock

```text
Reservation Transaction Confirmed
  -> request transfer from ordinary Stock Location
  -> allocate eligible Stock Layers
  -> transfer quantity to designated Virtual Stock Location
  -> preserve Receipt Source, Expiration Date, and Unit Valuation
  -> publish Stock Reserved

Reservation Release Confirmed
  -> request transfer from Virtual Stock Location
  -> transfer quantity back to ordinary Stock Location
  -> preserve provenance
  -> publish Stock Reservation Released
```

**Hasil:** Jumlah yang direservasi secara logis tidak tersedia bagi transaksi lokasi biasa tanpa memperkenalkan model saldo reservasi terpisah.

### 10.11 Synchronize Continued Legacy Stock Activity

```text
Reconstructed, Native, or Legacy-Synchronized Stock Ledger Representation
  -> legacy stock transaction occurs
  -> applicable legacy facts move beyond Synchronization Position
  -> Legacy Synchronization Required
  -> incorporate legacy stock consequence
  -> reconcile Legacy Stock Record and Stock Ledger Representation
       -> Current
       -> Inconsistent
```

**Hasil:** Stock Ledger tetap dapat dipakai sebagai representasi domain yang lebih kaya sementara transaksi stok legacy terus terjadi selama koeksistensi.

### 10.12 Maintain Legacy Coexistence

```text
During Coexistence

Legacy Stock Record
  = authoritative persisted stock truth

Stock Ledger Representation
  = richer provenance and Stock Layer representation

New-system stock transaction
  -> apply Stock Ledger business rules
  -> preserve legacy-compatible stock consequence
  -> keep Stock Ledger Representation synchronized

Legacy-system stock transaction
  -> update Legacy Stock Record
  -> require later Legacy Synchronization
```

**Hasil:** Kemampuan pemrosesan stok legacy dan yang baru boleh beroperasi secara bersamaan tanpa cutover kewenangan per Item atau per Receipt Source. Legacy Stock Record tetap menjadi sumber kebenaran untuk fakta jumlah dan mutasi stok yang tersimpan sampai ada keputusan cutover terpisah di masa depan.

---
