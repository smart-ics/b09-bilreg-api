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

public class PakaiBrgVoidHandlerTests
{
    private readonly Mock<ITglJamProvider> _clock = new();
    private readonly Mock<IPakaiBrgRepo> _pakaiBrgRepo = new();
    private readonly Mock<IMediator> _mediator = new();

    private static readonly DateTime FixedNow = new(2026, 10, 23, 15, 0, 0);

    private PakaiBrgVoidHandler CreateHandler()
    {
        _clock.Setup(x => x.Now).Returns(FixedNow);
        return new PakaiBrgVoidHandler(
            _clock.Object,
            _pakaiBrgRepo.Object,
            _mediator.Object);
    }

    private static PakaiBrgModel CreateSampleModel(
        string pakaiBrgId = "PK12345678",
        string petugasId = "USR01",
        string layananId = "LY01")
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
            new DateTime(2026, 10, 23, 10, 0, 0),
            new LayananReff(layananId, "Poli Bedah"),
            petugasId,
            "Pemakaian unit",
            [item],
            pakaiBrgId);
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
    public async Task Handle_EmptyPakaiBrgId_ThrowsArgumentException(string id)
    {
        var handler = CreateHandler();
        var cmd = new PakaiBrgVoidCommand(id, "USR01", "Salah input");
        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyUserId_ThrowsArgumentException(string userId)
    {
        var handler = CreateHandler();
        var cmd = new PakaiBrgVoidCommand("PK12345678", userId, "Salah input");
        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyAlasanVoid_ThrowsArgumentException(string alasan)
    {
        var handler = CreateHandler();
        var cmd = new PakaiBrgVoidCommand("PK12345678", "USR01", alasan);
        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_DocumentNotFound_ThrowsKeyNotFoundException()
    {
        var handler = CreateHandler();
        _pakaiBrgRepo
            .Setup(x => x.LoadEntity(It.Is<IPakaiBrgKey>(k => k.PakaiBrgId == "PKNOTFOUND")))
            .Returns(MayBe<PakaiBrgModel>.None);

        var cmd = new PakaiBrgVoidCommand("PKNOTFOUND", "USR01", "Salah input");
        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_DocumentAlreadyVoided_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler();
        var model = CreateSampleModel("PK12345678");
        model.Void("USR_OLD", "Alasan lama", new DateTime(2026, 10, 23, 11, 0, 0));

        _pakaiBrgRepo
            .Setup(x => x.LoadEntity(It.Is<IPakaiBrgKey>(k => k.PakaiBrgId == "PK12345678")))
            .Returns(MayBe.From(model));

        var cmd = new PakaiBrgVoidCommand("PK12345678", "USR01", "Void lagi");
        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan (Void) sebelumnya*");
    }

    [Theory]
    [InlineData(PostInternalConsumptionVoidOutcomeEnum.OriginalNotFound, "OriginalNotFound")]
    [InlineData(PostInternalConsumptionVoidOutcomeEnum.Unbound, "Unbound")]
    [InlineData(PostInternalConsumptionVoidOutcomeEnum.AlreadyReversed, "AlreadyReversed")]
    public async Task Handle_VoidConsequenceFailure_ThrowsInvalidOperationException(
        PostInternalConsumptionVoidOutcomeEnum failureOutcome,
        string expectedReasonFragment)
    {
        var handler = CreateHandler();
        var model = CreateSampleModel("PK12345678");

        _pakaiBrgRepo
            .Setup(x => x.LoadEntity(It.Is<IPakaiBrgKey>(k => k.PakaiBrgId == "PK12345678")))
            .Returns(MayBe.From(model));

        _mediator
            .Setup(x => x.Send(It.IsAny<PostInternalConsumptionVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionVoidConsequenceResult(
                failureOutcome,
                [],
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty));

        var cmd = new PakaiBrgVoidCommand("PK12345678", "USR01", "Batal pemakaian");
        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{expectedReasonFragment}*");
    }

    [Fact]
    public async Task Handle_VoidConsequenceAbortedInconsistent_ThrowsInvalidOperationException_WithDetailedReason()
    {
        var handler = CreateHandler();
        var model = CreateSampleModel("PK12345678");

        _pakaiBrgRepo
            .Setup(x => x.LoadEntity(It.Is<IPakaiBrgKey>(k => k.PakaiBrgId == "PK12345678")))
            .Returns(MayBe.From(model));

        _mediator
            .Setup(x => x.Send(It.IsAny<PostInternalConsumptionVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionVoidConsequenceResult(
                PostInternalConsumptionVoidOutcomeEnum.AbortedInconsistent,
                [],
                string.Empty,
                InconsistencyReason: "Lock timeout on batch table",
                FailedBrgId: "BRG01",
                FailedBrgMasukReffId: "DO01"));

        var cmd = new PakaiBrgVoidCommand("PK12345678", "USR01", "Batal pemakaian");
        var act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Lock timeout on batch table*");
    }

    [Fact]
    public async Task Handle_HappyPath_Success_UpdatesVoidStatusAndPersists()
    {
        // Arrange
        var handler = CreateHandler();
        var model = CreateSampleModel("PK12345678");

        _pakaiBrgRepo
            .Setup(x => x.LoadEntity(It.Is<IPakaiBrgKey>(k => k.PakaiBrgId == "PK12345678")))
            .Returns(MayBe.From(model));

        _mediator
            .Setup(x => x.Send(It.IsAny<PostInternalConsumptionVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionVoidConsequenceResult(
                PostInternalConsumptionVoidOutcomeEnum.Success,
                [],
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty));

        PakaiBrgModel? savedModel = null;
        _pakaiBrgRepo
            .Setup(x => x.SaveChanges(It.IsAny<PakaiBrgModel>()))
            .Callback<PakaiBrgModel>(m => savedModel = m);

        var cmd = new PakaiBrgVoidCommand("PK12345678", "USR_VOID", "Salah input pasien non-operasional");

        // Act
        var response = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.PakaiBrgId.Should().Be("PK12345678");
        response.IsVoid.Should().BeTrue();
        response.TglVoid.Should().Be(FixedNow);
        response.AlasanVoid.Should().Be("Salah input pasien non-operasional");

        savedModel.Should().NotBeNull();
        savedModel!.PakaiBrgId.Should().Be("PK12345678");
        savedModel.IsVoid.Should().BeTrue();
        savedModel.PetugasVoidId.Should().Be("USR_VOID");
        savedModel.AlasanVoid.Should().Be("Salah input pasien non-operasional");
        savedModel.TglVoid.Should().Be(FixedNow);
        savedModel.ListItem.Should().AllSatisfy(x => x.IsVoid.Should().BeTrue());

        _pakaiBrgRepo.Verify(x => x.SaveChanges(savedModel), Times.Once);
        _mediator.Verify(x => x.Send(
            It.Is<PostInternalConsumptionVoidConsequenceCommand>(c =>
                c.OriginalTrsReffId == "PK12345678" &&
                c.VoidTrsReffId == "PK12345678" &&
                c.TglMutasi == FixedNow &&
                c.UserId == "USR_VOID"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IdempotentOutcome_SucceedsWithoutError()
    {
        // Arrange
        var handler = CreateHandler();
        var model = CreateSampleModel("PK12345678");

        _pakaiBrgRepo
            .Setup(x => x.LoadEntity(It.Is<IPakaiBrgKey>(k => k.PakaiBrgId == "PK12345678")))
            .Returns(MayBe.From(model));

        _mediator
            .Setup(x => x.Send(It.IsAny<PostInternalConsumptionVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostInternalConsumptionVoidConsequenceResult(
                PostInternalConsumptionVoidOutcomeEnum.Idempotent,
                [],
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty));

        var cmd = new PakaiBrgVoidCommand("PK12345678", "USR_VOID", "Re-void idempotent");

        // Act
        var response = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsVoid.Should().BeTrue();
        _pakaiBrgRepo.Verify(x => x.SaveChanges(It.IsAny<PakaiBrgModel>()), Times.Once);
    }
}
