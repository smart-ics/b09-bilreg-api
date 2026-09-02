using System.Text.Json;
using Bilreg.Application.ApotekContext.IntegrationFeature.Handlers;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.Shared;
using FluentAssertions;
using MediatR;
using Moq;

namespace Bilreg.Test.ApotekContext.IntegrationFeature;

public class StockReserveHandlerTest
{
    [Fact]
    public void Handler_calls_reserve_and_returns_correlation()
    {
        var stock = new Mock<IStockPharmacyPort>();
        stock.Setup(x => x.ReserveToTemporaryUnit("ADP000000001", 1, "BRG1", 5m))
            .Returns("MUT-RES-1");
        var task = ReserveTask("ADP000000001", 1, "BRG1", 5m);

        var result = new StockReserveHandler(stock.Object).Handle(task);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("MUT-RES-1");
        stock.Verify(x => x.ReserveToTemporaryUnit("ADP000000001", 1, "BRG1", 5m), Times.Once);
    }

    [Fact]
    public void Adapter_routes_pharmacy_unit_to_dispensing_temporary_unit()
    {
        PostStockTransferConsequenceCommand? captured = null;
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<PostStockTransferConsequenceCommand>(), default))
            .Callback<IRequest<PostStockTransferConsequenceResult>, CancellationToken>((cmd, _) =>
                captured = (PostStockTransferConsequenceCommand)cmd)
            .ReturnsAsync(new PostStockTransferConsequenceResult(
                PostStockTransferOutcomeEnum.Success,
                [new PostStockTransferLineResult("MUT-OUT-1", "MUT-IN-1", "", "", "", "", "", "", "")],
                0m,
                "",
                "",
                ""));

        var reff = new StockPharmacyAdapter(mediator.Object)
            .ReserveToTemporaryUnit("ADP000000001", 2, "BRG9", 3m);

        captured.Should().NotBeNull();
        captured!.SourceLayananId.Should().Be(ApotekLocationIds.PharmacyUnitLayananId);
        captured.DestLayananId.Should().Be(ApotekLocationIds.DispensingTemporaryUnitLayananId);
        captured.BrgId.Should().Be("BRG9");
        captured.Qty.Should().Be(3m);
        reff.Should().Be("MUT-OUT-1");
    }

    private static AptIntegrationTaskModel ReserveTask(string dispensingId, int itemNo, string brgId, decimal qty) =>
        AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.StockReserve,
            AptIntegrationSourceKindEnum.Dispensing,
            dispensingId,
            $"{dispensingId}:I{itemNo}:RESERVE",
            AptIntegrationDestinationEnum.StockLedger,
            JsonSerializer.Serialize(new { DispensingId = dispensingId, ItemNo = itemNo, BrgId = brgId, Qty = qty }));
}
