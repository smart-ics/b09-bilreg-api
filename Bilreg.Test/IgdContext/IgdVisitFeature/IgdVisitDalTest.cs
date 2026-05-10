using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature;

public class IgdVisitDalTest
{
    private readonly IgdVisitDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IgdVisitDto Faker()
        => new IgdVisitDto(
            IgdVisitId: "TIGV0000000001",
            DaftarDateTime: new DateTime(2026, 1, 1, 8, 0, 0),
            VisitorName: "Tes Visitor",
            VisitorGender: "L",
            VisitorTglLahir: new DateTime(1990, 1, 1),
            VisitorKontak: "081",
            DokterId: "DK1",
            DokterName: "Dr. Tes",
            HasTriage: true,
            TriageLevel: "P3",
            AdministrativeState: "DAFTAR",
            RegId: "",
            PasienId: "",
            PasienName: "",
            RedirectRajalId: "",
            RedirectDateTime: new DateTime(3000, 1, 1),
            RedirectReason: "",
            BedIgdId: "",
            DischargeUser: "",
            DischargeDateTime: new DateTime(3000, 1, 1),
            CrtUser: "U1", CrtDate: new DateTime(2026, 1, 1, 8, 0, 0),
            UpdUser: "U1", UpdDate: new DateTime(2026, 1, 1, 8, 0, 0),
            VodUser: "", VodDate: new DateTime(3000, 1, 1));

    private static IIgdVisitKey FakerKey() => IgdVisitModel.Key("TIGV0000000001");
    private static Periode FakerPeriode() => new(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

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
        _sut.Insert(Faker());
        _sut.Update(Faker() with { VisitorName = "Updated" });
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(FakerPeriode());
        actual.Should().ContainEquivalentOf(Faker());
    }

    [Fact]
    public void ListAktifTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListAktif();
        actual.Should().ContainEquivalentOf(Faker());
    }
}
