using Bilreg.Application.ApotekContext.JualBebasFeature.UseCases;
using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Domain.ApotekContext.JualBebasFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.JualBebasFeature;

public class JualBebasCommandTest
{
    private readonly InMemoryJualBebasRepo _jb = new();
    private readonly InMemorySalesOrderRepo _so = new();
    private readonly AllowAllAuth _auth = new();

    [Fact]
    public async Task Accept_persists_one_header_with_catalog_items()
    {
        var response = await Accept();

        response.JualBebasId.Should().StartWith(JualBebasModel.IdPrefix);
        _jb.Store.Should().ContainKey(response.JualBebasId);
        var stored = _jb.Store.Values.Single();
        stored.RequestStatus.Should().Be(JualBebasRequestStatusEnum.Accepted);
        stored.Items.Select(x => (x.ItemNo, x.BrgId, x.Qty)).Should().Equal((1, "BRG1", 2m));
    }

    [Fact]
    public async Task Decline_after_accept_survives_save_load_roundtrip_with_actor_identity()
    {
        var accepted = await Accept();

        await new JualBebasDeclineAfterAcceptHandler(_jb, _auth)
            .Handle(new JualBebasDeclineAfterAcceptCmd("decliner", accepted.JualBebasId, 1), default);

        var reloaded = _jb.LoadEntity(JualBebasModel.Key(accepted.JualBebasId)).Value;
        reloaded.RequestStatus.Should().Be(JualBebasRequestStatusEnum.DeclinedAfterAccept);
        reloaded.DeclinedBy.Should().Be("decliner");
        reloaded.DeclinedAt.Year.Should().Be(DateTime.Now.Year);
        reloaded.Version.Should().Be(2);
    }

    [Fact]
    public async Task Decline_of_unknown_id_fails_without_creating_rows()
    {
        var act = () => new JualBebasDeclineAfterAcceptHandler(_jb, _auth)
            .Handle(new JualBebasDeclineAfterAcceptCmd("u", "ADQ-NOPE", 1), default);
        await act.Should().ThrowAsync<Exception>().WithMessage("*not found*");
        _jb.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Decline_with_stale_version_conflicts_and_keeps_row_accepted()
    {
        var accepted = await Accept();

        var act = () => new JualBebasDeclineAfterAcceptHandler(_jb, _auth)
            .Handle(new JualBebasDeclineAfterAcceptCmd("u", accepted.JualBebasId, 99), default);

        await act.Should().ThrowAsync<ApotekConcurrencyException>();
        _jb.Store.Values.Single().RequestStatus.Should().Be(JualBebasRequestStatusEnum.Accepted);
    }

    [Fact]
    public async Task Ordinary_pre_accept_decline_leaves_no_row_and_no_establishable_source()
    {
        _jb.Store.Should().BeEmpty();

        var act = () => EstablishSo("ADQ-Never-Accepted");

        await act.Should().ThrowAsync<Exception>().WithMessage("*not found*");
        _jb.Store.Should().BeEmpty();
        _so.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Sales_order_establishment_rejected_for_declined_demand()
    {
        var accepted = await Accept();
        await new JualBebasDeclineAfterAcceptHandler(_jb, _auth)
            .Handle(new JualBebasDeclineAfterAcceptCmd("decliner", accepted.JualBebasId, 1), default);

        var act = () => EstablishSo(accepted.JualBebasId);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("Only an accepted Jual Bebas can establish a Sales Order.");
        _so.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Establishment_converts_demand_and_blocks_decline_after_conversion()
    {
        var accepted = await Accept();

        await EstablishSo(accepted.JualBebasId);

        var converted = _jb.LoadEntity(JualBebasModel.Key(accepted.JualBebasId)).Value;
        converted.RequestStatus.Should().Be(JualBebasRequestStatusEnum.ConvertedToSalesOrder);
        converted.Version.Should().Be(2);
        var act = () => new JualBebasDeclineAfterAcceptHandler(_jb, _auth)
            .Handle(new JualBebasDeclineAfterAcceptCmd("u", accepted.JualBebasId, 2), default);
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("Only an accepted Jual Bebas can be cancelled after accept.");
    }

    [Fact]
    public async Task Establishment_rejected_after_conversion_so_no_duplicate_sales_order()
    {
        var accepted = await Accept();
        await EstablishSo(accepted.JualBebasId);

        var act = () => EstablishSo(accepted.JualBebasId);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("Only an accepted Jual Bebas can establish a Sales Order.");
        _so.Store.Should().ContainSingle();
    }

    private async Task<JualBebasAcceptResponse> Accept()
        => await new JualBebasAcceptHandler(_jb, _auth)
            .Handle(new JualBebasAcceptCmd("acceptor", "R1", "P1", "Pasien", [
                new JualBebasAcceptItem(1, "BRG1", "Obat", "TAB", 2, "3x1")
            ]), default);

    private async Task<SalesOrderEstablishResponse> EstablishSo(string jualBebasId)
        => await new SalesOrderEstablishHandler(_so, new InMemoryTelaahRepo(), new InMemoryResepKerjaRepo(),
                _jb, DeterministicAvailableStockPort.Full(), new InMemoryCopyResepRepo(),
                new InMemoryIntegrationTaskRepo(), _auth)
            .Handle(new SalesOrderEstablishCmd("u", SalesOrderSourceKindEnum.JualBebas, jualBebasId,
                PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None, null), default);
}
