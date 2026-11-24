# BUG-INVESTIGATION

## Context

Issue: [LAB-ORDER-PENJAMIN-DISPLAY-BUG-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PENJAMIN-DISPLAY-BUG-ISSUE.md)
Problem Summary: Panel rincian pesanan laboratorium (`LabOrderDetailPanel.vue`) selalu menampilkan "BPJS Kesehatan" untuk seluruh pasien rumah sakit karena terdapat fallback string literal hardcoded di template UI dan ketiadaan properti penjamin pada DTO respons `LabOrderGetResponse`.

## Current State

1. Pengguna membuka modul Laboratorium dan memilih sebuah pesanan pada worklist.
2. Panel rincian pesanan (`LabOrderDetailPanel.vue`) memuat detail order via `GET /api/LabContext/LabOrderFeature/{orderId}`.
3. Pada kartu **Identitas Pasien**, bagian `Penjamin / Cara Bayar` menampilkan:
   - `"Umum / Tunai"` hanya jika `order.orderSource === 4` (Pasien Luar Walk-In).
   - `"BPJS Kesehatan"` untuk seluruh pasien lainnya (`orderSource` 1 = Rawat Jalan, 2 = Rawat Inap, 3 = IGD).
4. Pengguna mendapati pasien yang aslinya mendaftar sebagai pasien umum/tunai atau asuransi swasta tetap tertulis sebagai "BPJS Kesehatan".

## Problem Analysis

### 1. Hardcoded Fallback pada Template Frontend
Pada [`LabOrderDetailPanel.vue:738-742`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/workspace/LabOrderDetailPanel.vue#L738-L742):
```vue
<div>
  <span class="text-muted-foreground block text-[10px]">Penjamin / Cara Bayar</span>
  <span class="font-semibold text-purple-700">{{
    order.orderSource === 4 ? 'Umum / Tunai' : 'BPJS Kesehatan'
  }}</span>
</div>
```
Ekspresi ternary ini mengasumsikan biner: jika bukan Walk-In External (`4`), pasti BPJS Kesehatan. Asumsi ini menyalahi model domain rumah sakit, di mana pasien internal dapat memiliki ragam jenis penjaminan (Umum/Bayar Sendiri, BPJS Kesehatan, Asuransi Swasta, Perusahaan IKS).

### 2. Ketiadaan Field Penjamin pada Kontrak Query
- Pada backend [`LabOrderGetQuery.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderGetQuery.cs#L15-L60), record `LabOrderGetResponse` mengembalikan informasi pasien (`RegId`, `PatientId`, `PatientName`, `BirthDate`, `Gender`, `AgeAtOrder`), namun belum menyertakan `PenjaminName`.
- Di sisi frontend, [`labOrderDetailSchema`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/types/lab-order.schema.ts#L78) belum mendefinisikan field `penjaminName`.
- Agregat registrasi [`RegModel`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/RegFeature/RegModel.cs#L86) sebenarnya telah memiliki referensi penjaminan lengkap (`reg.TipeJaminan.TipeJaminanName`), dan repository [`IRegRepo`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/IRegRepo.cs) sudah disuntikkan ke dalam `LabOrderGetHandler` untuk perhitungan tarif, sehingga resolusi nama penjamin dapat diperoleh langsung tanpa penambahan dependensi baru.

## Affected Components

- **Backend**:
  - `Bilreg.Application.LabContext.LabOrderFeature.UseCases.LabOrderGetResponse`: penambahan property `string PenjaminName = ""`
  - `Bilreg.Application.LabContext.LabOrderFeature.UseCases.LabOrderGetHandler`: resolusi `penjaminName` dari `reg.TipeJaminan.TipeJaminanName` jika `order.Patient.RegId` ada, atau `"Umum / Pasien Luar"` untuk `OrderSource == WalkInExternal`.
- **Frontend**:
  - `c012_myhospital_web/src/modules/Laboratory/types/lab-order.schema.ts`: penambahan `penjaminName: z.string().optional()` pada `labOrderDetailSchema`.
  - `c012_myhospital_web/src/modules/Laboratory/components/workspace/LabOrderDetailPanel.vue`: pengikatan nilai dinamis `order.penjaminName || (order.orderSource === 4 ? 'Umum / Tunai' : '—')`.

## Impact Assessment

- **Business Impact**: Menghilangkan misinformasi penjaminan pasien pada lembar kerja laboratorium, mencegah kesalahan identifikasi hak bayar pasien.
- **Operational Impact**: Petugas laboratorium dapat secara akurat mengetahui apakah pasien memerlukan otorisasi asuransi/BPJS atau pelunasan kasir tanpa harus membuka modul admisi secara terpisah.
- **Technical Impact**: Perubahan backward-compatible pada query endpoint `GET /api/LabContext/LabOrderFeature/{orderId}` tanpa perubahan skema tabel basis data.

## Assumptions

- **ASM-BUG-001**: Untuk pasien rumah sakit internal (`OrderSource` 1, 2, 3), sumber kebenaran penjamin terikat pada `ta_registrasi` (`RegId`) dan dapat di-resolve via `IRegRepo.LoadEntity(RegModel.Key(regId))`.
- **ASM-BUG-002**: Untuk pasien luar walk-in (`OrderSource == 4`), jika tidak memiliki registrasi penjamin khusus, nilai penjamin default adalah `"Umum / Pasien Luar"` atau `"Umum / Tunai"`.
- **ASM-BUG-003**: Penambahan properti opsional `PenjaminName` pada `LabOrderGetResponse` tidak merusak klien lain karena berupa backward-compatible property.

## Open Questions

Tidak ada pertanyaan terbuka (*Zero open questions*). Sumber data dan titik perbaikan telah teridentifikasi secara presisi.

## Recommended Decision

1. Perluas `LabOrderGetResponse` dengan properti `PenjaminName = ""`.
2. Pada `LabOrderGetHandler`, isi `PenjaminName` dari `reg.TipeJaminan.TipeJaminanName` saat registrasi ditemukan, atau fallback `"Umum / Pasien Luar"` bila `OrderSource == WalkInExternal`.
3. Pada schema frontend `labOrderDetailSchema`, tambahkan `penjaminName: z.string().optional()`.
4. Pada `LabOrderDetailPanel.vue`, gunakan `order.penjaminName || (order.orderSource === 4 ? 'Umum / Tunai' : '—')`.

## Decision

Realisasikan resolusi nama penjamin riil dari agregat registrasi pada `LabOrderGetHandler` backend dan perbarui binding komponen `LabOrderDetailPanel.vue` frontend agar mencerminkan data penjamin yang sebenarnya.

## Decision Rationale

1. **Akurasi Data Klinis & Administratif**: Menghapus asumsi salah bahwa seluruh pasien internal RS adalah BPJS.
2. **Efisiensi Implementasi**: `IRegRepo` sudah tersedia di dalam `LabOrderGetHandler`, sehingga tidak memerlukan kueri baru atau modifikasi tabel database.
3. **Resiko Nol terhadap State**: Perubahan terbatas pada layer DTO query dan presentasi UI.

## Architecture Applicability

### Decision

**`ARCHITECTURE-NOT-REQUIRED`**

### Rationale

Perbaikan ini merupakan defek DTO query dan template UI yang terisolasi:
- Tidak mengubah boundary bounded context.
- Tidak mengubah skema tabel database `BILRG_LabOrder` atau `ta_registrasi`.
- Tidak mengubah aturan state machine atau siklus hidup order laboratorium (`Ordered`, `Charged`, `Collected`, `Verified`, `Released`).
- Langsung dapat dituangkan ke dalam Implementation Plan perbaikan terarah.
