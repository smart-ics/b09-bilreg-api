using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.IgdContext.BhpIgdFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.IgdContext.BhpIgdFeature;

public class BhpIgdRepoTest
{
    private readonly Mock<IBhpIgdDal> _dalMock = new();
    private readonly BhpIgdRepo _sut;

    public BhpIgdRepoTest()
    {
        _sut = new BhpIgdRepo(_dalMock.Object);
    }

    [Fact]
    public void SaveChanges_NewModel_InsertsWithLayananIdAndPakaiBrgId()
    {
        var model = new BhpIgdModel(
            bhpIgdId: "BHI001",
            igdVisitId: "IGV001",
            regId: "-",
            bhpItemId: "BRG01",
            bhpItemName: "Kassa",
            qty: 2,
            price: 5000m,
            audit: new AuditInfoType("U1", new DateTime(2026, 1, 1, 8, 0, 0)),
            layananId: "IGD01",
            pakaiBrgId: "PK001");
        _dalMock.Setup(x => x.GetData(It.IsAny<IBhpIgdKey>())).Returns((BhpIgdDto)null!);

        _sut.SaveChanges(model);

        _dalMock.Verify(x => x.Insert(It.Is<BhpIgdDto>(d =>
            d.BhpIgdId == "BHI001" &&
            d.LayananId == "IGD01" &&
            d.PakaiBrgId == "PK001")), Times.Once);
    }

    [Fact]
    public void LoadEntity_HistoricalDataWithEmptyPakaiBrgId_LoadsCleanly()
    {
        var dto = new BhpIgdDto(
            BhpIgdId: "BHI001",
            IgdVisitId: "IGV001",
            RegId: "-",
            BhpItemId: "BRG01",
            BhpItemName: "Kassa",
            Qty: 2,
            Price: 5000m,
            TrsBillId: "",
            IsPosted: false,
            PostedDate: null,
            CrtUser: "U1",
            CrtDate: new DateTime(2026, 1, 1, 8, 0, 0),
            LayananId: "",
            PakaiBrgId: "");
        _dalMock.Setup(x => x.GetData(It.IsAny<IBhpIgdKey>())).Returns(dto);

        var result = _sut.LoadEntity(BhpIgdModel.Key("BHI001"));

        result.HasValue.Should().BeTrue();
        result.Value.LayananId.Should().BeEmpty();
        result.Value.PakaiBrgId.Should().BeEmpty();
    }
}
