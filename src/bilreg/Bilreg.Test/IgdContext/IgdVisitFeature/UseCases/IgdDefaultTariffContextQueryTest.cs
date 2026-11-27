using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.IgdContext;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature.UseCases;

public class IgdDefaultTariffContextQueryTest
{
    private readonly Mock<IGetKelasRadarService> _getKelasRadarServiceMock = new();
    private readonly Mock<IJaminanRepo> _jaminanRepoMock = new();

    private IgdDefaultTariffContextHandler CreateSut(string smassLayananId = "IGD-01")
    {
        var options = Options.Create(new IgdVisitOptions { SmassLayananId = smassLayananId });
        return new IgdDefaultTariffContextHandler(
            options,
            _getKelasRadarServiceMock.Object,
            _jaminanRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValid_ReturnsResolvedDefaultTariffContext()
    {
        // Arrange
        var sut = CreateSut("LYN-IGD");
        _getKelasRadarServiceMock.Setup(x => x.Execute())
            .Returns(new KelasType("K3", "Kelas 3", true, new KelasDkType("3", "Kelas 3")));

        var jaminan = new JaminanType(
            "000",
            "UMUM",
            true,
            AlamatType.Default,
            CaraBayarDkType.Default,
            new GroupJaminanReff("000", "UMUM"),
            new JaminanTipeTarifType(new TipeTarifReff("UMM", "Tarif Umum"), new TipeTarifReff("UMM", "Tarif Umum")),
            JaminanRekeningType.Default,
            JaminanTipeBarangType.Default);

        _jaminanRepoMock.Setup(x => x.LoadEntity(It.Is<IJaminanKey>(k => k.JaminanId == "000")))
            .Returns(MayBe.From(jaminan));

        // Act
        var result = await sut.Handle(new IgdDefaultTariffContextQuery(), CancellationToken.None);

        // Assert
        result.LayananId.Should().Be("LYN-IGD");
        result.KelasId.Should().Be("K3");
        result.TipeJaminanId.Should().Be("00000");
        result.TipeTarifId.Should().Be("UMM");
    }

    [Fact]
    public async Task Handle_WhenDefaultJaminanNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var sut = CreateSut("LYN-IGD");
        _getKelasRadarServiceMock.Setup(x => x.Execute())
            .Returns(new KelasType("K3", "Kelas 3", true, new KelasDkType("3", "Kelas 3")));
        _jaminanRepoMock.Setup(x => x.LoadEntity(It.Is<IJaminanKey>(k => k.JaminanId == "000")))
            .Returns(MayBe<JaminanType>.None);

        // Act
        var act = () => sut.Handle(new IgdDefaultTariffContextQuery(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
