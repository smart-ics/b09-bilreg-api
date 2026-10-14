using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.SalesOrderFeature;

public class SalesOrderMixedCommandTest
{
    private readonly InMemoryResepRepo _resep = new();
    private readonly InMemoryTelaahRepo _telaah = new();
    private readonly InMemorySalesOrderRepo _so = new();
    private readonly InMemoryInvoiceRepo _inv = new();
    private readonly InMemoryIntegrationTaskRepo _tasks = new();
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
        var regReff = new RegReff("R1", "P1", "Pasien");
        var resep = new ResepModel(
            "RS-MIX", regReff, BodyMetricType.Default(), DokterType.Default.ToReff(),
            LayananType.Default.ToReff(), LayananType.Default.ToReff(),
            UrgenitasType.Default, TipeBrgType.Default.ToReff(), 0, "desc",
            DateTime.Today, AppConst.DASH, AppConst.DASH, AuditTrailType.Default, []);
        resep.AddObat(BrgObatType.Default with { BrgId = "A", BrgName = "A" }, SatuanType.Default, 5m, 0, "3x1", "", "");
        resep.AddObat(BrgObatType.Default with { BrgId = "B", BrgName = "B" }, SatuanType.Default, 6m, 0, "3x1", "", "");
        _resep.SaveChanges(resep);

        var start = await new TelaahStartHandler(_telaah, _resep, _auth)
            .Handle(new TelaahStartCmd("u", resep.ResepId, 0), default);
        var v1 = await new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("u", start.TelaahResepId, start.Version, 1,
                TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 5, ""), default);
        var v2 = await new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("u", v1.TelaahResepId, v1.Version, 2,
                TelaahDispositionEnum.AcceptedAsPrescribed, "B", "B", 6, ""), default);
        await new TelaahCompleteHandler(_telaah, _resep, _auth)
            .Handle(new TelaahCompleteCmd("u", v2.TelaahResepId, v2.Version), default);
        return resep.ResepId;
    }
}
