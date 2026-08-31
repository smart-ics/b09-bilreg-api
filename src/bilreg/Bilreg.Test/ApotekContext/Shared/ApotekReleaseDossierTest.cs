using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.ApotekContext.Shared;

/// <summary>
/// APT-B30 backend release dossier: documents open production gates and integration contract surface.
/// Code-review GO does not clear these gates.
/// </summary>
public class ApotekReleaseDossierTest
{
    private static readonly (string Id, string Interim)[] OpenGates =
    [
        ("PD-09", "IAvailableStockPort fail-closed; never persist Available Stock"),
        ("PD-08", "Manual Tata Rekening workflow + TataRekeningCorrectionReff correlation"),
        ("BC-11", "Canonical ServedAt/DoneAt only; no call-purpose persistence"),
        ("BC-12", "Authenticated actor + policy seam; no ratified role matrix"),
        ("BC-13", "CaptureNote/DocumentRef only for physical prescriptions"),
    ];

    [Fact]
    public void Release_gate_ledger_lists_all_open_production_gates()
    {
        OpenGates.Select(x => x.Id).Should().BeEquivalentTo(["PD-09", "PD-08", "BC-11", "BC-12", "BC-13"]);
        foreach (var (id, interim) in OpenGates)
        {
            interim.Should().NotBeNullOrWhiteSpace($"{id} must document a safe interim");
        }
    }

    [Fact]
    public void Bc12_policy_seam_references_release_gate_constant()
    {
        ApotekReleaseGates.Bc12CommandRoleMatrix.Should().Be("BC-12");
    }

    [Theory]
    [InlineData(typeof(AptIntegrationTaskTypeEnum), "TrackerServedAt")]
    [InlineData(typeof(AptIntegrationTaskTypeEnum), "TrackerDoneAtPickup")]
    [InlineData(typeof(AptIntegrationTaskTypeEnum), "TrackerDoneAtNoShow")]
    [InlineData(typeof(AptIntegrationTaskTypeEnum), "TrackerWithdrawn")]
    [InlineData(typeof(AptIntegrationTaskTypeEnum), "StockReserve")]
    [InlineData(typeof(AptIntegrationTaskTypeEnum), "StockRemoveOnHandover")]
    [InlineData(typeof(AptIntegrationTaskTypeEnum), "StockReturnNoShow")]
    [InlineData(typeof(AptIntegrationTaskTypeEnum), "BillingCharge")]
    [InlineData(typeof(AptIntegrationTaskTypeEnum), "IterConsume")]
    public void Neighbor_integration_task_types_are_declared(Type enumType, string member)
    {
        Enum.Parse(enumType, member).Should().NotBeNull();
    }
}
