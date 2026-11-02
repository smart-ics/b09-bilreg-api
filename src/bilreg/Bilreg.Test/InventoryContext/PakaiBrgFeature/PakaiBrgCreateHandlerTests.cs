using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Application.InventoryContext.PakaiBrgFeature.UseCases;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.PakaiBrgFeature;

public class PakaiBrgCreateHandlerTests
{
    private readonly Mock<ITglJamProvider> _clock = new();
    private readonly Mock<ILayananRepo> _layananRepo = new();
    private readonly Mock<IBrgRepo> _brgRepo = new();
    private readonly Mock<IPakaiBrgRepo> _pakaiBrgRepo = new();
    private readonly Mock<IMediator> _mediator = new();

    private static readonly DateTime FixedNow = new(2026, 10, 23, 11, 30, 0);

    private PakaiBrgCreateHandler CreateHandler()
    {
        _clock.Setup(x => x.Now).Returns(FixedNow);
        return new PakaiBrgCreateHandler(
            _clock.Object,
            _layananRepo.Object,
            _brgRepo.Object,
            _pakaiBrgRepo.Object,
            _mediator.Object);
    }

    private static LayananType CreateLayanan(string id = "LY01", string name = "Poli Umum", bool isAktif = true)
    {
        return LayananType.Default with { LayananId = id, LayananName = name, IsAktif = isAktif };
    }

    private static Mock<IBrg> CreateMockBrg(
        string brgId = "BRG01",
        string brgName = "Spuit 3cc",
        bool isAktif = true,
        string satuanId = "PCS",
        string satuanName = "Pieces")
    {
        var mock = new Mock<IBrg>();
        mock.Setup(x => x.BrgId).Returns(brgId);
        mock.Setup(x => x.BrgName).Returns(brgName);
        mock.Setup(x => x.IsAktif).Returns(isAktif);
        mock.Setup(x => x.ToReff()).Returns(new BrgReff(brgId, brgName));
        mock.Setup(x => x.SatuanKecil).Returns(new SatuanType(satuanId, satuanName));
        return mock;
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_InvalidLayananId_ThrowsArgumentException(string layananId)
    {
        var handler = CreateHandler();
        var cmd = new PakaiBrgCreateCommand(
            layananId,
            "USR01",
            "Keperluan rawat jalan",
            [new PakaiBrgCreateItemRequest("BRG01", 1)]);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_InvalidUserId_ThrowsArgumentException(string userId)
    {
        var handler = CreateHandler();
        var cmd = new PakaiBrgCreateCommand(
            "LY01",
            userId,
            "Keperluan rawat jalan",
            [new PakaiBrgCreateItemRequest("BRG01", 1)]);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_EmptyItemList_ThrowsArgumentException()
    {
        var handler = CreateHandler();
        var cmd = new PakaiBrgCreateCommand("LY01", "USR01", "Keperluan", []);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ListItem*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5.5)]
    public async Task Handle_NonPositiveQty_ThrowsArgumentException(decimal qty)
    {
        var handler = CreateHandler();
        var cmd = new PakaiBrgCreateCommand(
            "LY01",
            "USR01",
            "Keperluan",
            [new PakaiBrgCreateItemRequest("BRG01", qty)]);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*harus lebih besar dari 0*");
    }

    [Fact]
    public async Task Handle_LayananNotFound_ThrowsKeyNotFoundException()
    {
        var handler = CreateHandler();
        _layananRepo
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LY99")))
            .Returns(MayBe<LayananType>.None);

        var cmd = new PakaiBrgCreateCommand(
            "LY99",
            "USR01",
            "Keperluan",
            [new PakaiBrgCreateItemRequest("BRG01", 1)]);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Layanan with Id 'LY99' not found*");
    }

    [Fact]
    public async Task Handle_LayananNotActive_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler();
        var inactiveLayanan = CreateLayanan("LY01", "Poli Nonaktif", isAktif: false);
        _layananRepo
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LY01")))
            .Returns(MayBe.From(inactiveLayanan));

        var cmd = new PakaiBrgCreateCommand(
            "LY01",
            "USR01",
            "Keperluan",
            [new PakaiBrgCreateItemRequest("BRG01", 1)]);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak aktif*");
    }

    [Fact]
    public async Task Handle_BarangNotFound_ThrowsKeyNotFoundException()
    {
        var handler = CreateHandler();
        var layanan = CreateLayanan("LY01");
        _layananRepo
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LY01")))
            .Returns(MayBe.From(layanan));

        _brgRepo
            .Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG_NOT_FOUND")))
            .Returns(MayBe<IBrg>.None);

        var cmd = new PakaiBrgCreateCommand(
            "LY01",
            "USR01",
            "Keperluan",
            [new PakaiBrgCreateItemRequest("BRG_NOT_FOUND", 1)]);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Barang with Id 'BRG_NOT_FOUND' not found*");
    }

    [Fact]
    public async Task Handle_BarangNotActive_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler();
        var layanan = CreateLayanan("LY01");
        _layananRepo
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LY01")))
            .Returns(MayBe.From(layanan));

        var inactiveBrg = CreateMockBrg("BRG01", "Kasa Lama", isAktif: false);
        _brgRepo
            .Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01")))
            .Returns(MayBe.From(inactiveBrg.Object));

        var cmd = new PakaiBrgCreateCommand(
            "LY01",
            "USR01",
            "Keperluan",
            [new PakaiBrgCreateItemRequest("BRG01", 1)]);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Barang 'Kasa Lama' (BRG01) tidak aktif*");
    }

    [Fact]
    public async Task Handle_ConsequenceInsufficientStock_ThrowsInvalidOperationException_WithShortageAmount()
    {
        var handler = CreateHandler();
        var layanan = CreateLayanan("LY01", "Poli Umum");
        _layananRepo
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LY01")))
            .Returns(MayBe.From(layanan));

        var brg = CreateMockBrg("BRG01", "Spuit 3cc");
        _brgRepo
            .Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01")))
            .Returns(MayBe.From(brg.Object));

        _mediator
            .Setup(x => x.Send(It.IsAny<PostInternalConsumptionConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionConsequenceResult(
                PostInternalConsumptionOutcomeEnum.InsufficientStock,
                [],
                ShortfallQty: 5m,
                string.Empty,
                "BRG01",
                string.Empty));

        var cmd = new PakaiBrgCreateCommand(
            "LY01",
            "USR01",
            "Keperluan",
            [new PakaiBrgCreateItemRequest("BRG01", 10m)]);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Spuit 3cc*tidak mencukupi*Kekurangan: 5*");
    }

    [Fact]
    public async Task Handle_ConsequenceAbortedInconsistent_ThrowsInvalidOperationException_WithReason()
    {
        var handler = CreateHandler();
        var layanan = CreateLayanan("LY01", "Poli Umum");
        _layananRepo
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LY01")))
            .Returns(MayBe.From(layanan));

        var brg = CreateMockBrg("BRG01", "Spuit 3cc");
        _brgRepo
            .Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01")))
            .Returns(MayBe.From(brg.Object));

        _mediator
            .Setup(x => x.Send(It.IsAny<PostInternalConsumptionConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionConsequenceResult(
                PostInternalConsumptionOutcomeEnum.AbortedInconsistent,
                [],
                ShortfallQty: 0,
                InconsistencyReason: "Watermark discrepancy detected",
                FailedBrgId: "BRG01",
                FailedBrgMasukReffId: "DO01"));

        var cmd = new PakaiBrgCreateCommand(
            "LY01",
            "USR01",
            "Keperluan",
            [new PakaiBrgCreateItemRequest("BRG01", 1m)]);

        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Watermark discrepancy detected*");
    }

    [Fact]
    public async Task Handle_HappyPath_AllocatesStock_PersistsAggregate_ReturnsResponse()
    {
        // Arrange
        var handler = CreateHandler();
        var layanan = CreateLayanan("LY01", "Poli Umum");
        _layananRepo
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LY01")))
            .Returns(MayBe.From(layanan));

        var brg = CreateMockBrg("BRG01", "Spuit 3cc", satuanId: "PCS", satuanName: "Pieces");
        _brgRepo
            .Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01")))
            .Returns(MayBe.From(brg.Object));

        var line = new PostInternalConsumptionLineResult(
            StokMutasiId: "MUT01",
            StokLokasiId: "STL01",
            StokBatchId: "STB01",
            BrgMasukReffId: "DO01",
            LegacyBukuId: "BK01",
            LegacyStokId: "ST01",
            QtyOut: 2m,
            NoBatch: "BATCH-A",
            TglEd: new DateTime(2027, 10, 1),
            Hpp: 5000m,
            StokCurrent: 10m);

        _mediator
            .Setup(x => x.Send(It.IsAny<PostInternalConsumptionConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionConsequenceResult(
                PostInternalConsumptionOutcomeEnum.Success,
                [line],
                ShortfallQty: 0,
                string.Empty,
                string.Empty,
                string.Empty));

        PakaiBrgModel? savedModel = null;
        _pakaiBrgRepo
            .Setup(x => x.SaveChanges(It.IsAny<PakaiBrgModel>()))
            .Callback<PakaiBrgModel>(m => savedModel = m);

        var cmd = new PakaiBrgCreateCommand(
            "LY01",
            "USR01",
            "Keperluan Tindakan",
            [new PakaiBrgCreateItemRequest("BRG01", 2m, new DateTime(2027, 10, 1), "Pemakaian dr. Budi")]);

        // Act
        var response = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.PakaiBrgId.Should().StartWith("PK");
        response.TglPakai.Should().Be(FixedNow);
        response.TotalNilai.Should().Be(10000m); // 2 * 5000
        response.JumlahItem.Should().Be(1);

        savedModel.Should().NotBeNull();
        savedModel!.PakaiBrgId.Should().Be(response.PakaiBrgId);
        savedModel.Layanan.LayananId.Should().Be("LY01");
        savedModel.PetugasId.Should().Be("USR01");
        savedModel.Keterangan.Should().Be("Keperluan Tindakan");
        savedModel.IsVoid.Should().BeFalse();
        savedModel.ListItem.Should().HaveCount(1);

        var item = savedModel.ListItem[0];
        item.NoUrut.Should().Be(1);
        item.Brg.BrgId.Should().Be("BRG01");
        item.Satuan.SatuanId.Should().Be("PCS");
        item.StokCurrent.Should().Be(10m);
        item.Qty.Should().Be(2m);
        item.NoBatch.Should().Be("BATCH-A");
        item.TglEd.Should().Be(new DateTime(2027, 10, 1));
        item.Hpp.Should().Be(5000m);
        item.SubTotal.Should().Be(10000m);
        item.Keterangan.Should().Be("Pemakaian dr. Budi");

        _pakaiBrgRepo.Verify(x => x.SaveChanges(It.IsAny<PakaiBrgModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleItems_MultiBatchSplit_CalculatesTotalsAndMaintainsSequence()
    {
        // Arrange
        var handler = CreateHandler();
        var layanan = CreateLayanan("LY01", "Poli Umum");
        _layananRepo
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LY01")))
            .Returns(MayBe.From(layanan));

        var brg1 = CreateMockBrg("BRG01", "Spuit 3cc", satuanId: "PCS");
        var brg2 = CreateMockBrg("BRG02", "Kasa Roll", satuanId: "ROL");
        _brgRepo.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01"))).Returns(MayBe.From(brg1.Object));
        _brgRepo.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG02"))).Returns(MayBe.From(brg2.Object));

        // BRG01 was split across 2 batches (1 from batch A @ 5000, 2 from batch B @ 5500)
        var line1A = new PostInternalConsumptionLineResult(
            "MUT01", "STL01", "STB01", "DO01", "BK01", "ST01",
            QtyOut: 1m, NoBatch: "BATCH-1A", TglEd: new DateTime(2027, 1, 1), Hpp: 5000m, StokCurrent: 10m);
        var line1B = new PostInternalConsumptionLineResult(
            "MUT02", "STL02", "STB02", "DO02", "BK02", "ST02",
            QtyOut: 2m, NoBatch: "BATCH-1B", TglEd: new DateTime(2027, 6, 1), Hpp: 5500m, StokCurrent: 20m);

        _mediator
            .Setup(x => x.Send(It.Is<PostInternalConsumptionConsequenceCommand>(c => c.BrgId == "BRG01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionConsequenceResult(
                PostInternalConsumptionOutcomeEnum.Success,
                [line1A, line1B],
                ShortfallQty: 0, string.Empty, string.Empty, string.Empty));

        // BRG02 came from single batch (5 @ 12000)
        var line2 = new PostInternalConsumptionLineResult(
            "MUT03", "STL03", "STB03", "DO03", "BK03", "ST03",
            QtyOut: 5m, NoBatch: "BATCH-2", TglEd: new DateTime(2028, 1, 1), Hpp: 12000m, StokCurrent: 50m);

        _mediator
            .Setup(x => x.Send(It.Is<PostInternalConsumptionConsequenceCommand>(c => c.BrgId == "BRG02"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionConsequenceResult(
                PostInternalConsumptionOutcomeEnum.Success,
                [line2],
                ShortfallQty: 0, string.Empty, string.Empty, string.Empty));

        PakaiBrgModel? savedModel = null;
        _pakaiBrgRepo
            .Setup(x => x.SaveChanges(It.IsAny<PakaiBrgModel>()))
            .Callback<PakaiBrgModel>(m => savedModel = m);

        var cmd = new PakaiBrgCreateCommand(
            "LY01",
            "USR01",
            "Keperluan Multi Item",
            [
                new PakaiBrgCreateItemRequest("BRG01", 3m),
                new PakaiBrgCreateItemRequest("BRG02", 5m)
            ]);

        // Act
        var response = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        // Total = (1 * 5000) + (2 * 5500) + (5 * 12000) = 5000 + 11000 + 60000 = 76000
        response.TotalNilai.Should().Be(76000m);
        response.JumlahItem.Should().Be(3);

        savedModel.Should().NotBeNull();
        savedModel!.ListItem.Should().HaveCount(3);
        savedModel.ListItem[0].NoUrut.Should().Be(1);
        savedModel.ListItem[0].Qty.Should().Be(1m);
        savedModel.ListItem[0].SubTotal.Should().Be(5000m);

        savedModel.ListItem[1].NoUrut.Should().Be(2);
        savedModel.ListItem[1].Qty.Should().Be(2m);
        savedModel.ListItem[1].SubTotal.Should().Be(11000m);

        savedModel.ListItem[2].NoUrut.Should().Be(3);
        savedModel.ListItem[2].Qty.Should().Be(5m);
        savedModel.ListItem[2].SubTotal.Should().Be(60000m);
    }
}
