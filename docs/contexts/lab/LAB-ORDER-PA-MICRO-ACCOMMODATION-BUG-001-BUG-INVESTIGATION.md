# BUG-INVESTIGATION

## Context

Issue: [LAB-ORDER-PA-MICRO-ACCOMMODATION-BUG-001-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-BUG-001-ISSUE.md)
Problem Summary: Pembuatan order laboratorium baru dari EMR canvas (`POST /api/LabContext/LabOrderFeature/fromEmr`) gagal dengan respon galat HTTP 400 Bad Request (`AQ_INVALID_REQUEST`) dan pesan `"Value cannot be null. (Parameter 'source')"`. Kegagalan terjadi akibat runtime exception pada pemeriksaan idempotensi order eksisting di layer persistence.

## Current State

1. Pengguna membuka canvas pemesanan laboratorium EMR (`LabOrderCreateCanvas.vue`) untuk pasien rawat jalan aktif.
2. Pengguna memilih item tes murni Patologi Klinik (misal: Hemoglobin dan SGOT), mengisi catatan klinis, dan menekan tombol **Kirim Permintaan Lab**.
3. Frontend mengirimkan payload HTTP `POST /api/LabContext/LabOrderFeature/fromEmr`.
4. Backend `LabOrderCreateFromEmrHandler` melakukan verifikasi awal idempotensi untuk memastikan pesanan dengan `EmrOrderId` terkait belum pernah dibuat sebelumnya, dengan memanggil `_labOrderRepo.LoadByEmrOrderId(request.EmrOrderId)` $\rightarrow$ `_orderDal.GetByEmrOrderId(emrOrderId)`.
5. Metode `GetByEmrOrderId` pada data access layer mengeksekusi kueri `SELECT TOP 1` ke tabel `BILRG_LabOrder` menggunakan helper `conn.Read<LabOrderDto>(sql, dp)`.
6. Karena pesanan tersebut merupakan order baru yang belum pernah ada di database, helper `conn.Read<T>()` mengembalikan referensi `null`.
7. Kode mengeksekusi metode ekstensi LINQ `.FirstOrDefault()` secara langsung pada referensi `null` tersebut, yang melempar exception:
   `System.ArgumentNullException: Value cannot be null. (Parameter 'source')`.
8. Exception ditangkap oleh `ErrorHandlerMiddleware` sebagai `ArgumentException` dan dikembalikan ke klien sebagai HTTP 400 `AQ_INVALID_REQUEST`.
9. Akibatnya, seluruh alur penerbitan order dari EMR canvas terblokir dan tidak ada order yang berhasil diterbitkan.

## Problem Analysis

### 1. Bukti Eksekusi & Tracing Akar Masalah
- **Lokasi Kode**: [`LabOrderDal.cs:line 176`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabOrderDal.cs#L176):
  ```csharp
  public LabOrderDto? GetByEmrOrderId(string emrOrderId)
  {
      var sql = $"""
          SELECT TOP 1 {SelectColumns}
          FROM BILRG_LabOrder aa
          WHERE aa.EmrOrderId = @EmrOrderId
          ORDER BY aa.CrtDate DESC
          """;

      var dp = new DynamicParameters();
      dp.AddParam("@EmrOrderId", emrOrderId, SqlDbType.VarChar);

      using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
      return conn.Read<LabOrderDto>(sql, dp).FirstOrDefault();
  }
  ```
- **Karakteristik Perilaku `conn.Read<T>()` pada Pustaka `Nuna.Lib`**:
  Berbeda dengan LINQ `IEnumerable` umum atau Dapper standar yang mengembalikan koleksi kosong bila tidak ada baris yang cocok, implementasi `conn.Read<T>()` di `Nuna.Lib.DataAccessHelper.DapperHelper` mengembalikan nilai `null` ketika hasil query tidak menghasilkan baris.
- **Rantai Kegagalan**:
  Pemanggilan ekspresi `conn.Read<LabOrderDto>(sql, dp).FirstOrDefault()` tanpa operator null-conditional (`?.`) atau tanpa fallback default menyebabkan parameter `source` pada `Enumerable.FirstOrDefault(source)` bernilai `null`. Hal ini memicu `ArgumentNullException` internal .NET runtime.

### 2. Perbandingan Pola Query Baris Tunggal di Kode Eksisting
Di seluruh repository `b09-bilreg-api`, kueri baris tunggal yang bersifat opsional (*nullable*) secara konsisten menggunakan:
- Pola bawaan Dapper: `conn.QueryFirstOrDefault<T>(sql, dp)` (contoh: pada `LabTestDefinitionDal.GetData`, `LabCollectionPreparationDal.GetHeader`, dsb.), atau
- Penanganan null eksplisit: `conn.Read<T>(sql, dp)?.FirstOrDefault() ?? null`.

Metode `GetByEmrOrderId` pada `LabOrderDal` menyimpang dari pola aman ini karena memanggil `.FirstOrDefault()` langsung pada hasil `conn.Read<T>()`.

## Affected Components

- **Bounded Context**: Laboratory Context (`b09-bilreg-api`)
- **Fitur Terpengaruh**:
  - `LabOrderFeature` — Alur penerbitan order laboratorium EMR (`LabOrderCreateFromEmrCmd`).
- **Komponen Teknis**:
  - `LabOrderDal` (`GetByEmrOrderId`) di `Bilreg.Infrastructure.LabContext.LabOrderFeature`.
  - Terkait: Seluruh use case yang memeriksa idempotensi order EMR melalui `ILabOrderRepo.LoadByEmrOrderId`.
- **File Terdampak**:
  - `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabOrderDal.cs`
  - `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderDalTest.cs` (penambahan test case verifikasi non-existent order).

## Impact Assessment

- **Business Impact**:
  - Layanan pemesanan laboratorium dari rawat jalan dan IGD terhenti total untuk pemesanan digital dari rekam medis (EMR).
  - Dokter dan perawat tidak dapat mengirimkan permintaan lab untuk pasien poliklinik.
- **Operational Impact**:
  - Alur pengujian akomodasi order laboratorium PA dan Mikrobiologi (`LAB-ORDER-PA-MICRO-ACCOMMODATION-TEST-PACKAGE.md`) terhenti pada tahap paling awal (`TC-LPAM-01` s/d `TC-LPAM-09`).
- **Technical Impact**:
  - Handler mengembalikan kode status 400 Bad Request palsu (`AQ_INVALID_REQUEST`), mengaburkan diagnosa operasional seolah-olah input payload dari klien yang tidak valid, padahal terjadi kegagalan runtime internal pada layer DAL.

## Assumptions

- **ASM-BUG-001**: Kueri `GetByEmrOrderId` dimaksudkan untuk mengembalikan entitas `LabOrderDto` jika order pernah dibuat, atau `null` jika order belum ada, tanpa menimbulkan runtime exception.
- **ASM-BUG-002**: Komponen domain dan application (`LabOrderCreateFromEmrHandler`, `LabOrderRepo`, domain invariant) sudah menangani kasus kembalian `null` / `MayBe.None` dengan benar.
- **ASM-BUG-003**: Penggunaan metode kueri baris tunggal yang aman (`conn.QueryFirstOrDefault<LabOrderDto>` atau null-safe navigation) sepenuhnya kompatibel dengan SQL Server dan Dapper yang terpasang di solusi.

## Open Questions

Tidak ada pertanyaan terbuka (*Zero open questions*). Masalah telah terisolasi secara tuntas dan telah dibuktikan melalui reproduksi eksekusi riil.

## Recommended Decision

Lakukan perbaikan pada metode pembacaan data `GetByEmrOrderId` pada data access layer `LabOrderDal` agar menangani kueri kosong secara aman dan mengembalikan `null` tanpa melempar exception `ArgumentNullException`, serta lengkapi unit test pada `LabOrderDalTest` untuk memverifikasi pencarian order yang belum ada.

## Decision

Gunakan pembacaan data baris tunggal yang aman terhadap kondisi kosong (*null-safe single-row query*) pada metode `GetByEmrOrderId` di `LabOrderDal`, sehingga pemanggilan idempotensi order baru mengembalikan nilai kosong yang valid dan use case pembuatan order EMR dapat berjalan normal.

## Decision Rationale

1. **Eliminasi Exception Null Pointer**: Menggunakan metode kueri baris tunggal yang aman menjamin runtime tidak melempar `ArgumentNullException` saat tabel `BILRG_LabOrder` belum memiliki catatan dengan `EmrOrderId` yang dicari.
2. **Konsistensi dengan Standar Repositori**: Menyelaraskan implementasi kueri baris tunggal opsional di `LabOrderDal` dengan standar Dapper yang digunakan pada bounded context lainnya (`conn.QueryFirstOrDefault<T>`).
3. **Penyelesaian Tuntas Tanpa Efek Samping**: Perubahan terisolasi di DAL tanpa memengaruhi kontrak interface `ILabOrderTableDal`, use case handler, model domain, maupun antarmuka klien Web.

## Architecture Applicability

### Decision

**`ARCHITECTURE-NOT-REQUIRED`**

### Rationale

Perbaikan ini merupakan defek implementasi terisolasi pada layer persistence DAL (`LabOrderDal.cs`). Perbaikan ini:
- Tidak mengubah batas bounded context, arsitektur data, skema tabel database, maupun kontrak API.
- Tidak mengubah alur domain atau invariant bisnis yang didefinisikan pada [LAB-ORDER-PA-MICRO-ACCOMMODATION-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-ARCHITECTURE.md).
- Keputusan investigasi telah cukup spesifik dan aman untuk langsung direalisasikan melalui perencanaan implementasi koreksi (*Implementation Plan update / slice remediation*).
