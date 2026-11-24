using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.BedUsageContext.BhpRanapFeature;
using Bilreg.Application.BedUsageContext.BhpRanapFeature.UseCases;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.BhpRanapFeature;

public class BhpRanapVoidHandlerTest
{
    private readonly Mock<IBhpRanapRepo> _bhpRanapRepoMock;
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock;
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepoMock;
    private readonly Mock<IJurnalRepo> _jurnalRepoMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;

    private readonly BhpRanapVoidHandler _sut;

    public BhpRanapVoidHandlerTest()
    {
        _bhpRanapRepoMock = new Mock<IBhpRanapRepo>();
        _trsBillingRepoMock = new Mock<ITrsBillingRepo>();
        _tataRekeningRepoMock = new Mock<ITataRekeningRepo>();
        _jurnalRepoMock = new Mock<IJurnalRepo>();
        _mediatorMock = new Mock<IMediator>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 2, 11, 11, 0, 0));

        _sut = new BhpRanapVoidHandler(
            _bhpRanapRepoMock.Object,
            _trsBillingRepoMock.Object,
            _tataRekeningRepoMock.Object,
            _jurnalRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object);
    }

    private static BhpRanapModel CreateBhp(
        string regId = "REG001",
        string layananId = "3RI02",
        string bhpItemId = "BHP01",
        int qty = 2,
        decimal price = 15000m)
    {
        var audit = new AuditInfoType("USER1", new DateTime(2026, 2, 11, 10, 0, 0));
        return BhpRanapModel.Create(
            pakaiBedId: "BED001",
            regId: regId,
            layananId: layananId,
            bhpItemId: bhpItemId,
            bhpItemName: "Infus Set Makro",
            qty: qty,
            price: price,
            audit: audit);
    }

    [Fact]
    public async Task Handle_Success_DeletesBillingAndJurnal_UpdatesTataRekening_ReversesStock_MarksVoided()
    {
        // Arrange
        const string userId = "USER_VOID";
        const string voidReason = "Salah input item";
        var bhp = CreateBhp();
        var bill = TataRekeningTestDataBuilder.CreateBill(bhp.BhpRanapId, "REG001", 30000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate("REG001", TataRekeningStatusEnum.Opened, bill);

        _bhpRanapRepoMock.Setup(x => x.LoadEntity(It.Is<IBhpRanapKey>(k => k.BhpRanapId == bhp.BhpRanapId)))
            .Returns(MayBe.From(bhp));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(tataRekening));

        var voidResult = new PostSaleVoidConsequenceResult(
            PostSaleVoidOutcomeEnum.Success,
            [],
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);

        _mediatorMock.Setup(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(voidResult);

        var cmd = new BhpRanapVoidCmd(bhp.BhpRanapId, userId, voidReason);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.BhpRanapId.Should().Be(bhp.BhpRanapId);

        bhp.IsVoid.Should().BeTrue();
        bhp.VoidAudit.UserId.Should().Be(userId);
        bhp.VoidKet.Should().Be(voidReason);

        _bhpRanapRepoMock.Verify(x => x.SaveChanges(bhp), Times.Once);
        tataRekening.ListTrsBill.Should().NotContain(x => x.TrsBillingId == bhp.BhpRanapId);
        _tataRekeningRepoMock.Verify(x => x.SaveChanges(tataRekening), Times.Once);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == bhp.BhpRanapId)), Times.Once);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.Is<IJurnalKey>(k => k.JurnalId == bhp.BhpRanapId)), Times.Once);

        var expectedVoidTrsReffId = bhp.BhpRanapId.Length <= 8
            ? $"{bhp.BhpRanapId}_V"
            : $"{bhp.BhpRanapId[..8]}_V";

        _mediatorMock.Verify(x => x.Send(
            It.Is<PostSaleVoidConsequenceCommand>(c =>
                c.OriginalSaleTrsReffId == bhp.BhpRanapId &&
                c.VoidTrsReffId == expectedVoidTrsReffId &&
                c.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BhpNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _bhpRanapRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBhpRanapKey>()))
            .Returns(MayBe<BhpRanapModel>.None);

        var cmd = new BhpRanapVoidCmd("BHR_UNKNOWN", "USER_VOID", "Salah");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*BhpRanap 'BHR_UNKNOWN' not found*");

        _mediatorMock.Verify(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _bhpRanapRepoMock.Verify(x => x.SaveChanges(It.IsAny<BhpRanapModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.IsAny<ITrsBillingKey>()), Times.Never);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.IsAny<IJurnalKey>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BhpAlreadyVoided_ThrowsInvalidOperationException()
    {
        // Arrange
        var bhp = CreateBhp();
        bhp.Void("PREV_USER", new DateTime(2026, 2, 11, 10, 30, 0), "Previous reason");

        _bhpRanapRepoMock.Setup(x => x.LoadEntity(It.Is<IBhpRanapKey>(k => k.BhpRanapId == bhp.BhpRanapId)))
            .Returns(MayBe.From(bhp));

        var cmd = new BhpRanapVoidCmd(bhp.BhpRanapId, "USER_VOID", "Double void attempt");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan sebelumnya*");

        _mediatorMock.Verify(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _bhpRanapRepoMock.Verify(x => x.SaveChanges(It.IsAny<BhpRanapModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.IsAny<ITrsBillingKey>()), Times.Never);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.IsAny<IJurnalKey>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BillingLockedClosed_ThrowsInvalidOperationException()
    {
        // Arrange
        var bhp = CreateBhp();
        var bill = TataRekeningTestDataBuilder.CreateBill(bhp.BhpRanapId, "REG001", 30000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed("REG001", bill);

        _bhpRanapRepoMock.Setup(x => x.LoadEntity(It.Is<IBhpRanapKey>(k => k.BhpRanapId == bhp.BhpRanapId)))
            .Returns(MayBe.From(bhp));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(tataRekening));

        var cmd = new BhpRanapVoidCmd(bhp.BhpRanapId, "USER_VOID", "Salah input");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*OPEN*");

        _mediatorMock.Verify(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _bhpRanapRepoMock.Verify(x => x.SaveChanges(It.IsAny<BhpRanapModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.IsAny<ITrsBillingKey>()), Times.Never);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.IsAny<IJurnalKey>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BillingLunas_ThrowsInvalidOperationException()
    {
        // Arrange
        var bhp = CreateBhp();
        var bill = TataRekeningTestDataBuilder.CreateBill(bhp.BhpRanapId, "REG001", 30000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate("REG001", TataRekeningStatusEnum.Lunas, bill);

        _bhpRanapRepoMock.Setup(x => x.LoadEntity(It.Is<IBhpRanapKey>(k => k.BhpRanapId == bhp.BhpRanapId)))
            .Returns(MayBe.From(bhp));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(tataRekening));

        var cmd = new BhpRanapVoidCmd(bhp.BhpRanapId, "USER_VOID", "Salah input");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*lunas*");

        _mediatorMock.Verify(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _bhpRanapRepoMock.Verify(x => x.SaveChanges(It.IsAny<BhpRanapModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.IsAny<ITrsBillingKey>()), Times.Never);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.IsAny<IJurnalKey>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StockVoidFails_ThrowsInvalidOperationException_AndRollsBack()
    {
        // Arrange
        var bhp = CreateBhp();
        var bill = TataRekeningTestDataBuilder.CreateBill(bhp.BhpRanapId, "REG001", 30000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate("REG001", TataRekeningStatusEnum.Opened, bill);

        _bhpRanapRepoMock.Setup(x => x.LoadEntity(It.Is<IBhpRanapKey>(k => k.BhpRanapId == bhp.BhpRanapId)))
            .Returns(MayBe.From(bhp));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(tataRekening));

        var voidResult = new PostSaleVoidConsequenceResult(
            PostSaleVoidOutcomeEnum.AbortedInconsistent,
            [],
            string.Empty,
            "Inconsistent stock ledger state",
            "BHP01",
            string.Empty);

        _mediatorMock.Setup(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(voidResult);

        var cmd = new BhpRanapVoidCmd(bhp.BhpRanapId, "USER_VOID", "Salah input");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pengembalian stok gagal*");

        _bhpRanapRepoMock.Verify(x => x.SaveChanges(It.IsAny<BhpRanapModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.IsAny<ITrsBillingKey>()), Times.Never);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.IsAny<IJurnalKey>()), Times.Never);
        _tataRekeningRepoMock.Verify(x => x.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RawatGabungBayiVsIbu_UsesBhpRegIdForTataRekening()
    {
        // Arrange
        const string regBayi = "REG_BAYI";
        var bhpBayi = CreateBhp(regId: regBayi);
        var bill = TataRekeningTestDataBuilder.CreateBill(bhpBayi.BhpRanapId, regBayi, 15000m);
        var tataRekeningBayi = TataRekeningTestDataBuilder.Hydrate(regBayi, TataRekeningStatusEnum.Opened, bill);

        _bhpRanapRepoMock.Setup(x => x.LoadEntity(It.Is<IBhpRanapKey>(k => k.BhpRanapId == bhpBayi.BhpRanapId)))
            .Returns(MayBe.From(bhpBayi));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regBayi)))
            .Returns(MayBe.From(tataRekeningBayi));

        _mediatorMock.Setup(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostSaleVoidConsequenceResult(
                PostSaleVoidOutcomeEnum.Success,
                [],
                string.Empty, string.Empty, string.Empty, string.Empty));

        var cmd = new BhpRanapVoidCmd(bhpBayi.BhpRanapId, "USER_VOID", "Salah input");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _tataRekeningRepoMock.Verify(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regBayi)), Times.Once);
        _tataRekeningRepoMock.Verify(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_IBU")), Times.Never);
        _tataRekeningRepoMock.Verify(x => x.SaveChanges(tataRekeningBayi), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTataRekeningIsNull_DeletesBillingAndJurnal_ReversesStock_MarksVoided()
    {
        // Arrange
        var bhp = CreateBhp();
        _bhpRanapRepoMock.Setup(x => x.LoadEntity(It.Is<IBhpRanapKey>(k => k.BhpRanapId == bhp.BhpRanapId)))
            .Returns(MayBe.From(bhp));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<TataRekeningModel>.None);

        _mediatorMock.Setup(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostSaleVoidConsequenceResult(
                PostSaleVoidOutcomeEnum.Success,
                [],
                string.Empty, string.Empty, string.Empty, string.Empty));

        var cmd = new BhpRanapVoidCmd(bhp.BhpRanapId, "USER_VOID", "Void without tata rekening");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        bhp.IsVoid.Should().BeTrue();
        _bhpRanapRepoMock.Verify(x => x.SaveChanges(bhp), Times.Once);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == bhp.BhpRanapId)), Times.Once);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.Is<IJurnalKey>(k => k.JurnalId == bhp.BhpRanapId)), Times.Once);
        _tataRekeningRepoMock.Verify(x => x.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStockVoidOutcomeIsIdempotent_Succeeds()
    {
        // Arrange
        var bhp = CreateBhp();
        var bill = TataRekeningTestDataBuilder.CreateBill(bhp.BhpRanapId, "REG001", 30000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate("REG001", TataRekeningStatusEnum.Opened, bill);

        _bhpRanapRepoMock.Setup(x => x.LoadEntity(It.Is<IBhpRanapKey>(k => k.BhpRanapId == bhp.BhpRanapId)))
            .Returns(MayBe.From(bhp));
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(tataRekening));

        _mediatorMock.Setup(x => x.Send(It.IsAny<PostSaleVoidConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostSaleVoidConsequenceResult(
                PostSaleVoidOutcomeEnum.Idempotent,
                [],
                string.Empty, string.Empty, string.Empty, string.Empty));

        var cmd = new BhpRanapVoidCmd(bhp.BhpRanapId, "USER_VOID", "Idempotent stock reversal");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        bhp.IsVoid.Should().BeTrue();
        _bhpRanapRepoMock.Verify(x => x.SaveChanges(bhp), Times.Once);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == bhp.BhpRanapId)), Times.Once);
        _jurnalRepoMock.Verify(x => x.DeleteEntity(It.Is<IJurnalKey>(k => k.JurnalId == bhp.BhpRanapId)), Times.Once);
    }
}
