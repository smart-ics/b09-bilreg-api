using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianEntryDalTest
{
    private readonly AntrianEntryDal _sut = new(ConnStringHelper.GetTestEnv());

    private static AntrianEntryDto Faker()
        => new AntrianEntryDto("A", 1, "B", "C", 2,
            new DateTime(2025, 10, 1),
            new DateTime(2025, 10, 2),
            new DateTime(2025, 10, 3),
            "D", "E") ;
    private static IAntrianKey Key() => AntrianModel.Key("A");

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
        _sut.Delete(Key(), 1);
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(Key(), 1);
        actual.Should().BeEquivalentTo(Faker());
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(Key());
        actual.Should().ContainEquivalentOf(Faker());
    }

    [Fact]
    public void UT6_UpdateFromAnonymousInService_IsCompareAndSet()
    {
        using var trans = TransHelper.NewScope();
        var createdAt = new DateTime(2025, 10, 1, 8, 0, 0);
        var servedAt = createdAt.AddMinutes(5);
        var anonymous = new AntrianEntryDto(
            "CAS-ADM", 1, "", "-", (int)AntrianStatusEnum.InService,
            createdAt, servedAt, new DateTime(3000, 1, 1), "-", "-");
        _sut.Insert(anonymous);
        var identified = anonymous with
        {
            PersonName = "SINTA",
            PasienTrackerId = "01JTRACKER0000000000000000"
        };

        _sut.UpdateFromAnonymousInService(identified).Should().Be(1);
        _sut.UpdateFromAnonymousInService(identified).Should().Be(0);
    }

    [Fact]
    public void UT7_UpdateWaitingToInService_IsCompareAndSet()
    {
        using var trans = TransHelper.NewScope();
        var createdAt = new DateTime(2025, 10, 1, 8, 0, 0);
        var waiting = new AntrianEntryDto(
            "CAS-WAIT", 1, "", "-", (int)AntrianStatusEnum.Waiting,
            createdAt, new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), "-", "-");
        _sut.Insert(waiting);
        var inService = waiting with
        {
            AntrianStatus = (int)AntrianStatusEnum.InService,
            ServedAt = createdAt.AddMinutes(5)
        };

        _sut.UpdateWaitingToInService(inService).Should().Be(1);
        _sut.UpdateWaitingToInService(inService).Should().Be(0);
    }

    [Fact]
    public void UT8_UpdateInServiceToDone_IsCompareAndSet()
    {
        using var trans = TransHelper.NewScope();
        var createdAt = new DateTime(2025, 10, 1, 8, 0, 0);
        var servedAt = createdAt.AddMinutes(5);
        var trackerId = "01JTRACKER0000000000000001";
        var inService = new AntrianEntryDto(
            "CAS-DONE", 1, "SINTA", trackerId, (int)AntrianStatusEnum.InService,
            createdAt, servedAt, new DateTime(3000, 1, 1), "-", "-");
        _sut.Insert(inService);
        var done = inService with
        {
            AntrianStatus = (int)AntrianStatusEnum.Done,
            DoneAt = servedAt.AddMinutes(10)
        };

        _sut.UpdateInServiceToDone(done).Should().Be(1);
        _sut.UpdateInServiceToDone(done).Should().Be(0);
    }

    [Fact]
    public void UT9_UpdateWaitingToInService_DoesNotDeleteSiblingEntry()
    {
        using var trans = TransHelper.NewScope();
        var createdAt = new DateTime(2025, 10, 1, 8, 0, 0);
        var sibling = new AntrianEntryDto(
            "CAS-SIB", 1, "OTHER", "-", (int)AntrianStatusEnum.Waiting,
            createdAt, new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), "-", "-");
        var target = new AntrianEntryDto(
            "CAS-SIB", 2, "", "-", (int)AntrianStatusEnum.Waiting,
            createdAt, new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), "-", "-");
        _sut.Insert(sibling);
        _sut.Insert(target);

        var inService = target with
        {
            AntrianStatus = (int)AntrianStatusEnum.InService,
            ServedAt = createdAt.AddMinutes(5)
        };
        _sut.UpdateWaitingToInService(inService).Should().Be(1);

        _sut.GetData(AntrianModel.Key("CAS-SIB"), 1).Should().BeEquivalentTo(sibling);
        _sut.GetData(AntrianModel.Key("CAS-SIB"), 2).AntrianStatus
            .Should().Be((int)AntrianStatusEnum.InService);
    }
}
