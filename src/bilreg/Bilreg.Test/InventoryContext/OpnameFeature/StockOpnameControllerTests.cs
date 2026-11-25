using System.Security.Claims;
using System.Text.Json;
using Bilreg.Api.Controllers.InventoryContext.OpnameFeature;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;
using Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Queries;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using Bilreg.Infrastructure.InventoryContext;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.OpnameFeature;

public class StockOpnameControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly StockOpnameController _sut;

    public StockOpnameControllerTests()
    {
        _sut = new StockOpnameController(_mediatorMock.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "USER001"),
            new Claim(ClaimTypes.Name, "Test User")
        }, "TestAuth"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public void Controller_HasExpectedAttributes()
    {
        typeof(StockOpnameController).Should().BeDecoratedWith<ApiControllerAttribute>();
        typeof(StockOpnameController).Should().BeDecoratedWith<AuthorizeAttribute>();
        typeof(StockOpnameController).Should().BeDecoratedWith<RouteAttribute>(a => a.Template == "api/[controller]");
    }

    [Fact]
    public async Task GetSessions_WhenExecuted_ReturnsOkWithList()
    {
        var expectedResponse = new List<StockOpnameSessionListDto>
        {
            new(
                SessionId: "SO-202611-0001",
                LayananId: "LY01",
                ScopeMode: SessionScopeEnum.Full,
                Status: SessionStatusEnum.Draft,
                CutoffDate: new DateTime(2026, 11, 20),
                CreatedBy: "USER001",
                CreatedAt: DateTime.UtcNow,
                CountingSubmittedBy: string.Empty,
                CountingSubmittedAt: null,
                VerifiedBy: string.Empty,
                VerifiedAt: null,
                ApprovedBy: string.Empty,
                ApprovedAt: null,
                RejectedBy: string.Empty,
                RejectedAt: null,
                CancelledBy: string.Empty,
                CancelledAt: null,
                CancelReason: string.Empty,
                RevisionNotes: string.Empty)
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetSessionListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.GetSessions("LY01", SessionStatusEnum.Draft, null, null);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<GetSessionListQuery>(q => q.LayananId == "LY01" && q.Status == SessionStatusEnum.Draft),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSession_WhenExecuted_ReturnsOkWithCreatedSession()
    {
        var request = new CreateSessionRequest(
            LayananId: "LY01",
            ScopeMode: SessionScopeEnum.Partial,
            CutoffDate: new DateTime(2026, 11, 20),
            CreatedBy: null,
            ScopeItems: new[] { new CreateSessionScopeItemRequest("BRG01", "Paracetamol") });

        var expectedResponse = new CreateSessionResponse(
            SessionId: "SO-202611-0001",
            LayananId: "LY01",
            ScopeMode: SessionScopeEnum.Partial,
            Status: SessionStatusEnum.Draft,
            CutoffDate: new DateTime(2026, 11, 20),
            CreatedBy: "USER001",
            CreatedAt: DateTime.UtcNow,
            ScopeItemsCount: 1);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.CreateSession(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<CreateSessionCommand>(cmd => cmd.LayananId == "LY01" && cmd.CreatedBy == "USER001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CreateSessionRequest_DeserializationFromClientJsonPayload_ResolvesFieldsCorrectly()
    {
        // Arrange: Payload JSON pengujian TC-SO-01
        var json = """{"layananId":"GDFAR","scope":1,"openedBy":"4FM01","notes":"Stok opname rutin","scopeItems":[]}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var request = JsonSerializer.Deserialize<CreateSessionRequest>(json, options);

        // Assert
        request.Should().NotBeNull();
        request!.LayananId.Should().Be("GDFAR");
        request.ResolvedCreatedBy.Should().Be("4FM01");
        request.ResolvedScopeMode.Should().Be(SessionScopeEnum.Full);
        request.ResolvedCutoffDate.Should().BeAfter(new DateTime(1753, 1, 1));
        request.Notes.Should().Be("Stok opname rutin");
        request.ScopeItems.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateSession_WithClientPayloadTCSO01_SendsCommandWithResolvedCreatedByAndValidCutoffDate()
    {
        // Arrange: Payload JSON pengujian TC-SO-01
        var json = """{"layananId":"GDFAR","scope":1,"openedBy":"4FM01","notes":"Stok opname rutin","scopeItems":[]}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var request = JsonSerializer.Deserialize<CreateSessionRequest>(json, options);
        request.Should().NotBeNull();

        var expectedResponse = new CreateSessionResponse(
            SessionId: "SO-20261123-0001",
            LayananId: "GDFAR",
            ScopeMode: SessionScopeEnum.Full,
            Status: SessionStatusEnum.Draft,
            CutoffDate: request!.ResolvedCutoffDate,
            CreatedBy: "4FM01",
            CreatedAt: DateTime.UtcNow,
            ScopeItemsCount: 0);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CreateSession(request!);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<CreateSessionCommand>(cmd =>
                cmd.LayananId == "GDFAR" &&
                cmd.CreatedBy == "4FM01" &&
                cmd.ScopeMode == SessionScopeEnum.Full &&
                cmd.CutoffDate > new DateTime(1753, 1, 1)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSessionDetail_WhenFound_ReturnsOk()
    {
        const string sessionId = "SO-202611-0001";
        var expectedResponse = new StockOpnameSessionDetailDto(
            SessionId: sessionId,
            LayananId: "LY01",
            ScopeMode: SessionScopeEnum.Full,
            Status: SessionStatusEnum.Draft,
            CutoffDate: new DateTime(2026, 11, 20),
            CreatedBy: "USER001",
            CreatedAt: DateTime.UtcNow,
            CountingSubmittedBy: string.Empty,
            CountingSubmittedAt: null,
            VerifiedBy: string.Empty,
            VerifiedAt: null,
            ApprovedBy: string.Empty,
            ApprovedAt: null,
            RejectedBy: string.Empty,
            RejectedAt: null,
            CancelledBy: string.Empty,
            CancelledAt: null,
            CancelReason: string.Empty,
            RevisionNotes: string.Empty,
            ScanProgress: new StockOpnameScanProgressDto(10, 8, 2, 80m),
            ScopeItems: Array.Empty<StockOpnameScopeItemResponseDto>());

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetSessionDetailQuery>(q => q.SessionId == sessionId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.GetSessionDetail(sessionId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetSessionDetail_WhenNotFound_ReturnsNotFound()
    {
        const string sessionId = "SO-NON-EXISTENT";
        _mediatorMock
            .Setup(x => x.Send(It.Is<GetSessionDetailQuery>(q => q.SessionId == sessionId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockOpnameSessionDetailDto?)null);

        var result = await _sut.GetSessionDetail(sessionId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task StartCounting_WhenExecuted_ReturnsOk()
    {
        const string sessionId = "SO-202611-0001";
        var expectedResponse = new StartCountingResponse(sessionId, SessionStatusEnum.Counting);

        _mediatorMock
            .Setup(x => x.Send(It.Is<StartCountingCommand>(c => c.SessionId == sessionId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.StartCounting(sessionId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task SubmitCounting_WhenExecuted_ReturnsOk()
    {
        const string sessionId = "SO-202611-0001";
        var request = new SubmitCountingRequest(SubmittedBy: "USER001");
        var expectedResponse = new SubmitCountingResponse(
            SessionId: sessionId,
            Status: SessionStatusEnum.WaitingVerification,
            TotalCountedItems: 10,
            TotalVarianceItems: 2,
            HasVariance: true,
            SurplusValuation: 50000m,
            DeficitValuation: 20000m,
            NetValuation: 30000m);

        _mediatorMock
            .Setup(x => x.Send(It.Is<SubmitCountingCommand>(c => c.SessionId == sessionId && c.SubmittedBy == "USER001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.SubmitCounting(sessionId, request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task StartVerification_WhenExecuted_ReturnsOk()
    {
        const string sessionId = "SO-202611-0001";
        var expectedResponse = new StartVerificationResponse(sessionId, SessionStatusEnum.Verifying);

        _mediatorMock
            .Setup(x => x.Send(It.Is<StartVerificationCommand>(c => c.SessionId == sessionId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.StartVerification(sessionId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetVarianceReview_WhenExecuted_ReturnsOk()
    {
        const string sessionId = "SO-202611-0001";
        var expectedResponse = new List<StockOpnameVarianceReviewDto>
        {
            new(
                VarianceItemId: "VAR01",
                SessionId: sessionId,
                BrgId: "BRG01",
                BatchNo: "BATCH01",
                ExpiredDate: "2026-12-31",
                BaseSatuanId: "TAB",
                PhysicalQty: 10m,
                InitialSystemQty: 12m,
                InitialVarianceQty: -2m,
                RecountQty: null,
                FinalVarianceQty: -2m,
                Hpp: 1000m,
                FinancialValue: -2000m,
                DiscrepancyReason: null,
                DiscrepancyReasonName: null,
                Notes: string.Empty,
                RecountedBy: string.Empty,
                RecountTime: null,
                State: 0,
                IsVerified: false)
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetVarianceReviewQuery>(q => q.SessionId == sessionId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.GetVarianceReview(sessionId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task RecordRecount_WhenExecuted_ReturnsOk()
    {
        const string sessionId = "SO-202611-0001";
        var request = new RecordRecountRequest(
            VarianceItemId: "VAR01",
            RecountQty: 11m,
            DiscrepancyReason: DiscrepancyReasonEnum.ADMIN_ERROR,
            Notes: "Salah input awal");

        var expectedResponse = new RecordRecountResponse(
            VarianceItemId: "VAR01",
            RecountQty: 11m,
            ReconstructedSystemQty: 12m,
            FinalVarianceQty: -1m,
            FinalValuation: -1000m,
            DiscrepancyReason: DiscrepancyReasonEnum.ADMIN_ERROR,
            Notes: "Salah input awal",
            IsVerified: true);

        _mediatorMock
            .Setup(x => x.Send(It.Is<RecordRecountCommand>(c => c.SessionId == sessionId && c.VarianceItemId == "VAR01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.RecordRecount(sessionId, request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task BulkVerifyMinor_WhenExecuted_ReturnsOk()
    {
        const string sessionId = "SO-202611-0001";
        var request = new BulkVerifyMinorRequest(
            MaxNominalThreshold: 10000m,
            StandardReason: DiscrepancyReasonEnum.ADMIN_ERROR,
            Notes: "Bulk auto-verified");

        var expectedResponse = new BulkVerifyMinorVariancesResponse(
            SessionId: sessionId,
            VerifiedCount: 3,
            VerifiedVarianceItemIds: new[] { "VAR01", "VAR02", "VAR03" });

        _mediatorMock
            .Setup(x => x.Send(It.Is<BulkVerifyMinorVariancesCommand>(c => c.SessionId == sessionId && c.MaxNominalThreshold == 10000m), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.BulkVerifyMinor(sessionId, request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task SubmitVerification_WhenExecuted_ReturnsOk()
    {
        const string sessionId = "SO-202611-0001";
        var request = new SubmitVerificationRequest();
        var expectedResponse = new SubmitVerificationResponse(
            SessionId: sessionId,
            Status: SessionStatusEnum.WaitingApproval,
            VerifiedBy: "USER001",
            VerifiedAt: DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(It.Is<SubmitVerificationCommand>(c => c.SessionId == sessionId && c.VerifiedBy == "USER001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.SubmitVerification(sessionId, request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetResultReport_WhenFound_ReturnsOk()
    {
        const string sessionId = "SO-202611-0001";
        var expectedResponse = new StockOpnameResultReportDto(
            SessionId: sessionId,
            LayananId: "LY01",
            ScopeMode: SessionScopeEnum.Full,
            Status: SessionStatusEnum.WaitingApproval,
            CutoffDate: new DateTime(2026, 11, 20),
            CreatedBy: "USER001",
            CreatedAt: DateTime.UtcNow,
            CountingSubmittedBy: "USER001",
            CountingSubmittedAt: DateTime.UtcNow,
            VerifiedBy: "SPV001",
            VerifiedAt: DateTime.UtcNow,
            ApprovedBy: string.Empty,
            ApprovedAt: null,
            RejectedBy: string.Empty,
            RejectedAt: null,
            CancelledBy: string.Empty,
            CancelledAt: null,
            CancelReason: string.Empty,
            RevisionNotes: string.Empty,
            TotalSurplusValuation: 10000m,
            TotalDeficitValuation: 5000m,
            NetDiscrepancyValuation: 5000m,
            TotalVarianceItems: 2,
            TotalVerifiedItems: 2,
            TotalUnverifiedItems: 0,
            AllVerified: true,
            Items: Array.Empty<StockOpnameResultReportItemDto>());

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetSessionResultReportQuery>(q => q.SessionId == sessionId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.GetResultReport(sessionId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetResultReport_WhenNotFound_ReturnsNotFound()
    {
        const string sessionId = "SO-NON-EXISTENT";
        _mediatorMock
            .Setup(x => x.Send(It.Is<GetSessionResultReportQuery>(q => q.SessionId == sessionId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockOpnameResultReportDto?)null);

        var result = await _sut.GetResultReport(sessionId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ApproveSession_WhenExecuted_ReturnsOk()
    {
        const string sessionId = "SO-202611-0001";
        var request = new ApproveSessionRequest();
        var expectedResponse = new ApproveSessionResponse(
            SessionId: sessionId,
            Status: SessionStatusEnum.Approved,
            ApprovedBy: "USER001",
            ApprovedAt: DateTime.UtcNow,
            SurplusValuation: 10000m,
            DeficitValuation: 5000m,
            NetValuation: 5000m,
            ApprovedItemsCount: 2);

        _mediatorMock
            .Setup(x => x.Send(It.Is<ApproveSessionCommand>(c => c.SessionId == sessionId && c.ApprovedBy == "USER001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.ApproveSession(sessionId, request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task RejectSession_WhenExecuted_ReturnsOk()
    {
        const string sessionId = "SO-202611-0001";
        var request = new RejectSessionRequest(
            Disposition: RejectionDispositionEnum.Rework,
            Notes: "Harap hitung ulang item antibiotik");

        var expectedResponse = new RejectSessionResponse(
            SessionId: sessionId,
            Status: SessionStatusEnum.Verifying,
            Disposition: RejectionDispositionEnum.Rework,
            RevisionNotes: "Harap hitung ulang item antibiotik");

        _mediatorMock
            .Setup(x => x.Send(It.Is<RejectSessionCommand>(c => c.SessionId == sessionId && c.Disposition == RejectionDispositionEnum.Rework), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.RejectSession(sessionId, request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CancelSession_WhenExecuted_ReturnsOk()
    {
        const string sessionId = "SO-202611-0001";
        var request = new CancelSessionRequest(Reason: "Salah buat tanggal cut off");

        var expectedResponse = new CancelSessionResponse(
            SessionId: sessionId,
            Status: SessionStatusEnum.Cancelled,
            CancelReason: "Salah buat tanggal cut off",
            CancelledBy: "USER001",
            CancelledAt: DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(It.Is<CancelSessionCommand>(c => c.SessionId == sessionId && c.Reason == "Salah buat tanggal cut off"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _sut.CancelSession(sessionId, request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public void DependencyInjection_RegistersAllRequiredServices()
    {
        var services = new ServiceCollection();
        services.AddOpnameFeature();

        var provider = services.BuildServiceProvider();

        // Check required service registrations
        services.Should().Contain(d => d.ServiceType == typeof(ISystemQuantityReconstructionService));
        services.Should().Contain(d => d.ServiceType == typeof(IStockOpnameSessionRepo));
        services.Should().Contain(d => d.ServiceType == typeof(IStockOpnameAdjustmentRepo));
    }
}
