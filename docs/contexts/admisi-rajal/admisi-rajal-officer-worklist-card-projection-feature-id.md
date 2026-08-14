# Proyeksi Data Kartu Worklist Petugas Admisi Rajal

## 1. Tujuan dokumen

Dokumen ini menjelaskan rancangan fitur Proyeksi Data Kartu Worklist Petugas pada Admisi Rawat
Jalan. Sasaran utama dokumen adalah programmer manusia yang akan mengembangkan backend maupun
frontend.

Versi bahasa Inggris yang dioptimalkan untuk konteks AI Agent tersedia di:
[`docs/contexts/admisi-rajal/admisi-rajal-officer-worklist-card-projection-feature.md`](admisi-rajal-officer-worklist-card-projection-feature.md).

Fitur ini bertujuan menghasilkan satu bentuk data kartu yang:

- mudah ditampilkan pada daftar antrean;
- memiliki aturan prioritas sumber data yang jelas;
- menjadi sumber yang sama untuk tampilan, sorting, dan filter;
- tetap benar ketika data dipaging;
- tidak mengubah kepemilikan data Queue, Booking, atau Registration.

## 2. Latar belakang

Worklist petugas saat ini menerima data dalam beberapa bagian:

- `queue`;
- `identity`;
- `booking`;
- `registration`.

Komponen frontend kemudian memilih sendiri nilai yang akan ditampilkan. Cara ini cukup untuk
menampilkan kartu sederhana, tetapi menimbulkan masalah saat fitur berkembang:

1. prioritas pemilihan data tidak konsisten;
2. nilai yang tampil dapat berbeda dengan nilai yang dipakai untuk sorting atau filter;
3. Queue sudah dipaging sebelum data Booking dan Registration dilengkapi;
4. sorting/filter di frontend hanya berlaku pada halaman yang sudah dimuat;
5. jumlah total hasil dapat salah setelah filter frontend diterapkan.

Solusinya adalah menambahkan objek `card` hasil resolusi backend. Objek ini berisi nilai efektif
yang sudah dipilih dari sumber yang benar.

## 3. Data yang dibutuhkan kartu

Data kartu yang diusulkan:

1. Queue Number;
2. Patient Name;
3. Pasien ID;
4. Booking ID;
5. Registration ID;
6. Service Point;
7. Source;
8. Arrival Time;
9. Layanan;
10. Dokter;
11. Jam Praktek.

Semua data tersebut boleh tersedia untuk sorting dan filter walaupun tidak selalu terlihat pada
setiap ukuran kartu. Misalnya, tampilan kartu kecil dapat menyembunyikan Dokter atau Jam Praktek,
tetapi backend tetap boleh mengirimkannya.

## 4. Prinsip utama pemilihan sumber

Aturan umum “Registration lebih tinggi daripada Booking, lalu Queue” benar untuk beberapa data,
tetapi tidak boleh diterapkan secara membabi buta.

Alasannya:

- nomor antrean Booking bisa berbeda makna dengan nomor Admission Queue;
- waktu kedatangan adalah fakta Queue, bukan waktu Booking atau Registration;
- Service Point adalah jalur antrean yang dipilih saat intake;
- Registration saat ini tidak menyimpan Jam Praktek;
- Source menjelaskan asal masuk antrean, bukan jenis data terakhir yang tersedia.

Karena itu, prioritas harus ditentukan per field.

## 5. Matriks sumber data efektif

| Field kartu | Sumber dan prioritas | Penjelasan |
|---|---|---|
| `queueNumber` | Queue `QueueLabel`, fallback Queue `NoUrut` | Jangan memakai `Booking.NoAntrian` karena dapat berasal dari sequence yang berbeda. |
| `patientName` | Registration -> Booking -> Queue/tracker | Nama dari Registration dianggap konteks pasien final setelah registrasi tersedia. |
| `pasienId` | Registration `PasienId` -> Booking `PasienId` -> referensi pasien dari Tracker jika tersedia -> kosong | `PasienTrackerId` bukan `PasienId` dan tidak boleh dikembalikan sebagai Pasien ID. |
| `bookingId` | Bukti linkage Queue/Booking Assistance -> Booking `BookingId` -> kosong | ID tetap boleh tersedia walaupun detail Booking gagal dimuat. |
| `regId` | Registration Outcome/referensi Registration pada Queue -> Registration `RegId` -> kosong | ID tetap boleh tersedia walaupun detail Registration gagal dimuat. |
| `servicePoint` | Queue saja | Service Point adalah jalur Admission Queue yang dipilih ketika nomor antrean dibuat. |
| `source` | Bukti intake yang persisten -> `Unknown` | Jangan hanya memeriksa apakah objek Booking tersedia. |
| `arrivalTime` | Queue `CreatedAt` saja | Menunjukkan waktu pasien masuk Admission Queue. |
| `layanan` | Registration -> Booking -> kosong | Registration menang jika tujuan layanan berubah saat registrasi. |
| `dokter` | Registration -> Booking -> kosong | Registration menang jika dokter final berbeda dengan rencana Booking. |
| `jamPraktek` | Booking -> kosong | Registration belum memiliki field ekuivalen yang otoritatif. |

### Contoh Layanan

```text
Booking.Layanan     = Poli Penyakit Dalam
Registration.Layanan = Poli Jantung

Nilai efektif kartu = Poli Jantung
```

Sorting dan filter juga harus menggunakan “Poli Jantung”, bukan nilai Booking.

### Contoh data belum tersedia

```text
Registration belum ada
Booking ada

Patient Name = Booking.PersonName
Layanan      = Booking.Layanan
Dokter       = Booking.Dokter
Jam Praktek  = Booking.JamPraktek
```

### Contoh Walk-In anonim

```text
Registration belum ada
Booking tidak ada
Queue belum memiliki identitas pasien

Patient Name = null
Layanan      = null
Dokter       = null
Jam Praktek  = null
```

Record tetap harus muncul. Nilai kosong tidak boleh menyebabkan Queue Entry hilang dari worklist.

## 6. Identitas Pasien, Booking, dan Registration

Ketiga identifier berikut mempunyai arti yang berbeda:

| Identifier | Arti |
|---|---|
| `pasienId` | Identitas pasien atau nomor master record rumah sakit. |
| `bookingId` | Identitas aggregate Booking yang terhubung dengan Queue Entry. |
| `regId` | Identitas aggregate Registration yang terhubung dengan Queue Entry. |

Ketiganya tidak boleh saling menggantikan.

Contoh:

```text
Booking ID diketahui dari linkage Queue
Detail Booking sementara gagal dimuat

card.bookingId = ID Booking yang diketahui
booking        = null
```

Aturan yang sama berlaku untuk `regId` apabila Registration Outcome sudah menyimpan identitas
Registration, tetapi detail Registration gagal dibaca.

Prioritas `pasienId` adalah:

```text
Registration.PasienId
-> Booking.PasienId
-> referensi pasien dari Tracker jika benar-benar tersedia
-> null
```

`PasienTrackerId` adalah identitas journey/tracker, bukan Patient Master ID. Nilai tersebut tidak
boleh dimasukkan ke `card.pasienId`.

## 7. Nomor antrean harus tetap milik Queue

`queueNumber` pada kartu adalah nomor Admission Queue:

```text
Queue.QueueLabel -> Queue.NoUrut
```

Contoh:

```text
Queue.QueueLabel = A0042
Queue.NoUrut     = 42
Booking.NoAntrian = 7
```

Nomor yang ditampilkan tetap `A0042`. `Booking.NoAntrian` dapat merupakan nomor antrean dokter atau
sequence lain, sehingga tidak boleh dijadikan fallback.

Identitas teknis record tetap:

```text
AntrianId + NoUrut
```

Jangan memakai Queue Label, Patient ID, Booking ID, atau Registration ID sebagai satu-satunya key
Vue maupun selection.

## 8. Service Point dan UMUM/BPJS

Istilah ini perlu dipastikan sebelum implementasi.

### Jika UMUM/BPJS adalah loket atau jalur antrean

Gunakan Service Point dari Queue:

```text
Queue.ServicePointId
Queue.ServicePointName
```

Nilai ini merupakan snapshot jalur antrean ketika pasien mengambil nomor.

### Jika UMUM/BPJS adalah kategori penjamin

Jangan memasukkannya ke field `servicePoint`. Buat field lain, misalnya:

```ts
type PayerClass = 'UMUM' | 'BPJS' | 'OTHER' | 'UNKNOWN'
```

Prioritas yang masuk akal:

```text
Registration.TipeJaminan
-> Booking.AsuransiName/Coverage
-> Unknown
```

Service Point dan penjamin dapat kebetulan memiliki label yang sama, tetapi makna bisnisnya
berbeda. Menyatukannya akan menghasilkan filter yang sulit dipercaya ketika pasien mengubah
jaminan saat registrasi.

## 9. Source harus eksplisit

Nilai Source:

```text
Booking
WalkIn
Unknown
```

Source menjelaskan bagaimana Queue Entry masuk ke alur Admisi.

Saat ini frontend menggunakan logika:

```ts
item.booking ? 'Booking' : 'Walk-In'
```

Logika tersebut belum cukup kuat. Objek Booking bisa kosong karena:

- proses enrichment gagal;
- referensi historis belum terbaca;
- data lama tidak mempunyai linkage lengkap;
- Booking sudah tidak aktif setelah Registration selesai.

Kondisi tersebut tidak otomatis berarti Walk-In.

Backend sebaiknya menentukan Source dari bukti yang tahan lama, misalnya:

- discriminator intake yang disimpan secara eksplisit;
- referensi Queue Entry;
- record Booking Assistance berdasarkan `AntrianId + NoUrut`;
- bukti intake lain yang telah disetujui.

Data yang ambigu harus menghasilkan `Unknown`. Lebih aman menampilkan “Tidak diketahui” daripada
salah menandai pasien Booking sebagai Walk-In.

Source juga tidak boleh berubah menjadi jenis lain hanya karena Registration sudah terbentuk.

## 10. Bentuk response yang disarankan

Objek lama tetap dipertahankan, lalu ditambahkan `card`:

```json
{
  "queue": {},
  "identity": {},
  "booking": {},
  "registration": {},
  "card": {
    "queueNumber": {
      "label": "A0042",
      "sequence": 42
    },
    "patientName": "SITI AMINAH",
    "pasienId": "P000123",
    "bookingId": "BKG-01JXYZ",
    "regId": "REG-20260730-001",
    "servicePoint": {
      "id": "ADM-BPJS",
      "name": "BPJS"
    },
    "source": "Booking",
    "arrivalTime": "2026-07-30T08:14:30",
    "layanan": {
      "id": "LYN-001",
      "name": "POLI PENYAKIT DALAM"
    },
    "dokter": {
      "id": "DR-001",
      "name": "dr. Budi"
    },
    "jamPraktek": "08:00"
  }
}
```

Alasan mempertahankan objek mentah:

- panel registrasi masih membutuhkan Booking dan Registration detail;
- action rules masih membutuhkan state Queue;
- migrasi frontend dapat dilakukan bertahap;
- kontrak API lama tidak langsung rusak.

Objek `card` menjadi satu-satunya sumber untuk sebelas field tampilan, sorting, dan filter.

## 11. Penanganan referensi

`layanan`, `dokter`, dan `servicePoint` terdiri dari ID dan nama. Keduanya harus dipilih sebagai
satu paket.

Jangan melakukan hal berikut:

```text
LayananId dari Registration
LayananName dari Booking
```

Jika referensi Registration tidak valid, anggap seluruh referensi kosong, lalu fallback ke
referensi Booking.

Nilai berikut dianggap kosong:

- `null`;
- string kosong;
- hanya whitespace;
- sentinel seperti `"-"` jika memang dipakai oleh kontrak sumber;
- tanggal sentinel;
- ID placeholder yang dipasangkan dengan nama placeholder.

## 12. Mengapa sorting/filter frontend saja tidak cukup

Alur backend saat ini kurang lebih:

```text
filter Queue
-> urutkan Queue
-> OFFSET/FETCH
-> ambil satu halaman
-> lengkapi Booking dan Registration per item
```

Misalnya terdapat 300 antrean, tetapi frontend baru memuat 100. Jika frontend mengurutkan
berdasarkan Patient Name, yang terurut hanya 100 record tersebut. Pasien bernama “Abdul” pada
record ke-250 tidak akan pindah ke halaman pertama.

Masalah yang sama terjadi pada filter:

```text
backend mengembalikan 100 record
frontend menyisakan 8 record setelah filter Dokter
```

Padahal mungkin ada 70 record lain yang cocok pada halaman berikutnya. `totalCount` juga menjadi
salah.

Karena itu urutan yang benar adalah:

```text
tentukan kandidat berdasarkan tanggal/status
-> resolve nilai efektif
-> filter nilai efektif
-> hitung total
-> sorting nilai efektif
-> paging
-> response
```

## 13. Rancangan sorting

### Sort key

```text
queueOrder
queueNumber
patientName
pasienId
bookingId
regId
servicePoint
source
arrivalTime
layanan
dokter
jamPraktek
```

### Default sorting

Default tetap `queueOrder`:

```text
Priority DESC
ArrivalTime ASC
QueueNumber.Sequence ASC
AntrianId ASC
```

Walaupun Priority tidak wajib terlihat sebagai field utama kartu, Priority tetap harus dihormati.
Jika default diubah menjadi Queue Number atau Arrival Time saja, pasien prioritas dapat turun ke
bawah antrean.

### Tie-breaker

Setiap sorting pilihan pengguna harus ditambah urutan stabil:

```text
field pilihan pengguna
Priority DESC
ArrivalTime ASC
QueueNumber.Sequence ASC
AntrianId ASC
```

Hal ini mencegah record berpindah secara acak ketika beberapa pasien memiliki nama, dokter, atau
jam praktek yang sama.

### Nilai kosong

- nilai berisi ditempatkan sebelum nilai kosong;
- nilai kosong tetap paling bawah untuk arah ascending maupun descending;
- string dibandingkan secara case-insensitive setelah trim;
- ID stabil digunakan sebagai tie-breaker jika nama sama.

Backend harus memakai allow-list atau enum untuk `sortBy` dan `sortDirection`. Jangan menerima nama
kolom SQL bebas dari request.

## 14. Rancangan filter

| Filter | Perilaku |
|---|---|
| Queue Number | Exact atau prefix pada label; numeric exact pada sequence jika dibutuhkan. |
| Patient Name | Contains, case-insensitive. |
| Pasien ID | Exact secara default; prefix hanya jika disediakan sebagai pencarian identifier khusus. |
| Booking ID | Exact secara default; prefix hanya jika disediakan sebagai pencarian identifier khusus. |
| Registration ID | Exact secara default; prefix hanya jika disediakan sebagai pencarian identifier khusus. |
| Service Point | Pilihan satu atau beberapa ID. |
| Source | Booking, WalkIn, Unknown. |
| Arrival Time | Rentang waktu kedatangan. |
| Layanan | Pilihan satu atau beberapa Layanan ID. |
| Dokter | Pilihan satu atau beberapa Dokter ID. |
| Jam Praktek | Rentang jam atau kelompok sesi. |

Filter harus memakai nilai efektif.

Contoh:

```text
Booking.Dokter     = dr. Andi
Registration.Dokter = dr. Budi

Filter dr. Budi => record masuk
Filter dr. Andi => record tidak masuk
```

Untuk field nullable, UI dapat menyediakan pilihan “Belum diketahui” jika berguna bagi petugas.

`totalCount`, `hasMore`, dan `nextOffset` harus dihitung dari hasil setelah filter efektif.

## 15. Batas arsitektur

`IAdmissionQueueOperationalProjection` harus tetap hanya berisi data milik Queue.

Batas ini penting karena:

- Patient Tracker memegang kebenaran operasional Queue;
- Booking dan Registration dimiliki konteks Admisi;
- read composition tidak boleh mengubah kepemilikan aggregate;
- Queue projection juga dipakai konsumen lain.

Sebaiknya dibuat projection Admisi khusus, misalnya:

```text
IAdmisiRajalOfficerWorklistProjection
```

Tanggung jawabnya:

```text
Queue
+ identity/tracker
+ Booking
+ Registration
+ resolusi field efektif
+ filter
+ sorting
+ count/paging
```

Nama interface dapat disesuaikan dengan konvensi repository, tetapi tanggung jawab tersebut tetap
berada di Admisi Rajal.

## 16. Strategi query yang disarankan

### Pilihan utama

Gunakan dedicated SQL read projection atau query set-based yang:

1. mengambil Queue Entry sesuai tanggal dan status;
2. menemukan referensi Registration dan Booking;
3. menghitung nilai efektif;
4. menerapkan filter;
5. menghitung total;
6. menerapkan stable sorting;
7. melakukan `OFFSET/FETCH`.

Pendekatan ini paling cocok untuk polling dan paging.

### Pilihan alternatif

Batch-load semua data berdasarkan kumpulan Queue identity, lalu resolve di memory. Pendekatan ini
hanya layak jika:

- jumlah kandidat dibatasi dengan jelas;
- tidak ada query repository per record;
- pengujian volume membuktikan performanya cukup;
- count dan paging tetap benar.

### Pendekatan yang harus dihindari

- memanggil Tracker/Booking/Registration repository satu per satu untuk seluruh kandidat;
- sorting hanya di `VirtualWorklistGrid.vue`;
- filter setelah halaman backend diterima;
- memperbesar limit agar seolah-olah semua data sudah dimuat;
- membuat tabel worklist mutable kedua yang menduplikasi state Queue.

## 17. Pertimbangan performa

Worklist dipolling secara berkala. Query lambat akan dieksekusi berulang kali oleh setiap workstation.

Hal yang perlu diperiksa:

- execution plan untuk default `queueOrder`;
- execution plan filter Patient Name, Layanan, Dokter, dan Jam Praktek;
- query count menggunakan kondisi filter yang sama;
- tidak ada pola N+1;
- scope Business Date diterapkan sedini mungkin;
- page limit maksimum tetap aman;
- pencarian `contains` Patient Name tidak dilakukan pada histori tanpa batas;
- indeks ditambahkan berdasarkan bukti execution plan, bukan dugaan.

Jika salah satu sumber enrichment gagal:

- Queue Entry tetap dikembalikan;
- field opsional menjadi `null`;
- diagnostic log tidak boleh membocorkan data pasien sensitif.

## 18. Perilaku frontend

### Data yang ditampilkan

Saran kepadatan:

- selalu terlihat: Queue Number dan Patient Name;
- metadata ringkas: Service Point, Source, Arrival Time;
- baris konteks jika tersedia: Layanan, Dokter, Jam Praktek.
- identifier saat dibutuhkan: Pasien ID, Booking ID, dan Registration ID; identifier boleh
  disembunyikan pada kartu ringkas dan ditampilkan pada mode list, detail, atau hasil pencarian.

Pada mode kartu kecil, sebagian metadata boleh disembunyikan. Pada mode list yang lebih lebar,
informasi dapat ditampilkan lebih lengkap.

### Aturan frontend

- gunakan objek `card` untuk sebelas field;
- jangan mengulang fallback Registration/Booking/Queue di banyak komponen;
- ubah sort/filter melalui query backend;
- saat sort/filter berubah, reset ke halaman pertama;
- virtual list kembali ke baris pertama;
- pertahankan selection menggunakan `AntrianId + NoUrut`;
- bedakan empty state “antrean kosong” dan “tidak ada hasil filter”;
- tampilkan filter aktif agar petugas mengetahui daftar sedang dibatasi.

### Data dapat berubah ketika polling

Saat Registration baru terbentuk, Layanan atau Dokter efektif dapat berubah dari nilai Booking ke
nilai Registration. Jika daftar sedang diurutkan berdasarkan field tersebut, posisi kartu dapat
berpindah setelah refresh.

Perpindahan ini valid, tetapi selection harus tetap menempel pada Queue Entry yang sama.

## 19. Kompatibilitas API

Perubahan awal harus bersifat additive:

```text
response lama + card baru
```

Tahapan migrasi:

1. backend mulai mengirim `card`;
2. frontend menambahkan schema nullable untuk `card`;
3. komponen kartu menggunakan `card`;
4. sort/filter dipindahkan ke backend;
5. fallback lama frontend dihapus setelah rollout stabil.

Jangan langsung menghapus `identity`, `booking`, atau `registration`, karena masih dipakai oleh
preview dan proses registrasi.

## 20. Skenario pengujian penting

### Resolusi field

1. Nama Registration mengalahkan Booking dan Queue.
2. Nama Booking mengalahkan Queue jika Registration belum ada.
3. Nama Queue dipakai jika dua sumber lain kosong.
4. Layanan Registration mengalahkan Booking.
5. Dokter Registration mengalahkan Booking.
6. Jam Praktek hanya berasal dari Booking.
7. Queue Number tidak pernah memakai `Booking.NoAntrian`.
8. Service Point Queue tidak tertimpa data penjamin.
9. Sentinel Registration menyebabkan fallback yang benar.
10. ID dan nama reference selalu fallback sebagai satu paket.
11. Pasien ID Registration mengalahkan Pasien ID Booking.
12. Pasien ID Booking dipakai jika Registration belum memiliki Pasien ID.
13. Pasien Tracker ID tidak pernah dikembalikan sebagai Pasien ID.
14. Booking ID tetap tersedia jika detail Booking gagal tetapi linkage persisten tersedia.
15. Registration ID tetap tersedia jika detail Registration gagal tetapi linkage persisten
    tersedia.
16. Booking ID dan Registration ID tidak pernah saling menggantikan.

### Source

1. Booking Assistance menghasilkan Source Booking.
2. Anonymous intake menghasilkan Source WalkIn.
3. Bukti legacy yang ambigu menghasilkan Unknown.
4. Source tidak berubah setelah Registration selesai.
5. Source tetap tersedia untuk future registered-history view.

### Sorting

1. Default tetap Priority-aware FIFO.
2. Semua key mendukung ascending dan descending.
3. Null tetap di bawah.
4. Nama yang sama menghasilkan urutan stabil.
5. Paging dataset yang sama menghasilkan batas halaman yang sama.
6. Perubahan nilai efektif setelah Registration mengubah posisi sesuai sorting.

### Filter dan paging

1. Filter memakai nilai Registration setelah Registration tersedia.
2. Record null tetap muncul jika tidak sedang difilter keluar.
3. Source Unknown dapat dicari.
4. `totalCount` sesuai hasil filter.
5. Record yang awalnya berada di page kedua dapat muncul di page pertama hasil filter.
6. Filter Pasien ID, Booking ID, dan Registration ID hanya mencari identifier masing-masing.

### Frontend

1. Kartu menggunakan field efektif backend.
2. Field opsional kosong tidak menampilkan label menyesatkan.
3. Pergantian filter/sort mereset paging dan scroll.
4. Selection tetap stabil setelah reorder.
5. Frontend tidak lagi menyimpulkan WalkIn hanya dari Booking null.

## 21. Tahapan implementasi yang disarankan

### Tahap 0 — Kontrak

- sepakati arti Service Point versus penjamin;
- inventaris bukti Source Booking/WalkIn;
- setujui matriks otoritas field;
- buat unit test resolver.

### Tahap 1 — Card projection additive

- tambah type backend `card`;
- implementasikan normalisasi dan resolver;
- pertahankan bagian response lama;
- tambahkan contract test.

### Tahap 2 — Query composition set-based

- resolve nilai sebelum paging;
- hilangkan N+1;
- pertahankan queue-only projection;
- pastikan count dan page konsisten.

### Tahap 3 — Server sorting

- tambah `sortBy` dan `sortDirection`;
- gunakan allow-list;
- pertahankan default Queue Order;
- tambahkan stable tie-breaker dan aturan null.

### Tahap 4 — Server filtering

- tambah filter nilai efektif;
- pastikan count memakai filter yang sama;
- uji kombinasi filter.

### Tahap 5 — Integrasi frontend

- tambah schema/type `card`;
- ubah kartu agar memakai projection;
- tambah kontrol sorting/filter;
- hapus resolver duplikat setelah rollout stabil.

### Tahap 6 — Pemakaian untuk antrean terdaftar

- gunakan projection yang sama untuk view “Sudah registrasi”;
- tetap kirim metadata non-kartu seperti `QueueStatus`, `DoneAt`, dan outcome; `RegId` sudah menjadi
  bagian dari card projection;
- lakukan pengujian volume dan dokumentasikan hasil rollout.

## 22. Kriteria selesai

Fitur dinyatakan selesai jika:

1. setiap item mempunyai objek `card` tambahan;
2. seluruh field mengikuti matriks sumber;
3. Pasien ID, Booking ID, dan Registration ID tetap berbeda dan mengikuti aturan sumbernya;
4. Booking ID dan Registration ID yang sudah diketahui tidak hilang ketika detail gagal dimuat;
5. Source eksplisit dan tidak bergantung pada keberadaan objek Booking;
6. tampilan, sorting, dan filter menggunakan nilai efektif yang sama;
7. sorting/filter dilakukan sebelum paging;
8. default sorting tetap menghormati Priority;
9. metadata paging sesuai hasil filter;
10. response lama tetap kompatibel;
11. queue-only projection tetap tidak mengambil kepemilikan data Admisi;
12. tidak ada N+1 pada seluruh kandidat;
13. selection frontend tetap memakai `AntrianId + NoUrut`;
14. unit, integration, API contract, frontend, dan performance test lulus.

## 23. Larangan implementasi

Jangan:

- menyelesaikan global sorting hanya di frontend;
- memfilter hanya page yang sudah dimuat;
- memakai `Booking.NoAntrian` untuk nomor Admission Queue;
- memakai Pasien ID, Booking ID, Registration ID, atau Pasien Tracker ID untuk saling
  menggantikan;
- menganggap Booking null selalu berarti WalkIn;
- mengubah Source karena Registration sudah ada;
- mencampur Service Point dan kategori penjamin tanpa keputusan produk;
- menghilangkan record Queue karena enrichment gagal;
- menambahkan data Admisi ke queue-only operational projection;
- membuat ledger Queue mutable kedua;
- menerima nama kolom sorting bebas dari request.

## 24. Referensi kode

Backend:

- `Bilreg.Application/AdmisiContext/RegFeature/AdmisiRajalOfficerWorklistQuery.cs`
- `Bilreg.Application/AdmisiContext/AntrianFeature/IAdmissionQueueOperationalProjection.cs`
- `Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AdmissionQueueOperationalProjection.cs`
- `Bilreg.Infrastructure/AdmisiContext/AntrianFeature/BookingAssistanceRepo.cs`
- `Bilreg.Infrastructure/AdmisiContext/RegFeature/RegistrationOutcomeOperationRepo.cs`

Frontend:

- `c012_myhospital_web/src/modules/Admisi/types/admissionQueue.ts`
- `c012_myhospital_web/src/modules/Admisi/queries/AdmissionQueueService.ts`
- `c012_myhospital_web/src/modules/Admisi/composables/useOfficerAdmissionQueue.ts`
- `c012_myhospital_web/src/modules/Admisi/composables/admissionQueueWorklistPresentation.ts`
- `c012_myhospital_web/src/modules/Admisi/components/admissionQueue/DenseWorklistTile.vue`
- `c012_myhospital_web/src/modules/Admisi/components/admissionQueue/VirtualWorklistGrid.vue`
- `c012_myhospital_web/src/modules/Admisi/components/admissionQueue/OfficerAdmissionQueueWorkspace.vue`
