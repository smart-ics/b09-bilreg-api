# SOP-RI-C1 — Process Admission

## 1. Tujuan

Mendokumentasikan proses administrasi penerimaan **Patient** sebagai pasien Rawat Inap melalui pembentukan **Admission**, sehingga proses Rawat Inap dapat dilanjutkan ke tahap **Bed Assignment**.

---

## 2. Ruang Lingkup

SOP ini berlaku untuk seluruh proses pembentukan **Admission** oleh petugas **Admisi**, baik berdasarkan **Opname Request**, **Reservation**, maupun kombinasi keduanya.

---

## 3. Prasyarat

* **Patient** telah terdaftar pada sistem.
* Salah satu kondisi berikut terpenuhi:

  * Terdapat **Opname Request** dengan status **Requested**, atau
  * Terdapat **Reservation** dengan status **Reserved**, atau
  * Terdapat **Reservation** yang terkait dengan **Opname Request**.
* Belum terdapat **Admission** yang masih aktif untuk proses Rawat Inap yang sama.

---

## 4. Aktor

| Aktor            | Peran                               |
| ---------------- | ----------------------------------- |
| Admisi           | Memproses **Admission**             |
| Patient / Family | Melengkapi persyaratan administrasi |

---

## 5. Partisipan

* **Admission**
* **Opname Request** *(opsional)*
* **Reservation** *(opsional)*

---

## 6. Alur Proses

1. **Admisi** menerima permintaan Rawat Inap.

2. **Admisi** melakukan verifikasi identitas **Patient**, kelengkapan administrasi, serta dokumen pendukung yang diperlukan.

3. Apabila tersedia, **Admisi** memilih **Opname Request** dan/atau **Reservation** yang menjadi dasar proses **Admission**.

4. **Admisi** memilih **Care Class** (kelas perawatan / `KelasDk`).

5. Sistem memvalidasi **Care Class** terhadap master `ta_kelas_dk`.

6. Sistem memuat daftar **Bangsal** yang memenuhi syarat untuk **Care Class** yang dipilih.

7. **Admisi** memilih **Bangsal** tujuan dari daftar yang tersedia.

8. **Admisi** melengkapi data **Admission** lainnya, termasuk informasi penjamin, dokter penanggung jawab, serta informasi administrasi lainnya.

9. Sistem melakukan validasi terhadap data **Admission**.

10. Apabila validasi berhasil, sistem membentuk **Admission** dengan status **Admitted**.

11. Apabila **Opname Request** digunakan, sistem menandainya sebagai telah diproses.

12. Apabila **Reservation** digunakan, sistem menandainya sebagai telah direalisasikan.

13. Proses selesai.

### Ketentuan Khusus — Tidak Ada Bangsal yang Memenuhi Syarat

Apabila tidak terdapat **Bangsal** yang memenuhi syarat untuk **Care Class** yang dipilih, sistem menolak penyimpanan **Admission** dan menampilkan pesan bahwa tidak ada **Bangsal** tersedia. **Admisi** harus memilih **Care Class** lain atau menunggu hingga ketersediaan **Bangsal** memenuhi syarat.

---

## 7. Hasil

* **Admission** berhasil dibuat.
* Status **Admission** menjadi **Admitted**.
* **Opname Request** (apabila ada) telah diproses.
* **Reservation** (apabila ada) telah direalisasikan.
* **Admission** siap dilanjutkan ke SOP-RI-D1 **Bed Assignment**.

---

## 8. Aturan Bisnis

**BR-RI-C1-01**

**Admission** dapat diproses berdasarkan **Opname Request**, **Reservation**, maupun kombinasi keduanya, sesuai kebijakan rumah sakit.

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

Setelah **Admission** terbentuk, proses operasional Rawat Inap dilanjutkan melalui SOP-RI-D1 **Bed Assignment**.

---

**BR-RI-C1-07**

**Care Class** harus dipilih sebelum **Bangsal** tujuan.

---

**BR-RI-C1-08**

**Bangsal** tujuan harus memenuhi syarat berdasarkan **Care Class** yang dipilih.

---

## 9. Post Condition

* **Admission** berada pada status **Admitted**.
* **Patient** telah resmi diterima sebagai pasien Rawat Inap secara administratif.
* **Admission** siap diproses pada SOP-RI-D1 **Bed Assignment**.
* Belum terdapat **Bed Assignment** yang terbentuk.
