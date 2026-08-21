# Legacy Sales Model Excavation

Dokumen ini mendeskripsikan bagaimana model Penjualan legacy dimodelkan di codebase `b09-bilreg-api` saat ini. Setiap kesimpulan merujuk class, property, method, repository, atau mapping persistence yang ditemukan. Jika tidak ditemukan, dinyatakan secara eksplisit.

Lingkup: modeling, persistence, aggregate, entity, value object, repository, use case. Reporting, telaah klinis, dan konteks IGD/Tata Rekening di luar penjualan apotek tidak dianalisis sebagai model penjualan.

---

## Ringkasan jawaban eksplisit

| Pertanyaan | Jawaban berdasarkan code |
|---|---|
| 1. Struktur Penjualan legacy? | Dua aggregate terpisah: `ResepModel` (kartu periksa/resep) dan `PenjualanModel` (dobill umum). Penjualan merujuk Resep lewat `ResepId` dan dapat dibuat dari resep. |
| 2. Line item? | `PenjualanItemType` dengan qty, satuan, etiket, dan value object `NilaiItemType`. Dipersist di `tb_trs_dobill_umum2`. |
| 3. Biaya line atau header? | Keduanya. Line: `Embalase` (`fn_biaya`) dan `Fee` (`fn_biaya_fee`). Header: `BiayaLain`, `Pembulatan`, `Bulat`, plus agregat `SumBiaya` dari embalase line. |
| 4. BHP? | Ada tipe katalog `BrgBhpType` (grup rek DK `BHP`). Di SalesContext, line penjualan hanya menyimpan `BrgReff`. Tidak ada perlakuan BHP khusus (plastik/kapsul/pot obat) pada model penjualan. |
| 5. Racikan? | Header-detail di memori: line hasil racikan (`PenjualanItemType` / `ResepObatType`) berisi `ListItemRacik`. Dipersist sebagai baris datar di tabel detail yang sama, dengan flag `fb_racik` / `fb_komponen` / `fs_kd_racik`. Tidak ada tabel racikan terpisah. |
| 6. Persistence? | Dapper + SqlBulkCopy, bukan ORM entity mapping. Header/detail penjualan: `tb_trs_dobill_umum` / `tb_trs_dobill_umum2`. Header/detail resep: `ta_trs_kartu_periksa` (+ `ta_trs_kartu_periksa_resep`) / `ta_trs_kartu_periksa3`. |
| 7. Rule yang diimplementasikan? | Duplikasi barang dilarang; racikan nested; hapus komponen terakhir menghapus parent; void penjualan men-void line dan menghitung ulang; total line dan header punya rumus terpisah. Lihat bagian 9. |

---

## 1. Aggregate Structure

### 1.1 Aggregate root transaksi penjualan

**Aggregate root:** `PenjualanModel` (`src/bilreg/Bilreg.Domain/SalesContext/PenjualanFeature/PenjualanModel.cs`), identity `IPenjualanKey.PenjualanId`.

Child di dalam aggregate:

| Peran | Class | Keterangan |
|---|---|---|
| Child entity (line) | `PenjualanItemType` | Koleksi privat `_listItem` |
| Detail nested (komponen racik) | `PenjualanItemRacikType` | Koleksi di dalam line |
| Value object nilai header | `NilaiPenjualanType` | |
| Value object nilai line | `NilaiItemType` | |
| Value object etiket | `EtiketType` (namespace ResepFeature) | Dipakai ulang di penjualan |

Property header yang terbukti di constructor `PenjualanModel`:

- `PenjualanId`, `ResepId`
- `Register` (`RegReff`)
- `Dokter` (`DokterReff`)
- `Layanan`, `LayananResep` (`LayananReff`)
- `TipeJaminan` (`TipeJaminanReff`)
- `TipeBrg` (`TipeBrgReff`)
- `Nilai` (`NilaiPenjualanType`)
- `AuditTrail` (`AuditTrailType`)
- `ListItem`

ID baru memakai prefix legacy `"DU"`: `NunaId.NewLegacy("DU", 'A')` di `CreateFromResep`.

### 1.2 Aggregate root transaksi resep

**Aggregate root:** `ResepModel` (`src/bilreg/Bilreg.Domain/SalesContext/ResepFeature/ResepModel.cs`), identity `IResepKey.ResepId`.

Child:

| Peran | Class |
|---|---|
| Child entity (obat) | `ResepObatType` |
| Detail nested (komponen racik) | `ResepItemRacikType` |
| Value object | `BodyMetricType`, `EtiketType`, `UrgenitasType`, `DokterType` / `DokterReff` |

ID baru memakai prefix `"KP"`: `NunaId.NewLegacy("KP", 'A')` di `ResepModel.Create`.

Resep **tidak** memiliki value object nilai uang. `ResepObatType` hanya: `NoUrut`, `Brg`, `Satuan`, `Qty`, `Iter`, `Etiket`, `ListItemRacik`.

### 1.3 Penjualan dan Resep: terpisah atau menyatu?

**Terpisah.** Dua class root, dua repository, dua set tabel.

Hubungan yang terbukti:

1. `PenjualanModel.ResepId` menyimpan id resep.
2. Factory `PenjualanModel.CreateFromResep(ResepModel resep, ...)` menyalin register, dokter, layanan resep, tipe barang, daftar obat, dan komponen racik ke penjualan. `NilaiItemType.Default` dan `NilaiPenjualanType.Default` dipakai (nilai uang mulai dari nol).
3. Use case `PenjualanCreateHandler` memuat `ResepModel` lalu memanggil `CreateFromResep`, lalu `IPenjualanRepo.SaveChanges`.
4. Mapping persistensi: `fs_kd_resep` di `tb_trs_dobill_umum` (`PenjualanDal`, `PenjualanDto.ResepId`).

Tidak ditemukan navigasi balik dari `ResepModel` ke penjualan. Tidak ditemukan invariant “satu resep hanya satu penjualan” di domain.

### 1.4 Hubungan antar aggregate (diagram)

```
ResepModel (aggregate)
├─ BodyMetricType
├─ DokterReff, LayananReff, TipeBrgReff, UrgenitasType
├─ AuditTrailType
└─ ListObat: ResepObatType
     ├─ BrgReff, SatuanType, Qty, Iter, EtiketType
     └─ ListItemRacik: ResepItemRacikType
           └─ BrgReff, SatuanType, Qty, Dosis, DosisTxt

        CreateFromResep / ResepId
                 │
                 ▼
PenjualanModel (aggregate)
├─ ResepId ──────────────────────────► ResepModel.ResepId (referensi, bukan composition)
├─ Register, Dokter, Layanan, LayananResep
├─ TipeJaminan, TipeBrg
├─ NilaiPenjualanType
├─ AuditTrailType
└─ ListItem: PenjualanItemType
     ├─ PenjualanItemId, NoUrut, BrgReff, SatuanType, Qty
     ├─ EtiketType, NilaiItemType, IsVoided
     └─ ListItemRacik: PenjualanItemRacikType
           └─ BrgReff, SatuanType, Qty, Dosis, DosisTxt
```

Folder `SalesContext/ReturJualFeature` dan `SalesContext/PricingPolicyFeature` hanya berisi `.gitkeep`. Tidak ada aggregate retur jual di codebase ini.

`TelaahModel` (`TelaahFeature`) menyimpan `resepId` tetapi bukan bagian transaksi penjualan/pembayaran. Tidak ada repository/use case Telaah di Application layer yang ditemukan pada penggalian ini.

---

## 2. Sales Line Modeling

### 2.1 Bagaimana line item dipersist?

Line penjualan dipersist di **satu tabel** `dbo.tb_trs_dobill_umum2`.

Alur:

1. Domain: `PenjualanModel.ListItem` (`PenjualanItemType`).
2. Flatten: `PenjualanItemDto.FlattenFromModel` mengubah setiap line (plus komponen racik) menjadi baris DTO.
3. Repository: `PenjualanRepo.SaveChanges` memanggil `_penjualanItemDal.Delete(model)` lalu `_penjualanItemDal.Insert(listItemDto)` (replace seluruh detail).
4. DAL: `PenjualanItemDal.Insert` memakai `SqlBulkCopy` ke `dbo.tb_trs_dobill_umum2`. `ListData` memakai query Dapper.

PK tabel: `(fs_kd_trs, fs_kd_trs2)` — lihat `src/bilreg/Bilreg.SqlDb/SalesContext/tb_trs_dobill_umum2.sql`.

### 2.2 Property line item (domain)

Class: `PenjualanItemType`

| Property | Tipe |
|---|---|
| `PenjualanItemId` | `string` |
| `NoUrut` | `int` |
| `Brg` | `BrgReff` |
| `Satuan` | `SatuanType` |
| `Qty` | `decimal` |
| `Etiket` | `EtiketType` |
| `Nilai` | `NilaiItemType` |
| `IsVoided` | `bool` |
| `ListItemRacik` | `IEnumerable<PenjualanItemRacikType>` |

Value object `NilaiItemType`:

| Property | Peran |
|---|---|
| `Harga` | Harga satuan |
| `Diskon` | Diskon line (nilai, bukan persen) |
| `Embalase` | Biaya line |
| `SubTotal` | Hasil hitung `(qty * harga) - diskon` |
| `TaxProsen` | Persen pajak (disimpan, tidak dipakai di rumus `Create`) |
| `Tax` | Pajak rupiah |
| `Fee` | Biaya fee line |
| `Bulat` | Pembulatan line |
| `NilaiKlaim` | Nilai klaim |
| `Total` | Total line |

### 2.3 Checklist field yang diminta

| Konsep | Ada di line? | Bukti |
|---|---|---|
| Qty | Ya | `PenjualanItemType.Qty`; kolom `fn_qty_barang` |
| Harga satuan | Ya | `NilaiItemType.Harga`; kolom `fn_harga_satuan` |
| Diskon | Ya | `NilaiItemType.Diskon`; kolom `fn_diskon` |
| Biaya | Ya | `NilaiItemType.Embalase` → `fn_biaya`; juga `Fee` → `fn_biaya_fee` |
| Tax | Ya | `TaxProsen` / `Tax` → `fn_tax_prosen` / `fn_tax_rupiah` |
| Subtotal | Ya | `NilaiItemType.SubTotal`; kolom `fn_sub_total` |
| Total | Ya | `NilaiItemType.Total`; kolom `fn_total` |

### 2.4 Perhitungan nilai line

Method: `NilaiItemType.Create` di `NilaiItemType.cs`:

```
subTotal = (qty * harga) - diskon
total    = subTotal + embalase + tax + fee + bulat
```

`TaxProsen` dan `NilaiKlaim` disimpan di object tetapi **tidak** masuk rumus `Create`.

`ApplyNilai` pada line hanya mengganti `Nilai`; tidak menghitung ulang dari `Qty` line. Pemanggil harus mengirim `NilaiItemType` yang sudah dihitung (`PenjualanModel.ApplyNilaiItem`).

`CreateFromResep` mengisi `NilaiItemType.Default` (semua 0).

---

## 3. Item-Level Charge

### 3.1 Apakah biaya tersimpan pada level line?

Ya. Dua field numerik pada `NilaiItemType`, dipersist per baris `tb_trs_dobill_umum2`.

### 3.2 Apakah biaya field langsung pada line?

Ya, sebagai bagian value object `Nilai` milik line, bukan entity charge terpisah.

Mapping DTO/DAL (`PenjualanItemDal`, `PenjualanItemDto`):

| Domain | DTO property | Kolom SQL |
|---|---|---|
| `Nilai.Embalase` | `Embalase` | `fn_biaya` |
| `Nilai.Fee` | `Fee` | `fn_biaya_fee` |

Nama “Embalase” hanya ada di domain/DTO C#. Nama kolom legacy adalah `fn_biaya`.

### 3.3 Apakah biaya entity terpisah?

Tidak ditemukan class/entity/tabel charge per line selain field di atas.

### 3.4 Lebih dari satu jenis biaya per line?

Ya, dua field: **Embalase** dan **Fee**. Keduanya masuk rumus `NilaiItemType.Create` (total line).

Kata “Jasa” **tidak ditemukan** di SalesContext penjualan.

Master `TipeBrgType` punya `BiayaPerBarang` dan `BiayaPerRacik` (`src/bilreg/Bilreg.Domain/BrgContext/PricingPolicyFeature/TipeBrgType.cs`, kolom `fn_biaya_per_barang` / `fn_biaya_per_racik`). **Tidak ada pemanggilan** property ini dari `PenjualanModel`, `NilaiItemType`, atau `NilaiPenjualanType`. Penjualan hanya menyimpan `TipeBrgReff` (id + nama).

### 3.5 Rumus line total (terbukti)

Dari `NilaiItemType.Create`:

```
SubTotal = (Qty × Harga) − Diskon
Total    = SubTotal + Embalase + Tax + Fee + Bulat
```

Komponen racik yang di-flatten **tidak** diberi nilai uang: `FlattenFromModel` mengisi `0` untuk harga/diskon/embalase/subtotal/tax/fee/total/bulat/nilaiklaim.

---

## 4. Invoice-Level Charge

### 4.1 Apakah ada biaya pada header transaksi?

Ya, pada `NilaiPenjualanType`:

| Property | Kolom `tb_trs_dobill_umum` |
|---|---|
| `SumSubTotal` | `fn_sum_sub_total` |
| `SumBiaya` | `fn_sum_biaya` |
| `SumTax` | `fn_sum_tax_rupiah` |
| `SubTotal` | `fn_sub_total` |
| `DiskonLain` | `fn_diskon_lain` |
| `BiayaLain` | `fn_biaya_lain` |
| `Pembulatan` | `fn_pembulatan` |
| `Bulat` | `fn_bulat` |
| `GrandTotal` | `fn_grand_total` |

Nama “Biaya Nota”, “Biaya Administrasi”, “Additional Charge”, “Header Charge” **tidak ditemukan**. Yang ada: `BiayaLain`, `Pembulatan`, `Bulat`, `DiskonLain`.

### 4.2 Pengaruh terhadap total transaksi

`NilaiPenjualanType.RecalcFrom`:

```
active     = items where !IsVoided
SumSubTotal = Σ line.Nilai.SubTotal
SumBiaya    = Σ line.Nilai.Embalase
SumTax      = Σ line.Nilai.Tax
SubTotal    = SumSubTotal + SumBiaya + SumTax
GrandTotal  = SubTotal − DiskonLain + BiayaLain + Pembulatan + Bulat
```

Fakta dari rumus ini:

- `SumBiaya` **hanya** menjumlahkan `Embalase`, bukan `Fee`.
- `Fee` line dan `Bulat` line **tidak** masuk `RecalcFrom` header.
- Line yang `IsVoided` dikeluarkan dari agregat.
- `SetHeaderAdjustment(diskonLain, biayaLain, pembulatan, bulat)` menulis ulang header dengan rumus yang sama (`PenjualanModel`).

Pemanggilan otomatis `Recalculate()` terjadi setelah `AddItem`, `RemoveItem`, `RemoveItemRacik`, `ApplyNilaiItem`, `Void`. `AddItemRacik` **tidak** memanggil `Recalculate`.

---

## 5. BHP Modeling

### 5.1 Pencarian Plastik, Kapsul, Pot Obat, Etiket, BHP

| Istilah | Hasil di model penjualan |
|---|---|
| Plastik | Tidak ditemukan class/property/line khusus |
| Kapsul (sebagai BHP kemasan) | Tidak ditemukan. “Kapsul” muncul di data KFA sebagai bentuk sediaan obat, bukan BHP penjualan |
| Pot Obat | Tidak ditemukan |
| Etiket | Ada sebagai **value object signa** (`EtiketType`: Signa, Instruction, Frequency, UnitDose, Note), bukan barang BHP |
| BHP | Ada di katalog barang (`BrgBhpType`), bukan di SalesContext |

### 5.2 Apakah BHP item katalog biasa?

Di `BrgContext`, BHP adalah tipe katalog terpisah dari obat, tetapi **satu tabel barang**:

- `BrgBhpType` vs `BrgObatType` (`src/bilreg/Bilreg.Domain/BrgContext/BrgFeature/`).
- `BrgRepo.LoadEntity` memilih tipe dari `fs_kd_grup_rek_dk`: `"OBT"` → obat, `"BHP"` → bhp.
- `GroupRekDkType.Bhp` = `("BHP", "Alkes dan Bhp")`.

`BrgBhpType` tidak punya farmakologi (generik/kelas terapi); `BrgDto.FromModel(BrgBhpType)` mengosongkan field tersebut.

### 5.3 Apakah BHP memakai line item yang sama dengan obat?

Di penjualan, line hanya menyimpan `BrgReff` (id + nama). `PenjualanItemType.AddItem` menerima `IBrg`, sehingga secara tipe **bisa** menerima `BrgBhpType` atau `BrgObatType`. Tidak ada flag `IsBhp` pada line atau kolom BHP di `tb_trs_dobill_umum2`.

Grep `BrgBhpType` / `GroupRekDkType.Bhp` di `SalesContext` **tidak menghasilkan match**.

Use case `ResepCreateHandler.LoadBrg` memakai `BrgObatType.Key` lalu `IBrgRepo.LoadEntity`. Repository tetap dapat mengembalikan `BrgBhpType` jika `fs_kd_grup_rek_dk = "BHP"`. Tidak ada cabang khusus BHP setelah load.

### 5.4 Perlakuan khusus persistence/domain penjualan untuk BHP

**Tidak ditemukan.** BHP IGD (`IgdContext/BhpIgdFeature`) berada di luar SalesContext dan tidak dipetakan ke `tb_trs_dobill_umum2`.

---

## 6. Racikan Modeling

Class yang ditemukan (tidak ada class bernama Puyer / Compound / Campur):

| Class | File |
|---|---|
| `PenjualanItemRacikType` | `.../PenjualanFeature/PenjualanItemRacikType.cs` |
| `ResepItemRacikType` | `.../ResepFeature/ResepItemRacikType.cs` |
| Flag persistensi | `IsRacik`, `IsKomponen`, `RacikId` pada DTO item |

### 6.1 Apakah hasil racikan menjadi line item?

Ya. Hasil racikan adalah `ResepObatType` / `PenjualanItemType` biasa yang `ListItemRacik`-nya tidak kosong.

Pada `ResepCreateHandler.BuildItemResep`: jika `ListItemRacik` ada, parent **tidak** di-load dari katalog. Parent dibuat stub:

```csharp
var brg = BrgObatType.Default with { BrgId = obat.BrgId, BrgName = obat.BrgId };
resep.AddObat(brg, satuan, ...);
```

Komponen di-load lewat `IBrgRepo`.

`CreateFromResep` menyalin parent + `PenjualanItemRacikType` dari setiap `ResepItemRacikType`.

### 6.2 Bagaimana komponen dipersist?

Tidak ada tabel racikan. Komponen di-flatten ke baris berikutnya di tabel detail yang sama.

`PenjualanItemDto.FlattenFromModel`:

- Parent: `IsRacik = item.ListItemRacik.Any()`, `IsKomponen = false`, `RacikId = ""`.
- Komponen: `IsRacik = false`, `IsKomponen = true`, `RacikId = item.Brg.BrgId` (id barang parent, bukan `PenjualanItemId`), nama barang di-prefix spasi `"   "`.
- Nilai uang komponen di-set 0.

`PenjualanDto.ToModel` menyusun ulang: jika `IsKomponen`, cari parent `Brg.BrgId == RacikId` lalu `parent.AddItemRacik(...)`. Jika parent tidak ada, baris komponen di-`continue` (diabaikan).

Pola yang sama: `ResepBrgDto.FlattenFromModel` / `ResepDto.ToModel` ke `ta_trs_kartu_periksa3`.

Bukti tes: `PenjualanRepoTest.GivenPenjualanWithRacik_WhenSaveChanges_ThenInsertParentAndKomponenRows`.

### 6.3 Header-Detail?

Ya, di domain (nested collection). Di database: **satu tabel detail, dua jenis baris** (parent racik vs komponen).

### 6.4 Property hasil racikan (parent line)

Sama dengan line biasa: `PenjualanItemId`, `NoUrut`, `Brg`, `Satuan`, `Qty`, `Etiket`, `Nilai`, `IsVoided`, plus `ListItemRacik`.

Pada persistensi parent: `fb_racik = 1` jika ada komponen.

### 6.5 Property komponen racikan

`PenjualanItemRacikType` / `ResepItemRacikType`:

| Property | Persistensi penjualan | Persistensi resep |
|---|---|---|
| `NoUrut` | `fn_no_urut` (nomor baris datar, di-renumber saat flatten) | `fn_no_urut` |
| `Brg` | `fs_kd_barang`, `fs_nm_barang` | sama |
| `Satuan` | `fs_kd_satuan`, `fs_nm_satuan` | `fs_kd_satuan` |
| `Qty` | `fn_qty_barang` | `fn_qty_barang` |
| `Dosis` | `fn_qty_racik` | `fn_qty_racik` |
| `DosisTxt` | `fs_racik_qty` | `fs_racik_qty` |

Komponen penjualan juga menyalin `IsVoided` dari parent saat flatten.

### 6.6 Diagram object racikan

```
PenjualanItemType / ResepObatType   ← hasil racikan (line)
├─ Brg, Satuan, Qty, Etiket [, Nilai pada penjualan]
└─ ListItemRacik
    ├─ PenjualanItemRacikType / ResepItemRacikType  (Komponen A)
    ├─ PenjualanItemRacikType / ResepItemRacikType  (Komponen B)
    └─ PenjualanItemRacikType / ResepItemRacikType  (Komponen C)
```

Flatten ke `tb_trs_dobill_umum2` / `ta_trs_kartu_periksa3`:

```
row n     fb_racik=1  fb_komponen=0  fs_kd_racik=''     ← parent
row n+1   fb_racik=0  fb_komponen=1  fs_kd_racik=parent.BrgId  ← A
row n+2   fb_racik=0  fb_komponen=1  fs_kd_racik=parent.BrgId  ← B
row n+3   fb_racik=0  fb_komponen=1  fs_kd_racik=parent.BrgId  ← C
```

---

## 7. Persistence Structure

Teknologi: **Dapper** + **SqlBulkCopy**. Tidak ada ORM entity mapping (EF/NHibernate) untuk penjualan/resep.

### 7.1 Tabel header penjualan — `tb_trs_dobill_umum`

Script: `src/bilreg/Bilreg.SqlDb/SalesContext/tb_trs_dobill_umum.sql`  
DAL: `PenjualanDal`  
DTO: `PenjualanDto`

Kolom yang di-write/read oleh DAL (bukan seluruh kolom tabel):

| DTO / domain | Kolom |
|---|---|
| `PenjualanId` | `fs_kd_trs` |
| tanggal/jam/user | `fd_tgl_trs`, `fs_jam_trs`, `fs_kd_petugas` |
| `ResepId` | `fs_kd_resep` |
| `LayananResepId` | `fs_kd_layanan_resep` |
| `DokterId` | `fs_kd_petugas_medis` |
| `LayananId` | `fs_kd_layanan` |
| `TipeJaminanId` | `fs_kd_tipe_jaminan` |
| `TipeBarangId` | `fs_kd_tipe_barang` |
| `RegId` / `PasienName` | `fs_kd_reg` / `fs_nm_pasien` |
| nilai header | `fn_sum_sub_total`, `fn_sum_biaya`, `fn_sum_tax_rupiah`, `fn_sub_total`, `fn_diskon_lain`, `fn_biaya_lain`, `fn_grand_total`, `fn_pembulatan`, `fn_bulat` |
| void | `fd_tgl_void`, `fs_jam_void`, `fs_kd_petugas_void` (update) |

Kolom ada di CREATE TABLE tetapi **tidak** di-map DAL insert/select: `fs_alm_pasien`, `fs_alm2_pasien`, `fs_kota_pasien`, `fs_sex`, dan sebagian field audit CRT/UPD selain `CRTUSR`/`UPDUSR`.

### 7.2 Tabel detail penjualan — `tb_trs_dobill_umum2`

Script: `src/bilreg/Bilreg.SqlDb/SalesContext/tb_trs_dobill_umum2.sql`  
DAL: `PenjualanItemDal`

Mapping bulk copy (cuplikan):

| DTO | Kolom |
|---|---|
| `PenjualanId` | `fs_kd_trs` |
| `PenjualanItemId` | `fs_kd_trs2` |
| `NoUrut` | `fn_no_urut` |
| `IsVoided` | `fb_void` |
| `BrgId` / `BrgName` | `fs_kd_barang` / `fs_nm_barang` |
| `TipeBarangId` | `fs_kd_tipe_barang` |
| `IsRacik` / `IsKomponen` / `RacikId` | `fb_racik` / `fb_komponen` / `fs_kd_racik` |
| `Dosis` / `DosisTxt` | `fn_qty_racik` / `fs_racik_qty` |
| `Qty` / `SatuanId` / `SatuanName` | `fn_qty_barang` / `fs_kd_satuan` / `fs_nm_satuan` |
| `Harga` / `Diskon` / `Embalase` | `fn_harga_satuan` / `fn_diskon` / `fn_biaya` |
| `SubTotal` / `TaxProsen` / `Tax` / `Fee` / `Total` / `Bulat` | `fn_sub_total` / `fn_tax_prosen` / `fn_tax_rupiah` / `fn_biaya_fee` / `fn_total` / `fn_bulat` |
| etiket | `fs_etiket`, `fn_etiket_qty`, `fn_etiket_hari`, `fs_etiket_catatan` |
| `TipeJaminanId` | `fs_kd_tipe_jaminan` |
| `NilaiKlaim` | `fn_nilai_klaim` |

Kolom ada di tabel tetapi **tidak** di-map DAL: antara lain `fs_etiket_qty` (varchar), `fs_etiket_kd_pakai`, `fs_etiket_jenis_obat`, `fb_belibebas`, `fb_paket_tarif`, `fs_waktu_odd`, field CRT/UPD.

Query load: `PenjualanItemDal.ListData` — `SELECT ... FROM tb_trs_dobill_umum2 aa LEFT JOIN tb_satuan bb ... WHERE aa.fs_kd_trs = @PenjualanId ORDER BY aa.fn_no_urut`.

### 7.3 Tabel racikan

**Tidak ada tabel header/detail racikan terpisah.** Racikan = baris di tabel detail penjualan atau resep (bagian 6).

### 7.4 Header resep

| Tabel | Peran | DAL |
|---|---|---|
| `ta_trs_kartu_periksa` | Header resep | `ResepDal.Insert/Update/GetData` |
| `ta_trs_kartu_periksa_resep` | Catatan (`fs_catatan_resep` ← `Description`) | `ResepDal` insert terpisah |

Script: `ta_trs_kartu_periksa.sql`, `ta_trs_kartu_periksa_resep.sql`.  
`fb_resep` di-set `1` pada insert.

### 7.5 Detail resep (termasuk racikan)

Tabel: `ta_trs_kartu_periksa3`  
Script: `src/bilreg/Bilreg.SqlDb/SalesContext/ta_trs_kartu_periksa3.sql`  
DAL: `ResepBrgDal` (SqlBulkCopy)

PK: `(fs_kd_trs, fn_no_urut)`.

### 7.6 Pola repository

`PenjualanRepo` / `ResepRepo`:

- `SaveChanges`: insert atau update header; **hapus semua detail lalu insert ulang**.
- `LoadEntity`: header + list detail, `Dto.ToModel`.
- `DeleteEntity`: hapus detail dulu, lalu header.
- `ListData` by `IRegKey`.

---

## 8. Use Cases

Use case Application yang **ada** (folder `Bilreg.Application/SalesContext/**/UseCases`):

### 8.1 Create Resep — `ResepCreateCommand`

- **Tujuan:** membuat `ResepModel`, mengisi obat dan komponen racik, persist.
- **Aggregate:** `ResepModel`.
- **API:** `POST api/Resep/create` (`ResepController.Create`).
- **Rule terbukti:** `UserId` wajib; `ListObat` tidak boleh kosong; register harus ada dan `IsAktif`; layanan, PPA, tipe barang, satuan, barang (komponen) harus ada; obat tanpa racik di-load dari katalog; obat dengan racik memakai stub `BrgId` sebagai nama; duplikasi `BrgId` ditolak di `ResepModel.AddObat`.

### 8.2 Get Resep — `ResepGetQuery`

- **Tujuan:** baca satu resep beserta nested racik.
- **Aggregate:** tidak memodifikasi.
- **API:** `GET api/Resep/{id}`.

### 8.3 List Resep by Register — `ResepListByRegQuery`

- **Tujuan:** daftar resep per `RegId` (header ringkas, tanpa list obat di response).
- **API:** `GET api/Resep/list/{regId}`.

### 8.4 Create Penjualan — `PenjualanCreateCmd`

- **Tujuan:** buat penjualan dari resep yang sudah ada.
- **Aggregate:** `PenjualanModel` (baca `ResepModel`).
- **API:** `POST api/Penjualan/create`.
- **Rule terbukti:** `ResepId`, `LayananId`, `TipeJaminanId`, `UserId` wajib; resep harus ada, tidak void, `ListObat` tidak kosong; layanan dan tipe jaminan harus ada; item disalin dengan `NilaiItemType.Default`.

### 8.5 Get Penjualan — `PenjualanGetQry`

- **Tujuan:** baca penjualan beserta line, nilai, dan nested racik.
- **API:** `GET api/Penjualan/{id}`.

### 8.6 Use case yang diminta tetapi tidak ada di Application

| Use case | Status |
|---|---|
| Update Penjualan | Tidak ada command. Domain punya `Modify`, `ApplyNilaiItem`, `SetHeaderAdjustment`. Repo bisa `Update` header jika `SaveChanges` dipanggil, tetapi tidak ada handler HTTP. |
| Add Item | Tidak ada use case. Ada `PenjualanModel.AddItem` / `ResepModel.AddObat`. |
| Remove Item | Tidak ada use case. Ada `RemoveItem` / `RemoveObat`. |
| Add Racikan | Tidak ada use case. Ada `AddItemRacik` di kedua model. Resep create dapat menyertakan `ListItemRacik` sekali di create. |
| Update Racikan | Tidak ada method update komponen; hanya add/remove. |
| Calculate Total | Tidak ada use case. Ada `Recalculate` / `NilaiItemType.Create` / `NilaiPenjualanType.RecalcFrom`. |
| Void Penjualan / Resep | Tidak ada use case. Ada `PenjualanModel.Void` dan `ResepModel.Void`. |
| Delete | Ada `DeleteEntity` di repo, tidak ada use case API. |

`IPenjualanRepo.ListData(IRegKey)` ada; tidak ada query/controller list penjualan.

---

## 9. Business Rules yang Tersirat (hanya yang terbukti di code)

### Resep

1. Barang pada list obat tidak boleh duplikat (`ResepModel.AddObat`).
2. Komponen racik tidak boleh duplikat di parent yang sama (`ResepObatType.AddItemRacik`).
3. `AddItemRacik` gagal jika parent tidak ditemukan (`KeyNotFoundException`).
4. `RemoveItemRacik`: jika parent tidak ada, no-op; jika komponen terakhir dihapus, parent dihapus dari list (`ResepModel.RemoveItemRacik`).
5. `RemoveObat` menghapus line dan merenumber `NoUrut`.
6. Create resep menolak register tidak aktif dan list obat kosong (`ResepCreateHandler`).
7. `Void` resep hanya `AuditTrail.Batal`; tidak men-void item obat.

### Penjualan

8. Duplikasi `BrgId` pada line yang belum void ditolak (`AddItem`).
9. Perubahan ditolak jika `AuditTrail.IsVoided` (`EnsureNotVoided`).
10. `RemoveItem` menghapus fisik dari list (bukan set `IsVoided`), lalu renumber dan recalc.
11. `AddItemRacik` hanya ke parent yang belum void; parent harus ada.
12. `RemoveItemRacik` pada komponen terakhir menghapus parent, lalu renumber dan recalc.
13. `Void` menandai header void, `VoidLine` semua line aktif, lalu recalc (line voided tidak masuk total → `GrandTotal` 0 pada tes).
14. `CreateFromResep` menyalin header/item/racik; nilai uang default 0.
15. Create penjualan menolak resep void atau tanpa obat (`PenjualanCreateHandler`).
16. Total header dihitung otomatis pada add/remove/apply nilai/void (`Recalculate`).
17. Penyesuaian header (`DiskonLain`, `BiayaLain`, `Pembulatan`, `Bulat`) lewat `SetHeaderAdjustment`.
18. Persistensi detail adalah replace-all (delete + insert) (`PenjualanRepo.SaveChanges`).

### Nilai

19. Line total = `(qty × harga) − diskon + embalase + tax + fee + bulat`.
20. Header `SumBiaya` = jumlah `Embalase` line aktif saja; `Fee` line tidak dijumlahkan ke header.
21. Biaya line tersimpan di field `Embalase`/`Fee`; biaya header di `BiayaLain` (+ `Pembulatan`/`Bulat`).

### Tidak terbukti (dinyatakan tidak ditemukan)

- Tidak ada rule yang membedakan BHP vs obat pada penjualan.
- Tidak ada rule “satu resep satu penjualan”.
- Tidak ada auto-apply `TipeBrgType.BiayaPerBarang` / `BiayaPerRacik` ke embalase.
- Tidak ada update in-place komponen racik (hanya add/remove).
- Tidak ada use case application untuk add/remove item setelah create penjualan.

---

## Lampiran: indeks file bukti

| Area | Path |
|---|---|
| Aggregate penjualan | `src/bilreg/Bilreg.Domain/SalesContext/PenjualanFeature/PenjualanModel.cs` |
| Line penjualan | `.../PenjualanItemType.cs` |
| Komponen racik penjualan | `.../PenjualanItemRacikType.cs` |
| Nilai line / header | `.../NilaiItemType.cs`, `.../NilaiPenjualanType.cs` |
| Aggregate resep | `src/bilreg/Bilreg.Domain/SalesContext/ResepFeature/ResepModel.cs` |
| Line resep | `.../ResepObatType.cs` |
| Komponen racik resep | `.../ResepItemRacikType.cs` |
| Etiket | `.../EtiketType.cs` |
| Repo penjualan | `src/bilreg/Bilreg.Infrastructure/SalesContext/PenjualanFeature/PenjualanRepo.cs` |
| DAL/DTO penjualan | `PenjualanDal.cs`, `PenjualanDto.cs`, `PenjualanItemDal.cs`, `PenjualanItemDto.cs` |
| Repo/DAL resep | `ResepRepo.cs`, `ResepDal.cs`, `ResepDto.cs`, `ResepBrgDal.cs`, `ResepBrgDto.cs` |
| SQL | `src/bilreg/Bilreg.SqlDb/SalesContext/tb_trs_dobill_umum.sql`, `tb_trs_dobill_umum2.sql`, `ta_trs_kartu_periksa.sql`, `ta_trs_kartu_periksa_resep.sql`, `ta_trs_kartu_periksa3.sql` |
| Use case | `Bilreg.Application/SalesContext/PenjualanFeature/UseCases/*`, `.../ResepFeature/UseCases/*` |
| API | `PenjualanController.cs`, `ResepController.cs` |
| Tes perilaku | `Bilreg.Test/SalesContext/PenjualanFeature/PenjualanModelTest.cs`, `PenjualanRepoTest.cs` |
| Katalog BHP | `BrgBhpType.cs`, `BrgRepo.cs`, `GroupRekDkType.cs` |
| Master biaya tipe barang | `TipeBrgType.cs` (tidak dipakai rumus penjualan) |
