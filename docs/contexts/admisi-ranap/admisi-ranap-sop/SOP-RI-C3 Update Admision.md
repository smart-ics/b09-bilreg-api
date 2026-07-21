# SOP-RI-C3 — Update Admission

## 1. Tujuan

Mendokumentasikan proses pembaruan informasi **Admission** yang telah dibuat sebelumnya, agar data perencanaan akomodasi Rawat Inap tetap sesuai dengan kondisi dan kebutuhan **Patient** sebelum dilakukan **Bed Assignment**.

---

## 2. Ruang Lingkup

SOP ini berlaku untuk perubahan **Care Class** dan **Bangsal** tujuan pada **Admission** yang masih aktif secara administratif.

> **Catatan kontrak (Juli 2026):** Perintah yang diimplementasikan adalah `PUT api/admisi-ranap/admission/{regId}` dengan body `kelasDkId`, `bangsalId`, `userId` saja. Perubahan penjamin, dokter, rujukan, atau field registrasi episode lainnya **belum** tersedia sebagai perintah inpatient tersertifikasi dan **ditunda** (Deferred) — lihat capability matrix persistent workspace. Aturan bisnis BR-RI-C3-01 (hanya sebelum Bed Assignment) belum ditegakkan di handler; penegakan Deferred.

---

## 3. Prasyarat

* **Admission** telah dibuat.
* Status **Admission** masih aktif (bukan **Completed** / **Cancelled**).
* Belum dilakukan **Bed Assignment** *(aturan bisnis; penegakan teknis Deferred)*.

---

## 4. Aktor

| Aktor            | Peran                                                                |
| ---------------- | -------------------------------------------------------------------- |
| Admisi           | Memperbarui **Care Class** dan **Bangsal** pada **Admission**        |
| Patient / Family | Menyampaikan perubahan kebutuhan akomodasi *(apabila diperlukan)*    |

---

## 5. Partisipan

* **Admission**
* **Opname Request** *(opsional — tidak diubah oleh update)*
* **Reservation** *(opsional — tidak diubah oleh update)*

---

## 6. Alur Proses

1. **Admisi** menerima permintaan atau kebutuhan untuk memperbarui perencanaan akomodasi **Admission**.

2. **Admisi** membuka data **Admission** yang akan diperbarui.

3. **Admisi** mengubah **Care Class** (`KelasDk`) dan/atau **Bangsal** tujuan.

4. Apabila **Care Class** diubah, sistem memuat ulang daftar **Bangsal** yang memenuhi syarat.

5. Apabila **Bangsal** tujuan yang sudah dipilih tidak lagi memenuhi syarat, sistem mengosongkan pilihan **Bangsal**.

6. **Admisi** harus memilih **Bangsal** baru sebelum perubahan dapat disimpan.

7. Sistem melakukan validasi terhadap perubahan yang dilakukan.

8. Apabila validasi berhasil, sistem menyimpan perubahan **Admission** (Care Class + Bangsal secara bersama). Status dapat menjadi **Updated** sesuai state machine.

9. Perubahan ini **tidak** menyinkronkan penempatan ke field legacy `RegModel` yang dipersistensi di `ta_registrasi` (Admission adalah source of truth untuk Care Class / Bangsal).

10. Proses selesai.

### Ketentuan Khusus — Perubahan Care Class

Apabila tidak terdapat **Bangsal** yang memenuhi syarat untuk **Care Class** yang baru dipilih, sistem menolak penyimpanan perubahan **Admission** dan menampilkan pesan bahwa tidak ada **Bangsal** tersedia. Apabila **Bangsal** tujuan telah dikosongkan karena tidak lagi memenuhi syarat, **Admission** tidak dapat disimpan hingga **Admisi** memilih **Bangsal** baru yang memenuhi syarat.

### Di luar ruang lingkup perintah ini

* Perubahan penjamin / peserta jaminan episode
* Perubahan dokter / rujukan / cara masuk / layanan / karcis
* Perubahan data Patient / guardian
* Alokasi Room / Bed

Field-field tersebut tetap dapat ditampilkan read-only di workspace hingga perintah pemilik agregat yang sesuai berstatus Ready.

---

## 7. Hasil

* Informasi perencanaan **Admission** (Care Class + Bangsal) berhasil diperbarui.
* **Admission** tetap siap dilanjutkan ke Waiting List / hand-off Bed Assignment (Ward).
* Tidak terbentuk **Admission** baru.

---

## 8. Aturan Bisnis

**BR-RI-C3-01**

**Admission** hanya dapat diperbarui selama belum dilakukan **Bed Assignment** *(aturan bisnis; penegakan teknis Deferred)*.

---

**BR-RI-C3-02**

Setelah dilakukan **Bed Assignment**, perubahan terhadap informasi **Admission** mengikuti prosedur operasional Rawat Inap yang berlaku.

---

**BR-RI-C3-03**

Perubahan **Admission** melalui perintah update penempatan tidak membentuk **Admission** baru.

---

**BR-RI-C3-04**

Perubahan **Admission** tidak secara otomatis mengubah **Opname Request** maupun **Reservation** yang menjadi dasar pembentukan **Admission**.

---

**BR-RI-C3-05**

Perubahan **Admission** tidak melakukan **Bed Assignment** maupun menentukan **Bed**.

---

**BR-RI-C3-06**

Perubahan **Care Class** memicu validasi ulang kelayakan **Bangsal** tujuan.

---

**BR-RI-C3-07**

**Admission** tidak dapat disimpan apabila **Bangsal** tujuan kosong atau tidak memenuhi syarat **Care Class** yang berlaku.

---

**BR-RI-C3-08**

Care Class dan Bangsal diperbarui secara **bersama** (coupled); tidak ada update mandiri salah satu tanpa validasi pasangan yang memenuhi syarat.

---

## 9. Post Condition

* Informasi Care Class / Bangsal **Admission** telah diperbarui sesuai kondisi terbaru.
* **Admission** tetap siap diproses pada SOP-RI-D1 (**Waiting List**) dan/atau hand-off ke SOP-RI-D2 (**Bed Assignment**, Ward).
* Belum terdapat UI Bed Assignment di dalam workspace Admisi.
