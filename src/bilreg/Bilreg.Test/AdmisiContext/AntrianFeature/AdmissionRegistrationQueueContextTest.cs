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
    [InlineData(null, null, null)]
    [InlineData(" ", null, " ")]
    public void HasAny_WhenNoMeaningfulQueueFields_AcceptsTheLegacyNoContextPath(
        string? antrianId,
        int? noUrut,
        string? rowVersion)
    {
        AdmissionRegistrationQueueContextResolver.HasAny(antrianId, noUrut, rowVersion)
            .Should().BeFalse();
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

    [Fact]
    public void ResolveBehavior_NoQueueContext_UsesLegacyCompatibility()
    {
        var behavior = AdmissionRegistrationQueueContextResolver.ResolveBehavior(
            null, null, null, isDirect: false);

        behavior.Should().Be(RegistrationAdmissionQueueBehavior.LegacyAutoComplete);
    }

    [Fact]
    public void ResolveBehavior_CompleteQueueContext_UsesQueueLinked()
    {
        var behavior = AdmissionRegistrationQueueContextResolver.ResolveBehavior(
            "Q1", 1, "AQ==", isDirect: false);

        behavior.Should().Be(RegistrationAdmissionQueueBehavior.QueueLinked);
    }

    [Fact]
    public void ResolveBehavior_PartialQueueContext_IsRejectedBeforeWorkstationResolution()
    {
        var act = () => AdmissionRegistrationQueueContextResolver.ResolveBehavior(
            "Q1", null, null, isDirect: false);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be supplied together*");
    }

    [Fact]
    public void ResolveBehavior_DirectRequestWithQueueContext_IsRejected()
    {
        var act = () => AdmissionRegistrationQueueContextResolver.ResolveBehavior(
            "Q1", 1, "AQ==", isDirect: true);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*must not include Admission Queue context*");
    }

    [Fact]
    public void ResolveBehavior_DirectRequestWithEmptyQueueField_IsRejected()
    {
        var act = () => AdmissionRegistrationQueueContextResolver.ResolveBehavior(
            "", null, null, isDirect: true);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*must not include Admission Queue context*");
    }
}
