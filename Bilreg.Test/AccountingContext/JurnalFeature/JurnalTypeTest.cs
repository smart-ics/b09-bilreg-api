using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.AccountingContext.JurnalFeature;

public class JurnalTypeTest
{
    [Fact]
    public void GivenEmptyParameters_WhenCreatingJurnalType_ThenPropertiesShouldBeInitialized()
    {
        // Arrange
        var jurnalId = "JRN001";
        var tglJurnal = DateTime.Now;
        var auditInfo = AuditInfoType.Default;
        var keterangan = new JurnalKetType("Test Jurnal", "REF001", "REF002", "REF003");
        var reg = RegModel.Default.ToReff();
        var pasien = PasienModel.Default.ToReff();
        var listJurnal2 = Array.Empty<Jurnal2Base>();

        // Act
        var jurnal = new JurnalType(jurnalId, tglJurnal, auditInfo, keterangan, reg, pasien, listJurnal2);

        // Assert
        jurnal.JurnalId.Should().Be(jurnalId);
        jurnal.TglJurnal.Should().Be(tglJurnal);
        jurnal.AuditInfo.Should().Be(auditInfo);
        jurnal.Keterangan.Should().Be(keterangan);
        jurnal.Reg.Should().Be(reg);
        jurnal.Pasien.Should().Be(pasien);
        jurnal.ListJurnal2.Should().BeEmpty();
    }

    [Fact]
    public void GivenJurnalWithDetails_WhenCreatingJurnalType_ThenListJurnal2ShouldBePopulated()
    {
        // Arrange
        var jurnalId = "JRN002";
        var tglJurnal = DateTime.Now;
        var jurnal2Detail = new Jurnal2JasaType(1,
            new Jurnal2NilaiType("REK001", "Test Detail", 1000m, 0m),
            new Jurnal2RLType(new UnitReff("UN001", ""), new JkReff("JK1", "")),
            "", "", "");

        var listJurnal2 = new[] { jurnal2Detail };

        // Act
        var jurnal = new JurnalType(jurnalId, tglJurnal, AuditInfoType.Default,
            JurnalKetType.Default, RegModel.Default.ToReff(),
            PasienModel.Default.ToReff(), listJurnal2);

        // Assert
        jurnal.ListJurnal2.Should().ContainSingle();
        jurnal.ListJurnal2.First().NoUrut.Should().Be(1);
    }

    [Fact]
    public void GivenJurnalTypeInstance_WhenAddingJurnal2_ThenListShouldContainNewItem()
    {
        // Arrange
        var jurnal = new JurnalType("JRN003", DateTime.Now, AuditInfoType.Default,
            JurnalKetType.Default, RegModel.Default.ToReff(),
            PasienModel.Default.ToReff(), Array.Empty<Jurnal2Base>());

        var newJurnal2 = new Jurnal2JasaType(1,
            new Jurnal2NilaiType("REK001", "New Detail", 2000m, 0m),
            new Jurnal2RLType(new UnitReff("UN001", ""), new JkReff("JK1", "")),
            "", "", "");

        // Act
        jurnal.AddJurnal2(newJurnal2);

        // Assert
        jurnal.ListJurnal2.Should().ContainSingle();
        jurnal.ListJurnal2.First().NoUrut.Should().Be(1);
    }

    [Fact]
    public void GivenJurnalTypeInstance_WhenAddingMultipleJurnal2_ThenListShouldContainAllItems()
    {
        // Arrange
        var jurnal = new JurnalType("JRN004", DateTime.Now, AuditInfoType.Default,
            JurnalKetType.Default, RegModel.Default.ToReff(),
            PasienModel.Default.ToReff(), Array.Empty<Jurnal2Base>());

        var jurnal2Item1 = new Jurnal2JasaType(1,
            new Jurnal2NilaiType("REK001", "Detail 1", 1000m, 0m),
            new Jurnal2RLType(new UnitReff("UN001", ""), new JkReff("JK1", "")),
            "", "", "");

        var jurnal2Item2 = new Jurnal2JasaType(2,
            new Jurnal2NilaiType("REK002", "Detail 2", 2000m, 0m),
            new Jurnal2RLType(new UnitReff("UN002", ""), new JkReff("JK2", "")),
            "", "", "");

        // Act
        jurnal.AddJurnal2(jurnal2Item1);
        jurnal.AddJurnal2(jurnal2Item2);

        // Assert
        jurnal.ListJurnal2.Should().HaveCount(2);
        jurnal.ListJurnal2.ElementAt(0).NoUrut.Should().Be(1);
        jurnal.ListJurnal2.ElementAt(1).NoUrut.Should().Be(2);
    }

    [Fact]
    public void GivenDefaultJurnalType_WhenAccessingStaticProperty_ThenShouldReturnDefaultInstance()
    {
        // Act
        var defaultJurnal = JurnalType.Default;

        // Assert
        defaultJurnal.JurnalId.Should().Be("-");
        defaultJurnal.TglJurnal.Should().Be(DateTime.MinValue);
        defaultJurnal.AuditInfo.Should().Be(AuditInfoType.Default);
        defaultJurnal.Keterangan.Should().Be(JurnalKetType.Default);
    }

    [Fact]
    public void GivenJurnalId_WhenUsingKeyMethod_ThenShouldReturnJurnalWithSpecifiedId()
    {
        // Arrange
        var jurnalId = "JRN999";

        // Act
        var jurnalKey = JurnalType.Key(jurnalId);

        // Assert
        jurnalKey.JurnalId.Should().Be(jurnalId);
    }
}

public class JurnalType_CreateFromTrsBilling_Test
{
    [Fact]
    public void GivenTrsBillingWithJasaPdp_WhenCreateFromTrsBilling_ThenJurnalShouldContainPendapatanAndPiutang()
    {
        // Arrange
        var trsBilling = CreateSampleTrsBillingWithJasaPdp();
        var layanan = CreateSampleLayanan();
        var mapJaminanJk = CreateSampleMapJaminanJk();

        // Act
        var jurnal = JurnalType.CreateFromTrsBilling(trsBilling, layanan, mapJaminanJk);

        // Assert
        jurnal.JurnalId.Should().Be(trsBilling.TrsBillingId);
        jurnal.Reg.RegId.Should().Be(trsBilling.Reg.RegId);
        jurnal.Pasien.PasienId.Should().Be(trsBilling.Reg.PasienName); // Sesuai dengan implementasi

        var jurnal2List = jurnal.ListJurnal2.ToList();
        jurnal2List.Should().HaveCount(2); // 1 pendapatan + 1 piutang

        var pendapatanJurnal = jurnal2List[0] as Jurnal2JasaType;
        pendapatanJurnal.Should().NotBeNull();
        pendapatanJurnal!.NilaiJurnal.Uraian.Should().Contain("Pendapatan");
        pendapatanJurnal.NilaiJurnal.NilaiK.Should().Be(50000m); // NilaiP dari billing
        pendapatanJurnal.NilaiJurnal.NilaiD.Should().Be(0m);

        var piutangJurnal = jurnal2List[1] as Jurnal2JasaType;
        piutangJurnal.Should().NotBeNull();
        piutangJurnal!.NilaiJurnal.Uraian.Should().Contain("Piutang Pasien Dalam Perawatan");
        piutangJurnal.NilaiJurnal.NilaiD.Should().Be(50000m); // NilaiP dari billing
        piutangJurnal.NilaiJurnal.NilaiK.Should().Be(0m);
    }

    [Fact]
    public void GivenTrsBillingWithJasaPotongan_WhenCreateFromTrsBilling_ThenJurnalShouldContainPotongan()
    {
        // Arrange
        var trsBilling = CreateSampleTrsBillingWithJasaPotongan();
        var layanan = CreateSampleLayanan();
        var mapJaminanJk = CreateSampleMapJaminanJk();

        // Act
        var jurnal = JurnalType.CreateFromTrsBilling(trsBilling, layanan, mapJaminanJk);

        // Assert
        var jurnal2List = jurnal.ListJurnal2.ToList();
        jurnal2List.Should().HaveCount(2); // 1 potongan + 1 piutang

        var potonganJurnal = jurnal2List[0] as Jurnal2JasaType;
        potonganJurnal.Should().NotBeNull();
        potonganJurnal!.NilaiJurnal.Uraian.Should().Contain("Potongan Pendapatan");
        potonganJurnal.NilaiJurnal.NilaiD.Should().Be(-2000m); // Negatif dari NilaiP
        potonganJurnal.NilaiJurnal.NilaiK.Should().Be(0m);
    }

    [Fact]
    public void GivenTrsBillingWithObatPdp_WhenCreateFromTrsBilling_ThenJurnalShouldContainObatPendapatan()
    {
        // Arrange
        var trsBilling = CreateSampleTrsBillingWithObatPdp();
        var layanan = CreateSampleLayanan();
        var mapJaminanJk = CreateSampleMapJaminanJk();

        // Act
        var jurnal = JurnalType.CreateFromTrsBilling(trsBilling, layanan, mapJaminanJk);

        // Assert
        var jurnal2List = jurnal.ListJurnal2.ToList();
        jurnal2List.Should().HaveCount(2); // 1 pendapatan obat + 1 piutang

        var pendapatanObatJurnal = jurnal2List[0] as Jurnal2ObatType;
        pendapatanObatJurnal.Should().NotBeNull();
        pendapatanObatJurnal!.NilaiJurnal.Uraian.Should().Contain("Pendapatan");
        pendapatanObatJurnal.NilaiJurnal.NilaiK.Should().Be(25000m);
        pendapatanObatJurnal.NilaiJurnal.NilaiD.Should().Be(0m);
    }

    [Fact]
    public void GivenTrsBillingWithObatTax_WhenCreateFromTrsBilling_ThenJurnalShouldContainTaxEntry()
    {
        // Arrange
        var trsBilling = CreateSampleTrsBillingWithObatTax();
        var layanan = CreateSampleLayanan();
        var mapJaminanJk = CreateSampleMapJaminanJk();

        // Act
        var jurnal = JurnalType.CreateFromTrsBilling(trsBilling, layanan, mapJaminanJk);

        // Assert
        var jurnal2List = jurnal.ListJurnal2.ToList();
        jurnal2List.Should().HaveCount(2); // 1 pajak + 1 piutang

        var taxJurnal = jurnal2List[0] as Jurnal2ObatType;
        taxJurnal.Should().NotBeNull();
        taxJurnal!.NilaiJurnal.Uraian.Should().Contain("Pajak");
        taxJurnal.NilaiJurnal.NilaiK.Should().Be(2500m);
        taxJurnal.NilaiJurnal.NilaiD.Should().Be(0m);
    }

    [Fact]
    public void GivenTrsBillingWithObatRetur_WhenCreateFromTrsBilling_ThenJurnalShouldContainReturEntry()
    {
        // Arrange
        var trsBilling = CreateSampleTrsBillingWithObatRetur();
        var layanan = CreateSampleLayanan();
        var mapJaminanJk = CreateSampleMapJaminanJk();

        // Act
        var jurnal = JurnalType.CreateFromTrsBilling(trsBilling, layanan, mapJaminanJk);

        // Assert
        var jurnal2List = jurnal.ListJurnal2.ToList();
        jurnal2List.Should().HaveCount(2); // 1 retur + 1 piutang

        var returJurnal = jurnal2List[0] as Jurnal2ObatType;
        returJurnal.Should().NotBeNull();
        returJurnal!.NilaiJurnal.Uraian.Should().Contain("Retur");
        returJurnal.NilaiJurnal.NilaiD.Should().Be(3000m); // Absolute value
        returJurnal.NilaiJurnal.NilaiK.Should().Be(0m);
    }

    [Fact]
    public void GivenTrsBillingWithObatBylPositive_WhenCreateFromTrsBilling_ThenJurnalShouldContainBylPendapatan()
    {
        // Arrange
        var trsBilling = CreateSampleTrsBillingWithObatBylPositive();
        var layanan = CreateSampleLayanan();
        var mapJaminanJk = CreateSampleMapJaminanJk();

        // Act
        var jurnal = JurnalType.CreateFromTrsBilling(trsBilling, layanan, mapJaminanJk);

        // Assert
        var jurnal2List = jurnal.ListJurnal2.ToList();
        jurnal2List.Should().HaveCount(2); // 1 byl + 1 piutang

        var bylJurnal = jurnal2List[0] as Jurnal2ObatType;
        bylJurnal.Should().NotBeNull();
        bylJurnal!.NilaiJurnal.Uraian.Should().Contain("Pendapatan Biaya+");
        bylJurnal.NilaiJurnal.NilaiK.Should().Be(1500m);
        bylJurnal.NilaiJurnal.NilaiD.Should().Be(0m);
    }

    [Fact]
    public void GivenTrsBillingWithObatBylNegative_WhenCreateFromTrsBilling_ThenJurnalShouldContainBylRetur()
    {
        // Arrange
        var trsBilling = CreateSampleTrsBillingWithObatBylNegative();
        var layanan = CreateSampleLayanan();
        var mapJaminanJk = CreateSampleMapJaminanJk();

        // Act
        var jurnal = JurnalType.CreateFromTrsBilling(trsBilling, layanan, mapJaminanJk);

        // Assert
        var jurnal2List = jurnal.ListJurnal2.ToList();
        jurnal2List.Should().HaveCount(2); // 1 byl retur + 1 piutang

        var bylJurnal = jurnal2List[0] as Jurnal2ObatType;
        bylJurnal.Should().NotBeNull();
        bylJurnal!.NilaiJurnal.Uraian.Should().Contain("Retur Biaya+");
        bylJurnal.NilaiJurnal.NilaiD.Should().Be(1000m); // Absolute value
        bylJurnal.NilaiJurnal.NilaiK.Should().Be(0m);
    }

    private static TrsBillingType CreateSampleTrsBillingWithJasaPdp()
    {
        var reg = new RegReff("REG001", "MR001", "John Doe");
        var layanan = new LayananReff("LY001", "Layanan Umum");
        var kelas = new KelasType("KL1", "Kelas 1", true, new KelasDkType("1","Kelas Dk 1")).ToReff();
        var auditInfo = new AuditInfoType("USR001", "2026-01-01", "09:00:00");
        var rekapCetak = new RekapCetakType("RC1", "Rekap Cetak", 1, true, 1, 
            new GroupRekapCetakType("1","Group Rekap Cetak"), new RekapCetakDkType("2", "Rekap Dk")).ToReff();
        var keterangan = new TrsBillKetType("Pemeriksaan Dokter", "Ket 2", "REF001", 1, "MAIN001");

        var trsBilling = new TrsBillingType(
            "JRN001", 1, DateTime.Now,
            reg, layanan, kelas,
            auditInfo, 50000m, 0m, 0m, 0m, rekapCetak,
            keterangan, Array.Empty<TrsBilling2Base>());

        var ppa = new PpaReff("PPA001", "Dokter Umum");
        var kasir = new PegType("KSR001", "Kasir Utama");
        var komponen = new KomponenReff("K01", "Konsultasi Dokter");
        var rekening = new RekJasaType("REK_PPDP", "REK_PDPT", "REK_DISC");

        var trsBilling2Jasa = new TrsBilling2JasaType(
            1, "PDP", DateTime.Now,
            new NilaiBillingType("PDP", 50000m, 0m), ppa, kasir,
            komponen, rekening);

        trsBilling.AddTrsBilling2(trsBilling2Jasa);

        return trsBilling;
    }

    private static TrsBillingType CreateSampleTrsBillingWithJasaPotongan()
    {
        var reg = new RegReff("REG002", "MR002", "Jane Doe");
        var layanan = new LayananReff("LYN002", "Layanan Spesialis");
        var kelas = new KelasType("KL2", "Kelas 2", true, new KelasDkType("2", "Kelas Dk 2")).ToReff();
        var auditInfo = new AuditInfoType("USR002", "2026-01-02", "10:00:00");
        var rekapCetak = new RekapCetakType("RC2", "Rekap Cetak 2", 2, true, 1,
            new GroupRekapCetakType("2", "Group Rekap Cetak 2"), new RekapCetakDkType("3", "Rekap Dk 2")).ToReff();
        var keterangan = new TrsBillKetType("Diskon Karyawan", "Ket 2", "REF002", 1, "MAIN002");

        var trsBilling = new TrsBillingType(
            "JRN002", 2, DateTime.Now,
            reg, layanan, kelas,
            auditInfo, 2000m, 0m, 0m, 0m, rekapCetak,
            keterangan, Array.Empty<TrsBilling2Base>());

        var ppa = new PpaReff("PPA002", "Poli Spesialis");
        var kasir = new PegType("KSR002", "Kasir 2");
        var komponen = new KomponenReff("K02", "Pemeriksaan");
        var rekening = new RekJasaType("REK_PPDP", "REK_PDPT", "REK_DISC");

        var trsBilling2Jasa = new TrsBilling2JasaType(
            1, "POT", DateTime.Now,
            new NilaiBillingType("POT", 2000m, 0m), ppa, kasir,
            komponen, rekening);

        trsBilling.AddTrsBilling2(trsBilling2Jasa);

        return trsBilling;
    }

    private static TrsBillingType CreateSampleTrsBillingWithObatPdp()
    {
        var reg = new RegReff("REG003", "MR003", "Bob Smith");
        var layanan = new LayananReff("LYN003", "Farmasi");
        var kelas = new KelasType("KL3", "Kelas 3", true, new KelasDkType("3", "Kelas Dk 3")).ToReff();
        var auditInfo = new AuditInfoType("USR003", "2026-01-03", "11:00:00");
        var rekapCetak = new RekapCetakType("RC3", "Rekap Cetak 3", 31, true, 1,
            new GroupRekapCetakType("1", "Group Rekap Cetak"), new RekapCetakDkType("2", "Rekap Dk")).ToReff();
        var keterangan = new TrsBillKetType("Pembelian Obat", "Ket 3", "REF003", 1, "MAIN003");

        var trsBilling = new TrsBillingType(
            "JRN003", 3, DateTime.Now,
            reg, layanan, kelas,
            auditInfo, 25000m, 0m, 0m, 0m, rekapCetak,
            keterangan, Array.Empty<TrsBilling2Base>());

        var kasir = new PegType("KSR003", "Kasir 3");
        var groupRek = new GroupRekReff("GR001", "Group Obat");
        var rekening = new RekObatType("REK_PPDP", "REK_PDPT", "REK_DISC", "REK_LAIN", "REK_PERS", "REK_TAX", "REK_RETUR");

        var trsBilling2Obat = new TrsBilling2ObatType(
            1, "PDP", DateTime.Now,
            new NilaiBillingType("PDP", 25000m, 0m), kasir,
            groupRek, rekening);

        trsBilling.AddTrsBilling2(trsBilling2Obat);

        return trsBilling;
    }

    private static TrsBillingType CreateSampleTrsBillingWithObatTax()
    {
        var reg = new RegReff("REG004", "MR004", "Alice Johnson");
        var layanan = new LayananReff("LYN004", "Farmasi");
        var kelas = new KelasType("KL1", "Kelas 1", true, new KelasDkType("1", "Kelas Dk 1")).ToReff();
        var auditInfo = new AuditInfoType("USR004", "2026-01-04", "12:00:00");
        var rekapCetak = new RekapCetakType("RC4", "Rekap Cetak 4", 41, true, 1,
            new GroupRekapCetakType("1", "Group Rekap Cetak 1"), new RekapCetakDkType("1", "Rekap Dk 1")).ToReff();
        var keterangan = new TrsBillKetType("Pajak Obat", "Ket 4", "REF004", 1, "MAIN004");

        var trsBilling = new TrsBillingType(
            "JRN004", 4, DateTime.Now,
            reg, layanan, kelas,
            auditInfo, 2500m, 0m, 0m, 0m, rekapCetak,
            keterangan, Array.Empty<TrsBilling2Base>());

        var kasir = new PegType("KSR004", "Kasir 4");
        var groupRek = new GroupRekReff("GR002", "Group Obat 2");
        var rekening = new RekObatType("REK_PPDP", "REK_PDPT", "REK_DISC", "REK_LAIN", "REK_PERS", "REK_TAX", "REK_RETUR");

        var trsBilling2Obat = new TrsBilling2ObatType(
            1, "TAX", DateTime.Now,
            new NilaiBillingType("TAX", 2500m, 0m), kasir,
            groupRek, rekening);

        trsBilling.AddTrsBilling2(trsBilling2Obat);

        return trsBilling;
    }

    private static TrsBillingType CreateSampleTrsBillingWithObatRetur()
    {
        var reg = new RegReff("REG005", "MR005", "Charlie Brown");
        var layanan = new LayananReff("LYN005", "Farmasi");
        var kelas = new KelasType("KL2", "Kelas 2", true, new KelasDkType("2", "Kelas Dk 2")).ToReff();
        var auditInfo = new AuditInfoType("USR005", "2026-01-05", "13:00:00");
        var rekapCetak = new RekapCetakType("RC5", "Rekap Cetak 5", 5, true, 1,
            new GroupRekapCetakType("5", "Group Rekap Cetak 5"), new RekapCetakDkType("3", "Rekap Dk 3")).ToReff();
        var keterangan = new TrsBillKetType("Retur Obat", "Ket 5", "REF005", 1, "MAIN005");

        var trsBilling = new TrsBillingType(
            "JRN005", 5, DateTime.Now,
            reg, layanan, kelas,
            auditInfo, -3000m, 0m, 0m, 0m, rekapCetak,
            keterangan, Array.Empty<TrsBilling2Base>());

        var kasir = new PegType("KSR005", "Kasir 5");
        var groupRek = new GroupRekReff("GR003", "Group Obat 3");
        var rekening = new RekObatType("REK_PPDP", "REK_PDPT", "REK_DISC", "REK_LAIN", "REK_PERS", "REK_TAX", "REK_RETUR");

        var trsBilling2Obat = new TrsBilling2ObatType(
            1, "RET", DateTime.Now,
            new NilaiBillingType("RET", -3000m, 0m), kasir,
            groupRek, rekening);

        trsBilling.AddTrsBilling2(trsBilling2Obat);

        return trsBilling;
    }

    private static TrsBillingType CreateSampleTrsBillingWithObatBylPositive()
    {
        var reg = new RegReff("REG006", "MR006", "Diana Prince");
        var layanan = new LayananReff("LYN006", "Laboratorium");
        var kelas = new KelasType("KL1", "Kelas 1", true, new KelasDkType("1", "Kelas Dk 1")).ToReff();
        var auditInfo = new AuditInfoType("USR006", "2026-01-06", "14:00:00");
        var rekapCetak = new RekapCetakType("RC6", "Rekap Cetak 6", 6, true, 1,
            new GroupRekapCetakType("5", "Group Rekap Cetak 5"), new RekapCetakDkType("3", "Rekap Dk 3")).ToReff();
        var keterangan = new TrsBillKetType("Biaya Lain", "Ket 6", "REF006", 1, "MAIN006");

        var trsBilling = new TrsBillingType(
            "JRN006", 6, DateTime.Now,
            reg, layanan, kelas,
            auditInfo, 1500m, 0m, 0m, 0m, rekapCetak,
            keterangan, Array.Empty<TrsBilling2Base>());

        var kasir = new PegType("KSR006", "Kasir 6");
        var groupRek = new GroupRekReff("GR004", "Group Biaya");
        var rekening = new RekObatType("REK_PPDP", "REK_PDPT", "REK_DISC", "REK_LAIN", "REK_PERS", "REK_TAX", "REK_RETUR");

        var trsBilling2Obat = new TrsBilling2ObatType(
            1, "BYL", DateTime.Now,
            new NilaiBillingType("BYL", 1500m, 0m), kasir,
            groupRek, rekening);

        trsBilling.AddTrsBilling2(trsBilling2Obat);

        return trsBilling;
    }

    private static TrsBillingType CreateSampleTrsBillingWithObatBylNegative()
    {
        var reg = new RegReff("REG007", "MR007", "Eve Wilson");
        var layanan = new LayananReff("LYN007", "Laboratorium");
        var kelas = new KelasType("KL1", "Kelas 1", true, new KelasDkType("1", "Kelas Dk 1")).ToReff();
        var auditInfo = new AuditInfoType("USR007", "2026-01-07", "15:00:00");
        var rekapCetak = new RekapCetakType("RC7", "Rekap Cetak 7", 7, true, 1, 
            new GroupRekapCetakType("5", "Group Rekap Cetak 5"), new RekapCetakDkType("3", "Rekap Dk 3")).ToReff();
        var keterangan = new TrsBillKetType("Retur Biaya", "Ket 7", "REF007", 1, "MAIN007");

        var trsBilling = new TrsBillingType(
            "JRN007", 7, DateTime.Now,
            reg, layanan, kelas,
            auditInfo, -1000m, 0m, 0m, 0m, rekapCetak,
            keterangan, Array.Empty<TrsBilling2Base>());

        var kasir = new PegType("KSR007", "Kasir 7");
        var groupRek = new GroupRekReff("GR005", "Group Biaya");
        var rekening = new RekObatType("REK_PPDP", "REK_PDPT", "REK_DISC", "REK_LAIN", "REK_PERS", "REK_TAX", "REK_RETUR");

        var trsBilling2Obat = new TrsBilling2ObatType(
            1, "BYL", DateTime.Now,
            new NilaiBillingType("BYL", -1000m, 0m), kasir,
            groupRek, rekening);

        trsBilling.AddTrsBilling2(trsBilling2Obat);

        return trsBilling;
    }

    private static LayananType CreateSampleLayanan()
    {
        var unitPcc = new UnitReff("UN001", "Unit PCC");
        return new LayananType("LY001", "Layanan Umum", true,
            new InstalasiReff("INT01", "Instalasi 1"),
            new LayananDkType("LK2", "Layanan Dk 2",1,2,3,4,5,6,7).ToReff(),
            new TipeLayananDkType(TipeLayananDkType.Default.TipeLayananDkId, TipeLayananDkType.Default.TipeLayananDkName),
            new InstalasiDkType(InstalasiDkType.Default.InstalasiDkId, InstalasiDkType.Default.InstalasiDkName),
            unitPcc,
            new PoliBpjsReff("-", "-"));
    }

    private static MapJaminanJkType CreateSampleMapJaminanJk()
    {
        return new MapJaminanJkType("JMN01", "JK1");
    }
}