using Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.DigitalSignFeature;

public class RanapDigitalSignModelTest
{
    private static readonly string SigningId = Guid.NewGuid().ToString("D");
    private static PasienReff SamplePasien() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static RanapDigitalSignModel CatatValid(string? signingId = null) =>
        RanapDigitalSignModel.CatatCreated(
            "RG00000001",
            "RG00000001",
            "HIS-DOC-2026-001",
            signingId ?? SigningId,
            SamplePasien(),
            Guid.NewGuid().ToString("D"),
            "informed-consent.pdf",
            "user1",
            new DateTime(2026, 9, 22, 10, 0, 0));

    [Fact]
    public void GivenValid_WhenCatatCreated_ThenStoresKeys()
    {
        var model = CatatValid();

        model.RegId.Should().Be("RG00000001");
        model.HisReference.Should().Be("RG00000001");
        model.DokumenId.Should().Be("HIS-DOC-2026-001");
        model.SigningRequestId.Should().Be(SigningId);
    }

    [Fact]
    public void GivenValid_WhenCatatCreated_ThenStoresExplicitHisReference()
    {
        var model = RanapDigitalSignModel.CatatCreated(
            "RG00000001", "HIS-ENC-999", "HIS-DOC-1", SigningId, SamplePasien(),
            "", "", "user1");

        model.RegId.Should().Be("RG00000001");
        model.HisReference.Should().Be("HIS-ENC-999");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-")]
    public void GivenBlankHisReference_WhenCatatCreated_ThenThrows(string hisReference)
    {
        var act = () => RanapDigitalSignModel.CatatCreated(
            "RG00000001", hisReference, "HIS-DOC-1", SigningId, SamplePasien(),
            "", "", "user1");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("", "HIS-DOC-1")]
    [InlineData(" ", "HIS-DOC-1")]
    [InlineData("-", "HIS-DOC-1")]
    [InlineData("RG00000001", "")]
    [InlineData("RG00000001", " ")]
    [InlineData("RG00000001", "-")]
    public void GivenBlankKey_WhenCatatCreated_ThenThrows(string regId, string dokumenId)
    {
        var act = () => RanapDigitalSignModel.CatatCreated(
            regId, regId, dokumenId, SigningId, SamplePasien(),
            "", "", "user1");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-")]
    [InlineData("not-a-guid")]
    public void GivenInvalidSigningRequestId_WhenCatatCreated_ThenThrows(string signingId)
    {
        var act = () => RanapDigitalSignModel.CatatCreated(
            "RG00000001", "RG00000001", "HIS-DOC-1", signingId, SamplePasien(),
            "", "", "user1");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenCreated_WhenBatal_ThenVoids()
    {
        var timestamp = new DateTime(2026, 9, 22, 11, 0, 0);
        var cancelled = CatatValid().Batal("void-user", "Batal oleh admisi", timestamp);

        cancelled.AuditTrail.Voided.UserId.Should().Be("void-user");
        cancelled.AuditTrail.Voided.Timestamp.Should().Be(timestamp);
    }
}
