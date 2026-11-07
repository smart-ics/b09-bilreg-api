using System.Reflection;
using Bilreg.Api.Controllers.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly LabResultController _sut;

    public LabResultControllerTest()
    {
        _sut = new LabResultController(_mediatorMock.Object);
    }

    #region Controller Attribute & Route Structure Tests

    [Fact]
    public void Controller_HasExpectedAttributesAndRoute()
    {
        var type = typeof(LabResultController);

        type.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();
        type.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();

        var routeAttr = type.GetCustomAttribute<RouteAttribute>();
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/LabContext/LabResultFeature");
    }

    [Fact]
    public void IngestLisAction_HasExpectedHttpAttributes()
    {
        var method = typeof(LabResultController).GetMethod(nameof(LabResultController.IngestLis));
        method.Should().NotBeNull();

        var postAttrs = method!.GetCustomAttributes<HttpPostAttribute>().ToList();
        postAttrs.Should().NotBeEmpty();
        postAttrs.Select(a => a.Template).Should().Contain(["ingest-lis", "ingestLis"]);
    }

    [Fact]
    public void RecordAction_HasExpectedHttpAttributes()
    {
        var method = typeof(LabResultController).GetMethod(nameof(LabResultController.Record));
        method.Should().NotBeNull();

        var postAttrs = method!.GetCustomAttributes<HttpPostAttribute>().ToList();
        postAttrs.Should().NotBeEmpty();
        postAttrs.Select(a => a.Template).Should().Contain("record");
    }

    [Fact]
    public void VerifyAction_HasExpectedHttpAttributes()
    {
        var method = typeof(LabResultController).GetMethod(nameof(LabResultController.Verify));
        method.Should().NotBeNull();

        var patchAttrs = method!.GetCustomAttributes<HttpPatchAttribute>().ToList();
        patchAttrs.Should().NotBeEmpty();
        patchAttrs.Select(a => a.Template).Should().Contain("verify");
    }

    #endregion

    #region Action Execution Tests

    [Fact]
    public async Task Record_SendsCommandWithCriticalNote_ReturnsOk()
    {
        var request = new LabResultRecordRequest(
            OrderId: "ORD100",
            UserId: "USR01",
            ResultSource: 1,
            Values: [new LabResultRecordValueDto("C01", "12.5")],
            CriticalNote: "Peringatan nilai kritis hemoglobin");

        _mediatorMock.Setup(m => m.Send(It.IsAny<LabResultRecordCmd>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Record(request);

        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(m => m.Send(It.Is<LabResultRecordCmd>(cmd =>
            cmd.OrderId == "ORD100" &&
            cmd.UserId == "USR01" &&
            cmd.CriticalNote == "Peringatan nilai kritis hemoglobin" &&
            cmd.Values.Count() == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Record_WithItemsPayload_MapsToCommandCorrectly()
    {
        var request = new LabResultRecordRequest(
            OrderId: "ORD101",
            UserId: "USR02",
            ResultSource: 1,
            Values: null,
            CriticalNote: "Catatan kritis",
            Items:
            [
                new LabResultRecordItemRequestDto(ComponentCode: "COMP1", Value: "15.0"),
                new LabResultRecordItemRequestDto(ComponentCode: "COMP2", Value: "Positive")
            ]);

        _mediatorMock.Setup(m => m.Send(It.IsAny<LabResultRecordCmd>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Record(request);

        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(m => m.Send(It.Is<LabResultRecordCmd>(cmd =>
            cmd.OrderId == "ORD101" &&
            cmd.CriticalNote == "Catatan kritis" &&
            cmd.Values.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Verify_SendsCommandWithExpertiseNote_ReturnsOk()
    {
        var verifiedDate = new DateTime(2026, 11, 7, 10, 0, 0);
        var request = new LabResultVerifyRequest(
            OrderId: "ORD200",
            VerifiedUserId: "dr. Hendra Sp.PK",
            VerifiedDate: verifiedDate,
            ExpertiseNote: "Morfologi eritrosit anisopoikilositosis ringan.");

        _mediatorMock.Setup(m => m.Send(It.IsAny<LabResultVerifyCmd>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Verify(request);

        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(m => m.Send(It.Is<LabResultVerifyCmd>(cmd =>
            cmd.OrderId == "ORD200" &&
            cmd.VerifiedUserId == "dr. Hendra Sp.PK" &&
            cmd.VerifiedDate == verifiedDate &&
            cmd.ExpertiseNote == "Morfologi eritrosit anisopoikilositosis ringan."), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestLis_SendsCommandWithLisData_ReturnsOk()
    {
        var validatedDate = new DateTime(2026, 11, 7, 11, 0, 0);
        var request = new LabResultIngestLisRequest(
            OrderId: "ORD300",
            LisRefNo: "LIS-202611-0099",
            ValidatorName: "Validator Lis RS",
            ValidatedDate: validatedDate,
            Items:
            [
                new LabResultIngestItemDto("HB01", "14.2", "g/dL", "13.0 - 17.0", "Normal"),
                new LabResultIngestItemDto("LEU01", "18500", "/uL", "4000 - 10000", "Critical")
            ],
            CriticalNote: "Leukositosis berat");

        _mediatorMock.Setup(m => m.Send(It.IsAny<LabResultIngestFromLisCmd>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.IngestLis(request);

        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(m => m.Send(It.Is<LabResultIngestFromLisCmd>(cmd =>
            cmd.OrderId == "ORD300" &&
            cmd.LisRefNo == "LIS-202611-0099" &&
            cmd.ValidatorName == "Validator Lis RS" &&
            cmd.CriticalNote == "Leukositosis berat" &&
            cmd.Items.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerificationWorklist_SendsQuery_ReturnsOk()
    {
        IEnumerable<LabResultVerificationWorklistView> response = new List<LabResultVerificationWorklistView>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<LabResultVerificationWorklistQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.VerificationWorklist("search", null, null);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_SendsQuery_ReturnsOk()
    {
        var response = new LabResultView(
            HasDocument: true,
            ResultDocumentId: "LRD001",
            OrderId: "ORD001",
            VersionNo: 1,
            IsCurrentVersion: true,
            ResultSource: 1,
            ResultStatus: 2,
            RecordedDate: DateTime.Now,
            RecordedUserId: "USR01",
            VerifiedDate: DateTime.Now,
            VerifiedUserId: "USR_SPPK",
            Items: []);

        _mediatorMock.Setup(m => m.Send(It.IsAny<LabResultGetQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.Get("ORD001");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Amend_SendsCommand_ReturnsOk()
    {
        var cmd = new LabResultAmendCmd("ORD001", "Koreksi nilai", "USR01");
        _mediatorMock.Setup(m => m.Send(It.IsAny<LabResultAmendCmd>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Amend(cmd);

        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(m => m.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pdf_SendsQuery_ReturnsFileResult()
    {
        var pdfFile = new LabResultPdfFile(new byte[] { 1, 2, 3 }, "LAB001.pdf", "application/pdf");
        _mediatorMock.Setup(m => m.Send(It.IsAny<LabResultPdfQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pdfFile);

        var result = await _sut.Pdf("ORD001");

        result.Should().BeOfType<FileContentResult>();
        var fileResult = result as FileContentResult;
        fileResult!.ContentType.Should().Be("application/pdf");
        fileResult.FileDownloadName.Should().Be("LAB001.pdf");
        fileResult.FileContents.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
    }

    #endregion
}
