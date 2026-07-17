# SOP-RI-A2 — Cancel Opname Request

## 1. Tujuan

Mendokumentasikan proses pembatalan **Opname Request** yang sebelumnya telah dibuat oleh **Doctor**, apabila keputusan Rawat Inap dibatalkan sebelum proses **Admission** dilakukan.

---

## 2. Ruang Lingkup

SOP ini berlaku untuk seluruh proses pembatalan **Opname Request** yang masih menunggu diproses oleh **Admisi**.

---

## 3. Prasyarat

* **Opname Request** telah dibuat.
* Status **Opname Request** masih **Requested**.
* **Admission** belum diproses.
* **Doctor** memutuskan bahwa **Patient** tidak lagi memerlukan Rawat Inap atau **Opname Request** dibuat karena kekeliruan.

---

## 4. Aktor

| Aktor  | Peran                                                |
| ------ | ---------------------------------------------------- |
| Doctor | Membatalkan **Opname Request**                       |
| Admisi | Mengetahui bahwa **Opname Request** telah dibatalkan |

---

## 5. Partisipan

* **Opname Request**
* **Admission Inbox**

---

## 6. Alur Proses

1. **Doctor** melakukan evaluasi ulang terhadap kondisi **Patient**.

2. Apabila **Patient** tidak lagi memerlukan Rawat Inap atau terjadi kesalahan dalam pembuatan permintaan, **Doctor** membatalkan **Opname Request**.

3. Sistem melakukan validasi bahwa **Opname Request** masih dapat dibatalkan.

4. Apabila validasi berhasil, sistem mengubah status **Opname Request** menjadi **Cancelled**.

5. Sistem menghapus **Opname Request** dari **Admission Inbox** sehingga tidak lagi dapat diproses oleh petugas **Admisi**.

6. Proses selesai.

---

## 7. Hasil

* **Opname Request** berhasil dibatalkan.
* Status **Opname Request** menjadi **Cancelled**.
* **Opname Request** tidak lagi tersedia pada **Admission Inbox**.
* Tidak terbentuk **Admission**.

---

## 8. Aturan Bisnis

**BR-RI-A2-01**

Hanya **Doctor** yang dapat membatalkan **Opname Request**.

---

**BR-RI-A2-02**

**Opname Request** hanya dapat dibatalkan apabila statusnya masih **Requested**.

---

**BR-RI-A2-03**

**Opname Request** tidak dapat dibatalkan apabila telah diproses menjadi **Admission**.

---

**BR-RI-A2-04**

Pembatalan **Opname Request** secara otomatis mengeluarkan permintaan tersebut dari **Admission Inbox**.

---

**BR-RI-A2-05**

Pembatalan **Opname Request** tidak memengaruhi **Waiting List** maupun **Bed Assignment**, karena keduanya belum terbentuk pada tahap ini.

---

## 9. Post Condition

* **Opname Request** berada pada status **Cancelled**.
* **Admisi** tidak lagi dapat memproses **Opname Request** tersebut.
* Belum terdapat **Admission**, **Waiting List**, maupun **Bed Assignment**.
