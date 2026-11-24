using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.InventoryContext.PakaiBrgFeature;
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

public class PakaiBrgInternalServiceTests
{
    private readonly Mock<ITglJamProvider> _clock = new();
    private readonly Mock<ILayananRepo> _layananRepo = new();
    private readonly Mock<IBrgRepo> _brgRepo = new();
    private readonly Mock<IPakaiBrgRepo> _pakaiBrgRepo = new();
    private readonly Mock<ISender> _sender = new();

    private static readonly DateTime FixedNow = new(2026, 11, 12, 10, 0, 0);

    private PakaiBrgInternalService CreateService()
    {
        _clock.Setup(x => x.Now).Returns(FixedNow);
        return new PakaiBrgInternalService(
            _clock.Object,
            _layananRepo.Object,
            _brgRepo.Object,
            _pakaiBrgRepo.Object,
            _sender.Object);
    }

    private static LayananType CreateLayanan(string id = "LY01", string name = "Poli Bedah", bool isAktif = true)
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

    private static PakaiBrgModel CreateSampleModel(
        string pakaiBrgId = "PK12345678",
        string petugasId = "USR01",
        string layananId = "LY01",
        string regId = "REG001")
    {
        var item = PakaiBrgItemModel.Create(
            1,
            new BrgReff("BRG01", "Spuit 3cc"),
            new SatuanReff("PCS", "Pieces"),
            stokCurrent: 10m,
            qty: 2m,
            noBatch: "BATCH-1",
            tglEd: new DateTime(2027, 10, 1),
            hpp: 5000m,
            keterangan: "Item note");

        return PakaiBrgModel.Create(
            new DateTime(2026, 11, 12, 9, 0, 0),
            new LayananReff(layananId, "Poli Bedah"),
            petugasId,
            "TDK:TDK001 TRF01",
            [item],
            pakaiBrgId);
    }

    #region CreateFromTindakan Validation Tests

    [Fact]
    public async Task CreateFromTindakan_NullCommand_ThrowsArgumentNullException()
    {
        var service = CreateService();
        var act = () => service.CreateFromTindakan(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateFromTindakan_InvalidRegId_ThrowsArgumentException(string regId)
    {
        var service = CreateService();
        var cmd = new PakaiBrgCreateFromTindakanCommand(
            regId,
            "LY01",
            "USR01",
            "Header",
            [new PakaiBrgCreateFromTindakanItem("BRG01", 1m)]);

        var act = () => service.CreateFromTindakan(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateFromTindakan_InvalidLayananId_ThrowsArgumentException(string layananId)
    {
        var service = CreateService();
        var cmd = new PakaiBrgCreateFromTindakanCommand(
            "REG001",
            layananId,
            "USR01",
            "Header",
            [new PakaiBrgCreateFromTindakanItem("BRG01", 1m)]);

        var act = () => service.CreateFromTindakan(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateFromTindakan_InvalidUserId_ThrowsArgumentException(string userId)
    {
        var service = CreateService();
        var cmd = new PakaiBrgCreateFromTindakanCommand(
            "REG001",
            "LY01",
            userId,
            "Header",
            [new PakaiBrgCreateFromTindakanItem("BRG01", 1m)]);

        var act = () => service.CreateFromTindakan(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateFromTindakan_EmptyItemList_ThrowsArgumentException()
    {
        var service = CreateService();
        var cmd = new PakaiBrgCreateFromTindakanCommand(
            "REG001",
            "LY01",
            "USR01",
            "Header",
            []);

        var act = () => service.CreateFromTindakan(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ListItem*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateFromTindakan_InvalidQty_ThrowsArgumentException(decimal qty)
    {
        var service = CreateService();
        var cmd = new PakaiBrgCreateFromTindakanCommand(
            "REG001",
            "LY01",
            "USR01",
            "Header",
            [new PakaiBrgCreateFromTindakanItem("BRG01", qty)]);

        var act = () => service.CreateFromTindakan(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Qty*");
    }

    [Fact]
    public async Task CreateFromTindakan_LayananNotFound_ThrowsKeyNotFoundException()
    {
        var service = CreateService();
        _layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe<LayananType>.None);

        var cmd = new PakaiBrgCreateFromTindakanCommand(
            "REG001",
            "UNKNOWN_LY",
            "USR01",
            "Header",
            [new PakaiBrgCreateFromTindakanItem("BRG01", 1m)]);

        var act = () => service.CreateFromTindakan(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*UNKNOWN_LY*");
    }

    [Fact]
    public async Task CreateFromTindakan_LayananInactive_ThrowsInvalidOperationException()
    {
        var service = CreateService();
        _layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe.From(CreateLayanan("LY01", "Poli Tutup", isAktif: false)));

        var cmd = new PakaiBrgCreateFromTindakanCommand(
            "REG001",
            "LY01",
            "USR01",
            "Header",
            [new PakaiBrgCreateFromTindakanItem("BRG01", 1m)]);

        var act = () => service.CreateFromTindakan(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak aktif*");
    }

    [Fact]
    public async Task CreateFromTindakan_BarangNotFound_ThrowsKeyNotFoundException()
    {
        var service = CreateService();
        _layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe.From(CreateLayanan()));
        _brgRepo.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe<IBrg>.None);

        var cmd = new PakaiBrgCreateFromTindakanCommand(
            "REG001",
            "LY01",
            "USR01",
            "Header",
            [new PakaiBrgCreateFromTindakanItem("UNKNOWN_BRG", 1m)]);

        var act = () => service.CreateFromTindakan(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*UNKNOWN_BRG*");
    }

    [Fact]
    public async Task CreateFromTindakan_BarangInactive_ThrowsInvalidOperationException()
    {
        var service = CreateService();
        _layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe.From(CreateLayanan()));
        var mockBrg = CreateMockBrg("BRG01", "Spuit Inaktif", isAktif: false);
        _brgRepo.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe.From(mockBrg.Object));

        var cmd = new PakaiBrgCreateFromTindakanCommand(
            "REG001",
            "LY01",
            "USR01",
            "Header",
            [new PakaiBrgCreateFromTindakanItem("BRG01", 1m)]);

        var act = () => service.CreateFromTindakan(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak aktif*");
    }

    #endregion

    #region CreateFromTindakan Success & Stock Allocation Tests

    [Fact]
    public async Task CreateFromTindakan_ValidCommand_PublishesConsequenceAndSavesDocumentWithRegIdAndAllocatedBatches()
    {
        // Arrange
        var service = CreateService();
        _layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe.From(CreateLayanan("LY01", "Poli Bedah")));

        var mockBrg = CreateMockBrg("BRG01", "Spuit 3cc");
        _brgRepo.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe.From(mockBrg.Object));

        var ed1 = new DateTime(2027, 6, 30);
        var ed2 = new DateTime(2027, 12, 31);
        var consequenceLines = new List<PostInternalConsumptionLineResult>
        {
            new("MUT01", "LOK01", "BAT01", "REF01", "BUK01", "STK01", QtyOut: 2m, NoBatch: "BATCH-A", TglEd: ed1, Hpp: 4000m, StokCurrent: 10m),
            new("MUT02", "LOK01", "BAT02", "REF02", "BUK02", "STK02", QtyOut: 3m, NoBatch: "BATCH-B", TglEd: ed2, Hpp: 4500m, StokCurrent: 8m)
        };

        _sender.Setup(x => x.Send(It.IsAny<PostInternalConsumptionConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionConsequenceResult(
                PostInternalConsumptionOutcomeEnum.Success,
                consequenceLines,
                ShortfallQty: 0m,
                InconsistencyReason: string.Empty,
                FailedBrgId: string.Empty,
                FailedBrgMasukReffId: string.Empty));

        PakaiBrgModel? savedModel = null;
        string? savedRegId = null;
        _pakaiBrgRepo.Setup(x => x.SaveChanges(It.IsAny<PakaiBrgModel>(), It.IsAny<string>()))
            .Callback<PakaiBrgModel, string>((m, r) => { savedModel = m; savedRegId = r; });

        var cmd = new PakaiBrgCreateFromTindakanCommand(
            RegId: "REG20261112001",
            LayananId: "LY01",
            UserId: "DR_BEDAH",
            KeteranganHeader: "TDK:TDK001 TRF01",
            ListItem: [new PakaiBrgCreateFromTindakanItem("BRG01", 5m)]);

        // Act
        var result = await service.CreateFromTindakan(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBrgId.Should().StartWith("PK");
        result.PakaiBrgId.Length.Should().BeLessOrEqualTo(10);
        result.TglPakai.Should().Be(FixedNow);
        result.JumlahItem.Should().Be(2); // 2 batch lines
        result.TotalNilai.Should().Be(2m * 4000m + 3m * 4500m); // 8000 + 13500 = 21500

        result.Items.Should().HaveCount(2);
        result.Items[0].NoBatch.Should().Be("BATCH-A");
        result.Items[0].Qty.Should().Be(2m);
        result.Items[0].Hpp.Should().Be(4000m);
        result.Items[0].SubTotal.Should().Be(8000m);
        result.Items[1].NoBatch.Should().Be("BATCH-B");
        result.Items[1].Qty.Should().Be(3m);
        result.Items[1].Hpp.Should().Be(4500m);
        result.Items[1].SubTotal.Should().Be(13500m);

        // Verify saved PakaiBrgModel and patient RegId
        savedModel.Should().NotBeNull();
        savedModel!.PakaiBrgId.Should().Be(result.PakaiBrgId);
        savedRegId.Should().Be("REG20261112001");
        savedModel.Layanan.LayananId.Should().Be("LY01");
        savedModel.PetugasId.Should().Be("DR_BEDAH");
        savedModel.Keterangan.Should().Be("TDK:TDK001 TRF01");
        savedModel.ListItem.Should().HaveCount(2);

        // Verify PostInternalConsumptionConsequenceCommand was sent
        _sender.Verify(x => x.Send(
            It.Is<PostInternalConsumptionConsequenceCommand>(c =>
                c.BrgId == "BRG01" &&
                c.LayananId == "LY01" &&
                c.Qty == 5m &&
                c.TrsReffId == result.PakaiBrgId &&
                c.UserId == "DR_BEDAH" &&
                c.TglMutasi == FixedNow),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFromTindakan_KeteranganHeaderExceeding40Chars_SafelyTruncatedTo40Chars()
    {
        // Arrange
        var service = CreateService();
        _layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe.From(CreateLayanan()));
        var mockBrg = CreateMockBrg();
        _brgRepo.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe.From(mockBrg.Object));

        _sender.Setup(x => x.Send(It.IsAny<PostInternalConsumptionConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionConsequenceResult(
                PostInternalConsumptionOutcomeEnum.Success,
                [new("MUT01", "LOK01", "BAT01", "REF01", "BUK01", "STK01", 1m, "B1", FixedNow, 1000m, 10m)],
                0m, "", "", ""));

        PakaiBrgModel? savedModel = null;
        _pakaiBrgRepo.Setup(x => x.SaveChanges(It.IsAny<PakaiBrgModel>(), It.IsAny<string>()))
            .Callback<PakaiBrgModel, string>((m, _) => savedModel = m);

        var longKeterangan = "TDK:TDK001 TRF000000000000000000000000000000000000000000000000000000000000000000";
        var cmd = new PakaiBrgCreateFromTindakanCommand(
            "REG001",
            "LY01",
            "USR01",
            longKeterangan,
            [new PakaiBrgCreateFromTindakanItem("BRG01", 1m)]);

        // Act
        var result = await service.CreateFromTindakan(cmd, CancellationToken.None);

        // Assert
        savedModel.Should().NotBeNull();
        savedModel!.Keterangan.Length.Should().BeLessOrEqualTo(40);
        savedModel.Keterangan.Should().Be(longKeterangan[..40]);
    }

    [Fact]
    public async Task CreateFromTindakan_InsufficientStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        _layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe.From(CreateLayanan()));
        var mockBrg = CreateMockBrg("BRG01", "Kassa Steril");
        _brgRepo.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe.From(mockBrg.Object));

        _sender.Setup(x => x.Send(It.IsAny<PostInternalConsumptionConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionConsequenceResult(
                PostInternalConsumptionOutcomeEnum.InsufficientStock,
                [],
                ShortfallQty: 3m,
                InconsistencyReason: string.Empty,
                FailedBrgId: "BRG01",
                FailedBrgMasukReffId: string.Empty));

        var cmd = new PakaiBrgCreateFromTindakanCommand(
            "REG001",
            "LY01",
            "USR01",
            "Header",
            [new PakaiBrgCreateFromTindakanItem("BRG01", 5m)]);

        // Act
        var act = () => service.CreateFromTindakan(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak mencukupi*");
    }

    [Fact]
    public async Task CreateFromTindakan_ConsequenceInconsistency_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        _layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe.From(CreateLayanan()));
        var mockBrg = CreateMockBrg();
        _brgRepo.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe.From(mockBrg.Object));

        _sender.Setup(x => x.Send(It.IsAny<PostInternalConsumptionConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionConsequenceResult(
                PostInternalConsumptionOutcomeEnum.AbortedInconsistent,
                [],
                0m,
                InconsistencyReason: "Buku mismatch detected",
                FailedBrgId: "BRG01",
                FailedBrgMasukReffId: string.Empty));

        var cmd = new PakaiBrgCreateFromTindakanCommand(
            "REG001",
            "LY01",
            "USR01",
            "Header",
            [new PakaiBrgCreateFromTindakanItem("BRG01", 1m)]);

        // Act
        var act = () => service.CreateFromTindakan(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*inkonsistensi: Buku mismatch detected*");
    }

    #endregion

    #region VoidFromTindakan Tests

    [Fact]
    public async Task VoidFromTindakan_NullCommand_ThrowsArgumentNullException()
    {
        var service = CreateService();
        var act = () => service.VoidFromTindakan(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VoidFromTindakan_InvalidPakaiBrgId_ThrowsArgumentException(string id)
    {
        var service = CreateService();
        var cmd = new PakaiBrgVoidFromTindakanCommand(id, "USR01");
        var act = () => service.VoidFromTindakan(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VoidFromTindakan_InvalidUserId_ThrowsArgumentException(string userId)
    {
        var service = CreateService();
        var cmd = new PakaiBrgVoidFromTindakanCommand("PK12345678", userId);
        var act = () => service.VoidFromTindakan(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task VoidFromTindakan_DocumentNotFound_ThrowsKeyNotFoundException()
    {
        var service = CreateService();
        _pakaiBrgRepo.Setup(x => x.LoadEntity(It.IsAny<IPakaiBrgKey>()))
            .Returns(MayBe<PakaiBrgModel>.None);

        var cmd = new PakaiBrgVoidFromTindakanCommand("PK_UNKNOWN", "USR01");
        var act = () => service.VoidFromTindakan(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*PK_UNKNOWN*");
    }

    [Fact]
    public async Task VoidFromTindakan_ValidCommand_PublishesVoidConsequenceAndMarksVoid()
    {
        // Arrange
        var service = CreateService();
        var sampleModel = CreateSampleModel("PK12345678", "USR01", "LY01", "REG001");
        _pakaiBrgRepo.Setup(x => x.LoadEntity(It.Is<IPakaiBrgKey>(k => k.PakaiBrgId == "PK12345678")))
            .Returns(MayBe.From(sampleModel));

        _sender.Setup(x => x.Send(It.IsAny<PostInternalConsumptionVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionVoidConsequenceResult(
                PostInternalConsumptionVoidOutcomeEnum.Success,
                [new("VOID_MUT01", "MUT01", "LOK01", "BAT01", "REF01", "BUK01", "STK01", 2m)],
                UnboundStokMutasiId: string.Empty,
                InconsistencyReason: string.Empty,
                FailedBrgId: string.Empty,
                FailedBrgMasukReffId: string.Empty));

        var cmd = new PakaiBrgVoidFromTindakanCommand("PK12345678", "DR_BATAL", "Batal tindakan tdk01");

        // Act
        var result = await service.VoidFromTindakan(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBrgId.Should().Be("PK12345678");
        result.IsVoid.Should().BeTrue();
        result.TglVoid.Should().Be(FixedNow);
        result.AlasanVoid.Should().Be("Batal tindakan tdk01");

        sampleModel.IsVoid.Should().BeTrue();
        sampleModel.PetugasVoidId.Should().Be("DR_BATAL");
        sampleModel.ListItem[0].IsVoid.Should().BeTrue();

        _sender.Verify(x => x.Send(
            It.Is<PostInternalConsumptionVoidConsequenceCommand>(c =>
                c.OriginalTrsReffId == "PK12345678" &&
                c.VoidTrsReffId == "PK12345678" &&
                c.UserId == "DR_BATAL" &&
                c.TglMutasi == FixedNow),
            It.IsAny<CancellationToken>()), Times.Once);

        _pakaiBrgRepo.Verify(x => x.SaveChanges(sampleModel), Times.Once);
    }

    [Fact]
    public async Task VoidFromTindakan_VoidConsequenceFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var sampleModel = CreateSampleModel();
        _pakaiBrgRepo.Setup(x => x.LoadEntity(It.IsAny<IPakaiBrgKey>()))
            .Returns(MayBe.From(sampleModel));

        _sender.Setup(x => x.Send(It.IsAny<PostInternalConsumptionVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionVoidConsequenceResult(
                PostInternalConsumptionVoidOutcomeEnum.Unbound,
                [],
                "MUT_UNBOUND",
                InconsistencyReason: "Mutasi binding not found",
                FailedBrgId: "BRG01",
                FailedBrgMasukReffId: string.Empty));

        var cmd = new PakaiBrgVoidFromTindakanCommand(sampleModel.PakaiBrgId, "USR01");

        // Act
        var act = () => service.VoidFromTindakan(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Mutasi binding not found*");
    }

    [Fact]
    public async Task VoidFromTindakan_AlreadyVoidDocument_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var sampleModel = CreateSampleModel();
        sampleModel.Void("PREV_USR", "Alasan void sebelumnya", new DateTime(2026, 11, 10));

        _pakaiBrgRepo.Setup(x => x.LoadEntity(It.IsAny<IPakaiBrgKey>()))
            .Returns(MayBe.From(sampleModel));

        var cmd = new PakaiBrgVoidFromTindakanCommand(sampleModel.PakaiBrgId, "USR02");

        // Act
        var act = () => service.VoidFromTindakan(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan*");
    }

    #endregion
}
