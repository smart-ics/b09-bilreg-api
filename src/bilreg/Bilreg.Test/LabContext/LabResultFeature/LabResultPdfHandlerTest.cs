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
        document.SetCriticalNote("Catatan nilai kritis uji");
        document.RecordResult(
            LabResultSourceEnum.Manual,
            [LabResultTestSupport.Capture(numeric: 15.5m)],
            "USR01",
            DateTime.Now);
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
    }
}
