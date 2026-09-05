using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.ApotekContext.WorklistFeature;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.ApotekContext.WorklistFeature;

public class AptWorklistHandlerTest
{
    private readonly Mock<IAptWorklistDal> _dal = new();

    [Fact]
    public async Task TelaahWorklistHandler_delegates_to_dal()
    {
        var expected = new List<TelaahWorklistItem>
        {
            new("", "ARX1", "Pasien A", TelaahStatusEnum.Available, DateTime.Now)
        };
        _dal.Setup(x => x.ListTelaah()).Returns(expected);
        var sut = new TelaahWorklistHandler(_dal.Object);

        var result = await sut.Handle(new TelaahWorklistQuery(), default);

        result.Should().BeSameAs(expected);
        _dal.Verify(x => x.ListTelaah(), Times.Once);
    }

    [Fact]
    public async Task PelayananWorklistHandler_passes_queue_filter_and_business_date()
    {
        var businessDate = new DateOnly(2026, 8, 27);
        PelayananWorklistQuery? captured = null;
        _dal.Setup(x => x.ListPelayanan(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<DateOnly>()))
            .Callback<string, int?, DateOnly>((antrianId, noUrut, date) =>
                captured = new PelayananWorklistQuery(antrianId, noUrut, date))
            .Returns([]);
        var sut = new PelayananWorklistHandler(_dal.Object);

        await sut.Handle(new PelayananWorklistQuery("ANT1", 2, businessDate), default);

        captured.Should().NotBeNull();
        captured!.AntrianId.Should().Be("ANT1");
        captured.NoUrut.Should().Be(2);
        captured.BusinessDate.Should().Be(businessDate);
    }

    [Fact]
    public void PelayananWorklistItem_exposes_computed_attention_labels_without_queue_milestones()
    {
        var names = typeof(PelayananWorklistItem).GetProperties().Select(x => x.Name).ToArray();
        names.Should().NotContain("ServedAt");
        names.Should().NotContain("DoneAt");
        names.Should().Contain("AttentionLabel");
    }

    [Fact]
    public void PelayananWorklistItem_supports_multiple_demands_for_one_queue_entry()
    {
        var items = new[]
        {
            new PelayananWorklistItem("ANT1", 1, QueueDemandKindEnum.ResepKerja, "RK1", "A", "", PayerPathEnum.GeneralPatientPay, "", null, "NeedSalesOrder"),
            new PelayananWorklistItem("ANT1", 1, QueueDemandKindEnum.JualBebas, "JB1", "B", "SO1", PayerPathEnum.GeneralPatientPay, "INV1", InvoiceStatusEnum.Established, "")
        };

        items.Select(x => (x.AntrianId, x.NoUrut)).Distinct().Should().ContainSingle();
        items.Select(x => x.DemandId).Should().Equal("RK1", "JB1");
    }

    [Fact]
    public async Task DispensingWorklistHandler_delegates_to_dal()
    {
        var expected = new List<DispensingWorklistItem>
        {
            new("DISP1", "SO1", DispensingStatusEnum.Released, DateTime.Now)
        };
        _dal.Setup(x => x.ListDispensing()).Returns(expected);
        var sut = new DispensingWorklistHandler(_dal.Object);

        var result = await sut.Handle(new DispensingWorklistQuery(), default);

        result.Should().BeSameAs(expected);
        _dal.Verify(x => x.ListDispensing(), Times.Once);
    }

    [Fact]
    public async Task SerahWorklistHandler_passes_asOf_and_collection_window_days()
    {
        var asOf = new DateTime(2026, 8, 27, 15, 0, 0);
        DateTime? capturedAsOf = null;
        int? capturedDays = null;
        _dal.Setup(x => x.ListSerah(It.IsAny<DateTime>(), It.IsAny<int>()))
            .Callback<DateTime, int>((clock, days) =>
            {
                capturedAsOf = clock;
                capturedDays = days;
            })
            .Returns([]);
        var window = new Mock<ICollectionWindowDaysProvider>();
        window.Setup(x => x.GetDays()).Returns(7);
        var sut = new SerahWorklistHandler(_dal.Object, window.Object);

        await sut.Handle(new SerahWorklistQuery(asOf), default);

        capturedAsOf.Should().Be(asOf);
        capturedDays.Should().Be(7);
        _dal.Verify(x => x.ListSerah(asOf, 7), Times.Once);
    }

    [Fact]
    public void DispensingWorklistItem_exposes_dispensing_status_not_payment_or_stock_fields()
    {
        var names = typeof(DispensingWorklistItem).GetProperties().Select(x => x.Name).ToArray();
        names.Should().Contain(nameof(DispensingWorklistItem.Status));
        names.Should().NotContain("PaymentClearance");
        names.Should().NotContain("StockQty");
    }

    [Fact]
    public void SerahWorklistItem_category_is_projection_string_not_dispensing_enum()
    {
        var categoryProp = typeof(SerahWorklistItem).GetProperty(nameof(SerahWorklistItem.Category));
        categoryProp!.PropertyType.Should().Be(typeof(string));
    }

    [Fact]
    public async Task JourneyHandler_delegates_to_dal()
    {
        var expected = new JourneyResponse("ANT1", 1, []);
        _dal.Setup(x => x.LoadJourney("ANT1", 1)).Returns(expected);
        var sut = new JourneyHandler(_dal.Object);

        var result = await sut.Handle(new JourneyQuery("ANT1", 1), default);

        result.Should().BeSameAs(expected);
        _dal.Verify(x => x.LoadJourney("ANT1", 1), Times.Once);
    }

    [Fact]
    public void JourneyDemand_preserves_separate_payer_paths_and_exposes_telaah_without_category_state()
    {
        var names = typeof(JourneyDemand).GetProperties().Select(x => x.Name).ToArray();
        names.Should().Contain(nameof(JourneyDemand.TelaahResepId));
        names.Should().Contain(nameof(JourneyDemand.TelaahStatus));
        names.Should().Contain(nameof(JourneyDemand.SalesOrders));
        names.Should().Contain(nameof(JourneyDemand.IntegrationTasks));
        names.Should().NotContain("Category");
        names.Should().NotContain("AttentionLabel");

        var demand = new JourneyDemand(
            QueueDemandKindEnum.ResepKerja,
            "ARX1",
            "ATR1",
            TelaahStatusEnum.Approved,
            [
                new JourneySalesOrderRef("ASO1", PayerPathEnum.GeneralPatientPay),
                new JourneySalesOrderRef("ASO2", PayerPathEnum.Bpjs)
            ],
            ["ASI1"],
            ["ADP1"],
            [new JourneyIntegrationTaskRef("AIT1", AptIntegrationTaskStatusEnum.Failed, "stock down")]);

        demand.SalesOrders.Select(x => x.PayerPath).Should().Equal(PayerPathEnum.GeneralPatientPay, PayerPathEnum.Bpjs);
        demand.IntegrationTasks.Should().ContainSingle(x =>
            x.IntegrationTaskId == "AIT1" && x.Status == AptIntegrationTaskStatusEnum.Failed);
    }

    [Fact]
    public async Task UnifiedSalesReportHandler_delegates_to_dal_with_date_range()
    {
        var date1 = new DateTime(2026, 8, 27);
        var date2 = new DateTime(2026, 8, 28);
        var expected = new List<UnifiedSalesReportItem>
        {
            new("APT", "ASI1", date1, "Pasien Apt", 150m)
        };
        DateTime? capturedDate1 = null;
        DateTime? capturedDate2 = null;
        _dal.Setup(x => x.ListUnifiedSales(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Callback<DateTime, DateTime>((start, end) =>
            {
                capturedDate1 = start;
                capturedDate2 = end;
            })
            .Returns(expected);
        var sut = new UnifiedSalesReportHandler(_dal.Object);

        var result = await sut.Handle(new UnifiedSalesReportQuery(date1, date2), default);

        result.Should().BeSameAs(expected);
        capturedDate1.Should().Be(date1);
        capturedDate2.Should().Be(date2);
        _dal.Verify(x => x.ListUnifiedSales(date1, date2), Times.Once);
    }

    [Fact]
    public void UnifiedSalesReportItem_distinguishes_same_document_id_across_sources()
    {
        var sharedId = "DOC001";
        var items = new[]
        {
            new UnifiedSalesReportItem("APT", sharedId, new DateTime(2026, 8, 27), "Apt Pasien", 100m),
            new UnifiedSalesReportItem("DU", sharedId, new DateTime(2026, 8, 27), "Du Pasien", 200m)
        };

        items.Select(x => (x.SourceKind, x.DocumentId)).Should().Equal(
            ("APT", sharedId),
            ("DU", sharedId));
        items.Select(x => x.PasienName).Should().Equal("Apt Pasien", "Du Pasien");
    }

    [Fact]
    public void JourneyResponse_groups_multiple_demands_under_one_queue_entry()
    {
        var response = new JourneyResponse(
            "ANT1",
            2,
            [
                new JourneyDemand(
                    QueueDemandKindEnum.ResepKerja,
                    "ARX1",
                    "ATR1",
                    TelaahStatusEnum.Approved,
                    [new JourneySalesOrderRef("ASO1", PayerPathEnum.Bpjs)],
                    [],
                    [],
                    []),
                new JourneyDemand(
                    QueueDemandKindEnum.JualBebas,
                    "ADQ1",
                    "",
                    null,
                    [new JourneySalesOrderRef("ASO2", PayerPathEnum.GeneralPatientPay)],
                    [],
                    [],
                    [])
            ]);

        response.Demands.Should().HaveCount(2);
        response.Demands.Select(x => x.DemandId).Should().Equal("ARX1", "ADQ1");
    }
}
