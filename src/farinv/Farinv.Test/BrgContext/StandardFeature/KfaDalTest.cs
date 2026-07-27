using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.BrgContext.StandardFeature;
using Farinv.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Test.BrgContext.StandardFeature;

public class KfaDalTest
{
    private readonly KfaDal _sut = new(ConnStringHelper.GetTestEnv());

    private static KfaDto Faker()
        => new KfaDto(
            KfaId: "KFA001",
            KfaName: "Obat A",
            Active: true,
            State: "Active",
            KfaTemplateId: "KT001",
            KfaTemplateName: "Template A",
            FarmalkesTypeId: "F001",
            FarmalkesTypeName: "Obat Keras",
            FarmalkesTypeGroup: "G01",
            FarmalkesHsCode: "HSC001",
            Produksi: "Lokal",
            NamaDagang: "Dagang A",
            Manufacturer: "Man A",
            Registrar: "Reg A",
            NomorIzinEdar: "NIE001",
            LkppCode: "L001",
            ControlledDrugId: "CD01",
            ControlledDrugName: "Narkotika Ringan",
            RutePemberianId: "RP001",
            RutePemberianName: "Oral",
            BentukSediaanId: "BS001",
            DosePerUnit: 500,
            UcumId: "UC001",
            UcumName: "Miligram",
            UomName: "mg",
            Generik: true,
            FixPrice: 10000,
            HetPrice: 15000,
            RxTerm: "Paracetamol 500mg",
            NetWeight: 100,
            NetWeightName: "gram",
            Volume: 50,
            VolumeName: "ml",
            Description: "Deskripsi Obat A",
            Indication: "Indikasi Obat A",
            Warning: "Peringatan Obat A",
            SideEffect: "Efek Samping Obat A",
            Tags: "obat,generik"
        );

    private static IKfaKey FakerKey()
        => KfaType.Default with { KfaId = "KFA001" };

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(filter: "Obat A");
        actual.Should().ContainEquivalentOf(Faker());
    }
}
