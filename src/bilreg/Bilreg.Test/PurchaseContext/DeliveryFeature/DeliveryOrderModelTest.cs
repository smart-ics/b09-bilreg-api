using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryOrderModelTest
{
    private static readonly DateTime DoDate = new(2026, 8, 22);
    private static readonly DateTime CreatedAt = new(2026, 8, 22, 8, 0, 0);
    private static readonly DateTime MutationAt = new(2026, 8, 22, 9, 30, 0);

    private static DeliveryOrderItemModel Item(
        string brgId = "BRG001",
        string layananId = "LYN001",
        decimal qtyOrder = 10,
        int itemNo = 0) =>
        new(itemNo, brgId, layananId, qtyOrder, "BOX", 100, 10, 11,
            new DateTime(2028, 12, 31), "BATCH-1");

    private static DeliveryOrderModel Order(
        DeliveryOrderStateEnum state = DeliveryOrderStateEnum.Draft,
        IEnumerable<DeliveryOrderItemModel>? items = null,
        AuditTrailType? auditTrail = null) =>
        new(
            "DLV000000001",
            "DO-001",
            new SupplierReff("SUP001", "Supplier Satu"),
            "PO-001",
            DoDate,
            state,
            "Catatan",
            auditTrail ?? AuditTrailType.Create("USR-CREATE", CreatedAt),
            items ?? []);

    [Fact]
    public void Constructor_MapsHeaderAndItems()
    {
        var item = Item(itemNo: 4);
        var audit = AuditTrailType.Create("USR-CREATE", CreatedAt);

        var sut = new DeliveryOrderModel(
            "DLV000000009", "DO-009", new SupplierReff("SUP009", "Supplier Sembilan"),
            "PO-009", DoDate, DeliveryOrderStateEnum.Open, "Notes", audit, [item]);

        sut.DeliveryOrderId.Should().Be("DLV000000009");
        sut.DoNo.Should().Be("DO-009");
        sut.Supplier.Should().Be(new SupplierReff("SUP009", "Supplier Sembilan"));
        sut.PoReffId.Should().Be("PO-009");
        sut.DoDate.Should().Be(DoDate);
        sut.State.Should().Be(DeliveryOrderStateEnum.Open);
        sut.Notes.Should().Be("Notes");
        sut.AuditTrail.Should().BeSameAs(audit);
        sut.ListItem.Should().ContainSingle().Which.Should().BeSameAs(item);
    }

    [Fact]
    public void Constructor_SnapshotsIncomingCollection()
    {
        var source = new List<DeliveryOrderItemModel> { Item(itemNo: 1) };
        var sut = Order(items: source);

        source.Add(Item("BRG002", itemNo: 2));

        sut.ListItem.Should().ContainSingle();
    }

    [Fact]
    public void Constructor_NullItems_UsesEmptyCollection()
    {
        var sut = new DeliveryOrderModel(
            "DLV000000001", "DO-001", SupplierReff.Default, "", DoDate,
            DeliveryOrderStateEnum.Draft, "", AuditTrailType.Default, null!);

        sut.ListItem.Should().BeEmpty();
        sut.IsFullyReceived.Should().BeFalse();
    }

    [Fact]
    public void Default_ReturnsDeterministicSentinels()
    {
        var sut = DeliveryOrderModel.Default;

        sut.DeliveryOrderId.Should().Be("-");
        sut.DoNo.Should().Be("-");
        sut.Supplier.Should().Be(SupplierReff.Default);
        sut.PoReffId.Should().BeEmpty();
        sut.DoDate.Should().Be(new DateTime(3000, 1, 1));
        sut.State.Should().Be(DeliveryOrderStateEnum.Draft);
        sut.Notes.Should().Be("-");
        sut.ListItem.Should().BeEmpty();
        sut.IsFullyReceived.Should().BeFalse();
    }

    [Fact]
    public void Key_ReturnsRequestedIdentity()
    {
        var result = DeliveryOrderModel.Key("DLV-KEY-1");

        result.Should().BeAssignableTo<IDeliveryOrderKey>();
        result.DeliveryOrderId.Should().Be("DLV-KEY-1");
    }

    [Fact]
    public void Create_ValidInput_ReturnsDraftWithAuditAndItems()
    {
        var supplier = new SupplierReff("SUP001", "Supplier Satu");
        var item = Item();
        var before = DateTime.Now;

        var sut = DeliveryOrderModel.Create(
            "DO-001", supplier, "PO-001", DoDate, "Notes", "USR-CREATE", [item]);

        var after = DateTime.Now;
        sut.DeliveryOrderId.Should().StartWith("DLV");
        sut.DoNo.Should().Be("DO-001");
        sut.Supplier.Should().Be(supplier);
        sut.PoReffId.Should().Be("PO-001");
        sut.DoDate.Should().Be(DoDate);
        sut.State.Should().Be(DeliveryOrderStateEnum.Draft);
        sut.Notes.Should().Be("Notes");
        sut.ListItem.Should().ContainSingle().Which.Should().BeSameAs(item);
        sut.AuditTrail.Created.UserId.Should().Be("USR-CREATE");
        sut.AuditTrail.Created.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_NullPoReference_UsesEmptyString()
    {
        var sut = DeliveryOrderModel.Create(
            "DO-001", SupplierReff.Default, null, DoDate, "", "USR-CREATE", []);

        sut.PoReffId.Should().BeEmpty();
    }

    [Fact]
    public void Create_EmptyItems_AllowsDraftAssembly()
    {
        var sut = DeliveryOrderModel.Create(
            "DO-001", SupplierReff.Default, null, DoDate, "", "USR-CREATE", []);

        sut.State.Should().Be(DeliveryOrderStateEnum.Draft);
        sut.ListItem.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidDoNo_Throws(string? doNo)
    {
        var act = () => DeliveryOrderModel.Create(
            doNo!, SupplierReff.Default, null, DoDate, "", "USR-CREATE", []);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("doNo");
    }

    [Fact]
    public void Create_NullSupplier_Throws()
    {
        var act = () => DeliveryOrderModel.Create(
            "DO-001", null!, null, DoDate, "", "USR-CREATE", []);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("supplier");
    }

    [Fact]
    public void Create_NullItems_Throws()
    {
        var act = () => DeliveryOrderModel.Create(
            "DO-001", SupplierReff.Default, null, DoDate, "", "USR-CREATE", null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("listItem");
    }

    [Fact]
    public void AddItem_NewItem_AssignsNumberAndReturnsItem()
    {
        var sut = Order();
        var item = Item();

        var result = sut.AddItem(item);

        result.Should().BeSameAs(item);
        result.ItemNo.Should().Be(1);
        sut.ListItem.Should().ContainSingle().Which.Should().BeSameAs(item);
    }

    [Fact]
    public void AddItem_AfterExistingMaximum_AssignsNextNumber()
    {
        var sut = Order(items: [Item(itemNo: 3), Item("BRG002", itemNo: 7)]);

        var result = sut.AddItem(Item("BRG003"));

        result.ItemNo.Should().Be(8);
    }

    [Fact]
    public void AddItem_SameGoodsAndLocation_MergesQuantityAndReturnsExistingItem()
    {
        var existing = Item(qtyOrder: 10, itemNo: 1);
        var sut = Order(items: [existing]);

        var result = sut.AddItem(Item(qtyOrder: 4));

        result.Should().BeSameAs(existing);
        result.QtyOrder.Should().Be(14);
        sut.ListItem.Should().ContainSingle();
    }

    [Fact]
    public void AddItem_SameGoodsDifferentLocation_AddsSeparateLine()
    {
        var sut = Order(items: [Item(itemNo: 1)]);

        sut.AddItem(Item(layananId: "LYN002"));

        sut.ListItem.Should().HaveCount(2);
    }

    [Fact]
    public void AddItem_NullItem_Throws()
    {
        var act = () => Order().AddItem(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("item");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_NonPositiveHydratedQuantity_Throws(decimal qtyOrder)
    {
        var act = () => Order().AddItem(Item(qtyOrder: qtyOrder));

        act.Should().Throw<ArgumentException>()
            .WithParameterName("item");
    }

    [Theory]
    [InlineData(DeliveryOrderStateEnum.Open)]
    [InlineData(DeliveryOrderStateEnum.Received)]
    [InlineData(DeliveryOrderStateEnum.Void)]
    public void AddItem_NonDraftState_Throws(DeliveryOrderStateEnum state)
    {
        var act = () => Order(state).AddItem(Item());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveItem_ExistingItem_RemovesAndRenumbersRemainingItems()
    {
        var sut = Order(items:
        [
            Item("BRG001", itemNo: 1),
            Item("BRG002", itemNo: 2),
            Item("BRG003", itemNo: 3)
        ]);

        sut.RemoveItem(2);

        sut.ListItem.Select(x => x.BrgId).Should().Equal("BRG001", "BRG003");
        sut.ListItem.Select(x => x.ItemNo).Should().Equal(1, 2);
    }

    [Fact]
    public void RemoveItem_FinalDraftItem_AllowsEmptyDraft()
    {
        var sut = Order(items: [Item(itemNo: 1)]);

        sut.RemoveItem(1);

        sut.ListItem.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_UnknownItem_Throws()
    {
        var act = () => Order(items: [Item(itemNo: 1)]).RemoveItem(99);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("itemNo");
    }

    [Theory]
    [InlineData(DeliveryOrderStateEnum.Open)]
    [InlineData(DeliveryOrderStateEnum.Received)]
    [InlineData(DeliveryOrderStateEnum.Void)]
    public void RemoveItem_NonDraftState_Throws(DeliveryOrderStateEnum state)
    {
        var act = () => Order(state, [Item(itemNo: 1)]).RemoveItem(1);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SetSupplier_Draft_ReplacesSupplier()
    {
        var sut = Order();
        var supplier = new SupplierReff("SUP002", "Supplier Dua");

        sut.SetSupplier(supplier);

        sut.Supplier.Should().Be(supplier);
    }

    [Fact]
    public void SetSupplier_Null_Throws()
    {
        var act = () => Order().SetSupplier(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("supplier");
    }

    [Theory]
    [InlineData(DeliveryOrderStateEnum.Open)]
    [InlineData(DeliveryOrderStateEnum.Received)]
    [InlineData(DeliveryOrderStateEnum.Void)]
    public void SetSupplier_NonDraftState_Throws(DeliveryOrderStateEnum state)
    {
        var act = () => Order(state).SetSupplier(SupplierReff.Default);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("PO-NEW", "PO-NEW")]
    [InlineData(null, "")]
    public void SetPoReff_Draft_NormalizesValue(string? value, string expected)
    {
        var sut = Order();

        sut.SetPoReff(value);

        sut.PoReffId.Should().Be(expected);
    }

    [Theory]
    [InlineData(DeliveryOrderStateEnum.Open)]
    [InlineData(DeliveryOrderStateEnum.Received)]
    [InlineData(DeliveryOrderStateEnum.Void)]
    public void SetPoReff_NonDraftState_Throws(DeliveryOrderStateEnum state)
    {
        var act = () => Order(state).SetPoReff("PO-NEW");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateNote_Draft_ReplacesNotes()
    {
        var sut = Order();

        sut.UpdateNote("Catatan baru");

        sut.Notes.Should().Be("Catatan baru");
    }

    [Theory]
    [InlineData(DeliveryOrderStateEnum.Open)]
    [InlineData(DeliveryOrderStateEnum.Received)]
    [InlineData(DeliveryOrderStateEnum.Void)]
    public void UpdateNote_NonDraftState_Throws(DeliveryOrderStateEnum state)
    {
        var act = () => Order(state).UpdateNote("Catatan baru");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReceiveItem_EmptyOrder_ThrowsAndDoesNotChangeStateOrAudit()
    {
        var sut = Order();
        var originalModified = sut.AuditTrail.Modified;

        var act = () => sut.ReceiveItem(1, 1, "USR-RECEIVE", MutationAt);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*minimal satu item*");
        sut.State.Should().Be(DeliveryOrderStateEnum.Draft);
        sut.AuditTrail.Modified.Should().Be(originalModified);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReceiveItem_NonPositiveQuantity_Throws(decimal qty)
    {
        var sut = Order(items: [Item(itemNo: 1)]);

        var act = () => sut.ReceiveItem(1, qty, "USR-RECEIVE", MutationAt);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("qtyReceived");
    }

    [Fact]
    public void ReceiveItem_UnknownItem_Throws()
    {
        var sut = Order(items: [Item(itemNo: 1)]);

        var act = () => sut.ReceiveItem(99, 1, "USR-RECEIVE", MutationAt);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("itemNo");
    }

    [Fact]
    public void ReceiveItem_QuantityExceedsRemaining_Throws()
    {
        var sut = Order(items: [Item(qtyOrder: 10, itemNo: 1)]);

        var act = () => sut.ReceiveItem(1, 11, "USR-RECEIVE", MutationAt);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("qty");
    }

    [Fact]
    public void ReceiveItem_PartialReceipt_UpdatesLineHeaderAndAudit()
    {
        var item = Item(qtyOrder: 10, itemNo: 1);
        var sut = Order(items: [item]);

        var result = sut.ReceiveItem(1, 4, "USR-RECEIVE", MutationAt);

        result.Should().BeSameAs(item);
        item.QtyReceived.Should().Be(4);
        item.QtyRemaining.Should().Be(6);
        item.State.Should().Be(DeliveryOrderItemStateEnum.Partial);
        sut.State.Should().Be(DeliveryOrderStateEnum.Open);
        sut.IsFullyReceived.Should().BeFalse();
        sut.AuditTrail.Modified.Should().Be(new AuditInfoType("USR-RECEIVE", MutationAt));
    }

    [Fact]
    public void ReceiveItem_MultipleInstallments_AccumulatesAndCompletesLine()
    {
        var item = Item(qtyOrder: 10, itemNo: 1);
        var sut = Order(items: [item]);

        sut.ReceiveItem(1, 4, "USR-1", MutationAt);
        sut.ReceiveItem(1, 6, "USR-2", MutationAt.AddHours(1));

        item.QtyReceived.Should().Be(10);
        item.QtyRemaining.Should().Be(0);
        item.State.Should().Be(DeliveryOrderItemStateEnum.Received);
        sut.State.Should().Be(DeliveryOrderStateEnum.Received);
        sut.IsFullyReceived.Should().BeTrue();
        sut.AuditTrail.Modified.Should().Be(new AuditInfoType("USR-2", MutationAt.AddHours(1)));
    }

    [Fact]
    public void ReceiveItem_MultipleLines_RemainsOpenUntilEveryLineReceived()
    {
        var first = Item("BRG001", qtyOrder: 2, itemNo: 1);
        var second = Item("BRG002", qtyOrder: 3, itemNo: 2);
        var sut = Order(items: [first, second]);

        sut.ReceiveItem(1, 2, "USR-1", MutationAt);

        sut.State.Should().Be(DeliveryOrderStateEnum.Open);
        sut.IsFullyReceived.Should().BeFalse();

        sut.ReceiveItem(2, 3, "USR-2", MutationAt.AddHours(1));

        sut.State.Should().Be(DeliveryOrderStateEnum.Received);
        sut.IsFullyReceived.Should().BeTrue();
    }

    [Theory]
    [InlineData(DeliveryOrderStateEnum.Received)]
    [InlineData(DeliveryOrderStateEnum.Void)]
    public void ReceiveItem_DisallowedState_Throws(DeliveryOrderStateEnum state)
    {
        var act = () => Order(state, [Item(itemNo: 1)])
            .ReceiveItem(1, 1, "USR-RECEIVE", MutationAt);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(DeliveryOrderStateEnum.Draft)]
    [InlineData(DeliveryOrderStateEnum.Open)]
    [InlineData(DeliveryOrderStateEnum.Received)]
    public void Void_AllowedState_MarksOrderVoidedAndAudited(DeliveryOrderStateEnum state)
    {
        var sut = Order(state, [Item(itemNo: 1)]);

        sut.Void("USR-VOID", MutationAt);

        sut.State.Should().Be(DeliveryOrderStateEnum.Void);
        sut.AuditTrail.IsVoided.Should().BeTrue();
        sut.AuditTrail.Voided.Should().Be(new AuditInfoType("USR-VOID", MutationAt));
    }

    [Fact]
    public void Void_AlreadyVoid_Throws()
    {
        var sut = Order(DeliveryOrderStateEnum.Void);

        var act = () => sut.Void("USR-VOID", MutationAt);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Void_PreventsFurtherMutationAndReceipt()
    {
        var sut = Order(items: [Item(itemNo: 1)]);
        sut.Void("USR-VOID", MutationAt);

        var mutate = () => sut.UpdateNote("Tidak boleh");
        var receive = () => sut.ReceiveItem(1, 1, "USR-RECEIVE", MutationAt.AddHours(1));

        mutate.Should().Throw<InvalidOperationException>();
        receive.Should().Throw<InvalidOperationException>();
    }
}
