# ISSUE

## Metadata

ID: ISSUE-BILREG-ANTRIAN-POLI-PREFIX-BUG-001
Type: BUG
Status: OPEN
Title: Prefix Ruangan Tidak Tampil pada Cetakan Struk Registrasi Rawat Jalan dan Query Detail Registrasi (GET /api/reg/{id})

## Source

Reported By: Tester (DEF-001 dari ANTRIAN-POLI-PREFIX-TEST-EXECUTION.md, TC-01)
Reported Date: 2026-11-13

## Description

Pada pengujian kasus uji TC-01 (Pendaftaran Walk-In dengan Ruangan Berprefix) untuk fitur antrian poli prefix, pendaftaran pasien ke Poli Penyakit Dalam pada ruangan dengan prefix `C` (bukti registrasi `RG01378282`) menghasilkan cetakan struk registrasi dengan nomor antrian polos tanpa huruf prefix (tercetak `7`, bukan `C7`).

Setelah diverifikasi, endpoint detail registrasi `GET /api/reg/{id}` yang dipanggil oleh antarmuka registrasi hanya mengembalikan nomor urut numerik murni (`noAntrian: 7`) tanpa menyertakan properti label antrian berprefix (`queueLabel` atau `prefixAntrian`). Akibatnya, nomor antrian pada bukti cetak pendaftaran di loket admisi tidak mencantumkan prefix ruangan poliklinik tujuan.

## Desired Outcome

1. Bukti cetak struk registrasi rawat jalan (`antrian_registrasi.html` melalui antarmuka pendaftaran petugas) menampilkan nomor antrian berprefix ringkas (contoh: `C7`) selaras dengan prefix ruangan pada jadwal dokter.
2. Endpoint detail registrasi `GET /api/reg/{id}` (`RegGetQuery` / `RegGetResponse`) menyediakan properti aditif label antrian (`queueLabel` dan `prefixAntrian`) tanpa mengubah tipe data `noAntrian: int` yang sudah ada (*zero breaking change*).

## Current Situation

1. **Backend (`b09-bilreg-api`)**:
   - Model respons `RegGetResponse` pada query `RegGetQuery` di `Bilreg.Application.AdmisiContext.RegFeature.UseCases` hanya memuat kolom `int NoAntrian`.
   - Respons `GET /api/reg/{id}` belum memetakan atau memformat snapshot prefix sesi antrian dokter ke dalam properti `QueueLabel` maupun `PrefixAntrian`.
2. **Frontend (`c012_myhospital_web`)**:
   - Fungsi penyiapan data cetak `preparePrintItems()` pada `LegacyRegistrationWorkspace.vue` saat ini memetakan nilai `noAntrian: reg.noAntrian` (angka polos `7`) secara langsung ke template cetak `antrian_registrasi.html`.
3. **Hasil Cetakan**:
   - Struk registrasi fisik maupun pratinjau cetak hanya memuat angka polos tanpa kode huruf prefix ruangan (`7`).

## Evidence

1. Dokumen pengujian:
   - [ANTRIAN-POLI-PREFIX-TEST-EXECUTION.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api-worktree-antrian-poli-prefix/docs/contexts/admisi/ANTRIAN-POLI-PREFIX-TEST-EXECUTION.md) (Hasil TC-01: FAIL, Cacat: `DEF-001`).
2. Tangkapan layar bukti cetak:
   - `Screenshot 2026-11-13 134635.png` (menampilkan nomor antrian tercetak polos `7` pada pendaftaran Poli Penyakit Dalam).
3. Respons JSON endpoint `GET http://localhost:5189/api/reg/RG01378282`:
   ```json
   {
       "status": "success",
       "code": "200",
       "data": {
           "regId": "RG01378282",
           "regDate": "2024-06-06",
           "regMasukAudit": {
               "userId": "AD00001",
               "timestamp": "2024-06-06T14:24:26"
           },
           "regKeluarAudit": {
               "userId": "",
               "timestamp": "3000-01-01T00:00:00"
           },
           "regCancelOutAudit": {
               "userId": "",
               "timestamp": "3000-01-01T00:00:00"
           },
           "isAktif": true,
           "jenisReg": 0,
           "jenisRegDesc": "RegJalan",
           "pasien": {
               "pasienId": "337502200003455",
               "pasienName": "ATUN",
               "tglLahir": "1998-10-20",
               "gender": "P"
           },
           "umur": "25 tahun, 7 bulan, 17 hari",
           "tipeJaminan": {
               "tipeJaminanId": "00000",
               "tipeJaminanName": "UMUM [BAYAR SENDIRI]"
           },
           "polis": {
               "polisId": "-",
               "noPolis": "-",
               "atasName": "-"
           },
           "kelas": {
               "kelasId": "000",
               "kelasName": "NON KELAS"
           },
           "caraMasukDk": {
               "caraMasukDkId": "8",
               "caraMasukDkName": "DATANG SENDIRI",
               "requiresRujukan": false
           },
           "rujukan": {
               "rujukanId": "-",
               "rujukanName": "-"
           },
           "dokter": {
               "ppaId": "DR00103",
               "ppaName": "MIRA ASTUTI, dr. Sp. PD, Finasim",
               "isDefault": false
           },
           "jamPraktek": "08:00",
           "layanan": {
               "layananId": "2RJ03",
               "layananName": "POLI PENY DALAM"
           },
           "karcis": {
               "karcisId": "24",
               "karcisName": "PAKET POLIKLINIK"
           },
           "tipeTarif": {
               "tipeTarifId": "UM",
               "tipeTarifName": "UMUM"
           },
           "tipeBarang": {
               "tipeBarangId": "RJ",
               "tipeBarangName": "RAWAT JALAN"
           },
           "noAntrian": 7,
           "sjpNo": "-"
       }
   }
   ```

## Notes

- Cacat teridentifikasi saat pengujian regresi dan verifikasi paket uji `ANTRIAN-POLI-PREFIX`.
- Sifat perubahan perbaikan yang diharapkan adalah aditif pada kontrak data (`queueLabel`, `prefixAntrian`) agar tidak menimbulkan regresi pada pemanggil lain.
