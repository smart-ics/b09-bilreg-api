using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class TerimaMutasiQueryTest
{
    private readonly Mock<ITerimaMutasiRepo> _terimaMutasiRepoMock = new();

    private static TerimaMutasiModel CreateSampleModel(
        string terimaMutasiId = "TM2600001A",
        string mutasiBrgId = "MT2600001A",
        DateTime? date = null)
    {
        var items = new List<TerimaMutasiItemModel>
        {
            TerimaMutasiItemModel.Load(
                1,
                new BrgReff("B001", "Paracetamol 500mg"),
                "BATCH1",
                "2027-12-31",
                15m,
                new SatuanType("S001", "Strip")),
            TerimaMutasiItemModel.Load(
                2,
                new BrgReff("B002", "Amoxicillin 500mg"),
                "BATCH2",
                "2027-06-30",
                30m,
                new SatuanType("S002", "Box"))
        };

        var trxDate = date ?? new DateTime(2026, 11, 26, 10, 15, 30);
        var auditTrail = AuditTrailType.Create("USR_TERIMA", trxDate);

        return TerimaMutasiModel.Load(
            terimaMutasiId,
            trxDate,
            TerimaMutasiStateEnum.Received,
            mutasiBrgId,
            "OM2600001A",
            new LayananReff("L01", "Gudang Farmasi Sentral"),
            new LayananReff("L02", "Depo Farmasi Rawat Inap"),
            "USR_TERIMA",
            "Diterima lengkap",
            auditTrail,
            items);
    }

    private static TerimaMutasiHeaderView CreateSampleHeaderView(
        string terimaMutasiId = "TM2600001A",
        string mutasiBrgId = "MT2600001A",
        string layananTujuanId = "L02")
    {
        return new TerimaMutasiHeaderView(
            terimaMutasiId,
            new DateTime(2026, 11, 26, 10, 15, 30),
            TerimaMutasiStateEnum.Received,
            mutasiBrgId,
            "OM2600001A",
            new LayananReff("L01", "Gudang Farmasi Sentral"),
            new LayananReff(layananTujuanId, "Depo Farmasi Rawat Inap"),
            "USR_TERIMA",
            "Diterima lengkap");
    }

    [Fact]
    public async Task GetQuery_GivenValidId_ShouldReturnFullDetailWithItems()
    {
        // Arrange
        var model = CreateSampleModel("TM2600001A", "MT2600001A");
        _terimaMutasiRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITerimaMutasiKey>(k => k.TerimaMutasiId == "TM2600001A")))
            .Returns(MayBe.From(model));

        var handler = new TerimaMutasiGetHandler(_terimaMutasiRepoMock.Object);
        var query = new TerimaMutasiGetQuery("TM2600001A");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TerimaMutasiId.Should().Be("TM2600001A");
        result.TerimaMutasiDate.Should().Be("2026-11-26 10:15:30");
        result.State.Should().Be("Received");
        result.MutasiBrgId.Should().Be("MT2600001A");
        result.OrderMutasiId.Should().Be("OM2600001A");
        result.LayananAsal.LayananId.Should().Be("L01");
        result.LayananTujuan.LayananId.Should().Be("L02");
        result.ReceiverUserId.Should().Be("USR_TERIMA");
        result.Note.Should().Be("Diterima lengkap");

        var items = result.ListItem.ToList();
        items.Should().HaveCount(2);
        items[0].NoUrut.Should().Be(1);
        items[0].BrgId.Should().Be("B001");
        items[0].BrgName.Should().Be("Paracetamol 500mg");
        items[0].BatchNo.Should().Be("BATCH1");
        items[0].ExpiredDate.Should().Be("2027-12-31");
        items[0].QtyTerima.Should().Be(15m);
        items[0].SatuanId.Should().Be("S001");
        items[0].SatuanName.Should().Be("Strip");

        items[1].NoUrut.Should().Be(2);
        items[1].BrgId.Should().Be("B002");
        items[1].QtyTerima.Should().Be(30m);
    }

    [Fact]
    public async Task GetQuery_GivenUnknownId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _terimaMutasiRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITerimaMutasiKey>()))
            .Returns(MayBe<TerimaMutasiModel>.None);

        var handler = new TerimaMutasiGetHandler(_terimaMutasiRepoMock.Object);
        var query = new TerimaMutasiGetQuery("TM_MISSING");

        // Act
        var act = () => handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*TM_MISSING*");
    }

    [Fact]
    public async Task GetByMutasiQuery_GivenValidMutasiBrgId_ShouldReturnFullDetailWithItems()
    {
        // Arrange
        var model = CreateSampleModel("TM2600001A", "MT2600001A");
        _terimaMutasiRepoMock
            .Setup(x => x.LoadByMutasiBrgId("MT2600001A"))
            .Returns(MayBe.From(model));

        var handler = new TerimaMutasiGetByMutasiHandler(_terimaMutasiRepoMock.Object);
        var query = new TerimaMutasiGetByMutasiQuery(" MT2600001A ");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TerimaMutasiId.Should().Be("TM2600001A");
        result.MutasiBrgId.Should().Be("MT2600001A");
        result.ListItem.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByMutasiQuery_GivenUnknownMutasiBrgId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _terimaMutasiRepoMock
            .Setup(x => x.LoadByMutasiBrgId("MT_MISSING"))
            .Returns(MayBe<TerimaMutasiModel>.None);

        var handler = new TerimaMutasiGetByMutasiHandler(_terimaMutasiRepoMock.Object);
        var query = new TerimaMutasiGetByMutasiQuery("MT_MISSING");

        // Act
        var act = () => handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*MT_MISSING*");
    }

    [Fact]
    public async Task ListQuery_WithoutLayananTujuanId_ShouldReturnHeaderList()
    {
        // Arrange
        var headers = new List<TerimaMutasiHeaderView>
        {
            CreateSampleHeaderView("TM2600001A", "MT2600001A", "L02"),
            CreateSampleHeaderView("TM2600002A", "MT2600002A", "L03")
        };

        _terimaMutasiRepoMock
            .Setup(x => x.ListData(
                It.Is<Periode>(p => p.Tgl1.Date == new DateTime(2026, 11, 1) && p.Tgl2.Date == new DateTime(2026, 11, 30)),
                string.Empty))
            .Returns(headers);

        var handler = new TerimaMutasiListHandler(_terimaMutasiRepoMock.Object);
        var query = new TerimaMutasiListQuery("2026-11-01", "2026-11-30");

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].TerimaMutasiId.Should().Be("TM2600001A");
        result[1].TerimaMutasiId.Should().Be("TM2600002A");
    }

    [Fact]
    public async Task ListQuery_WithLayananTujuanId_ShouldFilterByLayananTujuanId()
    {
        // Arrange
        var headers = new List<TerimaMutasiHeaderView>
        {
            CreateSampleHeaderView("TM2600001A", "MT2600001A", "L02")
        };

        _terimaMutasiRepoMock
            .Setup(x => x.ListData(
                It.Is<Periode>(p => p.Tgl1.Date == new DateTime(2026, 11, 1) && p.Tgl2.Date == new DateTime(2026, 11, 30)),
                "L02"))
            .Returns(headers);

        var handler = new TerimaMutasiListHandler(_terimaMutasiRepoMock.Object);
        var query = new TerimaMutasiListQuery("2026-11-01", "2026-11-30", " L02 ");

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].LayananTujuan.LayananId.Should().Be("L02");
    }

    [Fact]
    public async Task ListQuery_GivenInvalidDateFormat_ShouldThrowArgumentException()
    {
        // Arrange
        var handler = new TerimaMutasiListHandler(_terimaMutasiRepoMock.Object);
        var query = new TerimaMutasiListQuery("2026/11/01", "2026-11-30");

        // Act
        var act = () => handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
