# ISSUE

## Metadata

ID: ISS-STL-001
Type: BUG
Status: Open
Title: Pembuatan tindakan gagal dengan error duplicate primary key PK_BILRG_StokLegacyBinding saat sinkronisasi stok dari legacy

## Source

Reported By: Laporan operasional di lingkungan lokasi (via ica-issuer)
Reported Date: 2026-11-19

## Description

Saat pengguna membuat tindakan dengan BOM barang melalui endpoint `POST /api/tindakan/create` pada lingkungan lokasi (bukan database development), permintaan gagal dengan status HTTP 500 karena pelanggaran primary key `PK_BILRG_StokLegacyBinding` pada tabel `dbo.BILRG_StokLegacyBinding`.

Kegagalan ini terjadi ketika proses pembuatan tindakan menjalankan sinkronisasi saldo dan riwayat stok dari tabel legacy (`tb_buku`) ke tabel Stock Ledger v2 untuk barang-barang BOM yang terhubung pada tarif tindakan tersebut.

## Desired Outcome

1. Permintaan `POST /api/tindakan/create` untuk tarif yang memiliki BOM barang berhasil memproses sinkronisasi riwayat stok dari legacy dan menyelesaikan pencatatan tindakan tanpa error pelanggaran primary key pada `BILRG_StokLegacyBinding` maupun tabel Stock Ledger v2 lainnya.
2. Sinkronisasi riwayat stok dari `tb_buku` tetap berjalan andal pada database operasional dengan jumlah baris riwayat legacy yang besar.

## Current Situation

1. Pengguna mengirim permintaan `POST /api/tindakan/create` di server lokasi (`http://192.168.100.24:8888/BilregApi/api/tindakan/create`) untuk registrasi aktif `RG00039464`, layanan `RJ005`, dan `nilaiTarifId = 01M6G0G33FMX7YKKFBKN30YXHH` (`TRJ0000037`, `fb_charge_brg = 0`, 3 barang BOM).
2. Sebelum perbaikan indeks unik `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` di database lokasi, permintaan gagal pada `dbo.BILRG_StokMutasi` dengan kunci duplikat `(DU0011211A, 5, STL0AD2XH65G)`.
3. Setelah indeks `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` di database lokasi diperbarui menggunakan filter `WHERE MovementKind IN (1, 2, 3, 8, 9, 10)` sesuai definisi DDL terbaru, percobaan ulang permintaan yang sama gagal pada tahap berikutnya dengan error HTTP 500:
   `Violation of PRIMARY KEY constraint 'PK_BILRG_StokLegacyBinding'. Cannot insert duplicate key in object 'dbo.BILRG_StokLegacyBinding'. The duplicate key value is (SBD0AD36KXMT).`
4. Seluruh transaksi `POST /api/tindakan/create` ter-rollback sehingga tindakan tidak tersimpan.

## Evidence

### 1. Endpoint dan payload permintaan

Endpoint: `POST http://192.168.100.24:8888/BilregApi/api/tindakan/create`

```json
{
    "regId": "RG00039464",
    "layananId": "RJ005",
    "nilaiTarifId": "01M6G0G33FMX7YKKFBKN30YXHH",
    "qty": 1,
    "userId": "ICS0000002",
    "listPpa": [],
    "pakaiBedId": "",
    "isDiskonDokter": false
}
```

### 2. Respons error yang teramati

Error aktif (setelah indeks `BILRG_StokMutasi` difilter):

```json
{
    "status": "Internal Server Error",
    "code": "500",
    "data": "Violation of PRIMARY KEY constraint 'PK_BILRG_StokLegacyBinding'. Cannot insert duplicate key in object 'dbo.BILRG_StokLegacyBinding'. The duplicate key value is (SBD0AD36KXMT).\r\nThe statement has been terminated."
}
```

Error sebelumnya pada insiden yang sama (sebelum indeks `UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` di database lokasi diperbarui dengan filter `WHERE`):

```json
{
    "status": "Internal Server Error",
    "code": "500",
    "data": "Cannot insert duplicate key row in object 'dbo.BILRG_StokMutasi' with unique index 'UX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId'. The duplicate key value is (DU0011211A, 5, STL0AD2XH65G).\r\nThe statement has been terminated."
}
```

### 3. Data BOM tarif terkait di database lokasi

Hasil query `BILRG_NilaiTarif` + `TA_TARIF` + `TA_TARIF7` untuk `NilaiTarifId = '01M6G0G33FMX7YKKFBKN30YXHH'`:

| NilaiTarifId | TarifId | fb_charge_brg | fn_no_urut | fs_kd_barang | fs_kd_satuan | fn_qty_barang |
|---|---|---|---|---|---|---|
| 01M6G0G33FMX7YKKFBKN30YXHH | TRJ0000037 | 0 | 0 | ALK000000164 | PCS | 1.00 |
| 01M6G0G33FMX7YKKFBKN30YXHH | TRJ0000037 | 0 | 0 | ALK000000107 | PCS | 1.00 |
| 01M6G0G33FMX7YKKFBKN30YXHH | TRJ0000037 | 0 | 0 | ALK000000002 | PCS | 1.00 |

### 4. Referensi repositori terkait

- `b09-bilreg-api` (konteks `InventoryContext/StockLedgerFeature` dan `ChargeContext/TindakanFeature`).

## Notes

- Insiden terjadi pada database operasional lokasi, bukan database development.
- Error `PK_BILRG_StokLegacyBinding` muncul setelah kendala indeks lama pada `BILRG_StokMutasi` diperbaiki di database lokasi.
- Sebagai catatan operasional tambahan untuk proses provisioning database lokasi yang sudah pernah membuat tabel Stock Ledger v2 versi lama, skrip `BILRG_StokMutasi.sql` memakai guard `IF NOT EXISTS` sehingga indeks lama tanpa filter `WHERE` tidak otomatis diperbarui tanpa langkah migrasi tersendiri.
