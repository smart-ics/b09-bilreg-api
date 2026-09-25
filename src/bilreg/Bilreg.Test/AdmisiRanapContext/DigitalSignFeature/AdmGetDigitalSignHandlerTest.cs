using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature.UseCases;
using Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.DigitalSignFeature;

public class AdmGetDigitalSignHandlerTest
{
    private readonly Mock<IRanapDigitalSignRepo> _repoMock = new();

    private static RanapDigitalSignModel Sample(string dokumenId, string? signingId = null) =>
        RanapDigitalSignModel.CatatCreated(
            "RG00000001", "HIS-ENC-999", dokumenId,
            signingId ?? Guid.NewGuid().ToString("D"),
            new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            Guid.NewGuid().ToString("D"),
            "file.pdf",
            "user1", TestTglJamProvider.Instance.Now);

    [Fact]
    public async Task GivenDocExists_WhenGetByRegAndDoc_ThenReturnsSingle()
    {
        var model = Sample("HIS-DOC-001");
        _repoMock.Setup(x => x.LoadByRegDokumen("RG00000001", "HIS-DOC-001"))
            .Returns(MayBe.From(model));

        var result = await new AdmGetDigitalSignHandler(_repoMock.Object)
            .Handle(new AdmGetDigitalSignQry("RG00000001", "HIS-DOC-001"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items.Single().SigningRequestId.Should().Be(model.SigningRequestId);
        result.Items.Single().HisReference.Should().Be("HIS-ENC-999");
    }

    [Fact]
    public async Task GivenTwoDocs_WhenGetByRegOnly_ThenReturnsList()
    {
        _repoMock.Setup(x => x.ListByRegId("RG00000001"))
            .Returns([Sample("HIS-DOC-001"), Sample("HIS-DOC-002")]);

        var result = await new AdmGetDigitalSignHandler(_repoMock.Object)
            .Handle(new AdmGetDigitalSignQry("RG00000001"), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GivenNoKey_WhenGet_ThenThrows()
    {
        var act = () => new AdmGetDigitalSignHandler(_repoMock.Object)
            .Handle(new AdmGetDigitalSignQry(" "), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
