using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.BrgContext.StandardFeature;
using Farinv.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Test.BrgContext.StandardFeature;

public class FornasDalTest
{
    private readonly FornasDal _sut = new(ConnStringHelper.GetTestEnv());

    private static FornasDto Faker()
        => new FornasDto(
            FornasId: "F0001",
            FornasName: "Obat Fornas A",
            KelasTerapi: "KT1",
            KelasTerapi1: "KT1-1",
            KelasTerapi2: "KT1-2",
            KelasTerapi3: "KT1-3",
            NamaObat: "Paracetamol",
            Sediaan: "Tablet",
            Kekuatan: "500 mg",
            Satuan: "Strip",
            MaksPeresepan: "30",
            RestriksiKelasTerapi: "Hanya untuk KT1",
            RestriksiObat: "Tidak untuk anak <2 th",
            RestriksiSediaan: "Hanya tablet salut");

    private static IFornasKey FakerKey()
        => FornasType.Default with { FornasId = "F0001" };

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
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker());
    }
}
