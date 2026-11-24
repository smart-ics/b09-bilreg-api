using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.PakaiBrgFeature;

public class SettlePendingTindakanStockHandlerTests
{
    private readonly Mock<ITindakanStokPendingRepo> _pendingRepo = new();
    private readonly Mock<ILegacyStockReadPort> _legacyStockReadPort = new();
    private readonly Mock<IPakaiBrgInternalService> _pakai = new();
    private readonly Mock<ITglJamProvider> _tglJam = new();
    private readonly List<TindakanStokPendingType> _active = [];
    private int _pakaiSeq;

    private SettlePendingTindakanStockHandler CreateSut(decimal stock)
    {
        _tglJam.Setup(x => x.Now).Returns(new DateTime(2026, 1, 1));
        _legacyStockReadPort
            .Setup(x => x.ListBalancesAtLocation("B1", "L1"))
            .Returns(() => stock <= 0
                ? []
                : [new LegacyStockBalanceReadModel("STK1", "B1", "DO1", "L1", default, "", stock, 1m, default)]);
        _pendingRepo
            .Setup(x => x.ListFifoAktif(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => _active.Where(p => !p.StatusPotStok).ToList());
        _pendingRepo
            .Setup(x => x.MarkSettled(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .Callback((string id, string pk, decimal q, DateTime d, string u) =>
            {
                var i = _active.FindIndex(p => p.PendingId == id);
                _active[i] = _active[i] with { StatusPotStok = true, PakaiBrgId = pk };
            });
        _pakai
            .Setup(x => x.CreateFromTindakan(It.IsAny<PakaiBrgCreateFromTindakanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new PakaiBrgCreateFromTindakanResult($"PK{++_pakaiSeq}", DateTime.Now, 0m, 1, []));

        return new SettlePendingTindakanStockHandler(
            _pendingRepo.Object, _pakai.Object, _tglJam.Object, _legacyStockReadPort.Object);
    }

    private static TindakanStokPendingType Row(string id, decimal qty, string reg = "REG") =>
        new(id, "TDK" + id, "TB" + id, 1, reg, "L1", "B1", "PCS", qty);

    private static SettlePendingTindakanStockCommand Cmd => new("B1", "L1", "U1");

    [Fact]
    public async Task WholeUnits_FifoDrain_SettlesInOrderUntilStockRunsOut()
    {
        _active.AddRange([Row("P1", 2m, "R1"), Row("P2", 2m, "R2"), Row("P3", 1m, "R3")]);
        var sut = CreateSut(stock: 3m);

        var result = await sut.Settle(Cmd);

        result.SettledCount.Should().Be(1);
        result.Items.Single().PendingId.Should().Be("P1");
        _pakai.Verify(x => x.CreateFromTindakan(
            It.Is<PakaiBrgCreateFromTindakanCommand>(c =>
                c.RegId == "R1" && c.KeteranganHeader == "Pot-Tdk TDKP1" && c.ListItem.Single().Qty == 2m),
            It.IsAny<CancellationToken>()), Times.Once);
        _active.Single(p => p.PendingId == "P2").StatusPotStok.Should().BeFalse();
        _active.Single(p => p.PendingId == "P3").StatusPotStok.Should().BeFalse();
    }

    [Fact]
    public async Task Fractions_AccumulateToWholeUnit_UsesFirstRowRegId()
    {
        _active.AddRange([Row("P1", 0.4m, "R1"), Row("P2", 0.4m, "R2"), Row("P3", 0.4m, "R3")]);
        var sut = CreateSut(stock: 5m);

        var result = await sut.Settle(Cmd);

        result.SettledCount.Should().Be(3);
        result.PakaiBrgIds.Should().ContainSingle();
        _pakai.Verify(x => x.CreateFromTindakan(
            It.Is<PakaiBrgCreateFromTindakanCommand>(c => c.RegId == "R1" && c.ListItem.Single().Qty == 1m),
            It.IsAny<CancellationToken>()), Times.Once);
        _active.Should().OnlyContain(p => p.StatusPotStok && p.PakaiBrgId == "PK1");
    }

    [Fact]
    public async Task Fractions_BelowOneUnit_StayPending()
    {
        _active.AddRange([Row("P1", 0.4m), Row("P2", 0.4m)]);
        var sut = CreateSut(stock: 5m);

        var result = await sut.Settle(Cmd);

        result.SettledCount.Should().Be(0);
        _pakai.Verify(x => x.CreateFromTindakan(It.IsAny<PakaiBrgCreateFromTindakanCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _active.Should().OnlyContain(p => !p.StatusPotStok);
    }

    [Fact]
    public async Task InsufficientStock_NothingSettled()
    {
        _active.AddRange([Row("P1", 1m), Row("P2", 0.6m), Row("P3", 0.6m)]);
        var sut = CreateSut(stock: 0.5m);

        var result = await sut.Settle(Cmd);

        result.SettledCount.Should().Be(0);
        _pakai.Verify(x => x.CreateFromTindakan(It.IsAny<PakaiBrgCreateFromTindakanCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _pendingRepo.Verify(x => x.MarkSettled(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CalledTwice_IsIdempotent_NoDuplicateDocument()
    {
        _active.Add(Row("P1", 1m));
        var sut = CreateSut(stock: 5m);

        var first = await sut.Settle(Cmd);
        var second = await sut.Settle(Cmd);

        first.SettledCount.Should().Be(1);
        second.SettledCount.Should().Be(0);
        _pakai.Verify(x => x.CreateFromTindakan(It.IsAny<PakaiBrgCreateFromTindakanCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Trigger_SwallowsSenderFailure()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<SettlePendingTindakanStockCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("OCC"));

        var act = () => PendingSettleTrigger.TryDrain(sender.Object, null, "U1", [("B1", "L1")]);

        await act.Should().NotThrowAsync();
        sender.Verify(x => x.Send(It.IsAny<SettlePendingTindakanStockCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
