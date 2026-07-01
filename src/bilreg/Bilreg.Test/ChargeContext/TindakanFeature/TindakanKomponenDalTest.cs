using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanKomponenDalTest
{
    private readonly TindakanKomponenDal _sut = new(ConnStringHelper.GetTestEnv());
    
    private static TindakanKomponenDto Faker()
        => new TindakanKomponenDto("TND001",  1, "KTR", "Komponen Tarif A",
                "PPA001", "Ppa A",  2, 75000, 150000);

    private static ITindakanKey FakerKey()
        => TindakanModel.Key("TND001");
    
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
        var dto = Faker();
        _sut.Insert([dto]);
        var actual = _sut.ListData(FakerKey());
        actual.Should().ContainEquivalentOf(dto);
    }
}