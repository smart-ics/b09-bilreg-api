using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.IgdContext.TindakanIgdFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.IgdContext.TindakanIgdFeature;

public class TindakanIgdRepoTest
{
    private readonly Mock<ITindakanIgdDal> _dalMock = new();
    private readonly Mock<ITindakanIgdPpaDal> _ppaDalMock = new();
    private readonly Mock<ITindakanIgdBarangDal> _barangDalMock = new();
    private readonly TindakanIgdRepo _sut;

    public TindakanIgdRepoTest()
    {
        _sut = new TindakanIgdRepo(_dalMock.Object, _ppaDalMock.Object, _barangDalMock.Object);
    }

    private static TindakanIgdModel CreateModel(
        string pakaiBrgId = "PK001",
        IEnumerable<TindakanIgdPpaType>? listPpa = null,
        IEnumerable<TindakanIgdBarangType>? listBarang = null)
        => new(
            tindakanIgdId: "TKI001",
            igdVisitId: "IGV001",
            regId: "REG001",
            reffId: "TDK001",
            descriptions: "Jahit Luka",
            qty: 1,
            aktifitas: ActivityTindakanIgd.Tindakan,
            ppa: new PpaReff("DK1", "Dr. Tes"),
            audit: new AuditInfoType("U1", new DateTime(2026, 1, 1, 8, 0, 0)),
            layananId: "IGD01",
            pakaiBrgId: pakaiBrgId,
            listPpa: listPpa,
            listBarang: listBarang);

    [Fact]
    public void SaveChanges_NewModel_InsertsHeaderAndChildPpaAndChildBarang()
    {
        var listPpa = new[] { new TindakanIgdPpaType("K01", "DK1", "Dr. Tes") };
        var listBarang = new[]
        {
            new TindakanIgdBarangType(1, "BRG01", "PCS", 2m, "PCS", 2m, 2m, 0m, true, "PK001", "", false)
        };
        var model = CreateModel("PK001", listPpa, listBarang);
        _dalMock.Setup(x => x.GetData(It.IsAny<ITindakanIgdKey>())).Returns((TindakanIgdDto)null!);

        _sut.SaveChanges(model);

        _dalMock.Verify(x => x.Insert(It.Is<TindakanIgdDto>(d =>
            d.TindakanIgdId == "TKI001" &&
            d.LayananId == "IGD01" &&
            d.PakaiBrgId == "PK001")), Times.Once);
        _ppaDalMock.Verify(x => x.Delete(model), Times.Once);
        _ppaDalMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanIgdPpaDto>>(l => l.Count() == 1)), Times.Once);
        _barangDalMock.Verify(x => x.Delete(model), Times.Once);
        _barangDalMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanIgdBarangDto>>(l => l.Count() == 1)), Times.Once);
    }

    [Fact]
    public void LoadEntity_WithChildren_LoadsHeaderAndChildren()
    {
        var dto = TindakanIgdDto.FromModel(CreateModel());
        var ppaDtos = new[] { new TindakanIgdPpaDto("TKI001", "K01", "DK1", "Dr. Tes") };
        var barangDtos = new[]
        {
            new TindakanIgdBarangDto("TKI001", 1, "BRG01", "PCS", 2m, "PCS", 2m, 2m, 0m, true, "PK001", "", false)
        };

        _dalMock.Setup(x => x.GetData(It.IsAny<ITindakanIgdKey>())).Returns(dto);
        _ppaDalMock.Setup(x => x.ListData(It.IsAny<ITindakanIgdKey>())).Returns(ppaDtos);
        _barangDalMock.Setup(x => x.ListData(It.IsAny<ITindakanIgdKey>())).Returns(barangDtos);

        var result = _sut.LoadEntity(TindakanIgdModel.Key("TKI001"));

        result.HasValue.Should().BeTrue();
        result.Value.LayananId.Should().Be("IGD01");
        result.Value.PakaiBrgId.Should().Be("PK001");
        result.Value.ListPpa.Should().HaveCount(1);
        result.Value.ListBarang.Should().HaveCount(1);
    }

    [Fact]
    public void LoadEntity_HistoricalDataWithoutChildrenAndEmptyPakaiBrgId_LoadsCleanly()
    {
        var dto = TindakanIgdDto.FromModel(CreateModel(pakaiBrgId: ""));
        _dalMock.Setup(x => x.GetData(It.IsAny<ITindakanIgdKey>())).Returns(dto);
        _ppaDalMock.Setup(x => x.ListData(It.IsAny<ITindakanIgdKey>())).Returns([]);
        _barangDalMock.Setup(x => x.ListData(It.IsAny<ITindakanIgdKey>())).Returns([]);

        var result = _sut.LoadEntity(TindakanIgdModel.Key("TKI001"));

        result.HasValue.Should().BeTrue();
        result.Value.PakaiBrgId.Should().BeEmpty();
        result.Value.ListPpa.Should().BeEmpty();
        result.Value.ListBarang.Should().BeEmpty();
    }

    [Fact]
    public void DeleteEntity_DeletesChildrenAndHeader()
    {
        var key = TindakanIgdModel.Key("TKI001");

        _sut.DeleteEntity(key);

        _ppaDalMock.Verify(x => x.Delete(key), Times.Once);
        _barangDalMock.Verify(x => x.Delete(key), Times.Once);
        _dalMock.Verify(x => x.Delete(key), Times.Once);
    }
}
