using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OpCaseDalTest
{
    private readonly OpCaseDal _sut = new(ConnStringHelper.GetTestEnv());

    private static OpCaseDto Faker()
        => new OpCaseDto(
            OrderOpId: "A",
            OrderDate: new DateTime(2024, 1, 1, 10, 0, 0),
            NamaOperasi: "B",
            PasienId: "C",
            RegId: "D",
            UrgencyLevel: 1,
            ScheduleOpId: "E",
            ScheduledDate: new DateTime(2024, 1, 2, 8, 0, 0),
            DischargeOpId: "F",
            DischargedDate: new DateTime(2024, 1, 3, 12, 0, 0),
            OpCaseState: 1,
            PasienName: "G",
            TglLahir: "2000-01-01",
            Gender: "H"
        );

    private static IOrderOpKey FakerKey()
        => OrderOpModel.Key("A");

    private static Periode FakerPeriode()
        => new Periode(new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31));

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
            opt => opt.Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var opCase = Faker();
        _sut.Insert(opCase);
        
        var actual = _sut.ListData(FakerPeriode());
        actual.Should().ContainEquivalentOf(opCase,
            opt => opt.Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender));
    }
}
