# SOP-RI-C3 — Update Admission

## 1. Tujuan

Mendokumentasikan proses pembaruan informasi **Admission** yang telah dibuat sebelumnya, agar data administrasi Rawat Inap tetap sesuai dengan kondisi dan kebutuhan **Patient** sebelum dilakukan **Bed Assignment**.

---

## 2. Ruang Lingkup

SOP ini berlaku untuk seluruh proses perubahan informasi **Admission** yang masih berada pada tahap administrasi dan belum dilanjutkan ke proses **Bed Assignment**.

---

## 3. Prasyarat

* **Admission** telah dibuat.
* Status **Admission** masih **Admitted**.
* Belum dilakukan **Bed Assignment**.

---

## 4. Aktor

| Aktor            | Peran                                                                |
| ---------------- | -------------------------------------------------------------------- |
| Admisi           | Memperbarui informasi **Admission**                                  |
| Patient / Family | Menyampaikan perubahan informasi administrasi *(apabila diperlukan)* |

---

## 5. Partisipan

* **Admission**
* **Opname Request** *(opsional)*
* **Reservation** *(opsional)*

---

## 6. Alur Proses

1. **Admisi** menerima permintaan atau kebutuhan untuk memperbarui informasi **Admission**.

2. **Admisi** membuka data **Admission** yang akan diperbarui.

3. **Admisi** melakukan perubahan terhadap informasi administrasi yang diperbolehkan, seperti penjamin, kelas perawatan, dokter penanggung jawab, atau informasi administrasi lainnya sesuai kebijakan rumah sakit.

4. Sistem melakukan validasi terhadap perubahan yang dilakukan.

5. Apabila validasi berhasil, sistem menyimpan perubahan **Admission**.

6. Status **Admission** tetap **Admitted**.

7. Proses selesai.

---

## 7. Hasil

* Informasi **Admission** berhasil diperbarui.
* Status **Admission** tetap **Admitted**.
* **Admission** tetap siap dilanjutkan ke SOP-RI-D1 **Bed Assignment**.

---

## 8. Aturan Bisnis

**BR-RI-C3-01**

**Admission** hanya dapat diperbarui selama belum dilakukan **Bed Assignment**.

---

**BR-RI-C3-02**

Setelah dilakukan **Bed Assignment**, perubahan terhadap informasi **Admission** mengikuti prosedur operasional Rawat Inap yang berlaku.

---

**BR-RI-C3-03**

Perubahan **Admission** tidak mengubah status **Admission**.

---

**BR-RI-C3-04**

Perubahan **Admission** tidak membentuk **Admission** baru.

---

**BR-RI-C3-05**

Perubahan **Admission** tidak secara otomatis mengubah **Opname Request** maupun **Reservation** yang menjadi dasar pembentukan **Admission**.

---

**BR-RI-C3-06**

Perubahan **Admission** tidak melakukan **Bed Assignment** maupun menentukan **Bed**.

---

## 9. Post Condition

* **Admission** tetap berada pada status **Admitted**.
* Informasi **Admission** telah diperbarui sesuai kondisi terbaru.
* **Admission** tetap siap diproses pada SOP-RI-D1 **Bed Assignment**.
* Belum terdapat **Bed Assignment** yang terbentuk.
