using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpHistDalTest
{
    private readonly ScheduleOpHistDal _sut = new(ConnStringHelper.GetTestEnv());

    private static ScheduleOpHistDto Faker(int noUrut = 1)
        => new ScheduleOpHistDto(
            $"HIST_{noUrut}",
            "SCHED_01",
            noUrut,
            new DateTime(2026, 10, 20, 8, 0, 0),
            new DateTime(2026, 10, 20, 10, 0, 0),
            120,
            150,
            "OK_01",
            "OK_02",
            "DOKTER_01",
            "DOKTER_02",
            "Pergeseran jadwal karena ruang OK disterilisasi",
            true,
            "Disetujui kepala bedah",
            "USER_TEST",
            new DateTime(2026, 10, 19, 14, 30, 0));

    private static IScheduleOpKey FakerKey()
        => ScheduleOpModel.Key("SCHED_01");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker(1));
    }

    [Fact]
    public void InsertBulkTest()
    {
        using var trans = TransHelper.NewScope();
        var list = new List<ScheduleOpHistDto>
        {
            Faker(1),
            Faker(2)
        };
        _sut.Insert(list);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker(1));
        _sut.Delete(FakerKey());
        var actual = _sut.ListData(FakerKey());
        actual.Should().BeEmpty();
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var item1 = Faker(1);
        var item2 = Faker(2);
        _sut.Insert(item2);
        _sut.Insert(item1);

        var actual = _sut.ListData(FakerKey()).ToList();

        actual.Should().HaveCount(2);
        actual[0].Should().BeEquivalentTo(item1);
        actual[1].Should().BeEquivalentTo(item2);
        actual.Should().BeInAscendingOrder(x => x.NoUrut);
    }
}
