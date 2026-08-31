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

public class StockRemoveOnHandoverHandlerTest
{
    [Fact]
    public void Handler_calls_remove_on_handover_and_returns_correlation()
    {
        var stock = new Mock<IStockPharmacyPort>();
        stock.Setup(x => x.RemoveOnHandover("ADP000000001", 1, "BRG1", 5m))
            .Returns("MUT-ISSUE-1");
        var task = RemoveTask("ADP000000001", 1, "BRG1", 5m);

        var result = new StockRemoveOnHandoverHandler(stock.Object).Handle(task);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("MUT-ISSUE-1");
        stock.Verify(x => x.RemoveOnHandover("ADP000000001", 1, "BRG1", 5m), Times.Once);
    }

    [Fact]
    public void Adapter_routes_dispensing_temporary_unit_dispense_issue()
    {
        PostDispenseIssueConsequenceCommand? captured = null;
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<PostDispenseIssueConsequenceCommand>(), default))
            .Callback<IRequest<PostDispenseIssueConsequenceResult>, CancellationToken>((cmd, _) =>
                captured = (PostDispenseIssueConsequenceCommand)cmd)
            .ReturnsAsync(new PostDispenseIssueConsequenceResult(
                PostDispenseIssueOutcomeEnum.Success,
                [new PostDispenseIssueLineResult("MUT-DI-1", "", "", "", "", "", 0m)],
                0m,
                "",
                "",
                ""));

        var reff = new StockPharmacyAdapter(mediator.Object)
            .RemoveOnHandover("ADP000000002", 2, "BRG9", 3m);

        captured.Should().NotBeNull();
        captured!.LayananId.Should().Be(ApotekLocationIds.DispensingTemporaryUnitLayananId);
        captured.BrgId.Should().Be("BRG9");
        captured.Qty.Should().Be(3m);
        captured.TrsReffId.Should().Be("ADP000000002:2:I");
        reff.Should().Be("MUT-DI-1");
    }

    private static AptIntegrationTaskModel RemoveTask(string dispensingId, int itemNo, string brgId, decimal qty) =>
        AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.StockRemoveOnHandover,
            AptIntegrationSourceKindEnum.Dispensing,
            dispensingId,
            $"{dispensingId}:I{itemNo}:ISSUE",
            AptIntegrationDestinationEnum.StockLedger,
            JsonSerializer.Serialize(new { DispensingId = dispensingId, ItemNo = itemNo, BrgId = brgId, Qty = qty }));
}
