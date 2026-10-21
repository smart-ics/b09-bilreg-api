using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderGetHandlerTest
{
    private const string OrderId = "LBO09A8QKSFR";
    private const string EmrOrderId = "EMR-20261015-001";
    private const string OrderNo = "LAB20261015001";
    private static readonly DateTime FixedNow = new(2026, 10, 15, 14, 0, 0);

    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly LabOrderGetHandler _sut;

    public LabOrderGetHandlerTest()
    {
        _sut = new LabOrderGetHandler(_repo.Object);
    }

    private static LabOrderModel CreateSampleOrder(
        LabOrderUrgencyEnum orderUrgency = LabOrderUrgencyEnum.Cito,
        LabOrderUrgencyEnum itemUrgency = LabOrderUrgencyEnum.Cito)
    {
        var snapshot = new PatientSnapshotType(
            "REG001", "MR001", "Budi Santoso", new DateTime(1985, 5, 20), "L", 41);

        var item = LabOrderItemModel.Create(
            testDefinitionId: "DEF-CKMB",
            labTestCode: "CKMB",
            labTestName: "Creatine Kinase-MB",
            tarifId: "TRF-CKMB",
            tarifCode: "CKMB01",
            tarifName: "Pemeriksaan CK-MB",
            tubeType: VacutainerTypeEnum.Edta,
            specimenType: "Serum",
            requiredTubeCount: 1,
            urgency: itemUrgency);

        var component = LabOrderTestSupport.SampleComponent(
            componentId: "CMP-CKMB-VAL",
            componentCode: "CKMB_VAL",
            componentName: "Kadar CK-MB",
            resultType: LabResultTypeEnum.Numeric,
            unit: "U/L",
            referenceRangeText: "< 25",
            sequenceNo: 1,
            isMandatory: true);

        var line = new LabOrderModel.ResolvedOrderLine(item, [component]);
        var audit = new AuditInfoType("DOKTER01", FixedNow);

        return LabOrderModel.CreateFromEmr(
            emrOrderId: EmrOrderId,
            snapshot: snapshot,
            lines: [line],
            orderNo: OrderNo,
            audit: audit,
            dokterPerujukId: "DOK001",
            dokterPerujukName: "dr. Andi, Sp.PK",
            layananPerujukId: "LAY001",
            layananPerujukName: "Instalasi Gawat Darurat",
            clinicalNote: "Nyeri dada akut tipikal 2 jam",
            urgency: orderUrgency);
    }

    [Fact]
    public async Task Handle_GivenEmptyOrderId_ThrowsArgumentException()
    {
        var act = async () => await _sut.Handle(new LabOrderGetQuery(""), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_GivenOrderNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe<LabOrderModel>.None);

        var act = async () => await _sut.Handle(new LabOrderGetQuery(OrderId), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*LabOrder '{OrderId}' not found*");
    }

    [Fact]
    public async Task Handle_GivenCitoOrder_ReturnsFullDetailWithClinicalContextAndUrgency()
    {
        // Arrange
        var order = CreateSampleOrder(LabOrderUrgencyEnum.Cito, LabOrderUrgencyEnum.Cito);
        _repo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        // Act
        var result = await _sut.Handle(new LabOrderGetQuery(order.OrderId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(order.OrderId);
        result.EmrOrderId.Should().Be(EmrOrderId);
        result.OrderNo.Should().Be(OrderNo);

        // Clinical context & urgency assertions on header
        result.DokterPerujukId.Should().Be("DOK001");
        result.DokterPerujukName.Should().Be("dr. Andi, Sp.PK");
        result.LayananPerujukId.Should().Be("LAY001");
        result.LayananPerujukName.Should().Be("Instalasi Gawat Darurat");
        result.ClinicalNote.Should().Be("Nyeri dada akut tipikal 2 jam");
        result.Urgency.Should().Be((int)LabOrderUrgencyEnum.Cito);
        result.IsCito.Should().BeTrue();

        // Item-level urgency & TarifPrice assertions
        result.Items.Should().HaveCount(1);
        var item = result.Items.First();
        item.TestDefinitionId.Should().Be("DEF-CKMB");
        item.LabTestName.Should().Be("Creatine Kinase-MB");
        item.Urgency.Should().Be((int)LabOrderUrgencyEnum.Cito);
        item.IsCito.Should().BeTrue();
        item.TarifPrice.Should().Be(0m);

        // Component assertions
        result.ItemComponents.Should().HaveCount(1);
        var component = result.ItemComponents.First();
        component.ComponentCode.Should().Be("CKMB_VAL");
        component.Unit.Should().Be("U/L");
    }

    [Fact]
    public async Task Handle_GivenRutinOrder_ReturnsRutinUrgencyAndIsCitoFalse()
    {
        // Arrange
        var order = CreateSampleOrder(LabOrderUrgencyEnum.Rutin, LabOrderUrgencyEnum.Rutin);
        _repo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        // Act
        var result = await _sut.Handle(new LabOrderGetQuery(order.OrderId), CancellationToken.None);

        // Assert
        result.Urgency.Should().Be((int)LabOrderUrgencyEnum.Rutin);
        result.IsCito.Should().BeFalse();

        var item = result.Items.Single();
        item.Urgency.Should().Be((int)LabOrderUrgencyEnum.Rutin);
        item.IsCito.Should().BeFalse();
        item.TarifPrice.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_GivenNilaiTarifRepo_MapsTarifPriceToItem()
    {
        // Arrange
        var order = CreateSampleOrder();
        _repo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        var nilaiTarifRepoMock = new Mock<INilaiTarifRepo>();
        var sampleNilaiTarif = new NilaiTarifType(
            nilaiTarifId: "NT-001",
            tarifId: "TRF-CKMB",
            tarifName: "Pemeriksaan CK-MB",
            tipeTarif: TipeTarifType.Default.ToReff(),
            kelas: KelasType.Default.ToReff(),
            nilai: 185000m,
            listKomponen: []);

        nilaiTarifRepoMock
            .Setup(r => r.LoadEntity(It.Is<INilaiTarifCompositKey>(k => k.TarifId == "TRF-CKMB")))
            .Returns(MayBe.From(sampleNilaiTarif));

        var sutWithTarif = new LabOrderGetHandler(_repo.Object, nilaiTarifRepoMock.Object);

        // Act
        var result = await sutWithTarif.Handle(new LabOrderGetQuery(order.OrderId), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().TarifPrice.Should().Be(185000m);
    }

    [Fact]
    public async Task Handle_GivenNilaiTarifRepoAndRegRepo_ResolvesTarifPriceUsingPatientRegistration()
    {
        // Arrange
        var order = CreateSampleOrder();
        _repo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        var regRepoMock = new Mock<IRegRepo>();
        var regModel = new RegModel(
            "REG001",
            new DateOnly(2026, 10, 15),
            new AuditInfoType("USER01", FixedNow),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            new PasienReff("MR001", "Budi Santoso", new DateOnly(1985, 5, 20), "L"),
            new TipeJaminanReff("00000", "Umum"),
            PolisModel.Default.ToReff(),
            new KelasReff("I", "Kelas 1"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);

        regRepoMock.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(regModel));

        var nilaiTarifRepoMock = new Mock<INilaiTarifRepo>();
        var sampleNilaiTarif = new NilaiTarifType(
            nilaiTarifId: "NT-002",
            tarifId: "TRF-CKMB",
            tarifName: "Pemeriksaan CK-MB",
            tipeTarif: new TipeTarifReff("RJ", "Rawat Jalan"),
            kelas: new KelasReff("I", "Kelas 1"),
            nilai: 210000m,
            listKomponen: []);

        nilaiTarifRepoMock
            .Setup(r => r.LoadEntity(It.Is<INilaiTarifCompositKey>(k => k.TarifId == "TRF-CKMB" && k.KelasId == "I" && k.TipeTarifId == "RJ")))
            .Returns(MayBe.From(sampleNilaiTarif));

        var sut = new LabOrderGetHandler(_repo.Object, nilaiTarifRepoMock.Object, regRepoMock.Object);

        // Act
        var result = await sut.Handle(new LabOrderGetQuery(order.OrderId), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().TarifPrice.Should().Be(210000m);
    }

    [Fact]
    public async Task Handle_WhenTarifNotFound_DefaultsTarifPriceToZero()
    {
        // Arrange
        var order = CreateSampleOrder();
        _repo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        var nilaiTarifRepoMock = new Mock<INilaiTarifRepo>();
        nilaiTarifRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe<NilaiTarifType>.None);

        var sut = new LabOrderGetHandler(_repo.Object, nilaiTarifRepoMock.Object);

        // Act
        var result = await sut.Handle(new LabOrderGetQuery(order.OrderId), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().TarifPrice.Should().Be(0m);
    }
}
