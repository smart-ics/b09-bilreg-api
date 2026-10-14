using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.TelaahResepFeature;

public class TelaahCommandTest
{
    private readonly InMemoryResepKerjaRepo _resep = new();
    private readonly InMemoryTelaahRepo _telaah = new();
    private readonly InMemorySalesOrderRepo _so = new();
    private readonly InMemoryJualBebasRepo _jb = new();
    private readonly InMemoryCopyResepRepo _copy = new();
    private readonly InMemoryIntegrationTaskRepo _tasks = new();
    private readonly AllowAllAuth _auth = new();

    [Fact]
    public async Task Complete_freezes_resep_kerja_items_in_same_handler_transaction()
    {
        var resep = SeedResep(items: [Item(1, "A"), Item(2, "B")]);
        var start = await Start(resep.ResepKerjaId);
        var after1 = await Update(start, 1, TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "");
        var after2 = await Update(after1, 2, TelaahDispositionEnum.AcceptedAsPrescribed, "B", "B", 5, "");

        await new TelaahCompleteHandler(_telaah, _resep, _auth)
            .Handle(new TelaahCompleteCmd("pharm", after2.TelaahResepId, after2.Version), default);

        var frozen = _resep.LoadEntity(ResepKerjaModel.Key(resep.ResepKerjaId)).Value;
        frozen.ItemsFrozen.Should().BeTrue();
        var rewrite = () => frozen.RewriteItems(
            [Item(1, "CHANGED")], [], AuditTrailType.Create("u", DateTime.Now));
        rewrite.Should().Throw<ApotekDomainException>()
            .WithMessage("Resep Kerja items are frozen after terminal Telaah.");

        var completed = _telaah.LoadEntity(TelaahResepModel.Key(after2.TelaahResepId)).Value;
        completed.TelaahStatus.Should().Be(TelaahStatusEnum.Approved);
        completed.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public async Task Sales_order_establishment_rejected_for_rejected_telaah()
    {
        var resep = SeedResep(items: [Item(1, "A")]);
        var start = await Start(resep.ResepKerjaId);
        var updated = await Update(start, 1, TelaahDispositionEnum.Rejected, "A", "A", 0, "unsafe");
        await new TelaahCompleteHandler(_telaah, _resep, _auth)
            .Handle(new TelaahCompleteCmd("pharm", updated.TelaahResepId, updated.Version), default);

        var act = () => EstablishSo(resep.ResepKerjaId);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("Rejected or incomplete Telaah cannot establish a Sales Order.");
        _so.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Duplicate_establish_returns_same_sales_order_id()
    {
        var resep = SeedResep(items: [Item(1, "A")]);
        var start = await Start(resep.ResepKerjaId);
        var updated = await Update(start, 1, TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "");
        await new TelaahCompleteHandler(_telaah, _resep, _auth)
            .Handle(new TelaahCompleteCmd("pharm", updated.TelaahResepId, updated.Version), default);

        var first = await EstablishSo(resep.ResepKerjaId);
        var second = await EstablishSo(resep.ResepKerjaId);

        second.SalesOrderId.Should().Be(first.SalesOrderId);
        second.Version.Should().Be(first.Version);
        _so.Store.Should().ContainSingle();
    }

    [Fact]
    public async Task Partial_approval_establishes_sales_order_with_accepted_items_only()
    {
        var resep = SeedResep(items: [Item(1, "A"), Item(2, "B")]);
        var start = await Start(resep.ResepKerjaId);
        var after1 = await Update(start, 1, TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "");
        var after2 = await Update(after1, 2, TelaahDispositionEnum.Rejected, "B", "B", 0, "allergy");
        await new TelaahCompleteHandler(_telaah, _resep, _auth)
            .Handle(new TelaahCompleteCmd("pharm", after2.TelaahResepId, after2.Version), default);

        var so = await EstablishSo(resep.ResepKerjaId);

        var stored = _so.LoadEntity(SalesOrderModel.Key(so.SalesOrderId)).Value;
        stored.Items.Should().ContainSingle();
        stored.Items[0].BrgId.Should().Be("A");
        stored.Items[0].AcceptedQty.Should().Be(10m);
        stored.TelaahResepId.Should().Be(after2.TelaahResepId);
        _telaah.Store.Values.Single().TelaahStatus.Should().Be(TelaahStatusEnum.PartiallyApproved);
    }

    [Fact]
    public async Task Update_with_stale_version_conflicts()
    {
        var resep = SeedResep(items: [Item(1, "A")]);
        var start = await Start(resep.ResepKerjaId);

        var act = () => new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("pharm", start.TelaahResepId, 99, 1,
                TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, ""), default);

        await act.Should().ThrowAsync<ApotekConcurrencyException>();
        _telaah.Store.Values.Single().Version.Should().Be(start.Version);
        _telaah.Store.Values.Single().Items[0].Disposition.Should().Be(TelaahDispositionEnum.Pending);
    }

    [Fact]
    public async Task TelaahStart_initializes_13_kajian_items_with_default_safe_answers()
    {
        var resep = SeedResep(items: [Item(1, "A")]);
        var fakeCatalog = new FakeKemkesKajianResepCatalogPort();
        var handler = new TelaahStartHandler(_telaah, _resep, _auth, fakeCatalog);
        var res = await handler.Handle(new TelaahStartCmd("pharm", resep.ResepKerjaId, 0), default);

        var stored = _telaah.LoadEntity(TelaahResepModel.Key(res.TelaahResepId)).Value;
        stored.KajianItems.Should().HaveCount(13);
        stored.KajianItems.All(x => x.IsCompliant).Should().BeTrue();
        stored.KajianItems.First(x => x.KajianResepId == "1.1").IsYes.Should().BeTrue();
        stored.KajianItems.First(x => x.KajianResepId == "3.5").IsYes.Should().BeFalse();
    }

    [Fact]
    public async Task TelaahStart_copies_PrescribedBrg_from_ResepKerjaItems()
    {
        var resep = SeedResep(items: [Item(1, "MED-A")]);
        var start = await Start(resep.ResepKerjaId);

        var stored = _telaah.LoadEntity(TelaahResepModel.Key(start.TelaahResepId)).Value;
        stored.Items.Should().ContainSingle();
        var item = stored.Items[0];
        item.PrescribedBrgId.Should().Be("MED-A");
        item.PrescribedBrgName.Should().Be("MED-A");
        item.PrescribedQty.Should().Be(10m);
    }

    [Fact]
    public async Task TelaahUpdateKajian_handler_updates_kajian_and_bumps_version()
    {
        var resep = SeedResep(items: [Item(1, "A")]);
        var start = await Start(resep.ResepKerjaId);

        var handler = new TelaahUpdateKajianHandler(_telaah, _auth);
        var updateRes = await handler.Handle(new TelaahUpdateKajianCmd(
            "pharm",
            start.TelaahResepId,
            start.Version,
            [new TelaahKajianUpdate("3.5", true, "Interaksi potensial dimonitor")]), default);

        updateRes.Version.Should().Be(start.Version + 1);

        var stored = _telaah.LoadEntity(TelaahResepModel.Key(start.TelaahResepId)).Value;
        stored.Version.Should().Be(start.Version + 1);
        var item35 = stored.KajianItems.First(x => x.KajianResepId == "3.5");
        item35.IsYes.Should().BeTrue();
        item35.CatatanKajian.Should().Be("Interaksi potensial dimonitor");
        item35.RequiresNote.Should().BeTrue();
        item35.HasRequiredNote.Should().BeTrue();
    }

    [Fact]
    public async Task TelaahUpdateKajian_with_stale_version_conflicts()
    {
        var resep = SeedResep(items: [Item(1, "A")]);
        var start = await Start(resep.ResepKerjaId);

        var handler = new TelaahUpdateKajianHandler(_telaah, _auth);
        var act = () => handler.Handle(new TelaahUpdateKajianCmd(
            "pharm",
            start.TelaahResepId,
            99,
            [new TelaahKajianUpdate("3.5", true, "Interaksi")]), default);

        await act.Should().ThrowAsync<ApotekConcurrencyException>();
    }

    [Fact]
    public async Task TelaahComplete_handler_rejects_if_kajian_polarity_violation_without_note()
    {
        var resep = SeedResep(items: [Item(1, "A")]);
        var start = await Start(resep.ResepKerjaId);
        var afterItem = await Update(start, 1, TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "");

        // Violate negative polarity kajian (3.5 answered Yes without note)
        var updateKajianHandler = new TelaahUpdateKajianHandler(_telaah, _auth);
        var afterKajian = await updateKajianHandler.Handle(new TelaahUpdateKajianCmd(
            "pharm", afterItem.TelaahResepId, afterItem.Version,
            [new TelaahKajianUpdate("3.5", true, "")]), default);

        var completeHandler = new TelaahCompleteHandler(_telaah, _resep, _auth);
        var act = () => completeHandler.Handle(
            new TelaahCompleteCmd("pharm", afterKajian.TelaahResepId, afterKajian.Version), default);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*3.5*");
    }

    [Fact]
    public async Task TelaahUpdateItem_preserves_PrescribedBrg_when_substituting_or_rejecting()
    {
        var resep = SeedResep(items: [Item(1, "ORIG-BRG-1"), Item(2, "ORIG-BRG-2")]);
        var start = await Start(resep.ResepKerjaId);

        // Substitute item 1: Accepted item is SUB-BRG-1, but PrescribedBrg must remain ORIG-BRG-1
        var after1 = await Update(start, 1, TelaahDispositionEnum.AcceptedSubstitute, "SUB-BRG-1", "Substitute Drug 1", 5m, "Doctor agreed to brand substitute");

        // Reject item 2: Accepted qty becomes 0, but PrescribedBrg must remain ORIG-BRG-2 and PrescribedQty unchanged
        var after2 = await Update(after1, 2, TelaahDispositionEnum.Rejected, "ORIG-BRG-2", "Original Drug 2", 0m, "Patient allergy confirmed");

        var stored = _telaah.LoadEntity(TelaahResepModel.Key(after2.TelaahResepId)).Value;

        var item1 = stored.Items.First(x => x.ItemNo == 1);
        item1.Disposition.Should().Be(TelaahDispositionEnum.AcceptedSubstitute);
        item1.AcceptedBrgId.Should().Be("SUB-BRG-1");
        item1.AcceptedBrgName.Should().Be("Substitute Drug 1");
        item1.AcceptedQty.Should().Be(5m);
        item1.Reason.Should().Be("Doctor agreed to brand substitute");
        item1.PrescribedBrgId.Should().Be("ORIG-BRG-1");
        item1.PrescribedBrgName.Should().Be("ORIG-BRG-1");
        item1.PrescribedQty.Should().Be(10m);

        var item2 = stored.Items.First(x => x.ItemNo == 2);
        item2.Disposition.Should().Be(TelaahDispositionEnum.Rejected);
        item2.AcceptedQty.Should().Be(0m);
        item2.Reason.Should().Be("Patient allergy confirmed");
        item2.PrescribedBrgId.Should().Be("ORIG-BRG-2");
        item2.PrescribedBrgName.Should().Be("ORIG-BRG-2");
        item2.PrescribedQty.Should().Be(5m);
    }

    private ResepKerjaModel SeedResep(IEnumerable<ResepKerjaItemModel> items)
    {
        var resep = ResepKerjaModel.IntakeElectronic(
            ResepKerjaSourceKindEnum.LegacyResep, "RS-" + Guid.NewGuid().ToString("N")[..6],
            "R1", "P1", "Pasien", "D1", "Dokter", "LY01",
            0, 0, items, [], AuditTrailType.Create("u", DateTime.Now));
        _resep.SaveChanges(resep);
        return resep;
    }

    private Task<TelaahResponse> Start(string resepKerjaId)
        => new TelaahStartHandler(_telaah, _resep, _auth)
            .Handle(new TelaahStartCmd("pharm", resepKerjaId, 0), default);

    private Task<TelaahResponse> Update(
        TelaahResponse current,
        int itemNo,
        TelaahDispositionEnum disposition,
        string brgId,
        string brgName,
        decimal qty,
        string reason)
        => new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("pharm", current.TelaahResepId, current.Version, itemNo,
                disposition, brgId, brgName, qty, reason), default);

    private Task<SalesOrderEstablishResponse> EstablishSo(string resepKerjaId)
        => new SalesOrderEstablishHandler(
                _so, _telaah, _resep, _jb, DeterministicAvailableStockPort.Full(), _copy, _tasks, _auth)
            .Handle(new SalesOrderEstablishCmd("u", SalesOrderSourceKindEnum.ResepKerja, resepKerjaId,
                PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None, null), default);

    private static ResepKerjaItemModel Item(int no, string brg)
        => new(no, no, brg, brg, "TAB", "Tab", no == 1 ? 10m : 5m, 0, "3x1", "", "", false);

    private class FakeKemkesKajianResepCatalogPort : IKemkesKajianResepCatalogPort
    {
        public IReadOnlyList<TelaahResepKajianModel> GetAll() => TelaahResepKajianModel.DefaultCatalog();
    }
}
