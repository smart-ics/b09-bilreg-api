using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanDalTest
{
    private readonly TindakanDal _sut = new(ConnStringHelper.GetTestEnv());
    private static TindakanDto Faker()
        => new TindakanDto(
            TindakanId: "TND001",
            TindakanDate: new DateTime(2025, 12, 30),
            OrderTdkId: "ORD001",
            RegId: "REG001",
            PasienId: "PAS001",
            PasienName: "Pasien A",
            LayananId: "LAY01",
            LayananName: "Layanan A",
            KelasId: "K1",
            KelasName: "Kelas A",
            TipeTarifId: "TT",
            TipeTarifName: "Tipe Tarif A",
            TarifId: "TRF001",
            TarifName: "Tarif A",
            Total: 150000,
            CrtUser: "USR001",
            CrtDate: new DateTime(2025, 2, 3),
            UpdUser: "USR001",
            UpdDate: new DateTime(2025, 2, 4),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1),
            PakaiBedId: "BED001",
            Qty: 2
        );
    
    private static ITindakanKey FakerKey()
        => TindakanModel.Key("TND001");

    private static IRegKey FakerRegKey()
        => RegModel.Key("REG001");
    
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
        _sut.Insert(Faker());
        var updated = Faker() with { PakaiBedId = "BED999", Qty = 5 };
        _sut.Update(updated);
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(updated);
        actual.PakaiBedId.Should().Be("BED999");
        actual.Qty.Should().Be(5);
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
        actual.PakaiBedId.Should().Be("BED001");
        actual.Qty.Should().Be(2);
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(FakerRegKey());
        actual.Should().ContainEquivalentOf(Faker());
    }

    [Fact]
    public void ListTdkJualTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListTdkJual(FakerRegKey()).ToList();
        var item = actual.FirstOrDefault(x => x.TransaksiId == "TND001");
        item.Should().NotBeNull();
        item!.Qty.Should().Be(2);
        item.PakaiBedId.Should().Be("BED001");
    }
}
