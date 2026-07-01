using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpPpaDalTest
{
    private readonly ScheduleOpPpaDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<ScheduleOpPpaDto> FakerList()
        => new List<ScheduleOpPpaDto>
        {
            new ScheduleOpPpaDto(
                ScheduleOpId: "A",
                NoUrut: 1,
                PpaId: "B",
                ProfesiId: "C",
                GroupSpesialisId: "D",
                PpaName: "E",
                ProfesiName: "F",
                GroupSpesialisName: "G"
            ),
            new ScheduleOpPpaDto(
                ScheduleOpId: "A",
                NoUrut: 2,
                PpaId: "H",
                ProfesiId: "I",
                GroupSpesialisId: "J",
                PpaName: "K",
                ProfesiName: "L",
                GroupSpesialisName: "M"
            )
        };

    private static IScheduleOpKey FakerKey()
        => ScheduleOpModel.Key("A");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
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
        _sut.Insert(FakerList());
        var actual = _sut.ListData(FakerKey());
        actual.Should().BeEquivalentTo(FakerList(),
            opt => opt.Excluding(x => x.PpaName)
                .Excluding(x => x.ProfesiName)
                .Excluding(x => x.GroupSpesialisName));
    }
}