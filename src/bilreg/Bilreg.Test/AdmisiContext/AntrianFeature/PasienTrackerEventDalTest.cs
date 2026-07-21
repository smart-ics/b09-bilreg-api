using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public class PasienTrackerEventDalTest
{
    private readonly PasienTrackerEventDal _sut = new PasienTrackerEventDal(ConnStringHelper.GetTestEnv());

    private static PasienTrackerEventDto Faker()
        => new PasienTrackerEventDto("A", 2, "B", new DateTime(2025, 1, 2), "C");
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
        _sut.Delete("A", 2);
    }
    [Fact]
    public void UT4_DeleteAllTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }
    [Fact]
    public void UT5_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData("A", 2);
        actual.Should().BeEquivalentTo(Faker());
    }
    [Fact]
    public void UT6_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(FakerKey());
        actual.Should().ContainEquivalentOf(Faker());
    }
}