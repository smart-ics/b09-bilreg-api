using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using FluentAssertions;
using MediatR;
using Moq;

namespace Bilreg.Test.ApotekContext.Shared;

public class StockPharmacyAdapterTest
{
    [Fact]
    public void ReserveToTemporaryUnit_PassesTrimmedDispensingIdAsTrsReffId()
    {
        var mediator = new Mock<IMediator>();
        PostStockTransferConsequenceCommand? captured = null;
        mediator
            .Setup(m => m.Send(It.IsAny<PostStockTransferConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PostStockTransferConsequenceResult>, CancellationToken>((req, _) =>
                captured = (PostStockTransferConsequenceCommand)req)
            .ReturnsAsync(new PostStockTransferConsequenceResult(
                PostStockTransferOutcomeEnum.Success,
                [new PostStockTransferLineResult("MUT-OUT-1", "MUT-IN-1", "LYN1", "LYN2", "STB01", "DO01", "BK1", "BK2", "STK01")],
                0m,
                string.Empty,
                string.Empty,
                string.Empty));

        var sut = new StockPharmacyAdapter(mediator.Object);
        var result = sut.ReserveToTemporaryUnit(" DSP0000001 ", 1, "BRG01", 2m);

        result.Should().Be("MUT-OUT-1");
        captured.Should().NotBeNull();
        captured!.TrsReffId.Should().Be("DSP0000001");
        captured.TrsReffId.Length.Should().BeLessOrEqualTo(10);
    }

    [Fact]
    public void RemoveOnHandover_PassesTrimmedDispensingIdAsTrsReffId()
    {
        var mediator = new Mock<IMediator>();
        PostDispenseIssueConsequenceCommand? captured = null;
        mediator
            .Setup(m => m.Send(It.IsAny<PostDispenseIssueConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PostDispenseIssueConsequenceResult>, CancellationToken>((req, _) =>
                captured = (PostDispenseIssueConsequenceCommand)req)
            .ReturnsAsync(new PostDispenseIssueConsequenceResult(
                PostDispenseIssueOutcomeEnum.Success,
                [new PostDispenseIssueLineResult("MUT-1", "STL01", "STB01", "DO01", "BK1", "STK01", 2m)],
                0m,
                string.Empty,
                string.Empty,
                string.Empty));

        var sut = new StockPharmacyAdapter(mediator.Object);
        var result = sut.RemoveOnHandover(" DSP0000001 ", 1, "BRG01", 2m);

        result.Should().Be("MUT-1");
        captured.Should().NotBeNull();
        captured!.TrsReffId.Should().Be("DSP0000001");
        captured.TrsReffId.Length.Should().BeLessOrEqualTo(10);
    }

    [Fact]
    public void ReturnOnNoShow_PassesTrimmedDispensingIdAsTrsReffId()
    {
        var mediator = new Mock<IMediator>();
        PostStockTransferConsequenceCommand? captured = null;
        mediator
            .Setup(m => m.Send(It.IsAny<PostStockTransferConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PostStockTransferConsequenceResult>, CancellationToken>((req, _) =>
                captured = (PostStockTransferConsequenceCommand)req)
            .ReturnsAsync(new PostStockTransferConsequenceResult(
                PostStockTransferOutcomeEnum.Success,
                [new PostStockTransferLineResult("MUT-OUT-2", "MUT-IN-2", "LYN2", "LYN1", "STB01", "DO01", "BK1", "BK2", "STK01")],
                0m,
                string.Empty,
                string.Empty,
                string.Empty));

        var sut = new StockPharmacyAdapter(mediator.Object);
        var result = sut.ReturnOnNoShow(" DSP0000001 ", 1, "BRG01", 2m);

        result.Should().Be("MUT-OUT-2");
        captured.Should().NotBeNull();
        captured!.TrsReffId.Should().Be("DSP0000001");
        captured.TrsReffId.Length.Should().BeLessOrEqualTo(10);
    }
}
