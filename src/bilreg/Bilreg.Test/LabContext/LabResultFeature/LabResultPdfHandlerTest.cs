using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.LabContext.LabOrderFeature;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultPdfHandlerTest
{
    private readonly Mock<ILabOrderRepo> _labOrderRepoMock = new();
    private readonly Mock<ILabResultDocumentRepo> _labResultDocumentRepoMock = new();
    private readonly Mock<ILabResultPdfRenderer> _pdfRendererMock = new();
    private readonly LabResultPdfHandler _sut;

    public LabResultPdfHandlerTest()
    {
        var options = Options.Create(new LabResultPdfOptions { HospitalName = "RS Uji Coba" });
        _sut = new LabResultPdfHandler(
            _labOrderRepoMock.Object,
            _labResultDocumentRepoMock.Object,
            _pdfRendererMock.Object,
            options);
    }

    [Fact]
    public async Task Handle_MapsExpertiseAndCriticalNoteToPdfView()
    {
        var snapshot = new PatientSnapshotType(
            "REG123", "MR123", "Pasien Uji", new DateTime(1990, 5, 10), "L", 35);
        var order = LabOrderTestSupport.CreateEmrOrder(
            "LAB12345",
            lines: [LabOrderTestSupport.ResolvedLine()],
            snapshot: snapshot,
            audit: new AuditInfoType("U1", DateTime.Now));

        var audit = new AuditInfoType("USR01", new DateTime(2026, 11, 7, 10, 0, 0));
        var document = LabResultDocumentModel.CreateInitial(order.OrderId, audit);
        document.RecordResult(
            LabResultSourceEnum.Manual,
            [LabResultTestSupport.Capture(numeric: 15.5m)],
            "USR01",
            DateTime.Now);
        document.SetCriticalNote("Catatan nilai kritis uji");
        document.MarkRecorded("USR01", DateTime.Now);
        document.Verify("dr. Patolog Sp.PK", DateTime.Now, "Catatan ekspertise patolog uji");

        _labOrderRepoMock.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe.From(order));
        _labResultDocumentRepoMock.Setup(r => r.LoadByOrderId(order.OrderId))
            .Returns(MayBe.From(document));

        LabResultPdfView? capturedView = null;
        _pdfRendererMock.Setup(r => r.Render(It.IsAny<LabResultPdfView>()))
            .Callback<LabResultPdfView>(v => capturedView = v)
            .Returns([0x25, 0x50, 0x44, 0x46]); // %PDF

        var result = await _sut.Handle(new LabResultPdfQuery(order.OrderId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Content.Should().NotBeEmpty();
        capturedView.Should().NotBeNull();
        capturedView!.ExpertiseNote.Should().Be("Catatan ekspertise patolog uji");
        capturedView.CriticalNote.Should().Be("Catatan nilai kritis uji");
        capturedView.VerifiedUserId.Should().Be("dr. Patolog Sp.PK");
        capturedView.HospitalName.Should().Be("RS Uji Coba");
        capturedView.ServiceType.Should().Be(LaboratoryServiceType.ClinicalPathology);
        capturedView.DokterPerujukName.Should().BeEmpty();
        capturedView.LayananPerujukName.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsOrderMetadataAndCorrelatesDisciplineSnapshot_ToPdfView()
    {
        var snapshot = new PatientSnapshotType(
            "REG123", "MR123", "Pasien Uji", new DateTime(1990, 5, 10), "L", 35);

        var orderItem = LabOrderItemModel.Create(
            testDefinitionId: "LTD0010",
            labTestCode: "GLU",
            labTestName: "Glukosa Puasa",
            tarifId: "TR10",
            tarifCode: "T-GLU",
            tarifName: "Tarif Glukosa",
            tubeType: VacutainerTypeEnum.Serum,
            specimenType: "Plasma",
            requiredTubeCount: 1,
            disciplineName: "Kimia Klinik",
            testGroupName: "Metabolisme Karbohidrat");

        var orderLine = new LabOrderModel.ResolvedOrderLine(orderItem, [LabOrderTestSupport.SampleComponent()]);

        var order = LabOrderModel.CreateFromEmr(
            emrOrderId: "EMR-ORDER-002",
            snapshot: snapshot,
            lines: [orderLine],
            orderNo: "LAB202611005",
            audit: new AuditInfoType("U1", DateTime.Now),
            dokterPerujukName: "dr. Kartika Sp.PD",
            layananPerujukName: "Poliklinik Penyakit Dalam",
            serviceType: LaboratoryServiceType.ClinicalPathology);

        var audit = new AuditInfoType("USR01", new DateTime(2026, 11, 7, 10, 0, 0));
        var document = LabResultDocumentModel.CreateInitial(order.OrderId, audit);

        var capture = new LabResultItemCapture(
            ComponentId: "MLC0010",
            TestId: "LTD0010",
            TestName: "Glukosa Puasa",
            ComponentCode: "GLU-P",
            ComponentName: "Glukosa Puasa",
            SequenceNo: 1,
            ResultType: LabResultTypeEnum.Numeric,
            NumericValue: 110m,
            TextValue: "",
            OptionValue: "",
            NarrativeValue: "",
            Unit: "mg/dL",
            ReferenceRangeText: "70 - 100",
            IsMandatory: true);

        document.RecordResult(
            LabResultSourceEnum.Manual,
            [capture],
            "USR01",
            DateTime.Now);
        document.MarkRecorded("USR01", DateTime.Now);
        document.Verify("dr. Patolog Sp.PK", DateTime.Now);

        _labOrderRepoMock.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe.From(order));
        _labResultDocumentRepoMock.Setup(r => r.LoadByOrderId(order.OrderId))
            .Returns(MayBe.From(document));

        LabResultPdfView? capturedView = null;
        _pdfRendererMock.Setup(r => r.Render(It.IsAny<LabResultPdfView>()))
            .Callback<LabResultPdfView>(v => capturedView = v)
            .Returns([0x25, 0x50, 0x44, 0x46]);

        var result = await _sut.Handle(new LabResultPdfQuery(order.OrderId), CancellationToken.None);

        result.Should().NotBeNull();
        capturedView.Should().NotBeNull();
        capturedView!.ServiceType.Should().Be(LaboratoryServiceType.ClinicalPathology);
        capturedView.DokterPerujukName.Should().Be("dr. Kartika Sp.PD");
        capturedView.LayananPerujukName.Should().Be("Poliklinik Penyakit Dalam");

        var item = capturedView.Items.Should().ContainSingle().Subject;
        item.DisciplineName.Should().Be("Kimia Klinik");
        item.TestGroupName.Should().Be("Metabolisme Karbohidrat");
        item.ComponentCode.Should().Be("GLU-P");
        item.ResultType.Should().Be(LabResultTypeEnum.Numeric);
        item.DisplayValue.Should().Be("110");
        item.Unit.Should().Be("mg/dL");
        item.ReferenceRangeText.Should().Be("70 - 100");
    }

    [Fact]
    public async Task Handle_CorrelatesByTestName_AndMapsNarrativeAndOptionValues()
    {
        var snapshot = new PatientSnapshotType(
            "REG123", "MR123", "Pasien Uji", new DateTime(1990, 5, 10), "L", 35);

        var orderItem = LabOrderItemModel.Create(
            testDefinitionId: "LTD-PA-001",
            labTestCode: "PA-BIO",
            labTestName: "Biopsi Jaringan Kecil",
            tarifId: "TR-PA-1",
            tarifCode: "T-PA-BIO",
            tarifName: "Biopsi",
            tubeType: VacutainerTypeEnum.None,
            specimenType: "Jaringan",
            requiredTubeCount: 1,
            disciplineName: "Patologi Anatomi",
            testGroupName: "Histopatologi");

        var orderLine = new LabOrderModel.ResolvedOrderLine(orderItem, [LabOrderTestSupport.SampleComponent()]);

        var order = LabOrderModel.CreateFromEmr(
            emrOrderId: "EMR-PA-001",
            snapshot: snapshot,
            lines: [orderLine],
            orderNo: "LAB202611009",
            audit: new AuditInfoType("U1", DateTime.Now),
            dokterPerujukName: "dr. Bedah Sp.B",
            layananPerujukName: "Poli Bedah",
            serviceType: LaboratoryServiceType.AnatomicalPathology);

        var audit = new AuditInfoType("USR01", new DateTime(2026, 11, 7, 10, 0, 0));
        var document = LabResultDocumentModel.CreateInitial(order.OrderId, audit);

        var captureNarrative = new LabResultItemCapture(
            ComponentId: "MLC-PA-01",
            TestId: "DIFF-TEST-ID", // Different TestId to test fallback to TestName correlation
            TestName: "Biopsi Jaringan Kecil",
            ComponentCode: "PA_MACROSCOPIC",
            ComponentName: "Makroskopis",
            SequenceNo: 1,
            ResultType: LabResultTypeEnum.Narrative,
            NumericValue: 0m,
            TextValue: "",
            OptionValue: "",
            NarrativeValue: "Jaringan ukuran 2x1x0.5 cm, warna kecoklatan, konsistensi kenyal.",
            Unit: "",
            ReferenceRangeText: "",
            IsMandatory: true);

        var captureOption = new LabResultItemCapture(
            ComponentId: "MLC-PA-02",
            TestId: "DIFF-TEST-ID",
            TestName: "Biopsi Jaringan Kecil",
            ComponentCode: "PA_SPECIMEN_ADEQUACY",
            ComponentName: "Adekuasi Sediaan",
            SequenceNo: 2,
            ResultType: LabResultTypeEnum.Option,
            NumericValue: 0m,
            TextValue: "",
            OptionValue: "Adekuat",
            NarrativeValue: "",
            Unit: "",
            ReferenceRangeText: "",
            IsMandatory: true);

        document.RecordResult(
            LabResultSourceEnum.Manual,
            [captureNarrative, captureOption],
            "USR01",
            DateTime.Now);
        document.MarkRecorded("USR01", DateTime.Now);
        document.Verify("dr. Spesialis Sp.PA", DateTime.Now);

        _labOrderRepoMock.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe.From(order));
        _labResultDocumentRepoMock.Setup(r => r.LoadByOrderId(order.OrderId))
            .Returns(MayBe.From(document));

        LabResultPdfView? capturedView = null;
        _pdfRendererMock.Setup(r => r.Render(It.IsAny<LabResultPdfView>()))
            .Callback<LabResultPdfView>(v => capturedView = v)
            .Returns([0x25, 0x50, 0x44, 0x46]);

        var result = await _sut.Handle(new LabResultPdfQuery(order.OrderId), CancellationToken.None);

        result.Should().NotBeNull();
        capturedView.Should().NotBeNull();
        capturedView!.ServiceType.Should().Be(LaboratoryServiceType.AnatomicalPathology);
        capturedView.DokterPerujukName.Should().Be("dr. Bedah Sp.B");
        capturedView.LayananPerujukName.Should().Be("Poli Bedah");

        var items = capturedView.Items.ToList();
        items.Should().HaveCount(2);

        items[0].DisciplineName.Should().Be("Patologi Anatomi");
        items[0].TestGroupName.Should().Be("Histopatologi");
        items[0].ComponentCode.Should().Be("PA_MACROSCOPIC");
        items[0].ResultType.Should().Be(LabResultTypeEnum.Narrative);
        items[0].NarrativeValue.Should().Be("Jaringan ukuran 2x1x0.5 cm, warna kecoklatan, konsistensi kenyal.");
        items[0].DisplayValue.Should().Be("Jaringan ukuran 2x1x0.5 cm, warna kecoklatan, konsistensi kenyal.");

        items[1].DisciplineName.Should().Be("Patologi Anatomi");
        items[1].TestGroupName.Should().Be("Histopatologi");
        items[1].ComponentCode.Should().Be("PA_SPECIMEN_ADEQUACY");
        items[1].ResultType.Should().Be(LabResultTypeEnum.Option);
        items[1].OptionValue.Should().Be("Adekuat");
        items[1].DisplayValue.Should().Be("Adekuat");
    }
}
