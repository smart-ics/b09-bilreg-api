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

4. **Admisi** melengkapi data **Admission**, termasuk informasi penjamin, kelas perawatan, dokter penanggung jawab, serta informasi administrasi lainnya.

5. Sistem melakukan validasi terhadap data **Admission**.

6. Apabila validasi berhasil, sistem membentuk **Admission** dengan status **Admitted**.

7. Apabila **Opname Request** digunakan, sistem menandainya sebagai telah diproses.

8. Apabila **Reservation** digunakan, sistem menandainya sebagai telah direalisasikan.

9. Proses selesai.

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

## 9. Post Condition

* **Admission** berada pada status **Admitted**.
* **Patient** telah resmi diterima sebagai pasien Rawat Inap secara administratif.
* **Admission** siap diproses pada SOP-RI-D1 **Bed Assignment**.
* Belum terdapat **Bed Assignment** yang terbentuk.
