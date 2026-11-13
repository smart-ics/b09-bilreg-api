# ISSUE

## Metadata

ID: ISSUE-TR-BUG-001
Type: BUG
Status: Open
Title: Kegagalan Batalkan Finalisasi (Downstream Blocker Palsu), Investigasi Perilaku Reopen Billing (Closed ke Opened), dan Redundansi Tombol Settlement pada Layar Tata Rekening

## Source

Reported By: User / Verifikator Operasional Tata Rekening
Reported Date: 2026-11-06

## Description

Ditemukan tiga permasalahan fungsional dan antarmuka pada siklus hidup tagihan di modul Tata Rekening:

1. **Kegagalan Batalkan Finalisasi karena Terdeteksi Alokasi Pembayaran:**
   Ketika pengguna/verifikator mengeksekusi aksi **Batalkan Finalisasi** melalui dialog pembatalan finalisasi, sistem menolak aksi tersebut dan memunculkan pesan peringatan:
   > *"Pembatalan status Final ditolak: Alokasi Pembayaran atau Transaksi Kasir telah diproses. Batalkan transaksi kasir/alokasi pembayaran terlebih dahulu sebelum membuka kunci tagihan."*
   
   Padahal, tagihan baru saja difinalisasi di tingkat Tata Rekening dan belum pernah diserahkan atau diproses pembayarannya di kasir. Batal Finalisasi seharusnya tidak memvalidasi alokasi penjamin internal sebagai blocker transaksi luar, melainkan membatalkan/menghapus alokasi tersebut dan mengembalikan status tagihan dari `FINALIZED` ke `CLOSED`.

2. **Perilaku Transisi Status Reopen Billing (Closed ke Opened):**
   Saat pengguna memilih aksi **Buka Kembali (Reopen)** pada tagihan yang berstatus `CLOSED`, sistem langsung mengubah status tagihan menjadi `OPEN` (dibuka kembali). Diperlukan investigasi mendalam terhadap perilaku pembukaan kembali ini, termasuk konsekuensi terhadap pembersihan data alokasi finansial, verifikasi tarif, dan keterkaitan dengan hak subsistem sumber (*charge source*) untuk mengubah biaya operasional.

3. **Redundansi Tombol Settlement pada Tampilan Layar Tata Rekening:**
   Pada antarmuka pengguna Tata Rekening (Action Panel dan Action Toolbar), tombol **Inisiasi Settlement (Reg Out)** beserta panduan langkah berikutnya (*Next Action Guidance*) masih terlihat dan aktif. Pada model alur kerja Tata Rekening 2-langkah terbaru, tahap Finalisasi Billing sudah mencakup penetapan alokasi dan status keluar pasien (Reg Out). Keberadaan tombol Inisiasi Settlement pada antarmuka membingungkan verifikator dan tidak lagi relevan, sehingga tombol tersebut harus dihilangkan dari tampilan layar Tata Rekening.

## Desired Outcome

1. **Batalkan Finalisasi Berhasil Tanpa Terblokir Alokasi Internal:**
   - Aksi Batalkan Finalisasi dapat dieksekusi dengan sukses sebelum proses settlement kasir riil dimulai.
   - Alokasi tanggung jawab finansial (penjamin/kasir) yang dibuat saat finalisasi dibersihkan/dibatalkan secara otomatis.
   - Status tagihan berhasil kembali ke `CLOSED`.
   - Setelah pembatalan berhasil, indikator langkah berikutnya (*Next Action*) dan tombol yang aktif disorot pada antarmuka pengguna otomatis mengarah ke **Langkah 2: Finalisasi Billing**.

2. **Kejelasan dan Validitas Alur Reopen Billing:**
   - Hasil investigasi mendokumentasikan secara formal alur perubahan status dari `CLOSED` ke `OPEN` saat Reopen Billing.
   - Memastikan bahwa pembukaan kembali tagihan membatalkan kunci mutasi biaya dan membersihkan entitas alokasi sebelumnya secara konsisten dengan dokumen SOP tata rekening.

3. **Pembersihan Antarmuka dari Tombol Settlement:**
   - Tombol **Inisiasi Settlement (Reg Out)** dihilangkan sepenuhnya dari tampilan panel aksi (`TataRekeningActionPanel.vue`) dan toolbar aksi (`TataRekeningActionToolbar.vue`).
   - Panduan alur berikutnya (*Next Action Card*) tidak lagi mengarahkan pengguna ke inisiasi settlement pasca-finalisasi.

## Current Situation

- Pada backend API, pemanggilan aksi `CancelFinalization` memicu validasi downstream inspector yang mendeteksi keberadaan baris penjamin pada tabel proyeksi alokasi (`ta_trs_billing2`), sehingga memunculkan pesan penolakan downstream blocker meskipun belum ada transaksi kasir.
- Pada aksi `Reopen`, handler backend langsung mengubah status `TataRekeningModel` dari `Closed` ke `Opened` melalui `tataRekening.ReOpen()`, mereset verifikasi dan alokasi.
- Pada antarmuka frontend, komponen `TataRekeningActionPanel.vue` (baris 790–822) dan `TataRekeningActionToolbar.vue` masih menampilkan elemen tombol `Inisiasi Settlement (Reg Out)` dan mendaftarkan aksi `settle` pada `nextActionInfo`.

## Evidence

- Pesan penolakan yang muncul pada dialog:
  `"Pembatalan status Final ditolak: Alokasi Pembayaran atau Transaksi Kasir telah diproses. Batalkan transaksi kasir/alokasi pembayaran terlebih dahulu sebelum membuka kunci tagihan."`
- Berkas kode backend terkait:
  - `Bilreg.Domain/PaymentContext/TataRekeningFeature/TataRekeningModel.cs`
  - `Bilreg.Infrastructure/PaymentContext/TataRekeningFeature/DownstreamSettlementInspector.cs`
  - `Bilreg.Application/PaymentContext/AlokasiPembayaranFeature/CancelFinalizationCommand.cs`
  - `Bilreg.Application/PaymentContext/TataRekeningFeature/UseCases/ReopenBillingCommand.cs`
- Berkas kode frontend terkait:
  - `src/modules/TataRekening/components/actions/TataRekeningActionPanel.vue`
  - `src/modules/TataRekening/components/actions/TataRekeningActionToolbar.vue`
  - `src/modules/TataRekening/composables/useTataRekeningActionGuards.ts`
- Dokumen acuan SOP:
  - `docs/contexts/TataRekening/SOP-TR-08 — Cancel Finalization.md`
  - `docs/contexts/TataRekening/SOP-TR-09 — Reopen Billing.md`
  - `docs/contexts/TataRekening/SOP-TR-10 — Settlement Initiation.md`

## Notes

- ISSUE ini bersifat solution-neutral dan berfungsi sebagai intake formal bagi peran analis dan arsitek.
- Downstream berikutnya adalah tahap **BUG-INVESTIGATION** oleh `ica-analyst` atau `ica-architect`, dilanjutkan perancangan pembaruan arsitektur dan rencana implementasi.
