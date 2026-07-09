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

3. **Admisi** melakukan perubahan terhadap informasi administrasi yang diperbolehkan, seperti penjamin, **Care Class** (kelas perawatan / `KelasDk`), dokter penanggung jawab, atau informasi administrasi lainnya sesuai kebijakan rumah sakit.

4. Apabila **Care Class** diubah, sistem memuat ulang daftar **Bangsal** yang memenuhi syarat.

5. Apabila **Bangsal** tujuan yang sudah dipilih tidak lagi memenuhi syarat, sistem mengosongkan pilihan **Bangsal**.

6. **Admisi** harus memilih **Bangsal** baru sebelum perubahan dapat disimpan.

7. Sistem melakukan validasi terhadap perubahan yang dilakukan.

8. Apabila validasi berhasil, sistem menyimpan perubahan **Admission**.

9. Status **Admission** tetap **Admitted**.

10. Proses selesai.

### Ketentuan Khusus — Perubahan Care Class

Apabila tidak terdapat **Bangsal** yang memenuhi syarat untuk **Care Class** yang baru dipilih, sistem menolak penyimpanan perubahan **Admission** dan menampilkan pesan bahwa tidak ada **Bangsal** tersedia. Apabila **Bangsal** tujuan telah dikosongkan karena tidak lagi memenuhi syarat, **Admission** tidak dapat disimpan hingga **Admisi** memilih **Bangsal** baru yang memenuhi syarat.

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

**BR-RI-C3-07**

Perubahan **Care Class** memicu validasi ulang kelayakan **Bangsal** tujuan.

---

**BR-RI-C3-08**

**Admission** tidak dapat disimpan apabila **Bangsal** tujuan kosong atau tidak memenuhi syarat **Care Class** yang berlaku.

---

## 9. Post Condition

* **Admission** tetap berada pada status **Admitted**.
* Informasi **Admission** telah diperbarui sesuai kondisi terbaru.
* **Admission** tetap siap diproses pada SOP-RI-D1 **Bed Assignment**.
* Belum terdapat **Bed Assignment** yang terbentuk.
