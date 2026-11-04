using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanDalTest
{
    private readonly TindakanDal _sut = new(Options.Create(new DatabaseOptions
    {
        ServerName = "dev.smart-ics.com",
        DbName = "HOSPITAL_PKL"
    }));

    private static TindakanDto Faker()
        => new TindakanDto(
            TindakanId: "TND0000001",
            TindakanDate: new DateTime(2025, 12, 30),
            OrderTdkId: "ORD001",
            RegId: "RG00902457",
            PasienId: "337502200000001",
            PasienName: "AKSADARU MAHAMERU, AN",
            LayananId: "_AWAL",
            LayananName: "PERIKSA AWAL",
            KelasId: "000",
            KelasName: "NON KELAS",
            TipeTarifId: "AK",
            TipeTarifName: "ASKES",
            TarifId: "A0401101",
            TarifName: "Abdomen 1 Posisi",
            Total: 150000,
            CrtUser: "USR001",
            CrtDate: new DateTime(2025, 2, 3),
            UpdUser: "USR001",
            UpdDate: new DateTime(2025, 2, 4),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1),
            PakaiBedId: "",
            Qty: 1
        );
    
    private static ITindakanKey FakerKey()
        => TindakanModel.Key("TND0000001");

    private static IRegKey FakerRegKey()
        => RegModel.Key("RG00902457");
    
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
        var updated = Faker() with { Total = 250000, UpdUser = "USR002" };
        _sut.Update(updated);
        var actual = _sut.GetData(FakerKey());
        actual.Should().NotBeNull();
        actual.TindakanId.Should().Be("TND0000001");
        actual.Total.Should().Be(250000);
        actual.UpdUser.Should().Be("USR002");
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
        actual.Should().NotBeNull();
        actual.TindakanId.Should().Be("TND0000001");
        actual.RegId.Should().Be("RG00902457");
        actual.PasienName.Should().Be("AKSADARU MAHAMERU, AN");
        actual.LayananName.Should().Be("PERIKSA AWAL");
        actual.KelasName.Should().Be("NON KELAS");
        actual.TarifName.Should().Be("Abdomen 1 Posisi");
        actual.Total.Should().Be(150000);
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(FakerRegKey());
        actual.Should().Contain(x => x.TindakanId == "TND0000001");
    }

    [Fact]
    public void ListTdkJualTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListTdkJual(FakerRegKey()).ToList();
        var item = actual.FirstOrDefault(x => x.TransaksiId == "TND0000001");
        item.Should().NotBeNull();
        item!.Total.Should().Be(150000);
    }
}
