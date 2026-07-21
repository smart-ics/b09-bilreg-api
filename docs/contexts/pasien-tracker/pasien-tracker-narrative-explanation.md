# PASIEN TRACKER

## AGGREGATE DESIGN

Sistem terdiri dari 2 aggregate:

1. TrackerPasienAgg
2. AntrianSesionAgg

### ENTITAS AGGREGATE TRACKER PASIEN

Aggregate TrackerPasienAgg terdiri dari 2 entitas:

- TrackerPasien
- TrackerEvent

TrackerPasien mempunyai 3 property:

1. TrackerId
2. Name
3. TglLahir

TrackerEvent punya 4 property:

1. TrackerId
2. EventName
3. ReffId
4. Timestamp

Relasi TrackerPasien dan TrackerEvent adalah one-to-many

---

### ENTITAS AGGREGATE ANTRIAN SESSION

Aggregate AntrianSessionAgg terdiri dari 2 entitas

- AntrianSession
- AntrianEntry

AntrianSession punya 5 property

1. SesionId
2. ServicePoint
3. TglSession
4. JamMulai
5. JamSelesai

AntrianEntry punya 7 property:

1. SessionId
2. NoUrut
3. TrackerId
4. Name
5. CreatedAt
6. ServedAt
7. DoneAt

Relasi antara AntrianSession dengan AntrianEntry adalah one-to-many

---

## RUMUSAN SKENARIO

### LIST SKENARIO

Skenario yang akan dilakukan terdiri dari 9 aktifitas user:

1. Sinta Booking
2. Sinta Ambil Antrian Kiosk
3. Sinta Dipanggil Admisi
4. Sinta Selesai Dilayani Admisi
5. Sinta Masuk Ruang Dokter
6. Sinta Selesai Diperiksa Dokter
7. Sinta Menuju Apotek
8. Sinta Dipanggil Konfirmasi Obat
9. Sinta Dipanggil Ambil Obat

Masing-masing langkah akan dijelaskan bagaimana state setiap aggregate.

### SUPPORTING AGGREGATE BOOKING

Dalam penjelasan saya menambahkan satu aggregate `Booking` untuk menjelaskan bagaimana nomor antrian yg di dapatkan pasien saat booking terjadi.

Entitas booking punya 7 property:

1. BookingId
2. Name
3. TglLahir
4. Dokter
5. TglJadwal
6. JamMulai
7. NoAntrian

### BAGIAN-BAGIAN PENJELASAN

Dalam penjelasan akan saya sertakan 3 hal:

1. Pseudo Code langkah-langkah proses
2. Narasi penjelasan terbentuknya state di aggregate
3. State yang terbentuk di setiap langkah pada aggregate yang bersangkutan

---

## SKENARIO-1 : SINTA BOOKING

### Pseudo Code

- Step-0 : var booking = Booking.Create(“Sinta”, “05-MEI-2008”, “Dr.Agus”, “03-AUG-2025”, “07:00”)
- Step-1 : var tracker = Tracker.Create(booking)
- Step-2 : tracker.AddEvent(booking)
- Step-3 : var jadwal = JadwalPrakte.GetData(booking)
- Step-4 : var antrian = AntrianSession.LoadOrCreate(jadwal)
- Step-5 : var antrianEntry = antrian.AddEntry(tracker)
- Step-6 : booking.SetNoUrut(antrianEntry)

### STEP-0

A. NARASI

Event terjadi saat user klik “Save” di transaksi Booking.
Sistem insert ke table booking dengan data sesuai yang diinput user.

B. STATE AGGREGATE BOOKING

- BookingId = BK01
- Name = Sinta
- TglLahir = 05-MEI-2008
- Dokter = Dr.Agus
- TglJadwal = 03-AUG-2025
- JamMulai = 07:00
- NoAntrian = [empty]

C. STATE AGGREGATE TRACKER PASIEN (kosong)

D. STATE AGGREGATE ANTRIAN SESSION (kosong)

### STEP-1 and STEP-2

A. NARASI

Tracker adalah perekam posisi pasien secara fisik.

“Fisik Pasien” diwakili di system dengan sebuat token yaitu “TrackerID”. Token ini tidak berubah dari awal pasien tercatat di system s/d keluar.

Tracker terdiri dari 2 table header-detil: Tracker Header dan TrackerEvent. Kita bisa gunakan “RegID” sebagai token. Tapi karena pada kasus ini pasien belum mempunyai RegID, maka digeneratekan sebuah token “VISITORxxx”.

Selain itu dicatat event yang terjadi (booking) di system dan juga timestamp event-nya.

B. STATE AGGREGATE BOOKING (no change from step-0)

C. STATE AGGREGATE TRACKER PASIEN

TrackerPasien:

- TrackerId = VSTR01
- Name = Sinta
- TglLahir = 05-MEI-2008

TrackerEvent:

- TrackerId = VSTR01
- EventName = Booking
- ReffId = BK01
- Timestamp = 2025-08-01 21:43

D. STATE AGGREGATE ANTRIAN SESSION (kosong)

### STEP-3, STEP-4 and STEP-5

A. NARASI

Antrian terdiri dari table Header-Detail. Header Session mencatat service-point, dan Detail Entry mencatat pasien-pasiennya.

Entry punya 3 timestamp; CreatedAt, ServedAt, DoneAt.

Di titik ini system akan mengambil data jadwal praktek untuk meng-create Antrian Session (atau cukup load saja jika memang sudah terbentuk).

Selanjutnya data tracker dipassingkan untuk masuk dalam antrian dan digeneratekan nomor urutnya.

Timestamp yang tercatat saat ini hanya CreatedAt saja.

B. STATE AGGREGATE BOOKING (no change)

C. STATE AGGREGATE TRACKER PASIEN (no change)

D. STATE AGGREGATE ANTRIAN SESSION

AntrianSession:

- SessionId = AN001
- ServicePoint = Dr.Agus
- TglSession = 03-AUG-2025
- JamMulai = 07:00
- JamSelesai = 09:00

AntrianEntry:

- SessionId = AN001
- NoUrut = 1
- TrackerId = VSTR01
- Name = Sinta
- CreatedAt = 2025-08-01 21:43
- ServedAt = [empty]
- DoneAt = [empty]

### STEP-6

A. NARASI

Nomor Antrian yang terbentuk di step sebelumnya, di-assignkan ke booking pasien tersebut.

B. STATE AGGREGATE BOOKING

- BookingId = BK01
- Name = Sinta
- TglLahir = 05-MEI-2008
- Dokter = Dr.Agus
- TglJadwal = 03-AUG-2025
- JamMulai = 07:00
- NoAntrian = 1

## SKENARIO-2 : SINTA AMBIL ANTRIAN KIOSK

### PSEUDO CODE

Step-0 : var antrian = AntrianSession.LoadOrCreate(“Loket BPJS”)
Step-1 : var antrianEntry = antrian.AddEntry()

### STEP-0 dan STEP-1

A. NARASI

Event ini terjadi saat pasien berada di depan mesin kiosk.

Saat pasien klik salah satu loket untuk ambil nomor antrian, yang terjadi adalah:

Sistem akan mencari antraian loket tersebut dan otomatis meng-createnya jika tidak ditemukan.

Kemudian buat entry 1 pasien tapi tidak terisi identitas pasiennya (karena di mesin kiosk pasien tidak input apa2).

Yang tercatat hanyalah timestamp CreatedAt saja

B. STATE ANTRIAN SESSION

- SessionId = AN002
- ServicePoint = Loket BPJS
- TglSession = 03-AUG-2025
- JamMulai = 00:00
- JamSelesai = 23:59

C. STATE ANTRIAN ENTRY

- SessionId = AN002
- NoUrut = 1
- TrackerId = [empty]
- Name = [empty]
- CreatedAt = 2025-08-03 06:51
- ServedAt = [empty]
- DoneAt = [empty]

## SKENARIO-3 : SINTA DIPANGGIL ADMISI

### PSEUDO CODE

Step-0 : var tracker = PasienTracker.Find(“Sinta”, “05-Mei-2008”)
Step-1 : var antrian = AntrianSession.LoadOrCreate(“Loket BPJS”)
Step-2 : var antrianEntry = antrian.ListEntry.First(x => x.NoUrut = 1)
Step-3 : antrianEntry.AssignPasien(tracker)
Step-4 : antrianEntry.Serve()
Step-5 : tracker.AddEvent(“Check In”)
Step-6 : tracker.AddEvent(“Reg-Start”)

### STEP-0

A. NARASI

Petugas admisi di Loket BPJS memanggil pasien nomor urut 1.

Sinta datang dan menyerahkan bukti nomor urut ke petugas admisi.

Petugas admisi menanyakan Nama dan Tgl Lahir, kemudian mencarinya di system tracker.

Jika ditemukan, maka di-load trackernya. (Tapi jika tidak ditemukan, maka akan dibuatkan tracker baru)

B. STATE AGGREGATE TRACKER PASIEN (no change, only get data)

- TrackerId = VSTR01
- Name = Sinta
- TglLahir = 05-MEI-2008

### STEP-1 and STEP-2

A. NARASI

Sistem nge-load system antrian Loket BPJS hari itu dan mencari pasien dengan nomor urut = 1 (sesuai nomor urut yang diserahkan pasien).

B. STATE AGGREGATE ANTRIAN SESSION (no change, only get data)

- SessionId = AN002
- ServicePoint = Loket BPJS
- TglSession = 03-AUG-2025  
- JamMulai = 00:00
- JamSelesai = 23:59

### STEP-3 and STEP-4

A. NARASI

Sistem meng-update antrian entry pada field TrackerID, Nama dan timestamp ServedAt (karena di sini pasien mulai dilayani oleh admisi)

B. STATE AGGREGATE ANTRIAN ENTRY

- SessionId = AN002
- NoUrut = 1
- TrackerId = VSTR01 (updated)
- Name = Sinta  (updated)
- CreatedAt = 2025-08-03 06:51
- ServedAt = 2025-08-03 06:57  (updated)
- DoneAt = [empty]

### STEP-5 and STEP-6

A. NARASI

Sistem mencatat kedua timestamp (CreatedAt dan ServedAt) di tracker event.

Karena tidak ada transaksi yang terbentuk, maka ReffID untuk tracker event ini adalah nomor antrian-nya saja.

B. STATE AGGREGATE TRACKER EVENT

- TrackerId = VSTR01
- EventName = Check In
- ReffId = AN002/No.1
- Timestamp = 2025-08-03 06:51

- TrackerId = VSTR01
- EventName = Reg-Start
- ReffId = AN002/No.1
- Timestamp = 2025-08-03 06:57

## SKENARIO-4 : SINTA SELESAI DILAYANI ADMISI

### PSEUDO CODE

Step-0 : var antrian = AntrianSession.LoadOrCreate(“Loket BPJS”)
Step-1 : var antrianEntry = antrian.ListEntry.First(x => x.NoUrut = 1)
Step-2 : antrianEntry.Done()
Step-3 : var tracker = PasienTracker.Load(antrianEntry)
Step-4 : tracker.AddEvent(“Reg Done”, antrianEntry)

### STEP-0 and STEP-1

A. NARASI

Event ini terjadi ketika pasien klik tombol SAVE REGISTER.
Sistem akan nge-load system antrian dan mencari nomor urut-1.

B. STATE AGGREGATE ANTRIAN SESSION (no change, only get data)

- SessionId = AN002
- ServicePoint = Loket BPJS
- TglSession = 03-AUG-2025
- JamMulai = 00:00
- JamSelesai = 23:59

### STEP-2

A. NARASI

Kemudian meng-update timestampe selesai atau DoneAt

B. STATE AGGREGATE ANTRIAN ENTRY

- SessionId = AN002
- NoUrut = 1
- TrackerId = VSTR01
- Name = Sinta
- CreatedAt = 2025-08-03 06:51
- ServedAt = 2025-08-03 06:57
- DoneAt = 2025-08-03 07:05 (updated)

### STEP-3 and STEP-4

A. NARASI

Selanjutnya tracker nge-load data visitor tersebut (Token Pasien VSTR01 di dapat dari AntrianEntry).
Kemudian menambahkan event selesai registrasi atau Reg-Done.
Karena terbentuk kode registrasi RG, maka itu dijadikan ReffID.

B. STATE AGGREGATE TRACKER EVENT

- TrackerId = VSTR01
- EventName = Reg Done
- ReffId = RG007
- Timestamp = 2025-08-03 07:05

## SKENARIO-5 : SINTA MASUK RUANG DOKTER

### PSEUDO CODE

0 var antrian = AntrianSession.LoadOrCreate(“Dr.Agus”)
1 var antrianEntry = antrian.ListEntry.First(x => x.NoUrut = 1)
2 antrianEntry.Serve()
3 var tracker = PasienTracker.Load(antrianEntry)
4 tracker.AddEvent(“Konsul-Start”, antrianEntry)

### STEP-0 and STEP-1

A. NARASI

Event Terjadi Ketika Perawat Poli menandai bahwa pasien masuk ruang pemerksaan (Screen EMR-Web: memindahkan antrian pasien ke kolom Periksa)

Sistem ngeload antrian dokter ybs dan mencari nomor antrian pasien tersebut (nomor-1)

B. STATE AGGREGATE ANTRIAN SESSION (no change, only get data)

- SessionId = AN001
- ServicePoint = Dr.Agus
- TglSession = 03-AUG-2025
- JamMulai = 07:00
- JamSelesai = 09:00

C. STATE AGGREGATE ANTRIAN ENTRY (no change, only get data)

- SessionId = AN001
- NoUrut = 1
- TrackerId = VSTR01
- Name = Sinta
- CreatedAt = 2025-08-01 21:43
- ServedAt = [empty]
- DoneAt = [empty]

### STEP-2

A. NARASI

Lalu meng-update timestamp ServeAt di antrian entry; menandakan bahwa pasien mulai di layani oleh dokter.

B. STATE AGGREGATE ANTRIAN ENTRY

- SessionId = AN001
- NoUrut = 1
- TrackerId = VSTR01
- Name = Sinta
- CreatedAt = 2025-08-01 21:43
- ServedAt = 2025-08-03 07:20 (updated)
- DoneAt = [empty]

### STEP-3 and STEP-4

A. NARASI

Tracker nge-load pasien tersebut dan menambahkan event “Konsul Start”.

Karena tidak ada transaksi yang terbentuk, maka ReffID-nya adalah Nomor Antrian

B. STATE AGGREGATE TRACKER PASIEN (no change, only get data)

- TrackerId = VSTR01
- Name = Sinta
- TglLahir = 05-MEI-2008

C. STATE AGGREGATE TRACKER EVENT

- TrackerId = VSTR01
- EventName = Konsul Start
- ReffId = AN001/No.1
- Timestamp = 2025-08-03 07:20

## SKENARIO-6 : SINTA SELESAI DIPERIKSA DOKTER

### PSEUDO CODE

Step-0 : var antrian = AntrianSession.LoadOrCreate(“Dr.Agus”)
Step-1 : var antrianEntry = antrian.ListEntry.First(x => x.NoUrut = 1)
Step-2 : antrianEntry.Done()
Step-3 : var tracker = PasienTracker.Load(antrianEntry)
Step-4 : tracker.AddEvent(“Konsul-Don.e”, antrianEntry)
Step-5 : var antrian = AntrianSession LoadOrCreate(“Apotek RJ”)
Step-6 : var antrianEntry = antrian.AddEntry(tracker)

### STEP-0 and STEP-1

A. NARASI

Event ini terjadi Ketika dokter klik Save Medical Chart.

Sistem antrian akan ngeload antrian pasien ybs dan nomor urut pasien yang diperiksa.

B. STATE AGGREGATE ANTRIAN SESSION (no change, only get data)

- SessionId = AN001
- ServicePoint = Dr.Agus
- TglSession = 03-AUG-2025
- JamMulai = 07:00
- JamSelesai = 09:00

C. STATE AGGREGATE ANTRIAN ENTRY (no change, only get data)

- SessionId = AN001
- NoUrut = 1
- TrackerId = VSTR01
- Name = Sinta
- CreatedAt = 2025-08-01 21:43
- ServedAt = 2025-08-03 07:20
- DoneAt = [empty]

### STEP-2

A. NARASI

Kemudian update timestamp DoneAt; menandai bahwa pasien telah selesai di antrian dokter tsb.

B. STATE AGGREGATE ANTRIAN ENTRY

- SessionId = AN001
- NoUrut = 1
- TrackerId = VSTR01
- Name = Sinta
- CreatedAt = 2025-08-01 21:43
- ServedAt = 2025-08-03 07:20
- DoneAt = 2025-08-03 07:40 (updated)

### STEP-3 and STEP-4

A. NARASI

Sistem tracker ngeload pasien yang bersangkutan.
Lalu add event Konsul-Done.

B. STATE AGGREGATE TRACKER PASIEN (no change, only get data)

- TrackerId = VSTR01
- Name = Sinta
- TglLahir = 05-MEI-2008

C. STATE AGGREGATE TRACKER EVENT

- TrackerId = VSTR01
- EventName = Konsul-Done
- ReffId = CH-003 (berasal dari kode medical chart)
- Timestamp = 2025-08-03 07:40

### STEP-5 and STEP-6

A. NARASI

Saat Generate KP Resep (yang terjadi juga saat klik save chart), maka system antrian akan bekerja lagi.

Yaitu dengan nge-load antrian apotek, atau nge-create jika memang belum ada.

Dan menambahkan antrian apotek atas pasien tsb. 

Ini otomatis dilakukan agar pasien tidak perlu ambil antrian lagi di apotek, tapi apoteker bisa mengetahui bahwa ada pasien tsb di apotek.

Note: tidak ada event baru yang tercatat di tracker, karena pasien belum dilayani di apotek,

B. STATE AGGREGATE ANTRIAN SESSION

- SessionId = AN003
- ServicePoint = Apotek RJ
- TglSession = 03-AUG-2025
- JamMulai = 00:00
- JamSelesai = 23:59

C. STATE AGGREGATE ANTRIAN ENTRY

- SessionId = AN003
- NoUrut = 1
- TrackerId = VSTR01
- Name = Sinta
- CreatedAt = 2025-08-03 07:40
- ServedAt = [empty]
- DoneAt = [empty]

## SKENARIO-7 : SINTA MENUJU APOTEK

### PSEUDO CODE

(nothing)

Tidak terjadi apapun di sini.

Pasien berjalan ke apotek dan langsung duduk di ruang tunggu apotek.

Perlu diperhatikan bahwa identitas pasien atau token yang dipegang adalah sama dengan yang diberikan oleh petugas registrasi (VSTR01)

## SKENARIO-8 : SINTA DIPANGGIL KONFIRMASI OBAT

### PSEUDO CODE

0 var antrian = AntrianSession.LoadOrCreate(“Apotek RJ”)
1 var antrianEntry = antrian.ListEntry.First(x => x.TrackerID = VSTR01)
2 antrianEntry.Serve()
3 var tracker = PasienTracker.Load(antrianEntry)
4 tracker.AddEvent(“Apotek-Start”, antrianEntry)

### STEP-0 and STEP-1

A. NARASI

Event ini terjadi Ketika apoteker klik Save DU (Transaksi Jual Obat). Tapi sebelum klik save, mak a kejadianya adalah sbb;

Apoteker memanggil pasien BERDASARKAN TOKEN atau NAMA, bukan nomor antrian. Karena pasien tidak mendapat nomor antrian apotek.

Saat proses Save DU, maka system akan nge-load antrian apotek dan mencari entry sesuai Tracker-Id.

B. STATE AGGREGATE ANTRIAN SESSION (no change, only get data)

- SessionId = AN003
- ServicePoint = Apotek RJ
- TglSession = 03-AUG-2025
- JamMulai = 00:00
- JamSelesai = 23:59

C. STATE AGGREGATE ANTRIAN ENTRY (no change, only get data)

- SessionId = AN003
- NoUrut = 1
- TrackerId = VSTR01
- Name = Sinta
- CreatedAt = 2025-08-03 07:40
- ServedAt = [empty]
- DoneAt = [empty]

### STEP-2

A. NARASI

Kemudian mengupdate timestamp “ServedAt” untuk menandai dimulainya pelayanan di apotek

B. STATE AGGREGATE ANTRIAN ENTRY

- SessionId = AN003
- NoUrut = 1
- TrackerId = VSTR01
- Name = Sinta
- CreatedAt = 2025-08-03 07:40
- ServedAt = 2025-08-03 07:51 (updated)
- DoneAt = [empty]

### STEP-3 and STEP-4

A. NARASI

Tracker nge-load data pasien ybs 

Dan menambahkan event “Apotek-Start” di history pasien ybs. Karena terbentuk transaksi DU, maka kode DU dijadikan ReffID

B. STATE AGGREGATE TRACKER PASIEN (no change, only get data)

- TrackerId = VSTR01
- Name = Sinta
- TglLahir = 05-MEI-2008

C. STATE AGGREGATE TRACKER EVENT

- TrackerId = VSTR01
- EventName = Apotek-Start
- ReffId = DU-071
- Timestamp = 2025-08-03 07:51

## SKENARIO-9 : SINTA DIPANGGIL AMBIL OBAT

### PSEUDO CODE

Step-0 var antrian = AntrianSession.LoadOrCreate(“Apotek RJ”)
Step-1 var antrianEntry = antrian.ListEntry.First(x => x.TrackerID = VSTR01)
Step-2 antrianEntry.Done()
Step-3 var tracker = PasienTracker.Load(antrianEntry)
Step-4 tracker.AddEvent(“Apotek-Done”, antrianEntry)

### STEP-0 and STEP-1

A. NARASI

Event terjadi Ketika pasien dipanggil oleh apoteker untuk penyerahan obat dan menjelaskan etiket / cara konsumsi obat.

Secara system akan dibuatkan tampilan antrian serah obat, dan ada tombol “Serah Obat”.

System akan nge-load antrian apotek dan ambil data sesuai tracker-id.

B. STATE AGGREGATE ANTRIAN SESSION (no change, only get data)

- SessionId = AN003
- ServicePoint = Apotek RJ
- TglSession = 03-AUG-2025
- JamMulai = 00:00
- JamSelesai = 23:59

C. STATE AGGREGATE ANTRIAN ENTRY (no change, only get data)

- SessionId = AN003
- NoUrut = 1
- TrackerId = VSTR01
- Name = Sinta
- CreatedAt = 2025-08-03 07:40
- ServedAt = 2025-08-03 07:51
- DoneAt = [empty]

## STEP-2

A. NARASI

Kemudian mengupdate timestamp “DoneAt” untuk menandai selesainya pelayanan di apotek

B. STATE AGGREGATE ANTRIAN ENTRY

- SessionId = AN003
- NoUrut = 1
- TrackerId = VSTR01
- Name = Sinta
- CreatedAt = 2025-08-03 07:40
- ServedAt = 2025-08-03 07:51
- DoneAt = 2025-08-03 08:12 (updated)

### STEP-3 and STEP-4

A. NARASI

Sistem Tracker akan nge-load pasien dengan token tsb dan menambahkan event “Apotek-Done”

B. STATE AGGREGATE TRACKER PASIEN (no change, only get data)

- TrackerId = VSTR01
- Name = Sinta
- TglLahir = 05-MEI-2008

C. STATE AGGREGATE TRACKER EVENT

- TrackerId = VSTR01
- EventName = Apotek-Done
- ReffId = AN003/No.1
- Timestamp = 2025-08-03 08:12
