using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanKomponenDalTest
{
    private readonly TindakanKomponenDal _sut = new(Options.Create(new DatabaseOptions
    {
        ServerName = "dev.smart-ics.com",
        DbName = "HOSPITAL_PKL"
    }));
    
    private static TindakanKomponenDto Faker(int qty = 2)
        => new TindakanKomponenDto("TND0000001", 1, "101", "JASA DOKTER",
                "10002", "JOSEPH RACHMAT, DR.", qty, 75000, 75000 * qty);

    private static ITindakanKey FakerKey()
        => TindakanModel.Key("TND0000001");
    
    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert([Faker()]);
    }
    
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker(qty: 3);
        _sut.Insert([dto]);
        var actual = _sut.ListData(FakerKey()).ToList();
        actual.Should().NotBeEmpty();
        var item = actual.First(x => x.TindakanId == "TND0000001");
        item.KomponenTarifId.Should().Be("101");
        item.KomponenTarifName.Should().Be("JASA DOKTER");
        item.PpaId.Should().Be("10002");
        item.PpaName.Should().Be("JOSEPH RACHMAT, DR.");
        item.Qty.Should().Be(3);
        item.SubTotal.Should().Be(225000);
    }
}
