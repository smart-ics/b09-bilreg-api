# SOP-RI-D3 — Release Bed Assignment

## 1. Tujuan

Mendokumentasikan proses pelepasan **Bed Assignment** bagi **Patient** yang masih menjalani Rawat Inap, sehingga **Patient** dapat dipindahkan kembali ke **Waiting List** untuk menunggu **Bed Assignment** berikutnya oleh **Ward** yang akan menerima **Patient**.

---

## 2. Ruang Lingkup

SOP ini berlaku untuk seluruh proses pelepasan **Bed Assignment** akibat perpindahan **Patient** ke unit perawatan lain selama episode Rawat Inap masih berlangsung.

---

## 3. Prasyarat

* **Admission** telah berstatus **Admitted**.
* Terdapat **Bed Assignment** yang masih aktif.
* **Patient** masih menjalani Rawat Inap.
* Terdapat kebutuhan perpindahan **Patient** ke **Ward** lain.

---

## 4. Aktor

| Aktor     | Peran                                                                             |
| --------- | --------------------------------------------------------------------------------- |
| Ward Asal | Mengakhiri **Bed Assignment** dan melepas **Patient** dari **Bed** yang digunakan |

---

## 5. Partisipan

* **Admission**
* **Bed Assignment**
* **Waiting List**
* **Bed**

---

## 6. Alur Proses

1. **Ward Asal** memutuskan bahwa **Patient** perlu dipindahkan ke **Ward** lain.

2. **Ward Asal** mengajukan pelepasan **Bed Assignment**.

3. Sistem melakukan validasi bahwa **Bed Assignment** masih aktif.

4. Apabila validasi berhasil, sistem mengakhiri **Bed Assignment**.

5. Sistem mengubah status **Bed** menjadi **Available**.

6. Sistem membentuk atau mengaktifkan kembali **Waiting List** berdasarkan **Admission**, **Care Class**, dan **Care Level** yang berlaku.

7. Sistem menempatkan **Patient** ke dalam **Waiting List** sehingga dapat diproses oleh **Ward** tujuan melalui SOP-RI-D2 **Assign Room & Bed**.

8. Proses selesai.

---

## 7. Hasil

* **Bed Assignment** telah berakhir.
* **Bed** sebelumnya kembali tersedia.
* **Patient** berada pada **Waiting List**.
* **Patient** siap diproses kembali melalui SOP-RI-D2 **Assign Room & Bed**.

---

## 8. Aturan Bisnis

**BR-RI-D3-01**

**Release Bed Assignment** hanya dapat dilakukan apabila terdapat **Bed Assignment** yang masih aktif.

---

**BR-RI-D3-02**

**Release Bed Assignment** tidak mengubah status **Admission**.

---

**BR-RI-D3-03**

Setelah **Bed Assignment** dilepas, **Patient** tidak lagi menempati **Bed** mana pun.

---

**BR-RI-D3-04**

Pelepasan **Bed Assignment** secara otomatis mengubah status **Bed** menjadi **Available**.

---

**BR-RI-D3-05**

Pelepasan **Bed Assignment** secara otomatis membentuk atau mengaktifkan **Waiting List** untuk **Admission** tersebut.

---

**BR-RI-D3-06**

**Ward Asal** tidak menentukan **Room** maupun **Bed** tujuan.

---

**BR-RI-D3-07**

**Room** dan **Bed** berikutnya hanya dapat ditentukan melalui SOP-RI-D2 **Assign Room & Bed** oleh **Ward** yang menerima **Patient**.

---

**BR-RI-D3-08**

Setiap **Bed Assignment** hanya merepresentasikan satu periode penggunaan **Bed**. Perpindahan **Patient** menghasilkan **Bed Assignment** baru setelah SOP-RI-D2 dijalankan kembali.

---

## 9. Post Condition

* **Admission** tetap berstatus **Admitted**.
* **Bed Assignment** sebelumnya telah berakhir.
* **Bed** sebelumnya berstatus **Available**.
* **Patient** berada pada **Waiting List**.
* Belum terdapat **Bed Assignment** yang aktif hingga SOP-RI-D2 **Assign Room & Bed** dilakukan oleh **Ward** tujuan.
