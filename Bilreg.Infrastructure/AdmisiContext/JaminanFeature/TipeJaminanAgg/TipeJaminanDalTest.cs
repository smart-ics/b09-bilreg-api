using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature.TipeJaminanAgg;

public class TipeJaminanDalTest
{
    private readonly TipeJaminanDal _sut = new(ConnStringHelper.GetTestEnv());

    private static TipeJaminanDto Faker()
        => new TipeJaminanDto("A", "B", true, "C", "D", "E", "F");

    private static ITipeJaminanKey FakerKey()
        => TipeJaminanType.Key("A");

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
        actual.Should().BeEquivalentTo(Faker(), 
            opt => opt.Excluding(x => x.fs_nm_jaminan)
                .Excluding(x => x.fs_kd_cara_bayar_dk)
                .Excluding(x => x.fs_nm_cara_bayar_dk));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt.Excluding(x => x.fs_nm_jaminan)
                .Excluding(x => x.fs_kd_cara_bayar_dk)
                .Excluding(x => x.fs_nm_cara_bayar_dk));
    }
}
