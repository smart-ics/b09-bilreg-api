# Indeks SOP RUANG RANAP Operational System

**Batas authorization (ARCH-020).** SOP tetap mendefinisikan tanggung jawab bisnis, semantik authority, dan evidence. Implementasi saat ini hanya mengasumsikan autentikasi dasar dan akses aplikasi coarse-grained; contextual authorization fine-grained sengaja di luar scope fase ini dan ditunda ke Phase-99.

## Tujuan dan batas dokumen

Dokumen ini memuat inventaris prosedur operasional RUANG RANAP. Setiap SOP menjelaskan satu prosedur, menyebut aktor secara tegas pada setiap langkah, dan memakai istilah aplikasi yang telah tersedia pada artefak sumber. Bila nama menu belum ditetapkan, SOP memakai nama modul atau daftar kerja tanpa mengarang nama menu.

Batas tanggung jawab yang berlaku:

- Admisi Rawat Inap memiliki dan mengelola Waiting List. RUANG RANAP meninjau entri untuk Ward-nya tanpa mengambil alih tanggung jawab. Hanya Bed Assignment berhasil yang membuat akomodasi, menyebabkan Admisi menutup Waiting List, dan otomatis memindahkan tanggung jawab ke RNA.
- Perpindahan antar-RUANG RANAP memakai Release-to-Waiting-List. RNA asal melepas akomodasi dan memberi tahu Admisi. Setelah Release, Waiting List dan tanggung jawab operasional berada pada Admisi hingga Bed Assignment tujuan berhasil; konten klinis antar-ward dimiliki EMR.
- RUANG RANAP memiliki assignment, occupancy, transfer execution, release, dan Bed Readiness History transaksional. Bed menampilkan status readiness saat ini sebagai proyeksi transaksi terakhir. RNA juga menyimpan bukti otoritatif bahwa Service tertentu dilaksanakan oleh Performer tertentu pada waktu tertentu.
- Tarif Context memiliki Service Definition, Service aktif, `AllowedLayanan`, dan service configuration. Setiap Bangsal memiliki `LayananId` otoritatif; RNA hanya memilih Service eligible untuk eksekusi billable dan tidak membuat master Service lokal.
- CPOE memiliki Clinical Order intent, authorization, routing, lifecycle, final disposition, serta asosiasi dan validitas Fulfilment Outcome. RNA menerima Clinical Order aktif, memiliki bukti pelaksanaan, dan mengirim koreksi bukti yang terkait `ClinicalOrderId` bila berlaku.
- Tata Rekening adalah satu-satunya pemilik Charge Eligibility dan seluruh konsekuensi finansial. RNA hanya mempublikasikan Service Execution Fact tanpa interpretasi finansial.

## Hasil validasi sumber

- `docs/contexts/bangsal/RNA-DOMAIN.md` digunakan sebagai otoritas utama untuk scope, aturan, lifecycle, dan workflow RUANG RANAP.
- `docs/contexts/cpoe/CPOE-DOMAIN.md` digunakan hanya untuk batas Clinical Order, receiver coordination, final disposition, Fulfilment Outcome Validity, dan Outcome Review setelah bukti Completion diinvalidasi.
- `docs/contexts/admisi-ranap/admisi-ranap-domain.md` digunakan hanya untuk batas Admission dan Waiting List. Pernyataan utamanya bahwa room/bed assignment berada di RUANG RANAP dipertahankan.
- SOP Admisi digunakan sebagai referensi istilah dan granularitas. Bagian SOP referensi yang menempatkan Bed Assignment pada Admisi atau membuat bed langsung available setelah release tidak diterapkan karena bertentangan dengan otoritas RUANG RANAP Domain dan batas tanggung jawab yang ditetapkan.
- Workflow transfer pada Admisi adalah jalur operasional yang berlaku. RNA tidak mempunyai transfer queue atau ownership tujuan sebelum Bed Assignment berhasil. Penolakan Ward menjaga tanggung jawab pada Admisi.

## Inventaris SOP yang disetujui

| ID dan judul | Peran akuntabel utama | Pemicu | Hasil bisnis | Workflow domain terkait | Dependensi SOP |
|---|---|---|---|---|---|
| [SOP-RNA-A01 — Pemrosesan Waiting List Admisi dan Penetapan Akomodasi](SOP-RNA-A01-Pemrosesan-Waiting-List-dan-Penetapan-Akomodasi.md) | RUANG RANAP Coordinator / Head Nurse | Entri Waiting List Admisi untuk Ward tujuan | Bed Assignment berhasil mengaktifkan Clinical Accommodation; Admisi menutup Waiting List dan tanggung jawab berpindah ke RNA | RNA 10.1 Standard Accommodation Assignment; RI Direct/Planned/Elective Admission | Upstream: SOP Admisi `SOP-RI-C1` dan, bila menunggu, `SOP-RI-D1`; downstream: A02–A07 |
| [SOP-RNA-A02 — Pengelolaan Retained Accommodation](SOP-RNA-A02-Pengelolaan-Retained-Accommodation.md) | Perawat Ruangan atau Kepala Ruangan | Pasien menerima perawatan aktif pada akomodasi lain sementara akomodasi lama akan dipertahankan | Alokasi lama tetap Active, tetap memakai kapasitas, dan tetap menghasilkan fakta akomodasi sampai dilepas; seluruh retained release saat discharge | RNA 10.4 ICU Transfer with Retained VIP Accommodation | Upstream: A04 atau A05; downstream: A06 |
| [SOP-RNA-A03 — Pengelolaan Rooming-In](SOP-RNA-A03-Pengelolaan-Rooming-In.md) | Perawat Ruangan atau Kepala Ruangan | Patient Social Data membuktikan Baby Medical Record mereferensikan Mother Medical Record | Mother dan Baby tetap berbeda registrasi/histori; satu bed memuat satu Primary dan satu Baby tanpa penambahan kapasitas | RNA 10.5 Mother and Baby Rooming-In | Upstream: A01; downstream: A04, A05, atau A06 |
| [SOP-RNA-A04 — Transfer Akomodasi Internal RUANG RANAP](SOP-RNA-A04-Transfer-Akomodasi-Internal-RNA.md) | RUANG RANAP Coordinator / Head Nurse | Kebutuhan perpindahan bed atau room tanpa perpindahan tanggung jawab RUANG RANAP | Akomodasi tujuan aktif; alokasi lama dilepas atau direklasifikasi; histori berlanjut | RNA 10.2 Internal Accommodation Transfer | Upstream: A01; downstream: A02, A06, A07 |
| [SOP-RNA-A05 — Release ke Waiting List untuk Perpindahan Antar-RUANG RANAP](SOP-RNA-A05-Transfer-Antar-RNA.md) | Perawat Ruangan / Kepala Ruangan asal untuk Release; Admisi setelah Release | Kebutuhan perpindahan ke RUANG RANAP lain | Akomodasi asal dilepas, Admisi diberi tahu, pasien kembali ke Waiting List, dan Ward tujuan kelak memprosesnya seperti entri biasa | RNA 10.3 Inter-RUANG RANAP Release to Waiting List | Upstream: A01; downstream RNA asal: A06/A07; downstream Admisi: Waiting List; RNA tujuan: A01 |
| [SOP-RNA-A06 — Pelepasan dan Koreksi Akomodasi](SOP-RNA-A06-Pelepasan-Akomodasi.md) | Kepala Ruangan untuk Release; Head Nurse untuk koreksi | Tujuan Allocation berakhir atau Accommodation Fact yang masih dimiliki Ward yang sama salah | Pelepasan tercatat atau correction fact append-only dipublikasikan sebelum Tata Rekening `FINALIZED` | RNA 10.6 dan 10.6a | Upstream: A01–A05 atau kesalahan fakta; downstream: A07 dan bounded context penerima correction fact |
| [SOP-RNA-A07 — Pemulihan Bed Readiness](SOP-RNA-A07-Pemulihan-Bed-Readiness.md) | RUANG RANAP Coordinator / Head Nurse | Bed dilepas atau ditemukan dalam kondisi yang memerlukan cleaning, inspection, blocking, atau maintenance | Transaksi readiness tercatat dengan aktor/verifikator, waktu bisnis, alasan, dan bukti/referensi bila ada; Bed memproyeksikan status terakhir yang Ready atau tetap tidak tersedia | RNA 10.6 Accommodation Release and Bed Readiness | Upstream: A04–A06; downstream: A01, A04, atau A05 |
| [SOP-RNA-S01 — Penyelesaian Clinical Order oleh RUANG RANAP](SOP-RNA-S01-Pelaksanaan-RNA-Service-Berdasarkan-Clinical-Order.md) | Receiver dan Performer RUANG RANAP | Clinical Order berstatus `Active` diarahkan ke RNA | Clinical Order berakhir `Completed`, `Not Performed`, atau `Cancelled`; Completion merujuk bukti pelaksanaan RNA | CPOE Standard Clinical Order, Activity Not Performed, dan Order Cancellation | Upstream: CPOE dan Tarif; downstream: S04; bila bukti salah: S03 |
| [SOP-RNA-S02 — Pencatatan Tindakan Ad Hoc atau Independen](SOP-RNA-S02-Pelaksanaan-Tindakan-Ad-Hoc-atau-Independen.md) | Performer RUANG RANAP | Tindakan dilakukan secara sah tanpa Clinical Order prospektif individual | Fakta pelaksanaan tercatat dengan sumber, dasar kewenangan, waktu aktual, serta Service eligible bila billable atau description bila non-billable; tidak membuat Clinical Order retrospektif | RNA Ad Hoc/Independent Execution | Upstream: Tarif; downstream: S04; bila salah: S03; governance follow-up berada di luar CPOE |
| [SOP-RNA-S03 — Koreksi Fakta Pelaksanaan Layanan RUANG RANAP](SOP-RNA-S03-Koreksi-RNA-Service-Execution.md) | Kepala Ruangan atau Reviewer Independen sesuai materialitas; Responsible Clinician untuk Outcome Review | Kesalahan fakta pelaksanaan teridentifikasi | Fakta asli dipertahankan; koreksi dicatat append-only; Clinical Order tetap `Completed` dengan outcome `Corrected` atau `Invalidated` | CPOE Post-Completion Outcome Correction | Upstream: S01/S02; downstream: CPOE dan/atau S04 |
| [SOP-RNA-S04 — Publikasi Service Execution Fact kepada Tata Rekening](SOP-RNA-S04-Penetapan-dan-Penyerahan-Charge-Eligibility.md) | Sistem RNA / Aktor Pemulihan Integrasi | Service Execution Fact atau correction fact committed | Fakta dikirim idempoten dan acknowledgement terlihat; Tata Rekening menilai seluruh konsekuensi billing | RNA 10.12 | Upstream: S01–S03; downstream: Tata Rekening |

## Dasar pengelompokan

- Peninjauan pekerjaan aktif dan koordinasi kepada Performer merupakan rangkaian awal S01; keduanya bukan bukti pelaksanaan.
- Legacy order coexistence diperlakukan sebagai variasi sumber yang wajib mempertahankan identitas sumber, bukan prosedur baru.
- S04 hanya mempublikasikan fakta pelaksanaan. Charge Eligibility tidak termasuk prosedur RNA karena seluruh evaluasinya dimiliki Tata Rekening.
- Companion Accommodation adalah ekstensi SOP-RNA-A01: memakai Bed Assignment biasa dengan flag `IsCompanionBed = true`, terkait pada registrasi pasien tetapi bukan pasien dan tidak memiliki registrasi sendiri. Temporary Absence tidak dibutuhkan dan tidak dimodelkan oleh RNA.

## Referensi otoritatif

- `docs/contexts/bangsal/RNA-DOMAIN.md`
- `docs/contexts/cpoe/CPOE-DOMAIN.md`
- `docs/contexts/admisi-ranap/admisi-ranap-domain.md`
- `docs/contexts/admisi-ranap/admisi-ranap-sop/`
- `docs/skills/sop-creation-skill.md`
