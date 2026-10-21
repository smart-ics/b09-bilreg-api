using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpDalTest
{
    private readonly ScheduleOpDal _sut = new(ConnStringHelper.GetTestEnv());

    private static ScheduleOpDto Faker()
        => new ScheduleOpDto("A", new DateTime(2023, 1, 2), "E", "F", 1, 120, new DateTime(2023, 1, 4), "G", "H", "I",
            "Pasien demam tinggi", 30,
            "B", new DateTime(2023, 1, 1), "C", new DateTime(2023, 1, 2), "",
            new DateTime(3000, 1, 1), new DateTime(2023, 1, 5), "J", "K", "2023-01-06", "M", "N", "O",
            new DateTime(2023, 1, 5), new DateTime(2023, 1, 5), 0);

    private static IScheduleOpKey FakerKey()
        => ScheduleOpModel.Key("A");

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
            opt => opt
                .Excluding(x => x.OrderDate)
                .Excluding(x => x.NamaOperasi)
                .Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender)
                .Excluding(x => x.KamarName)
                .Excluding(x => x.PpaName)
                .Excluding(x => x.StartedDate)
                .Excluding(x => x.FinishedDate)
                .Excluding(x => x.OrderOpState)
                .Excluding(x => x.ProfesiId)
                .Excluding(x => x.ProfesiName));
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(new DateTime(2023, 1, 4));
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.OrderDate)
                .Excluding(x => x.NamaOperasi)
                .Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender)
                .Excluding(x => x.KamarName)
                .Excluding(x => x.PpaName)
                .Excluding(x => x.StartedDate)
                .Excluding(x => x.FinishedDate)
                .Excluding(x => x.OrderOpState)
                .Excluding(x => x.ProfesiId)
                .Excluding(x => x.ProfesiName));
    }

    [Fact]
    public void ListData_ByPasienKey_Test()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(PasienModel.Key("F"));
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.OrderDate)
                .Excluding(x => x.NamaOperasi)
                .Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender)
                .Excluding(x => x.KamarName)
                .Excluding(x => x.PpaName)
                .Excluding(x => x.StartedDate)
                .Excluding(x => x.FinishedDate)
                .Excluding(x => x.OrderOpState)
                .Excluding(x => x.ProfesiId)
                .Excluding(x => x.ProfesiName));
    }

    [Fact]
    public void ListActiveSchedulesInRoomTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListActiveSchedulesInRoom("G", new DateTime(2023, 1, 4));
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.OrderDate)
                .Excluding(x => x.NamaOperasi)
                .Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender)
                .Excluding(x => x.KamarName)
                .Excluding(x => x.PpaName)
                .Excluding(x => x.StartedDate)
                .Excluding(x => x.FinishedDate)
                .Excluding(x => x.OrderOpState)
                .Excluding(x => x.ProfesiId)
                .Excluding(x => x.ProfesiName));
    }

    [Fact]
    public void ListActiveSchedulesForDoctorTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListActiveSchedulesForDoctor("I", new DateTime(2023, 1, 4));
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.OrderDate)
                .Excluding(x => x.NamaOperasi)
                .Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender)
                .Excluding(x => x.KamarName)
                .Excluding(x => x.PpaName)
                .Excluding(x => x.StartedDate)
                .Excluding(x => x.FinishedDate)
                .Excluding(x => x.OrderOpState)
                .Excluding(x => x.ProfesiId)
                .Excluding(x => x.ProfesiName));
    }

    [Fact]
    public void ListActiveSchedulesForPpaInTeamTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());

        var ppaDal = new ScheduleOpPpaDal(ConnStringHelper.GetTestEnv());
        ppaDal.Insert(new[]
        {
            new ScheduleOpPpaDto("A", 1, "PPA_01", "PROF1", "S01", "", "", "")
        });

        var actual = _sut.ListActiveSchedulesForPpaInTeam("PPA_01", new DateTime(2023, 1, 4)).ToList();

        actual.Should().NotBeEmpty();
        var item = actual.First(x => x.ScheduleOpId == "A");
        item.ProfesiId.Should().Be("PROF1");
        item.Should().BeEquivalentTo(Faker(),
            opt => opt
                .Excluding(x => x.OrderDate)
                .Excluding(x => x.NamaOperasi)
                .Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender)
                .Excluding(x => x.KamarName)
                .Excluding(x => x.PpaName)
                .Excluding(x => x.StartedDate)
                .Excluding(x => x.FinishedDate)
                .Excluding(x => x.OrderOpState)
                .Excluding(x => x.ProfesiId)
                .Excluding(x => x.ProfesiName));
    }
}
