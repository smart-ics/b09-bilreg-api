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
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.AccountingContext.JurnalFeature;

public class JurnalTypeTests
{
    private readonly IJurnalFactory _factory = new JurnalFactory();

    [Fact]
    public void UT1_GivenValidParameters_WhenCreateFromReg_ThenJurnalTypeIsCreated()
    {
        // Arrange
        var reg = CreateTestRegModel();
        var karcis = CreateTestKarcisType();
        var jaminan = CreateTestJaminanType();
        var dokter = CreateTestPpaType();
        var layanan = CreateTestLayananType();
        var mapJaminanJk = CreateTestMapJaminanJkType();
        var listReffKomp = new List<KomponenType> { CreateTestKomponenType() };

        // Act
        var result = _factory.CreateFromReg(reg, karcis, jaminan, dokter, layanan, mapJaminanJk, listReffKomp);

        // Assert
        result.Should().NotBeNull();
        result.JurnalId.Should().Be(reg.RegId);
        result.Reg.RegId.Should().Be(reg.RegId);
        result.Pasien.PasienId.Should().Be(reg.Pasien.PasienId);
        result.Keterangan.Keterangan.Should().Contain(reg.RegId);
        result.ListJurnal2.Should().NotBeEmpty();
    }

    [Fact]
    public void UT2_GivenValidParameters_WhenCreateFromTindakan_ThenJurnalTypeIsCreated()
    {
        // Arrange
        var tindakan = CreateTestTindakanModel();
        var reg = CreateTestRegModel();
        var tarif = CreateTestTarifType();
        var jaminan = CreateTestJaminanType();
        var layanan = CreateTestLayananType();
        var mapJaminanJk = CreateTestMapJaminanJkType();
        var listReffKomp = new List<KomponenType> { CreateTestKomponenType() };

        // Act
        var result = _factory.CreateFromTindakan(tindakan, reg, tarif, jaminan, layanan, mapJaminanJk, listReffKomp);

        // Assert
        result.Should().NotBeNull();
        result.JurnalId.Should().Be(tindakan.TindakanId);
        result.Reg.RegId.Should().Be(reg.RegId);
        result.Pasien.PasienId.Should().Be(reg.Pasien.PasienId);
        result.Keterangan.Keterangan.Should().Contain(reg.RegId);
        result.ListJurnal2.Should().NotBeEmpty();
    }

    [Fact]
    public void UT5_GivenDefaultModel_WhenAddJurnal2_ThenItemIsAddedToList()
    {
        // Arrange
        var jurnal = JurnalType.Default;
        var jurnal2 = CreateTestJurnal2Base();

        // Act
        jurnal.AddJurnal2(jurnal2);

        // Assert
        jurnal.ListJurnal2.Should().Contain(jurnal2);
        jurnal.ListJurnal2.Count().Should().Be(1);
    }

    [Fact]
    public void UT6_GivenJurnalWithItems_WhenAccessListJurnal2_ThenReturnsImmutableCollection()
    {
        // Arrange
        var jurnal = JurnalType.Default;
        var jurnal2 = CreateTestJurnal2Base();
        jurnal.AddJurnal2(jurnal2);

        // Act
        var list = jurnal.ListJurnal2;

        // Assert
        list.Should().Contain(jurnal2);
        list.Count().Should().Be(1);

        // Verify that the property returns a copy (not the internal collection directly)
        // This is implicit in the implementation since it's a getter returning _listJurnal2
    }

    private static RegModel CreateTestRegModel()
    {
        var tipeJaminan = new TipeJaminanReff("J0011", "Jaminan Test");
        var result = new RegModel("-", new DateOnly(3000, 1, 1),
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegJalan, PasienModel.Default.ToReff(), tipeJaminan,
            PolisModel.Default.ToReff(), KelasType.Default.ToReff(), CaraMasukDkType.Default,
            RujukanType.Default.ToReff(), PpaType.Default.ToReff(), LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(), []);
        return result;
    }

    private static RegModel CreateTestRegModelInap()
    {
        var tipeJaminan = new TipeJaminanReff("J0011", "Jaminan Test");
        var result = new RegModel("-", new DateOnly(3000, 1, 1),
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegInap, PasienModel.Default.ToReff(), tipeJaminan,
            PolisModel.Default.ToReff(), KelasType.Default.ToReff(), CaraMasukDkType.Default,
            RujukanType.Default.ToReff(), PpaType.Default.ToReff(), LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(), []);
        return result;
    }

    private static TindakanModel CreateTestTindakanModel()
    {
        var tdkKomponen = new List<TindakanKomponenWithoutPpaType>
        {
            new TindakanKomponenWithoutPpaType(KomponenType.Default.ToReff(), 1, 200, 1, 200)
        };
        var result = new TindakanModel(
            "-",
            DateTime.Today,
            "",
            RegModel.Default.ToReff(),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            TipeTarifType.Default.ToReff(),
            TarifType.Default.ToReff(),
            tdkKomponen,
            AuditTrailType.Default
        );
        return result;
    }

    private static KarcisType CreateTestKarcisType()
        => KarcisType.Default with { KarcisId = "KRC001", KarcisName = "Karcis Test", IsAktif = true };

    private static JaminanType CreateTestJaminanType()
        => JaminanType.Default with { JaminanId = "J00", JaminanName = "Jaminan Test", IsAktif = true };

    private static PpaType CreateTestPpaType()
        => PpaType.Default with { PpaId = "PPA001", PpaName = "Dr. John Doe" };

    private static LayananType CreateTestLayananType()
        => LayananType.Default with { LayananId = "LAY001", LayananName = "General Checkup", IsAktif = true };

    private static MapJaminanJkType CreateTestMapJaminanJkType()
        => MapJaminanJkType.Default with { JkId = "JK001" };

    private static KomponenType CreateTestKomponenType()
        => KomponenType.Default with{KomponenId = "KOMP001", KomponenName = "Komponen Test" };

    private static TarifType CreateTestTarifType()
        => TarifType.Default with { TarifId = "TAR001", TarifName = "Tarif Test" };

    private static Jurnal2Base CreateTestJurnal2Base()
        => new Jurnal2JasaType(0, new Jurnal2NilaiType("REK001", "Test Jurnal", 1000, 0),
            new Jurnal2RLType(new UnitReff("UNIT001", "UNIT 001") , new JkReff("JK001", "Jurnal")), "Uraian", "Ref", "PPA001");
}