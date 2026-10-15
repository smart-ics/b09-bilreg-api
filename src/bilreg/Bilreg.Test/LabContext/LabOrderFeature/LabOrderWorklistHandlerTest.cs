using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderWorklistHandlerTest
{
    private readonly Mock<ILabOrderWorklistDal> _dal = new();
    private readonly LabOrderWorklistHandler _sut;

    public LabOrderWorklistHandlerTest()
    {
        _sut = new LabOrderWorklistHandler(_dal.Object);
    }

    [Fact]
    public async Task Handle_PassesFilterToDal_ReturnsDalResult()
    {
        var expected = new[]
        {
            new LabOrderWorklistView(
                "LBO000000001",
                "LAB00000001",
                (int)LabOrderStatusEnum.Ordered,
                1,
                "MR0001",
                "Pasien Tes",
                "L",
                36,
                1,
                0,
                0,
                new DateTime(2026, 5, 18, 10, 0, 0),
                "",
                TestNames: ["Hemoglobin"])
        };

        LabOrderWorklistFilter? captured = null;
        _dal.Setup(d => d.List(It.IsAny<LabOrderWorklistFilter>()))
            .Callback<LabOrderWorklistFilter>(f => captured = f)
            .Returns(expected);

        var result = await _sut.Handle(
            new LabOrderWorklistQuery(
                LabOrderStatus: (int)LabOrderStatusEnum.Ordered,
                SearchTerm: "HB",
                Date1: new DateTime(2026, 5, 1),
                Date2: new DateTime(2026, 5, 31)),
            CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        captured.Should().NotBeNull();
        captured!.LabOrderStatus.Should().Be((int)LabOrderStatusEnum.Ordered);
        captured.SearchTerm.Should().Be("HB");
        captured.Date1.Should().Be(new DateTime(2026, 5, 1));
        captured.Date2.Should().Be(new DateTime(2026, 5, 31));
        captured.IsCitoOnly.Should().BeNull();
        _dal.Verify(d => d.List(It.IsAny<LabOrderWorklistFilter>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenIsCitoOnlyTrue_PassesFilterToDal()
    {
        var expected = new[]
        {
            new LabOrderWorklistView(
                "LBO000000002",
                "LAB00000002",
                (int)LabOrderStatusEnum.Ordered,
                1,
                "MR0002",
                "Pasien Cito",
                "P",
                28,
                2,
                0,
                0,
                new DateTime(2026, 5, 18, 11, 0, 0),
                "",
                IsCito: true,
                DokterPerujukName: "dr. Andi, Sp.PK",
                LayananPerujukName: "IGD",
                DeferredReason: "",
                TestNames: ["Troponin I", "CK-MB"])
        };

        LabOrderWorklistFilter? captured = null;
        _dal.Setup(d => d.List(It.IsAny<LabOrderWorklistFilter>()))
            .Callback<LabOrderWorklistFilter>(f => captured = f)
            .Returns(expected);

        var result = await _sut.Handle(
            new LabOrderWorklistQuery(
                LabOrderStatus: (int)LabOrderStatusEnum.Ordered,
                SearchTerm: null,
                Date1: null,
                Date2: null,
                IsCitoOnly: true),
            CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        captured.Should().NotBeNull();
        captured!.IsCitoOnly.Should().BeTrue();

        var first = result.First();
        first.IsCito.Should().BeTrue();
        first.DokterPerujukName.Should().Be("dr. Andi, Sp.PK");
        first.LayananPerujukName.Should().Be("IGD");
    }

    [Fact]
    public async Task Handle_GivenDeferredWorklistItems_PresentsDeferredReason()
    {
        var expected = new[]
        {
            new LabOrderWorklistView(
                "LBO000000003",
                "LAB00000003",
                (int)LabOrderStatusEnum.Deferred,
                1,
                "MR0003",
                "Pasien Tunda",
                "L",
                45,
                1,
                0,
                0,
                new DateTime(2026, 5, 18, 12, 0, 0),
                "",
                IsCito: false,
                DokterPerujukName: "dr. Budi, Sp.PD",
                LayananPerujukName: "Poli Penyakit Dalam",
                DeferredReason: "Reagen HB habis, menunggu pengiriman",
                TestNames: ["Hemoglobin"])
        };

        _dal.Setup(d => d.List(It.IsAny<LabOrderWorklistFilter>()))
            .Returns(expected);

        var result = await _sut.Handle(
            new LabOrderWorklistQuery(
                LabOrderStatus: (int)LabOrderStatusEnum.Deferred,
                SearchTerm: null,
                Date1: null,
                Date2: null,
                IsCitoOnly: false),
            CancellationToken.None);

        var item = result.Single();
        item.DeferredReason.Should().Be("Reagen HB habis, menunggu pengiriman");
        item.DokterPerujukName.Should().Be("dr. Budi, Sp.PD");
        item.LayananPerujukName.Should().Be("Poli Penyakit Dalam");
        item.IsCito.Should().BeFalse();
    }
}
