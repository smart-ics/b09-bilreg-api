# SOP-RI-D2 — Assign Room & Bed

## 1. Tujuan

Mendokumentasikan proses penetapan **Room** dan **Bed** bagi **Admission** sehingga **Patient** memperoleh lokasi perawatan Rawat Inap sesuai kebutuhan klinis dan ketersediaan fasilitas.

---

## 2. Ruang Lingkup

SOP ini berlaku untuk seluruh proses **Bed Assignment** setelah **Admission** selesai diproses, baik secara langsung apabila **Bed** tersedia maupun setelah **Admission** keluar dari **Waiting List**.

---

## 3. Prasyarat

* **Admission** telah dibuat.
* Status **Admission** masih **Admitted**.
* Belum terdapat **Bed Assignment** yang aktif.
* Tersedia **Bed** yang memenuhi kebutuhan **Patient**.
* Apabila sebelumnya berada pada **Waiting List**, **Admission** telah siap untuk dilakukan **Bed Assignment**.

---

## 4. Aktor

| Aktor          | Peran                                                                          |
| -------------- | ------------------------------------------------------------------------------ |
| Admisi         | Melakukan **Bed Assignment**                                                   |
| Ward / Bangsal | Menerima informasi **Bed Assignment** dan mempersiapkan penerimaan **Patient** |

---

## 5. Partisipan

* **Admission**
* **Bed Assignment**
* **Room**
* **Bed**
* **Waiting List** *(opsional)*

---

## 6. Alur Proses

1. **Admisi** memeriksa ketersediaan **Room** dan **Bed** yang sesuai dengan kebutuhan **Patient**.

2. Apabila **Admission** masih berada pada **Waiting List**, sistem memastikan bahwa **Admission** telah memenuhi syarat untuk dikeluarkan dari **Waiting List**.

3. **Admisi** memilih **Room** dan **Bed** yang akan digunakan oleh **Patient**.

4. Sistem melakukan validasi terhadap ketersediaan **Bed** serta kesesuaian dengan kelas perawatan dan kebutuhan pelayanan.

5. Apabila validasi berhasil, sistem membentuk **Bed Assignment**.

6. Sistem mengubah status **Bed** menjadi **Occupied** atau status operasional yang berlaku.

7. Apabila **Admission** sebelumnya berada pada **Waiting List**, sistem mengeluarkan **Admission** dari **Waiting List**.

8. Sistem meneruskan informasi **Bed Assignment** kepada **Ward** untuk proses penerimaan **Patient**.

9. Proses selesai.

---

## 7. Hasil

* **Bed Assignment** berhasil dibuat.
* **Patient** memperoleh **Room** dan **Bed**.
* Status **Bed** berubah menjadi **Occupied**.
* **Admission** siap memasuki proses pelayanan Rawat Inap di **Ward**.

---

## 8. Aturan Bisnis

**BR-RI-D2-01**

**Bed Assignment** hanya dapat dilakukan untuk **Admission** yang berstatus **Admitted**.

---

**BR-RI-D2-02**

Satu **Admission** hanya dapat memiliki satu **Bed Assignment** yang aktif.

---

**BR-RI-D2-03**

Satu **Bed** hanya dapat digunakan oleh satu **Patient** pada waktu yang sama.

---

**BR-RI-D2-04**

Sistem hanya mengizinkan **Bed Assignment** apabila **Bed** masih tersedia dan memenuhi kebutuhan pelayanan **Patient**.

---

**BR-RI-D2-05**

Apabila **Admission** masih berada pada **Waiting List**, **Bed Assignment** secara otomatis mengakhiri status **Waiting List** tersebut.

---

**BR-RI-D2-06**

Setelah **Bed Assignment** berhasil dilakukan, perubahan **Room** atau **Bed** harus mengikuti SOP-RI-D3 **Change Bed Assignment**.

---

## 9. Post Condition

* **Admission** tetap berada pada status **Admitted**.
* **Bed Assignment** telah terbentuk.
* **Room** dan **Bed** telah dialokasikan kepada **Patient**.
* **Waiting List** *(apabila ada)* telah berakhir.
* **Patient** siap menjalani pelayanan Rawat Inap di **Ward**.
