using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Infrastructure.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Moq;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.RoomChargeSub.BedAgg;

public class BedDalTest
{
    private readonly BedDal _sut;
    private readonly Mock<IBangsalKey> _bangsalKey;

    public BedDalTest()
    {
        _sut = new BedDal(ConnStringHelper.GetTestEnv());
        _bangsalKey = new Mock<IBangsalKey>();
    }
    
    [Fact]
    public void Insert_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new BedDto();
        expected.SetTestDataBed();
        _sut.Insert(expected);
    }
    
    [Fact]
    public void Update_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new BedDto();
        expected.SetTestDataBed();
        _sut.Update(expected);
    }
    
    [Fact]
    public void Delete_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new BedDto();
        expected.SetTestDataBed();
        _sut.Delete(expected);
    }

    [Fact]
    public void GetQuery_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new BedDto();
        expected.SetTestDataBed();
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void ListData_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new BedDto();
        expected.SetTestDataBed();
        _sut.Insert(expected);
        _bangsalKey.Setup(x => x.BangsalId)
            .Returns(expected.BangsalId);
        var actual = _sut.ListData(_bangsalKey.Object);
        actual.Should().ContainEquivalentOf(expected);
    }
    
}