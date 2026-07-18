# SOP-RI-C2 — Cancel Admission

## 1. Tujuan

Mendokumentasikan proses pembatalan **Admission** yang telah dibuat sebelumnya, apabila **Patient** batal menjalani Rawat Inap sebelum dilakukan **Bed Assignment**.

---

## 2. Ruang Lingkup

SOP ini berlaku untuk seluruh proses pembatalan **Admission** yang masih berada pada tahap administrasi dan belum dilanjutkan ke proses **Bed Assignment**.

---

## 3. Prasyarat

* **Admission** telah dibuat.
* Status **Admission** masih **Admitted**.
* Belum dilakukan **Bed Assignment**.
* Terdapat alasan yang sah untuk membatalkan proses Rawat Inap, seperti permintaan **Patient**, perubahan keputusan klinis, atau kesalahan administrasi.

---

## 4. Aktor

| Aktor            | Peran                                                              |
| ---------------- | ------------------------------------------------------------------ |
| Admisi           | Membatalkan **Admission**                                          |
| Patient / Family | Menyampaikan permintaan pembatalan *(apabila berasal dari pasien)* |

---

## 5. Partisipan

* **Admission**
* **Opname Request** *(opsional)*
* **Reservation** *(opsional)*

---

## 6. Alur Proses

1. **Admisi** menerima permintaan atau keputusan untuk membatalkan proses Rawat Inap.

2. **Admisi** membuka data **Admission** yang akan dibatalkan.

3. Sistem melakukan validasi bahwa **Admission** masih dapat dibatalkan.

4. Apabila validasi berhasil, **Admisi** mengisi alasan pembatalan.

5. Sistem mengubah status **Admission** menjadi **Cancelled**.

6. Apabila **Reservation** digunakan sebagai dasar **Admission**, sistem mengembalikan status **Reservation** menjadi **Reserved**, sesuai kebijakan rumah sakit.

7. **Opname Request**, apabila ada, tetap dipertahankan sebagai riwayat klinis dan tidak diaktifkan kembali secara otomatis.

8. Proses selesai.

---

## 7. Hasil

* **Admission** berhasil dibatalkan.
* Status **Admission** menjadi **Cancelled**.
* **Reservation** *(apabila ada dan diizinkan oleh kebijakan rumah sakit)* kembali berstatus **Reserved**.
* Belum terdapat **Bed Assignment**.

---

## 8. Aturan Bisnis

**BR-RI-C2-01**

**Admission** hanya dapat dibatalkan apabila belum dilakukan **Bed Assignment**.

---

**BR-RI-C2-02**

Setelah dilakukan **Bed Assignment**, **Admission** tidak dapat dibatalkan dan harus diselesaikan melalui proses operasional Rawat Inap yang berlaku.

---

**BR-RI-C2-03**

Pembatalan **Admission** wajib disertai alasan pembatalan.

---

**BR-RI-C2-04**

Pembatalan **Admission** tidak menghapus data **Admission**, tetapi mengubah statusnya menjadi **Cancelled** untuk keperluan audit.

---

**BR-RI-C2-05**

Pembatalan **Admission** tidak secara otomatis mengubah status **Opname Request**, karena **Opname Request** merupakan keputusan klinis yang menjadi bagian dari riwayat pelayanan.

---

**BR-RI-C2-06**

Apabila **Admission** berasal dari **Reservation**, status **Reservation** dapat dikembalikan menjadi **Reserved** sesuai kebijakan operasional rumah sakit.

---

## 9. Post Condition

* **Admission** berada pada status **Cancelled**.
* Tidak terdapat **Bed Assignment** untuk **Admission** tersebut.
* **Reservation** *(apabila berlaku)* dapat kembali diproses untuk **Admission** di kemudian hari.
* **Opname Request** *(apabila ada)* tetap tersimpan sebagai riwayat klinis.
