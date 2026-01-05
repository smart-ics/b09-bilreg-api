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
            CrtDate: new DateTime(2025,2,3),
            UpdUser: "USR001",
            UpdDate: new DateTime(2025,2,4),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1)
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
        var actual = _sut.ListData(FakerRegKey());
        actual.Should().ContainEquivalentOf(Faker());
    }
}