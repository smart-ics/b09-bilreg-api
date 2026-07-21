# SOP-RNA-A01 — Pemrosesan Waiting List Admisi dan Penetapan Akomodasi

## 1. Tujuan

Menempatkan pasien rawat inap pada akomodasi yang sesuai dari Waiting List milik Admisi. Pasien perpindahan antar-ward yang telah dikembalikan ke Waiting List diproses dengan prosedur yang sama. Companion Accommodation menggunakan prosedur Bed Assignment, validasi, assignment, release, dan audit yang sama dengan flag `IsCompanionBed = true`, tanpa membuat registrasi companion atau workflow approval terpisah.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Petugas Admisi | Manusia | Memiliki dan mengelola Waiting List; menerima hasil Bed Assignment atau penolakan Ward. |
| Kepala Ruangan | Manusia | Meninjau Waiting List, menolak dengan alasan bila Ward tidak dapat menempatkan, menetapkan penempatan, dan menangani eskalasi. |
| Perawat Ruangan | Manusia | Memeriksa kesiapan penempatan dan mencatat fakta penempatan aktual. |
| Sistem | Sistem | Menampilkan Waiting List dan status bed, mencatat hasil review, dan mengirim hasil Bed Assignment kepada Admisi. |
| Pasien atau Perwakilan Pasien | Manusia | Memberikan informasi atau persetujuan apabila diminta kebijakan rumah sakit. |

## 3. Prasyarat

- Petugas terkait telah masuk ke Sistem dan memiliki hak akses yang sesuai.
- Waiting List milik Admisi memuat identitas registrasi dan ruang rawat tujuan yang sah.
- Bila kebutuhan berasal dari perpindahan antar-ward, Ward tujuan memproses entri Waiting List Admisi yang sama; tidak ada permintaan langsung dari RNA asal.
- Bed yang akan dipilih memenuhi Mandatory Bed Assignability: ada dan aktif, berada pada Ward tujuan yang benar, Ready, tidak memiliki alokasi aktif yang bertentangan, serta kapasitas tersedia sesuai occupancy policy.

## 4. Langkah Operasional

1. **Kepala Ruangan** membuka Waiting List Admisi untuk Ward pada **Sistem**.
2. **Kepala Ruangan** memeriksa identitas registrasi dan Ward tujuan pada entri Waiting List.
3. **Perawat Ruangan** memeriksa kandidat room dan bed yang tersedia pada **Sistem**.
4. **Kepala Ruangan** memilih bed yang memenuhi Mandatory Bed Assignability dan menetapkan tujuan penggunaan akomodasi. Untuk Companion Accommodation, Kepala Ruangan memakai prosedur yang sama dan menetapkan `IsCompanionBed = true`; companion terkait pada registrasi pasien tetapi bukan pasien dan tidak menjadi Clinical Accommodation. Pertimbangan gender, isolasi, peralatan, dan kebijakan operasional lokal tetap merupakan keputusan manual Ward dan tidak menjadi penolakan otomatis RNA.
5. **Sistem** memvalidasi Mandatory Bed Assignability pada saat penetapan dan menyimpan aktor, waktu, serta hasil validasi.
6. **Perawat Ruangan** menerima pasien pada bed yang telah ditetapkan dan mencatat waktu penerimaan aktual pada **Sistem**.
7. **Sistem** membuat Accommodation Fact dan menampilkan hasil penempatan. Untuk penempatan pasien dari Waiting List, sistem mengirim hasil Bed Assignment kepada Admisi. Untuk Companion Accommodation, sistem menyimpan dan mempublikasikan flag `IsCompanionBed = true` kepada consumer yang berwenang; tidak ada Waiting List atau hasil Bed Assignment kepada Admisi.
8. **Admisi** menutup entri Waiting List sebagai konsekuensi Bed Assignment berhasil; tanggung jawab otomatis berpindah ke RNA.

## 5. Pengecualian Operasional

- Bila entri Waiting List tidak dapat diproses, **Kepala Ruangan** menolak dengan alasan; **Sistem** menyampaikan hasil tersebut kepada **Petugas Admisi**. Tanggung jawab tetap pada Admisi dan tidak ada alokasi aktif yang dibuat.
- Bila tidak ada bed yang memenuhi Mandatory Bed Assignability, **Kepala Ruangan** menolak penempatan dan menyampaikan alasannya kepada **Petugas Admisi**.
- Bila ada alokasi aktif yang bertentangan, **Kepala Ruangan** menunda penetapan dan menggunakan SOP transfer atau pelepasan yang sesuai.
- RNA tidak menolak Bed Assignment karena gender, isolasi, peralatan, atau kebijakan operasional rumah sakit; Ward menangani pertimbangan tersebut secara manual.
- Bila RNA asal mengirim permintaan perpindahan langsung, **Kepala Ruangan** mengarahkan proses ke Admisi Waiting List sesuai SOP-RNA-A05.

## 6. Kriteria Penyelesaian

- **Sistem** menampilkan akomodasi aktif dan waktu penerimaan pasien, atau flag Companion Accommodation yang terkait dengan registrasi pasien.
- Admisi telah menerima hasil Bed Assignment dan menutup Waiting List; tanggung jawab berada pada RNA.
- Bila Ward menolak, alasan terlihat bagi Admisi, Waiting List tetap terbuka di Admisi, dan tidak ada alokasi aktif yang terbentuk.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — Waiting List, penetapan akomodasi, dan Mandatory Bed Assignability.
- `docs/contexts/admisi-ranap/admisi-ranap-domain.md` — Admission dan Waiting List.
- `docs/contexts/bangsal/RNA-ARCHITECTURE.md` — istilah tampilan kerja yang tersedia/ditargetkan.
- `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md` — GAP-RNA-001 dan GAP-RNA-002 CLOSED.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-A05-Transfer-Antar-RNA.md` — Release-to-Waiting-List untuk perpindahan antar-ward.
