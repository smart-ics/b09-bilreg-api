---
Title: Hasil Pemeriksaan Laboratorium Terverifikasi (Termasuk Bridging LIS via Oware)
Code: OC-LAB-HASIL-LAB
Artifact: FEATURE
Version: 1.1
LastUpdated: 2026-11-05
---

# 1. Purpose

Rumah sakit membutuhkan kapabilitas operasional dan klinis pada sistem informasi rumah sakit (HIS / MyHospital Web) untuk mencatat, mengevaluasi parameter analitis, memverifikasi secara medis, memelihara versi amandemen (*full snapshot versioning*), serta merilis dokumen resmi hasil pemeriksaan laboratorium (*validated laboratory result*) sebagai fakta bisnis persisten (*persisted business fact*).

Fungsionalitas ini menjamin:
1. Menjamin mutu analitik spesimen pasien yang telah diambil (`SampleCollection`), dinilai kesesuaiannya terhadap rentang nilai rujukan (*reference range*) berbasis usia dan jenis kelamin, serta diidentifikasi nilai kritisnya (*critical / panic value*).
2. Mendukung masukan data hasil pemeriksaan baik secara manual (*manual entry*) oleh petugas analis laboratorium maupun secara otomatis melalui *bridging* realtime LIS pihak ketiga via middleware Oware ([`a046_oware_ordermiddlewareapi`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi)).
3. Mengamankan keabsahan legal dan medis hasil laboratorium dengan membatasi kewenangan verifikasi entri langsung kepada Dokter Spesialis Patologi Klinik (`ORG-PPA` via klaim role/permission), dan mengadopsi status validasi asal LIS beserta data validatornya untuk hasil yang masuk via bridging LIS.
4. Memisahkan secara tegas antara keabsahan klinis-medis (*clinical visibility* untuk DPJP) dengan penyerahan berkas fisik/resmi ke pasien (*administrative result release*) yang tunduk pada aturan penyelesaian tagihan di Tata Rekening (`TRK-BILLING`).
5. Menyediakan meja kerja terpadu yang adaptif pada `Tindakan Lab` (`LabWorkflowView.vue`) yang memfasilitasi alur kerja tanpa friksi dari order, flebotomi, entri hasil analitis, verifikasi Sp.PK, rilis tagihan, hingga monitoring bridging LIS.

# 2. Business Outcome

Dokumen hasil pemeriksaan laboratorium atas nama pasien telah dicatat dan diverifikasi secara medis oleh Dokter Spesialis Patologi Klinik sebagai fakta bisnis persisten (`Validated Laboratory Result exists`) dan siap digunakan sebagai dasar pertimbangan klinis DPJP serta rilis administratif penyerahan hasil ke pasien.

# 3. Participating Domains

Berdasarkan arsitektur fungsional MyHosWeb dan spesifikasi kanonikal [OC-LAB-HASIL-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB.md):

- **Laboratory (`LAB`)**: Primary Domain. Mengelola perancah komponen hasil (*scaffold*), mencatat nilai parameter uji, mengevaluasi rentang rujukan (*auto-flagging*), mengelola versi amandemen, serta mengeksekusi verifikasi medis dan rilis hasil (`LAB-RESULT`, `LAB-ORDER`, `LAB-COLLECT`).
- **Pasien (`PAS`)**: Contributing Domain. Menyediakan data demografi pasien (`PAS-DATSOS`) seperti jenis kelamin dan tanggal lahir untuk penentuan rentang nilai rujukan yang akurat.
- **Organisasi (`ORG`)**: Contributing Domain. Menyediakan data kredensial Dokter Spesialis Patologi Klinik (`ORG-PPA`) sebagai satu-satunya pihak yang berwenang memverifikasi hasil secara medis pada entri lokal.
- **Tata Rekening (`TRK`)**: Contributing Domain. Menyediakan otoritas validasi kelayakan rilis administratif (*Billing Release Eligibility*) melalui `TRK-BILLING` saat penyerahan dokumen hasil ke pasien dilakukan.
- **Middleware Integrasi (`OWR`)**: Contributing System. Bertindak sebagai jembatan integrasi (*bridging middleware*) yang menerima hasil analitik dari LIS pihak ketiga dan meneruskannya secara realtime ke endpoint ingesti Bilreg.
- **Pelayanan Klinis (`RJL` / `RNA` / `IGD` / `EMR`)**: Contributing Domains. Konsumen hasil terverifikasi untuk evaluasi kondisi klinis pasien.

# 4. Trigger

1. **Alur Manual**: Petugas analis laboratorium menginputkan nilai-nilai parameter pemeriksaan melalui antarmuka HIS setelah analisis laboratorium selesai.
2. **Alur Bridging LIS (Oware)**: Mesin analyzer atau sistem LIS pihak ketiga menyelesaikan pengujian spesimen dan mengirimkan payload data hasil analitik ke middleware Oware, yang selanjutnya memanggil REST API endpoint ingesti Bilreg secara realtime.

# 5. Preconditions

1. Order laboratorium berstatus `Collected` (spesimen biologis telah berhasil diambil dan tidak berstatus ditolak).
2. Order tidak sedang dalam status dibatalkan (`Cancelled`) atau diterminasi (`Terminated`).
3. Komponen-komponen uji terstruktur (*scaffold*) telah terbentuk dari katalog definisi tes pada order (`OC-LAB-TEST`).

# 6. Operational Flow

1. **Perekaman Nilai Analitik (Manual / Bridging Ingestion)**:
   - *Manual*: Analis membuka meja kerja `LabWorkflowView`, memilih antrean `Input Hasil` (order berstatus `Collected`), memasukkan nilai analitik pada baris komponen uji (Numerik, Teks, Opsi, Naratif), dan mencatat hasil (`LabResultRecordCmd`). Status dokumen dan order bertransisi menjadi `Recorded`.
   - *Bridging LIS*: Middleware Oware menerima hasil dari LIS pihak ketiga, memanggil `POST /api/LabContext/LabResultFeature/ingest-lis` di Bilreg, memetakan nilai analitik ke perancah komponen order berbasis kode/nama, menandai `ResultSource = External-LIS`. Karena hasil dari LIS telah tervalidasi di LIS asal, status dokumen hasil langsung tercatat `Verified` dengan validator merujuk ke data verifikator LIS.
2. **Evaluasi Rentang Nilai Rujukan & Auto-Flagging**:
   - Sistem secara otomatis membandingkan nilai analitik terhadap rentang normal sesuai usia dan jenis kelamin pasien:
     - `Normal`
     - `Low (L)`
     - `High (H)`
     - `Critical (Panic Value)`
3. **Peringatan Nilai Kritis (*Critical Panic Value - Soft Warning*)**:
   - Jika terdapat parameter dengan flag `Critical`, sistem menampilkan indikator visual yang mencolok (badge merah terang dan banner peringatan) pada daftar kerja, form hasil, dan lembar PDF.
   - Sistem menyediakan field teks 'CriticalNote' (Catatan Nilai Kritis) yang dapat diisi secara fleksibel untuk mencatat bukti komunikasi telepon ke perawat/DPJP, yang bersifat non-blocking dan tidak memblokir alur rilis atau verifikasi.
4. **Verifikasi Medis oleh Dokter Spesialis Patologi Klinik (Khusus Entri Manual)**:
   - Dokter Sp.PK membuka sub-antrean verifikasi pada meja kerja `LabWorkflowView`.
   - Sistem memvalidasi klaim role pengguna (`doktersppk` atau `lab.result.verify`) dan memeriksa kelengkapan seluruh komponen mandatori (`IsMandatory == true`).
   - Dokter Sp.PK memasukkan catatan ekspertise/kesimpulan klinis (opsional/bila ada) dan menyetujui verifikasi (`LabResultVerifyCmd`).
   - Status dokumen hasil dan status order bertransisi menjadi `Verified`, mengunci dokumen secara permanen (*immutable*).
5. **Amandemen Hasil Terverifikasi (Kondisional / Koreksi)**:
   - Jika terdapat kesalahan teknis atau pengulangan uji setelah status `Verified`, koreksi wajib melalui alur amandemen (`LabResultAmendCmd`).
   - Pengguna wajib memasukkan alasan resmi amandemen.
   - Sistem membuat salinan penuh seluruh komponen hasil (*full snapshot*) menjadi versi baru (`VersionNumber + 1`) berstatus `Recorded` untuk diverifikasi ulang, serta menandai versi sebelumnya sebagai non-aktif (`IsCurrentVersion = false`).
6. **Rilis Administratif ke Pasien & Pengecekan Kelayakan Billing**:
   - Petugas administrasi laboratorium melakukan aksi rilis dokumen hasil ke pasien.
   - Sistem melakukan pengecekan seketika ke domain Tata Rekening (`TRK-BILLING`):
     - Jika status billing `CLEAR`: Status hasil/order bertransisi menjadi `Released` dan dokumen resmi dapat diserahkan/dicetak untuk pasien.
     - Jika status billing `BLOCKED`: Sistem menahan penyerahan hasil resmi (*Blocked*), mencatat alasan penahanan tagihan, namun status medis internal rumah sakit tetap `Verified` sehingga DPJP tetap dapat melihat hasil demi keselamatan pasien.
7. **Pencetakan Dokumen Resmi (PDF)**:
   - Dokumen hasil laboratorium resmi dapat diunduh/dicetak dalam format PDF yang menampilkan identitas pasien, rincian hasil beserta flag, catatan ekspertise, tanda tangan verifikasi Sp.PK/validator LIS, serta nomor versi dokumen terkini.

# 7. Domain Orchestration

1. **Laboratory (`LAB-RESULT`) ↔ Pasien (`PAS-DATSOS`)**:
   - Mengambil data jenis kelamin dan tanggal lahir pasien untuk menentukan interval nilai rujukan normal parameter uji secara akurat.
2. **Laboratory (`LAB-RESULT`) ↔ Organisasi (`ORG-PPA`)**:
   - Memvalidasi bahwa pengguna yang melakukan eksekusi verifikasi medis manual memiliki klaim role/permission Dokter Spesialis Patologi Klinik.
3. **Laboratory (`LAB-RESULT`) ↔ Middleware Integrasi (`OWR`)**:
   - Menghubungkan LIS pihak ketiga dengan HIS melalui pemanggilan endpoint REST internal realtime, memetakan kode uji vendor ke komponen uji internal HIS, dan mencatat validator asal LIS.
4. **Laboratory (`LAB-RESULT`) ↔ Tata Rekening (`TRK-BILLING`)**:
   - Melakukan validasi gerbang tagihan (*Billing Clearance*) saat rilis hasil administratif. Hambatan finansial tidak menghalangi akses klinis internal.
5. **Laboratory (`LAB-RESULT`) ↔ Pelayanan Klinis (`EMR` / Rawat Jalan / Rawat Inap / IGD)**:
   - Menyediakan dokumen hasil terverifikasi secara *real-time* untuk dikonsumsi oleh DPJP dalam resume medis dan rencana terapi pasien.

# 8. Constraints

1. **Prinsip Immutability Dokumen Terverifikasi**:
   - Sekali dokumen hasil berstatus `Verified`, seluruh baris nilai parameter di dalamnya terkunci permanen. Pengguna dilarang melakukan pembaruan langsung (*in-place update*).
2. **Kewajiban Full Snapshot Versioning saat Amandemen**:
   - Perubahan atas hasil yang telah diverifikasi wajib menghasilkan dokumen versi baru dengan salinan penuh seluruh komponen hasil (*full snapshot*), bukan sekadar selisih parsial. Versi terdahulu tetap dipertahankan dengan `IsCurrentVersion = false`.
3. **Pemisahan Validitas Klinis dan Finansial**:
   - Tagihan yang belum lunas tidak membatalkan keabsahan klinis hasil uji. Staf medis internal tetap dapat membaca hasil, tetapi dokumen penyerahan fisik ke pasien diblokir sementara.
4. **Keaslian Nilai Rujukan Historis**:
   - Nilai rujukan yang tercatat pada baris komponen adalah snapshot pada saat pengujian dilakukan dan tidak berubah meskipun master rentang rujukan diubah di kemudian hari.
5. **Eksklusivitas Otoritas Verifikasi Manual**:
   - Verifikasi medis hasil laboratorium lokal hanya sah jika dieksekusi oleh pengguna berklaim Dokter Spesialis Patologi Klinik (`Sp.PK`).

# 9. Exceptions

- **EX-01: Verifikasi oleh Petugas Non-Patolog**: Pengguna tanpa klaim role Sp.PK mencoba memverifikasi hasil lokal. Sistem menolak aksi verifikasi dan menampilkan notifikasi penolakan kewenangan.
- **EX-02: Komponen Pemeriksaan Mandatori Kosong**: Dokter/petugas mencoba memverifikasi hasil lokal padahal satu atau lebih komponen wajib (`IsMandatory == true`) belum memiliki nilai. Sistem menolak verifikasi dan menampilkan daftar parameter yang masih kosong.
- **EX-03: Amandemen Hasil Tanpa Alasan Koreksi**: Pengguna membuat amandemen hasil tanpa memasukkan keterangan alasan revisi. Sistem memblokir pembentukan versi baru dan mewajibkan pengisian alasan amandemen secara jelas.
- **EX-04: Rilis Administratif Terhalang Tagihan (Blocked by Billing)**: Petugas mencoba merilis hasil ke pasien, namun Tata Rekening mengembalikan status `BLOCKED`. Sistem merespons secara informatif bahwa rilis ditahan karena syarat tagihan belum terpenuhi. Status hasil tetap `Verified` dan dapat diakses tim medis internal, namun penyerahan berkas ke pasien ditahan.
- **EX-05: Nilai Hasil Terdeteksi Sebagai Nilai Kritis (Panic Value)**: Nilai analit berada pada rentang kritis. Sistem menampilkan indikator visual merah mencolok (*soft warning*) dan menyediakan form pencatatan pelaporan yang tidak memblokir penanganan pasien.
- **EX-06: Ingesti LIS Oware untuk Order Tidak Valid**: Oware menerima hasil dari LIS tetapi order tidak ditemukan di HIS atau berstatus tidak memenuhi syarat. Sistem menolak ingesti dan Oware melakukan pencatatan kegagalan/retry.

# 10. Acceptance Criteria

- **AC-01**: Sistem berhasil mencatat dokumen hasil laboratorium baru dengan identifier unik `ResultDocumentId`, terikat secara valid pada order dan spesimen yang sah.
- **AC-02**: Seluruh baris parameter uji memuat nilai analitik, satuan, rentang rujukan berbasis usia/gender, serta indikator flag (`Normal`, `Low`, `High`, `Critical`) yang akurat.
- **AC-03**: Sistem mendukung penerimaan realtime dan pemetaan hasil analitik dari LIS pihak ketiga melalui REST API endpoint Bilreg via middleware Oware dengan penandaan sumber `External-LIS` dan adopsi validator LIS.
- **AC-04**: Verifikasi medis hasil lokal berhasil dieksekusi eksklusif oleh Dokter Spesialis Patologi Klinik (`ORG-PPA` via klaim role/permission), mengubah status dokumen menjadi `Verified`, serta mencatat identitas verifikator, stempel waktu, dan catatan ekspertise.
- **AC-05**: Upaya verifikasi lokal oleh staf non-Sp.PK atau saat komponen wajib masih kosong berhasil dicegah dengan pesan validasi yang jelas.
- **AC-06**: Hasil yang telah berstatus `Verified` terkunci permanen terhadap perubahan langsung; koreksi berhasil membuat dokumen versi baru dengan *full snapshot versioning* disertai alasan amandemen.
- **AC-07**: Hasil terverifikasi dengan status tagihan belum lunas (*Blocked by Billing*) tetap dapat diakses oleh dokter internal rumah sakit, namun penyerahan resmi ke pasien ditahan secara tertib.
- **AC-08**: Parameter yang menyentuh ambang nilai kritis berhasil mendapatkan flag `Critical`, menampilkan peringatan visual mencolok, dan menyediakan form audit pelaporan telepon non-blocking.
- **AC-09**: Dokumen hasil laboratorium terverifikasi dapat ditampilkan *on-demand* dalam format PDF resmi yang mencerminkan versi operasional terkini.
- **AC-10**: Antarmuka web frontend menyediakan meja kerja laboratorium terpadu (`LabWorkflowView.vue`) dengan sub-antrean dinamis dan panel detail adaptif yang mendukung seluruh siklus dari order, sampling, hasil analitik, verifikasi Sp.PK, rilis billing, hingga bridging LIS.
