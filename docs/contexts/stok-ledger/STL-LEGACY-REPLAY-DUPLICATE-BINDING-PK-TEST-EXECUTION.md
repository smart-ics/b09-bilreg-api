---
Title: Catatan Eksekusi Uji Koreksi Pembuatan Tindakan Ber-BOM Barang dan Keandalan Sinkronisasi Stok
Code: STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK
Artifact: TEST-EXECUTION
Version: 1.0
LastUpdated: 2026-11-20
Status: IN-PROGRESS
---

# 1. Ringkasan Eksekusi

- **Test Package**: [STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-TEST-PACKAGE.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api-worktree-stl-replay/docs/contexts/stok-ledger/STL-LEGACY-REPLAY-DUPLICATE-BINDING-PK-TEST-PACKAGE.md)
- **Lingkungan Uji**: `HOSPITAL_PKL` (`ZCOMPILER` / `dev.smart-ics.com`)
- **Progress**: `TC-01` s.d. `TC-11` dan `TC-15` **PASS** (12/15 selesai; `TC-12` s.d. `TC-14` belum dijalankan)

# 2. Hasil Per Kasus Uji

| ID | Judul | Status | Bukti / Catatan |
|---|---|---|---|
| `TC-01` | Indeks `BILRG_StokMutasi` sudah non-unik | `PASS` | `IX_BILRG_StokMutasi_TrsReffId_MovementKind_StokLokasiId` ada dengan `is_unique = 0`, `has_filter = 0`; tidak ada indeks `UX_`. |
| `TC-02` | Migrasi aman dijalankan ulang | `PASS` | Eksekusi ulang `BILRG_StokMutasi.sql` selesai tanpa error; jumlah baris `BILRG_StokMutasi` tetap `0`; indeks tetap non-unik. |
| `TC-03` | Simpan tindakan ber-3 barang BOM | `PASS` | `POST /api/tindakan/create` (`RG01850254`, `2RJ04`, `01KY4C6TF1YC1RXEKCM5GCV4X9`, BOM `A0401101`: `1 SUSU 5`, `1 SUSU 6`, `1 SUSU 7`) -> HTTP 200, `TDK0ADSUCW`. 3 baris di `BILRG_TindakanBarang` (`TBG0ADSUD2Y5`, `TBG0ADSUD30C`, `TBG0ADSUD30Y`) dan 3 baris di `BILRG_TindakanStokPending` (`PND0ADSUD30G`, `PND0ADSUD30R`, `PND0ADSUD30S`) dengan ID unik semua. |
| `TC-04` | Stok v2 dan stok lama tetap sama setelah simpan | `PASS` | `BILRG_StokLokasi`, `tb_stok`, dan `BILRG_StokLegacyScope` tetap konsisten (selisih `0`, tidak ada status `Inconsistent = 3`). |
| `TC-05` | Simpan tindakan ulang segera (stok sudah sinkron) | `PASS` | `POST /api/tindakan/create` ulang -> HTTP 200, `TDK0ADSWFJ`. `JmlBinding = 0` dan `JmlMutasi = 0` sebelum & sesudah, tidak ada duplikasi. |
| `TC-06` | Simpan tindakan lewat endpoint `save` | `PASS` | `POST /api/tindakan/save` -> HTTP 200, `TDK0ADSXDA`. 3 baris di `BILRG_TindakanBarang` (`TBG0ADSXDKON`, `TBG0ADSXDKOQ`, `TBG0ADSXDKOR`) dan 3 baris di `BILRG_TindakanStokPending` (`PND0ADSXDKO3`, `PND0ADSXDKOH`, `PND0ADSXDKOI`). |
| `TC-07` | Stok tidak cukup masuk antrean tertunda | `PASS` | Terverifikasi pada `TDK0ADSUCW` & `TDK0ADSXDA`: `QtyKecilDeducted = 0.00`, `QtyKecilPending = 1.00`, `StatusPotStok = False (0)`, stok tidak negatif. |
| `TC-08` | Riwayat stok lama bermasalah tidak menghentikan tindakan | `PASS` | `BILRG_StokLegacyScope` untuk `1 SUSU 5` ditandai `AlignmentStatus = 3` (`Inconsistent`). `POST /api/tindakan/create` (`RG01850321`, `2RJ03`, `01KY4C6TF1ZF5A0WT28SZQF7R5`) -> HTTP 200, `TDK0ADT6K8`. `1 SUSU 5` masuk `BILRG_TindakanStokPending` (`PND0ADT6KHZX`, `QtyPending = 1.00`), 2 barang lain diproses normal. |
| `TC-09` | Tarif dengan dua baris barang yang sama | `PASS` | `TA_TARIF7` (`A0401102`) diisi 2 baris `2.CATR.1` (`BH`, qty 1). `POST /api/tindakan/create` (`RG01850288`, `4RD01`, `01KY4C6TG1BDKVWG3DKKNRD9FV`) -> HTTP 200, `TDK0ADTHJA`. 2 baris di `BILRG_TindakanBarang` (`TBG0ADTHK8Y3`, `TBG0ADTHK8YQ`), keduanya `QtyKecilDeducted = 1.00` ke satu `PakaiBrgId = PKA7O1Q72A`. Stok `2.CATR.1` di `4RD01` berkurang tepat 2 (`4.00` -> `2.00` di `tb_stok`, `2` di `BILRG_StokLokasi`). |
| `TC-10` | Sinkronisasi riwayat besar tidak lambat atau gagal | `PASS` | `TA_TARIF7` (`A0401102`) diisi `2.ABBR.1` (`BH`, qty 1). `POST /api/tindakan/create` -> HTTP 200, `TDK0ADTLHH` dalam **25.8 s** tanpa timeout. `TBG0ADTLR9T1` terpotong (`QtyKecilDeducted = 1.00`, `PakaiBrgId = PKA7O1V2GM`), `BILRG_StokMutasi` untuk `2.ABBR.1` tercatat 2 baris. |
| `TC-11` | Sinkronisasi scope `2.ABBR.2` | `PASS` | `POST /api/tindakan/create` -> HTTP 200, `TDK0ADU0X3`. `TBG0ADU0Y3TI` terpotong (`QtyKecilDeducted = 1.00`, `PakaiBrgId = PKA7O2CPKL`), `BILRG_StokLegacyScope` `AlignmentStatus = 1` (`Aligned`). |
| `TC-15` | Pembatalan tindakan mengembalikan stok | `PASS` | `PATCH /api/tindakan/batal` (`TDK0ADU0X3`) -> HTTP 200. `TBG0ADU0Y3TI` `IsVoid = True`, stok `2.ABBR.2` di `4RD01` kembali ke `2.00` (`tb_stok`) dan `2` (`BILRG_StokLokasi`). |
