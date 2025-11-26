using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class PasienTrackerDalTest
{
    private readonly PasienTrackerDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PasienTrackerDto Faker()
        => new PasienTrackerDto("A", "B", new DateTime(2025, 12, 1), new DateTime(2025, 12, 2));
    
    private static IPasienTrackerKey FakerKey()
        => PasienTrackerModel.Key("A");
    
    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }

    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }
    
    [Fact]
    public void UT4_GetData()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(Faker());
    }
    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(new Periode(new DateTime(2025, 12, 1), new DateTime(2025, 12, 3)));
        actual.Should().ContainEquivalentOf(Faker());
    }
}