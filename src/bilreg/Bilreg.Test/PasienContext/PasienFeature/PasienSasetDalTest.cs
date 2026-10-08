using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienSasetDalTest
{
    private readonly PasienSasetDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PasienSasetDto Faker() =>
        new(
            KodeMr: "TEST01",
            KodeSaset: "SAS-12345",
            IsApprovedUpload: true,
            TglJamApprovedUpload: new DateTime(2026, 10, 7, 10, 30, 0),
            FileGeneralConcentUpload: "consent_upload.pdf",
            IsApprovedView: false,
            TglJamApprovedView: new DateTime(3000, 1, 1, 0, 0, 0),
            FileGeneralConcentView: "-"
        );

    private static IPasienKey FakerKey() =>
        PasienModel.Key("TEST01");

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
        var updated = Faker() with
        {
            KodeSaset = "SAS-99999",
            FileGeneralConcentUpload = "new_consent.pdf"
        };
        _sut.Update(updated);
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
}
