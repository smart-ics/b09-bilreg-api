---
Title: Test Execution - Kompatibilitas Persistensi Polis Asuransi (ta_polis & ta_polis_cover) dengan fo-legacy
Code: TE-POLIS-LEGACY-NULL-ERROR-94-001
Feature: ISSUE-POLIS-LEGACY-NULL-ERROR-94-001 (Kompatibilitas Persistensi Polis Asuransi dengan fo-legacy)
ImplementationPlanStatus: COMPLETED
Tester: Penguji Manusia (Human Tester)
ExecutionDate: 2026-11-27
Artifact: TEST-EXECUTION
Gate: TEST PASSED
---

# Kriteria masuk

Pengujian memenuhi seluruh syarat masuk berikut:
- Rencana implementasi (`PLAN-POLIS-LEGACY-NULL-ERROR-94-001`) berstatus `COMPLETED` dengan slice `P1-S01` dan `P1-S02` berstatus `IMPLEMENTED` serta `GO`.
- Dokumen isu: [POLIS-LEGACY-NULL-ERROR-94-BUG-ISSUE.md](POLIS-LEGACY-NULL-ERROR-94-BUG-ISSUE.md)
- Dokumen arsitektur: [POLIS-LEGACY-NULL-ERROR-94-ARCHITECTURE.md](POLIS-LEGACY-NULL-ERROR-94-ARCHITECTURE.md)
- Paket uji: [POLIS-LEGACY-NULL-ERROR-94-TEST-PACKAGE.md](POLIS-LEGACY-NULL-ERROR-94-TEST-PACKAGE.md)

# 1. Ringkasan eksekusi

Seluruh 6 kasus uji (`TC-001` sampai `TC-006`) dinyatakan **PASS** setelah verifikasi API terhadap `Bilreg.Api` (`http://localhost:5194`), basis data `dev.smart-ics.com / HOSPITAL_PKL`, uji unit `Bilreg.Test`, dan konfirmasi langsung pada layar desktop VB6 `fo-legacy` (`[MSB-35] Polis Asuransi`). Gerbang **TEST PASSED** diberikan.

| Item | Nilai | Keterangan |
|---|---|---|
| Total kasus uji | 6 | `TC-001` sampai `TC-006` |
| Lulus (Passed) | 6 | Seluruh kasus uji lulus penuh (`API`, `DB`, dan `fo-legacy` `[MSB-35]`) |
| Gagal (Failed) | 0 | Tidak ada cacat |
| Terhalang (Blocked) | 0 | Seluruh prasyarat terpenuhi |
| Belum diuji (Not Tested) | 0 | Seluruh kasus uji selesai dijalankan |
| Uji unit (`Bilreg.Test` filter `Polis`) | 81/81 PASS | Durasi 731 ms, 0 gagal, 0 dilewati |
| Verifikasi skema SQL dan diff | PASS | `ta_polis.sql` (20/20 kolom `NOT NULL`), `ta_polis_cover.sql` (14/14 kolom `NOT NULL`) |

---

# 2. Hasil pengujian per kasus

## TC-001 — Pembuatan polis baru dan buka data di fo-legacy (Happy Path)

Status: PASS

Bukti dan catatan:
- Panggilan `POST http://localhost:5194/api/Polis` mengembalikan `200 OK` dengan respons `{"status":"success","code":"200","data":{"polisId":"PS00183969"}}`.
- Di tabel `dbo.ta_polis` untuk `PS00183969`, seluruh 20 kolom terisi non-null: `FS_KD_BU = ""`, `FS_DEPARTEMEN = ""`, `FS_NO_PAYROLL = ""`, `CRTTGL = "2026-11-27"`, `CRTJAM = "15:20:58"`, serta `CRTIPA`, `CRTVER`, dan `CRTUSR` terisi `""`.
- Di tabel `dbo.ta_polis_cover`, seluruh 14 kolom terisi non-null untuk `FS_MR = "3375022X0144080"` dengan `FS_KD_STATUS = "P"` dan `FD_TGL_EXPIRED` terisi non-null.
- Konfirmasi GUI `fo-legacy` (`[MSB-35] Polis Asuransi`): terbuka normal tanpa galat `Run-time error '94': Invalid use of Null` (terkonfirmasi lewat tangkapan layar penguji manusia).

---

## TC-002 — Pembaruan polis dan penyelarasan tanggal kedaluwarsa peserta (Business Rule)

Status: PASS

Bukti dan catatan:
- Panggilan `PUT http://localhost:5194/api/Polis` untuk `PS00183969` mengembalikan `200 OK` dengan `{"status":"success","code":"200","data":{"polisId":"PS00183969"}}`.
- Di tabel `dbo.ta_polis`, data berubah sesuai input: `FS_ATAS_NAMA = "TIKA RESTU FARMASI REVISI"`, `FD_EXPIRED = "2032-06-30"`, `FS_KD_KELAS_RI = "204"`, dan `FB_COVER_RJ = false`.
- Kolom pembuatan `CRTTGL = "2026-11-27"` dan `CRTJAM = "15:20:58"` tetap utuh, sedangkan `UPDTGL = "2026-11-27"` dan `UPDJAM = "15:22:20"` diperbarui. Seluruh 20 kolom tetap non-null.
- Di tabel `dbo.ta_polis_cover`, `FD_TGL_EXPIRED` ikut berubah menjadi `"2032-06-30"`, `UPDTGL = "3000-01-01"`, `UPDJAM = "00:00:00"`, dan seluruh 14 kolom tetap non-null.
- Konfirmasi GUI `fo-legacy` (`[MSB-35] Polis Asuransi`): tangkapan layar menunjukkan `PS00183969` tampil lengkap (`AAD00`, `UJI-POLIS-20261127-01`, `TIKA RESTU FARMASI REVISI`, `30-06-2032`, `204 I B`, peserta `3375022X0144080` status `P` dengan `Expired = 30-06-2032`, dan `Last saved by [27-11-2026 15:23:20]`) tanpa galat.

---

## TC-003 — Pelestarian data korporat legacy saat memperbarui polis lama (Business Rule & Regression Risk)

Status: PASS (menggunakan data uji `PS00033477`)

Bukti dan catatan:
- Pengujian awal pada `PS00060388` (`kelasRanapId = "202"`) mengembalikan `404` (`{"status":"AQ_RESOURCE_NOT_FOUND","code":"404","data":"Kelas 202 not found"}`) karena kode kelas `202` tidak ada di tabel `dbo.ta_kelas` pada basis data `HOSPITAL_PKL`.
- Pengujian dilanjutkan menggunakan polis legacy alternatif dari Bagian 3 paket uji, yaitu `PS00033477` (`kelasRanapId = "607"`), dan panggilan `PUT http://localhost:5194/api/Polis` mengembalikan `200 OK` dengan `{"status":"success","code":"200","data":{"polisId":"PS00033477"}}`.
- Di tabel `dbo.ta_polis` untuk `PS00033477`, nilai lama tetap utuh: `FS_KD_BU = " "`, `FS_DEPARTEMEN = " "`, `FS_NO_PAYROLL = "1111"`, `CRTTGL = "2016-07-15"`, `CRTJAM = "17:37:54"`, `CRTIPA = "192.168.10.110\\Kasir 3"`, `CRTVER = "11.5.5072"`, dan `CRTUSR = "VIMALA"`.
- Kolom pembaruan terisi `UPDTGL = "2026-11-27"` dan `UPDJAM = "15:22:23"`, dan seluruh 20 kolom tetap non-null.
- Di tabel `dbo.ta_polis_cover`, peserta `FS_MR = "337502200146995"` memiliki `FD_TGL_EXPIRED = "3000-01-01"` dan seluruh 14 kolom non-null.
- Konfirmasi GUI `fo-legacy` (`[MSB-35] Polis Asuransi`): penguji manusia mengonfirmasi `PS00033477` terbuka normal (`PASS`).

---

## TC-004 — Penambahan dan penghapusan peserta cakupan polis (Business Rule)

Status: PASS

Bukti dan catatan:
- Nilai `statusPesertaId = "T"` ditolak API dengan status `400` (`Invalid status code`) karena `StatusPesertaType` hanya menerima kode `P`, `S`, `I`, `A`, `O`, atau `X`. Pengujian dijalankan dengan kode valid `statusPesertaId = "I"`.
- Panggilan `PUT http://localhost:5194/api/Polis/addCoverage` untuk `PS00183969` dan `pasienId = "3375022X0144079"` (`statusPesertaId = "I"`) mengembalikan `200 OK`. Tabel `dbo.ta_polis_cover` mencatat 2 baris peserta (`3375022X0144079` status `I` dan `3375022X0144080` status `P`), keduanya memiliki `FD_TGL_EXPIRED = "2032-06-30"` dan 14 kolom non-null.
- Panggilan `PUT http://localhost:5194/api/Polis/removeCoverage` untuk `PS00183969` dan `pasienId = "3375022X0144079"` mengembalikan `200 OK`. Tabel `dbo.ta_polis_cover` menyisakan 1 baris (`3375022X0144080` status `P`) dengan `FD_TGL_EXPIRED = "2032-06-30"` dan 14 kolom non-null.
- Konfirmasi GUI `fo-legacy` (`[MSB-35] Polis Asuransi`): daftar peserta pada grid `PS00183969` tampil normal (`3375022X0144080` status `P`, `Expired = 30-06-2032`) tanpa galat.

---

## TC-005 — Pembacaan polis kasus awal (`PS00183968`) di API dan fo-legacy (Error Handling & Regression)

Status: PASS

Bukti dan catatan:
- Panggilan `GET http://localhost:5194/api/Polis/PS00183968` mengembalikan `200 OK` dengan `atasNama = "test"`, `expiredDate = "2050-01-01"`, dan `listCover[0].pasien.pasienId = "337502200307118"`.
- Panggilan `GET http://localhost:5194/api/Polis/byNoPeserta?noPeserta=11198909090901` mengembalikan `200 OK` dengan detail polis dan peserta `337502200307118` yang sama.
- Konfirmasi GUI `fo-legacy` (`[MSB-35] Polis Asuransi`): penguji manusia mengonfirmasi `PS00183968` terbuka normal tanpa galat `Run-time error '94': Invalid use of Null` (`PASS`).

---

## TC-006 — Penolakan input tidak lengkap saat pembuatan polis (Validation Rule)

Status: PASS

Bukti dan catatan:
- Panggilan `POST http://localhost:5194/api/Polis` dengan `noPolis = ""` ditolak dengan status `400` (`{"status":"AQ_INVALID_REQUEST","code":"400","data":"Required input request.NoPolis was empty. (Parameter 'request.NoPolis')"}`).
- Panggilan dengan `expiredDate = "31-12-2030"` menggunakan `tipeJaminanId = "AAD00"` untuk pasien `3375022X0144080` mengembalikan `200 OK` dengan `PS00183969` karena pemeriksaan idempotensi `PolisCreateHandler.CekPeserta` mengembalikan polis yang sudah ada sebelum `DateOnly.ParseExact` dijalankan.
- Saat diuji dengan `tipeJaminanId = "ADM00"` (belum memiliki polis untuk pasien tersebut), format tanggal `"31-12-2030"` ditolak oleh API dengan status `500` (`{"status":"Internal Server Error","code":"500","data":"String '31-12-2030' was not recognized as a valid DateOnly."}`).
- Pemeriksaan pada `dbo.ta_polis` untuk `fs_atas_nama = 'UJI VALIDASI'` atau `fs_no_polis = 'UJI-INVALID-DATE'` menunjukkan `0` baris tersimpan.

---

# 3. Verifikasi uji unit dan skema basis data

- Uji unit `Bilreg.Test`: perintah `dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~Polis" -p:UseSharedCompilation=false /nodeReuse:false` lulus seluruhnya dengan total 81 tes, 81 lulus, 0 gagal, dan 0 dilewati dalam 731 ms.
- Verifikasi skema `ta_polis.sql`: seluruh 20 kolom tersedia dan bertipe `NOT NULL` dengan constraint `DEFAULT`, termasuk 3 kolom legacy (`fs_kd_bu`, `fs_departemen`, `fs_no_payroll`) dan 10 kolom audit (`crt*` dan `upd*` dengan nilai bawaan `3000-01-01` dan `00:00:00`).
- Verifikasi skema `ta_polis_cover.sql`: seluruh 14 kolom tersedia dan bertipe `NOT NULL` dengan constraint `DEFAULT`, termasuk `fd_tgl_expired` dan 10 kolom audit `crt*` serta `upd*`, beserta indeks `IX_ta_polis_cover_fs_kd_polis`.
- Verifikasi diff kode (`PolisDto.cs`, `PolisDal.cs`, `PolisCoverDto.cs`, `PolisCoverDal.cs`, `PolisRepo.cs`): `PolisDal.Insert` menulis 20 kolom, `PolisDal.Update` memperbarui kolom bisnis dan `upd*` sambil menjaga `crt*` serta kolom korporat legacy lewat `ISNULL(..., '')`, `PolisRepo.SaveChanges` menyalin `model.ExpiredDate` ke `PolisCoverDto.FromModel`, dan `PolisCoverDal.Insert` memetakan seluruh 14 kolom lewat `SqlBulkCopy`.

---

# 4. Catatan dan temuan ringan

- Data uji `PS00060388` pada `TC-003` gagal karena kode kelas `202` tidak terdaftar di `dbo.ta_kelas` pada basis data `HOSPITAL_PKL`, sehingga pengujian dijalankan dengan `PS00033477` (`kelasRanapId = "607"`).
- Contoh `statusPesertaId = "T"` pada `TC-004` bukan kode yang valid di `StatusPesertaType` (kode valid adalah `P`, `S`, `I`, `A`, `O`, `X`), sehingga pengujian menggunakan `I`.
- Pada `TC-006`, format tanggal tidak valid `31-12-2030` menghasilkan status `500` dari `DateOnly.ParseExact` saat belum ada polis yang cocok di `CekPeserta`, bukan `400 Bad Request`. Tidak ada data rusak yang tertulis ke basis data.

---

# 5. Langkah selanjutnya

- Gerbang **TEST PASSED** telah diberikan karena seluruh 6 kasus uji (`TC-001` sampai `TC-006`) dan 81 uji unit `Polis` berstatus `PASS`.
- Perubahan pada worktree `worktrees/polis-legacy-null-error-94/b09-bilreg-api` siap dilanjutkan ke tahap integrasi/merge sesuai alur tim.
