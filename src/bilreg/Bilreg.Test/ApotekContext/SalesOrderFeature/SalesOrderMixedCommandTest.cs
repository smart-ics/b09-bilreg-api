using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Application.ApotekContext.ResepKerjaFeature.UseCases;
using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.SalesOrderFeature;

public class SalesOrderMixedCommandTest
{
    private readonly InMemoryResepKerjaRepo _resep = new();
    private readonly InMemoryTelaahRepo _telaah = new();
    private readonly InMemorySalesOrderRepo _so = new();
    private readonly InMemoryInvoiceRepo _inv = new();
    private readonly InMemoryIntegrationTaskRepo _tasks = new();
    private readonly FakePrescriptionPort _rx = new();
    private readonly ConfigurableSepPort _sep = new();
    private readonly AllowAllAuth _auth = new();

    [Fact]
    public async Task Establish_mixed_creates_independent_bpjs_and_patient_pay_orders()
    {
        var resepId = await IntakeAndApproveTwoLines();
        _sep.CoverageByBrgId["A"] = FornasCoverageEnum.Covered;
        _sep.CoverageByBrgId["B"] = FornasCoverageEnum.NotCovered;

        var response = await CreateHandler().Handle(
            new SalesOrderEstablishMixedCmd("u", resepId, "SEP-MIX-1"), default);

        response.BpjsSalesOrderId.Should().NotBeNullOrWhiteSpace();
        response.PatientPaySalesOrderId.Should().NotBeNullOrWhiteSpace();
        response.BpjsSalesOrderId.Should().NotBe(response.PatientPaySalesOrderId);

        var orders = _so.Store.Values.ToList();
        orders.Should().HaveCount(2);
        var bpjs = orders.Single(x => x.PayerPath == PayerPathEnum.Bpjs);
        var patientPay = orders.Single(x => x.PayerPath == PayerPathEnum.GeneralPatientPay);
        bpjs.Items.Should().ContainSingle(x => x.SourceItemNo == 1 && x.AcceptedQty == 5m);
        patientPay.Items.Should().ContainSingle(x => x.SourceItemNo == 2 && x.AcceptedQty == 6m);
        patientPay.PartialReason.Should().Be(PartialReasonEnum.FornasNotCovered);
        bpjs.Items.Single().FornasCoverage.Should().Be(FornasCoverageEnum.Covered);
        bpjs.Items.Single().SepNo.Should().Be("SEP-MIX-1");
    }

    [Fact]
    public async Task Establish_mixed_rejects_when_all_items_covered()
    {
        var resepId = await IntakeAndApproveTwoLines();
        _sep.CoverageByBrgId["A"] = FornasCoverageEnum.Covered;
        _sep.CoverageByBrgId["B"] = FornasCoverageEnum.Covered;

        var act = () => CreateHandler().Handle(new SalesOrderEstablishMixedCmd("u", resepId, "SEP-1"), default);
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*both Covered and Not Covered*");
        _so.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Mixed_coverage_read_returns_both_payer_arms()
    {
        var resepId = await IntakeAndApproveTwoLines();
        _sep.CoverageByBrgId["A"] = FornasCoverageEnum.Covered;
        _sep.CoverageByBrgId["B"] = FornasCoverageEnum.NotCovered;
        await CreateHandler().Handle(new SalesOrderEstablishMixedCmd("u", resepId, "SEP-1"), default);

        var read = await new MixedCoverageReadHandler(_so, _inv)
            .Handle(new MixedCoverageReadQuery(resepId), default);

        read.BpjsOrder.Should().NotBeNull();
        read.PatientPayOrder.Should().NotBeNull();
        read.BpjsOrder!.PayerPath.Should().Be(PayerPathEnum.Bpjs);
        read.PatientPayOrder!.PayerPath.Should().Be(PayerPathEnum.GeneralPatientPay);
        read.BpjsOrder.Items.Should().ContainSingle(x => x.BrgId == "A");
        read.PatientPayOrder.Items.Should().ContainSingle(x => x.BrgId == "B");
    }

    private SalesOrderEstablishMixedHandler CreateHandler()
        => new(_so, _telaah, _resep, DeterministicAvailableStockPort.Full(), _tasks, _sep, _auth);

    private async Task<string> IntakeAndApproveTwoLines()
    {
        _rx.Contract = new PrescriptionContract(
            ResepKerjaSourceKindEnum.LegacyResep, "RS-MIX", "R1", "P1", "Pasien", "D1", "Dokter", "LY01", 0, 0,
            [
                new PrescriptionContractItem(1, "A", "A", "TAB", "TAB", 5, 0, "3x1", "", "", false),
                new PrescriptionContractItem(2, "B", "B", "TAB", "TAB", 6, 0, "3x1", "", "", false)
            ], []);
        var intake = await new ResepKerjaIntakeElectronicHandler(_resep, _rx, _auth)
            .Handle(new ResepKerjaIntakeElectronicCmd("u", ResepKerjaSourceKindEnum.LegacyResep, "RS-MIX"), default);
        var start = await new TelaahStartHandler(_telaah, _resep, _auth)
            .Handle(new TelaahStartCmd("u", intake.ResepKerjaId, 0), default);
        var v1 = await new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("u", start.TelaahResepId, start.Version, 1,
                TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 5, ""), default);
        var v2 = await new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("u", v1.TelaahResepId, v1.Version, 2,
                TelaahDispositionEnum.AcceptedAsPrescribed, "B", "B", 6, ""), default);
        await new TelaahCompleteHandler(_telaah, _resep, _auth)
            .Handle(new TelaahCompleteCmd("u", v2.TelaahResepId, v2.Version), default);
        return intake.ResepKerjaId;
    }
}
