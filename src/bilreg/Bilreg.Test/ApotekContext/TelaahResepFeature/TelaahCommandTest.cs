using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
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
}
