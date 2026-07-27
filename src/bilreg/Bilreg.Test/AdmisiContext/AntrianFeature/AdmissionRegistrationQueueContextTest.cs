using Bilreg.Application.AdmisiContext.RegFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionRegistrationQueueContextTest
{
    [Fact]
    public void Resolve_NoQueueFields_RemainsBackwardCompatible()
    {
        AdmissionRegistrationQueueContextResolver.Resolve(null, null, null, null)
            .Should().BeNull();
    }

    [Theory]
    [InlineData("Q1", null, null)]
    [InlineData("Q1", 1, null)]
    [InlineData(null, 1, "AQID")]
    public void Resolve_PartialQueueContext_IsRejected(
        string? antrianId,
        int? noUrut,
        string? rowVersion)
    {
        var act = () => AdmissionRegistrationQueueContextResolver.Resolve(
            antrianId, noUrut, rowVersion, "L1");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be supplied together*");
    }

    [Fact]
    public void Resolve_CompleteContext_UsesServerResolvedLoketAndDecodedVersion()
    {
        var version = new byte[] { 1, 2, 3, 4 };
        var result = AdmissionRegistrationQueueContextResolver.Resolve(
            " Q1 ",
            7,
            Convert.ToBase64String(version),
            " L1 ");

        result.Should().NotBeNull();
        result!.AntrianId.Should().Be("Q1");
        result.NoUrut.Should().Be(7);
        result.LoketKey.Should().Be("L1");
        result.ExpectedRowVersion.Should().Equal(version);
    }

    [Fact]
    public void Resolve_InvalidBase64_IsRejected()
    {
        var act = () => AdmissionRegistrationQueueContextResolver.Resolve(
            "Q1", 1, "not-base64", "L1");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be Base64*");
    }
}
