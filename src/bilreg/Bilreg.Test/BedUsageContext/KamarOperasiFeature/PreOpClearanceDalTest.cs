using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class PreOpClearanceDalTest
{
    private readonly PreOpClearanceDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PreOpClearanceDto Faker(
        string clearanceId = "CLR-TEST-001",
        string scheduleOpId = "SCH-TEST-001",
        int noUrut = 1,
        DateTime? vodDate = null)
        => new(
            ClearanceId: clearanceId,
            ScheduleOpId: scheduleOpId,
            OrderOpId: "ORD-TEST-001",
            RegId: "REG-001",
            NoUrut: noUrut,
            ClearanceStatus: (int)PreOpClearanceStatusEnum.PendingClearance,
            VerifikatorPpaId: "PPA-VER-01",
            DokterPjId: "PPA-DOK-01",
            CompletedAt: new DateTime(3000, 1, 1),
            DokterOverrideId: "",
            AlasanOverride: "",
            AlasanNotCleared: "",
            CrtUser: "CRT-USR",
            CrtDate: new DateTime(2026, 11, 24, 8, 0, 0),
            UpdUser: "UPD-USR",
            UpdDate: new DateTime(2026, 11, 24, 8, 15, 0),
            VodUser: vodDate.HasValue ? "VOD-USR" : "",
            VodDate: vodDate ?? new DateTime(3000, 1, 1)
        );

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
        var dto = Faker();
        _sut.Insert(dto);

        var updated = dto with
        {
            ClearanceStatus = (int)PreOpClearanceStatusEnum.Cleared,
            CompletedAt = new DateTime(2026, 11, 24, 9, 0, 0),
            UpdUser = "UPD-USR-2",
            UpdDate = new DateTime(2026, 11, 24, 9, 0, 0)
        };
        _sut.Update(updated);

        var actual = _sut.GetData(dto.ClearanceId);
        actual.Should().BeEquivalentTo(updated);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = Faker();
        _sut.Insert(expected);

        var actualById = _sut.GetData(expected.ClearanceId);
        var actualByKey = _sut.GetData(PreOpClearanceModel.Key(expected.ClearanceId));

        actualById.Should().BeEquivalentTo(expected);
        actualByKey.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetLatestByScheduleOpIdTest()
    {
        using var trans = TransHelper.NewScope();
        var item1 = Faker("CLR-TEST-001", "SCH-TEST-LATEST", 1);
        var item2 = Faker("CLR-TEST-002", "SCH-TEST-LATEST", 2);
        var item3Voided = Faker("CLR-TEST-003", "SCH-TEST-LATEST", 3, new DateTime(2026, 11, 24, 10, 0, 0));

        _sut.Insert(item1);
        _sut.Insert(item2);
        _sut.Insert(item3Voided);

        var actual = _sut.GetLatestByScheduleOpId("SCH-TEST-LATEST");

        actual.Should().BeEquivalentTo(item2);
    }

    [Fact]
    public void CountByScheduleOpIdTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker("CLR-TEST-001", "SCH-TEST-COUNT", 1));
        _sut.Insert(Faker("CLR-TEST-002", "SCH-TEST-COUNT", 2));
        _sut.Insert(Faker("CLR-TEST-003", "SCH-TEST-COUNT", 3, new DateTime(2026, 11, 24, 10, 0, 0)));

        var actual = _sut.CountByScheduleOpId("SCH-TEST-COUNT");

        actual.Should().Be(2);
    }
}
