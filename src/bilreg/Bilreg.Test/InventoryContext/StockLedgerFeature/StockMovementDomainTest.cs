using System.Reflection;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockMovementDomainTest
{
    private static readonly DateTime EffectiveAt = new(2026, 8, 7, 10, 30, 0);
    private static readonly BrgReff Item = new("BRG01", "Paracetamol");
    private static readonly ReceiptSourceType ReceiptSource = ReceiptSourceType.Create("DO001");
    private static readonly ILayananKey Gudang = LayananType.Key("GDN01");
    private static readonly ILayananKey Apotek = LayananType.Key("APT01");
    private static readonly UnitValuationType Valuation = UnitValuationType.Create(1250.50m);
    private static readonly SourceTransactionReferenceType SourceTrs =
        SourceTransactionReferenceType.Create("DM-20260807-001");

    [Fact]
    public void UT01_CreateReceipt_ValidMovement_Succeeds()
    {
        var line = Line(1, Gudang, StockMovementDirectionEnum.Inbound, 10m);

        var movement = StockMovementModel.CreateReceipt(
            SourceTrs, EffectiveAt, [line], StockFactOriginEnum.Native, "MOV-RCPT-1");

        movement.StockMovementId.Should().Be("MOV-RCPT-1");
        movement.MovementKind.Should().Be(StockMovementKindEnum.Receipt);
        movement.SourceTransactionId.Should().Be("DM-20260807-001");
        movement.EffectiveBusinessTime.Should().Be(EffectiveAt);
        movement.Origin.Should().Be(StockFactOriginEnum.Native);
        movement.Lines.Should().HaveCount(1);
        movement.ReversedMovementId.Should().BeNull();
        movement.CorrectedMovementId.Should().BeNull();
    }

    [Fact]
    public void UT02_CreateLine_NonPositiveQuantity_IsRejected()
    {
        Action zero = () => Line(1, Gudang, StockMovementDirectionEnum.Inbound, 0m);
        Action negative = () => Line(1, Gudang, StockMovementDirectionEnum.Inbound, -5m);

        zero.Should().Throw<ArgumentException>();
        negative.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UT03_CompletedMovement_CannotBeMutated()
    {
        var movement = StockMovementModel.CreateReceipt(
            SourceTrs, EffectiveAt,
            [Line(1, Gudang, StockMovementDirectionEnum.Inbound, 4m)],
            StockFactOriginEnum.Native);

        movement.Lines.Should().BeAssignableTo<IReadOnlyList<StockMovementLineType>>();

        var list = movement.Lines.Should().BeAssignableTo<IList<StockMovementLineType>>().Subject;
        list.IsReadOnly.Should().BeTrue();

        var mutate = () => list.Add(Line(99, Gudang, StockMovementDirectionEnum.Inbound, 1m));
        mutate.Should().Throw<NotSupportedException>();

        var originalId = movement.StockMovementId;
        var originalLineCount = movement.Lines.Count;

        var reversal = movement.Reverse(
            SourceTransactionReferenceType.Create("VOID-001"),
            EffectiveAt.AddHours(1),
            StockFactOriginEnum.Native);

        movement.StockMovementId.Should().Be(originalId);
        movement.Lines.Should().HaveCount(originalLineCount);
        movement.MovementKind.Should().Be(StockMovementKindEnum.Receipt);
        reversal.StockMovementId.Should().NotBe(originalId);
    }

    [Fact]
    public void UT04_Reverse_CreatesNewMovementReferencingOriginal()
    {
        var original = StockMovementModel.CreateOutbound(
            SourceTrs, EffectiveAt,
            [Line(1, Apotek, StockMovementDirectionEnum.Outbound, 3m)],
            StockFactOriginEnum.Native,
            "MOV-OUT-1");

        var reversal = original.Reverse(
            SourceTransactionReferenceType.Create("VOID-OUT-1"),
            EffectiveAt.AddMinutes(15),
            StockFactOriginEnum.Native,
            "MOV-REV-1");

        reversal.StockMovementId.Should().Be("MOV-REV-1");
        reversal.MovementKind.Should().Be(StockMovementKindEnum.Reversal);
        reversal.ReversedMovementId.Should().Be("MOV-OUT-1");
        reversal.CorrectedMovementId.Should().BeNull();
        reversal.SourceTransactionId.Should().Be("VOID-OUT-1");
        reversal.Lines.Should().ContainSingle();
        reversal.Lines[0].Direction.Should().Be(StockMovementDirectionEnum.Inbound);
        reversal.Lines[0].Quantity.Should().Be(3m);

        original.MovementKind.Should().Be(StockMovementKindEnum.Outbound);
        original.Lines[0].Direction.Should().Be(StockMovementDirectionEnum.Outbound);
    }

    [Fact]
    public void UT05_Correct_CreatesNewMovementAndPreservesOriginalHistory()
    {
        var original = StockMovementModel.CreateReceipt(
            SourceTrs, EffectiveAt,
            [Line(1, Gudang, StockMovementDirectionEnum.Inbound, 10m)],
            StockFactOriginEnum.Native,
            "MOV-RCPT-2");

        var correctionLine = Line(1, Gudang, StockMovementDirectionEnum.Outbound, 2m);
        var correction = original.Correct(
            SourceTransactionReferenceType.Create("CORR-001"),
            EffectiveAt.AddHours(2),
            [correctionLine],
            StockFactOriginEnum.Native,
            "MOV-CORR-1");

        correction.StockMovementId.Should().Be("MOV-CORR-1");
        correction.MovementKind.Should().Be(StockMovementKindEnum.Correction);
        correction.CorrectedMovementId.Should().Be("MOV-RCPT-2");
        correction.ReversedMovementId.Should().BeNull();
        correction.Lines.Should().ContainSingle();
        correction.Lines[0].Quantity.Should().Be(2m);
        correction.Lines[0].Direction.Should().Be(StockMovementDirectionEnum.Outbound);

        original.StockMovementId.Should().Be("MOV-RCPT-2");
        original.MovementKind.Should().Be(StockMovementKindEnum.Receipt);
        original.Lines.Should().ContainSingle();
        original.Lines[0].Quantity.Should().Be(10m);
        original.CorrectedMovementId.Should().BeNull();
    }

    [Fact]
    public void UT06_CreateTransfer_EqualOutboundAndInbound_Succeeds()
    {
        var lines = new[]
        {
            Line(1, Gudang, StockMovementDirectionEnum.Outbound, 7m),
            Line(2, Apotek, StockMovementDirectionEnum.Inbound, 7m)
        };

        var transfer = StockMovementModel.CreateTransfer(
            SourceTransactionReferenceType.Create("TR-001"),
            EffectiveAt,
            lines,
            StockFactOriginEnum.Native,
            "MOV-TR-1");

        transfer.MovementKind.Should().Be(StockMovementKindEnum.Transfer);
        transfer.TotalQuantity(StockMovementDirectionEnum.Outbound).Should().Be(7m);
        transfer.TotalQuantity(StockMovementDirectionEnum.Inbound).Should().Be(7m);
        transfer.Lines.Should().HaveCount(2);
    }

    [Fact]
    public void UT07_CreateTransfer_UnequalOutboundAndInbound_IsRejected()
    {
        var lines = new[]
        {
            Line(1, Gudang, StockMovementDirectionEnum.Outbound, 7m),
            Line(2, Apotek, StockMovementDirectionEnum.Inbound, 5m)
        };

        Action act = () => StockMovementModel.CreateTransfer(
            SourceTransactionReferenceType.Create("TR-BAD"),
            EffectiveAt,
            lines,
            StockFactOriginEnum.Native);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*not conserved*");
    }

    [Fact]
    public void UT08_MovementLine_PreservesItemReceiptSourceLocationValuationAndOrigin()
    {
        var line = StockMovementLineType.Create(
            1,
            Item,
            ReceiptSource,
            Apotek,
            StockMovementDirectionEnum.Outbound,
            4.5m,
            Valuation,
            StockFactOriginEnum.LegacySynchronized);

        var movement = StockMovementModel.CreateOutbound(
            SourceTrs, EffectiveAt, [line], StockFactOriginEnum.LegacySynchronized);

        var stored = movement.Lines.Single();
        stored.BrgId.Should().Be("BRG01");
        stored.ReceiptSourceId.Should().Be("DO001");
        stored.LayananId.Should().Be("APT01");
        stored.Quantity.Should().Be(4.5m);
        stored.UnitValuation.AmountPerUnit.Should().Be(1250.50m);
        stored.Origin.Should().Be(StockFactOriginEnum.LegacySynchronized);
        stored.Direction.Should().Be(StockMovementDirectionEnum.Outbound);
        movement.Origin.Should().Be(StockFactOriginEnum.LegacySynchronized);
    }

    [Fact]
    public void UT09_CreateReceipt_OutboundLine_IsRejected()
    {
        Action act = () => StockMovementModel.CreateReceipt(
            SourceTrs, EffectiveAt,
            [Line(1, Gudang, StockMovementDirectionEnum.Outbound, 1m)],
            StockFactOriginEnum.Native);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Inbound*");
    }

    [Fact]
    public void UT10_OriginEnum_DoesNotEncodeAuthority()
    {
        typeof(StockMovementModel)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(m => m.Name)
            .Should()
            .NotContain(name => name.Contains("Authoritative", StringComparison.OrdinalIgnoreCase));

        typeof(StockMovementLineType)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(m => m.Name)
            .Should()
            .NotContain(name => name.Contains("Authoritative", StringComparison.OrdinalIgnoreCase));
    }

    private static StockMovementLineType Line(
        int lineNo,
        ILayananKey location,
        StockMovementDirectionEnum direction,
        decimal quantity)
        => StockMovementLineType.Create(
            lineNo,
            Item,
            ReceiptSource,
            location,
            direction,
            quantity,
            Valuation,
            StockFactOriginEnum.Native);
}
