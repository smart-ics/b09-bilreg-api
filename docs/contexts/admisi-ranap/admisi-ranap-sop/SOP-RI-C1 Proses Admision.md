# SOP-RI-C1 — Process Admission

## 1. Tujuan

Mendokumentasikan proses administrasi penerimaan **Patient** sebagai pasien Rawat Inap melalui pembentukan **Admission**, sehingga proses Rawat Inap dapat dilanjutkan ke tahap akomodasi (**Waiting List** dan/atau **Bed Assignment** di domain Ward).

---

## 2. Ruang Lingkup

SOP ini berlaku untuk seluruh proses pembentukan **Admission** oleh petugas **Admisi** berdasarkan **satu** sumber proses: **Opname Request** *atau* **Reservation**.

> **Catatan kontrak (Juli 2026):** Endpoint proses menerima tepat satu sumber (`from-opname-request` *atau* `from-reservation`). Opname Request dan Reservation dapat keduanya ada sebagai agregat independen untuk pasien yang sama, tetapi operator memilih **satu** perintah proses. Jalur “kombinasi keduanya dalam satu transaksi proses” **ditunda** (Deferred).

---

## 3. Prasyarat

* **Patient** telah terdaftar pada sistem.
* Salah satu kondisi berikut terpenuhi:

  * Terdapat **Opname Request** dengan status **Requested**, atau
  * Terdapat **Reservation** dengan status yang dapat direalisasikan (**Reserved** / **Maintained** sesuai aturan Maintain Reservation).
* Belum terdapat **Admission** yang masih aktif untuk **Patient** yang sama.

---

## 4. Aktor

| Aktor            | Peran                               |
| ---------------- | ----------------------------------- |
| Admisi           | Memproses **Admission**             |
| Patient / Family | Melengkapi persyaratan administrasi |

---

## 5. Partisipan

* **Admission**
* **Opname Request** *(opsional — jika sumber proses adalah Opname)*
* **Reservation** *(opsional — jika sumber proses adalah Reservation)*
* **Registration** legacy (`ta_registrasi`) — dibuat dengan `RegId` yang sama

---

## 6. Alur Proses

1. **Admisi** menerima permintaan Rawat Inap.

2. **Admisi** melakukan verifikasi identitas **Patient**, kelengkapan administrasi, serta dokumen pendukung yang diperlukan.

3. **Admisi** memilih **satu** sumber proses: **Opname Request** *atau* **Reservation**.

4. **Admisi** memilih **Care Class** (kelas perawatan / `KelasDk`).

5. Sistem memvalidasi **Care Class** terhadap master `ta_kelas_dk`.

6. Sistem memuat daftar **Bangsal** yang memenuhi syarat untuk **Care Class** yang dipilih.

7. **Admisi** memilih **Bangsal** tujuan dari daftar yang tersedia.

8. **Admisi** melengkapi data registrasi yang wajib dikirim bersama **Admission**:
   * Tipe Jaminan dan Peserta Jaminan
   * Cara Masuk *(klasifikasi masuk untuk pelaporan / `ta_registrasi`)*
   * **Prosedur Masuk Inap** *(prosedur operasional masuk inap; wajib; terpisah dari Cara Masuk)*
   * Rujukan *(wajib bila Cara Masuk memerlukan rujukan)*
   * Dokter

9. Sistem melakukan validasi terhadap data **Admission** dan data registrasi, termasuk:
   * memastikan **Prosedur Masuk Inap** terisi dan valid terhadap master `ta_caramasuk_inap`;
   * menurunkan **Layanan Rawat Inap** dari **Bangsal** tujuan (`ta_bangsal.fs_kd_layanan`) — operator tidak memilih Layanan secara manual;
   * menolak proses bila Bangsal tidak memiliki mapping Layanan yang valid / instalasi Rawat Inap;
   * tidak meminta atau menetapkan **Karcis** untuk Rawat Inap (disimpan sebagai sentinel kosong `-`).

10. Apabila validasi berhasil, sistem membentuk **Admission** dengan status **Admitted** dan membuat **Registration** legacy dengan `RegId` yang sama dalam satu transaksi.

11. Apabila sumber proses adalah **Opname Request**, sistem menandainya sebagai telah diproses (**Fulfilled**).

12. Apabila sumber proses adalah **Reservation**, sistem menandainya sebagai telah direalisasikan (**Realized**).

13. Proses selesai.

### Ketentuan Khusus — Tidak Ada Bangsal yang Memenuhi Syarat

Apabila tidak terdapat **Bangsal** yang memenuhi syarat untuk **Care Class** yang dipilih, sistem menolak penyimpanan **Admission** dan menampilkan pesan bahwa tidak ada **Bangsal** tersedia. **Admisi** harus memilih **Care Class** lain atau menunggu hingga ketersediaan **Bangsal** memenuhi syarat.

---

## 7. Hasil

* **Admission** berhasil dibuat.
* Status **Admission** menjadi **Admitted**.
* **Registration** legacy (`ta_registrasi`) dibuat dengan `RegId` yang sama.
* **Opname Request** (apabila menjadi sumber proses) telah diproses.
* **Reservation** (apabila menjadi sumber proses) telah direalisasikan.
* **Admission** siap dilanjutkan ke:
  * SOP-RI-D1 **Waiting List Management** (apabila akomodasi belum tersedia), dan/atau
  * hand-off ke modul Ward untuk SOP-RI-D2 **Assign Room & Bed**.
* Workspace Admisi **tidak** menampilkan UI alokasi Room/Bed; hanya status dan tautan hand-off (ADR-002).

---

## 8. Aturan Bisnis

**BR-RI-C1-01**

**Admission** diproses berdasarkan **tepat satu** sumber: **Opname Request** *atau* **Reservation**. Opname dan Reservation independen dapat keduanya ada sebelum proses; kombinasi keduanya dalam satu perintah proses **tidak didukung** pada implementasi saat ini (Deferred).

---

**BR-RI-C1-02**

Satu **Admission** hanya berlaku untuk satu episode Rawat Inap.

---

**BR-RI-C1-03**

Satu **Opname Request** hanya dapat digunakan untuk membentuk satu **Admission**.

---

**BR-RI-C1-04**

Satu **Reservation** hanya dapat direalisasikan menjadi satu **Admission**.

---

**BR-RI-C1-05**

Pembentukan **Admission** tidak secara otomatis melakukan **Bed Assignment**.

---

**BR-RI-C1-06**

Setelah **Admission** terbentuk, proses akomodasi dilanjutkan melalui **Waiting List** (SOP-RI-D1) dan/atau **Bed Assignment** di domain Ward (SOP-RI-D2). Modul Admisi hanya melakukan hand-off, bukan alokasi bed.

---

**BR-RI-C1-07**

**Care Class** harus dipilih sebelum **Bangsal** tujuan.

---

**BR-RI-C1-08**

**Bangsal** tujuan harus memenuhi syarat berdasarkan **Care Class** yang dipilih.

---

**BR-RI-C1-09**

**Layanan Rawat Inap** diturunkan dari **Bangsal** tujuan (satu Bangsal → tepat satu Layanan). Operator tidak memilih Layanan secara manual. Proses ditolak bila mapping Layanan tidak ada atau bukan instalasi Rawat Inap.

---

**BR-RI-C1-10**

**Karcis** tidak berlaku untuk Rawat Inap pada proses Admission. Sistem tidak meminta, menginferensi, atau menetapkan karcis tagihan; nilai yang disimpan adalah sentinel kosong.

---

## 9. Post Condition

* **Admission** berada pada status **Admitted**.
* **Patient** telah resmi diterima sebagai pasien Rawat Inap secara administratif.
* **Admission** siap diproses pada SOP-RI-D1 (**Waiting List**) dan/atau hand-off ke SOP-RI-D2 (**Bed Assignment**, Ward).
* Belum terdapat **Bed Assignment** yang terbentuk di dalam workspace Admisi.
