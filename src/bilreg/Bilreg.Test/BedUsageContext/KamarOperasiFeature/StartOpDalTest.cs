using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class StartOpDalTest
{
    private readonly StartOpDal _sut = new(ConnStringHelper.GetTestEnv());

    private static StartOpDto Faker()
        => new StartOpDto(
            StartOpId: "START-OP-1",
            StartOpTime: new DateTime(2024, 1, 1, 10, 0, 0),
            OrderOpId: "ORD-1",
            ScheduleOpId: "SCH-1",
            RegId: "REG-1",
            PasienId: "PSN-1",
            KamarOpId: "KOP-1",

            CrtUser: "CRT",
            CrtDate: new DateTime(2024, 1, 1, 9, 0, 0),
            UpdUser: "UPD",
            UpdDate: new DateTime(2024, 1, 1, 9, 30, 0),
            VodUser: "VOD",
            VodDate: new DateTime(3000, 1, 1),

            NamaOperasi: "Appendectomy",
            PasienName: "Test Pasien",
            TglLahir: "1990-01-01",
            Gender: "L",
            KamarName: "OK 1"
        );

    private static IStartOpKey FakerKey()
        => StartOpModel.Key("START-OP-1");

    private static DateTime FakerTanggal()
        => new DateTime(2024, 1, 1);

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

        actual.Should().BeEquivalentTo(
            Faker(),
            opt => opt.Excluding(x => x.NamaOperasi)
                      .Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender)
                      .Excluding(x => x.KamarName)
        );
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var startOp = Faker();
        _sut.Insert(startOp);

        var actual = _sut.ListData(FakerTanggal());

        actual.Should().ContainEquivalentOf(
            startOp,
            opt => opt.Excluding(x => x.NamaOperasi)
                      .Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender)
                      .Excluding(x => x.KamarName)
        );
    }
}
