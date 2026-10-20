using Bilreg.Api.Configurations;
using Bilreg.Api.Controllers.PurchaseContext.DeliveryFeature;
using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly DeliveryController _sut;

    public DeliveryControllerTest()
    {
        _sut = new DeliveryController(_mediatorMock.Object);
    }

    [Fact]
    public void Controller_HasExpectedAttributes()
    {
        typeof(DeliveryController).Should().BeDecoratedWith<ApiControllerAttribute>();
        typeof(DeliveryController).Should().BeDecoratedWith<AuthorizeAttribute>();
        typeof(DeliveryController).Should().BeDecoratedWith<RouteAttribute>(a => a.Template == "api/purchase/delivery");
    }

    [Fact]
    public async Task CreateDraft_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new DeliveryCreateDraftCommand(
            "SJ-001",
            "PO-001",
            "SUP-01",
            "GDFAR",
            "Penerimaan barang batch 1",
            "USR01",
            new List<DeliveryCreateDraftItemCommand>()
        );
        var expectedResponse = new DeliveryCreateDraftResponse("DM001");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.CreateDraft(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDraft_GivenValidCommand_WhenExecuted_ThenSendsCommandWithRouteIdAndReturnsOk()
    {
        // ARRANGE
        const string doId = "DM001";
        var cmd = new DeliveryUpdateDraftCommand(
            "",
            "SJ-001",
            "PO-001",
            "SUP-01",
            "GDFAR",
            "Update draft keterangan",
            "USR01",
            new List<DeliveryUpdateDraftItemCommand>()
        );
        var expectedResponse = new DeliveryUpdateDraftResponse(doId);

        _mediatorMock
            .Setup(x => x.Send(It.Is<DeliveryUpdateDraftCommand>(c => c.DeliveryOrderId == doId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.UpdateDraft(doId, cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.Is<DeliveryUpdateDraftCommand>(c => c.DeliveryOrderId == doId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Confirm_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new DeliveryConfirmCommand("DM001", "USR01");
        var expectedResponse = new DeliveryConfirmResponse("DM001", "Confirmed");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Confirm(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DirectConfirm_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new DeliveryDirectConfirmCommand(
            "SJ-001",
            "PO-001",
            "SUP-01",
            "GDFAR",
            "Direct confirm",
            "USR01",
            new List<DeliveryDirectConfirmItemCommand>()
        );
        var expectedResponse = new DeliveryDirectConfirmResponse("DM001", "Confirmed");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.DirectConfirm(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VoidDraft_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new DeliveryVoidDraftCommand("DM001", "USR01");
        var expectedResponse = new DeliveryVoidDraftResponse("DM001", "Voided");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.VoidDraft(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_GivenId_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string doId = "DM001";
        var expectedResponse = new DeliveryGetResponse(
            doId,
            "SJ-001",
            "PO-001",
            "2026-10-15 10:00:00",
            new PartnerReff("SUP-01", "Vendor A"),
            new LayananReff("GDFAR", "Gudang Farmasi"),
            "Catatan",
            "Draft",
            100000m,
            11000m,
            0m,
            111000m,
            0m,
            0m,
            111000m,
            new DeliveryGetConfirmedResponse("", ""),
            new List<DeliveryGetItemResponse>()
        );

        _mediatorMock
            .Setup(x => x.Send(It.Is<DeliveryGetQuery>(q => q.DeliveryOrderId == doId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Get(doId);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.Is<DeliveryGetQuery>(q => q.DeliveryOrderId == doId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListByPeriode_GivenDateRange_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string tglAwal = "2026-10-01";
        const string tglAkhir = "2026-10-15";
        var expectedResponse = new List<DeliveryListByPeriodeResponse>
        {
            new(
                "DM001",
                "2026-10-15 10:00:00",
                "SJ-001",
                "PO-001",
                "GDFAR",
                "Gudang Farmasi",
                "SUP-01",
                "Vendor A",
                "Catatan",
                "Draft",
                111000m
            )
        };

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<DeliveryListByPeriodeQuery>(q => q.TglYmdAwal == tglAwal && q.TglYmdAkhir == tglAkhir),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.ListByPeriode(tglAwal, tglAkhir);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<DeliveryListByPeriodeQuery>(q => q.TglYmdAwal == tglAwal && q.TglYmdAkhir == tglAkhir),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListPendingPo_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        var expectedResponse = new List<DeliveryPendingPoResponse>
        {
            new(
                "PO-001",
                "2026-10-10",
                "SUP-01",
                "Vendor A",
                "GDFAR",
                "Gudang Farmasi",
                "PO Catatan",
                0m,
                new List<DeliveryPendingPoItemResponse>()
            )
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<DeliveryListPendingPoQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.ListPendingPo();

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<DeliveryListPendingPoQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateSuratJalan_GivenQueryParams_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string supplierId = "SUP-01";
        const string suratJalanNo = "SJ-001";
        const string excludeDoId = "DM001";
        var expectedResponse = new DeliveryValidateSuratJalanResponse(true, "", "", "");

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<DeliveryValidateSuratJalanQuery>(q => q.SupplierId == supplierId && q.SuratJalanNo == suratJalanNo && q.ExcludeDoId == excludeDoId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.ValidateSuratJalan(supplierId, suratJalanNo, excludeDoId);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<DeliveryValidateSuratJalanQuery>(q => q.SupplierId == supplierId && q.SuratJalanNo == suratJalanNo && q.ExcludeDoId == excludeDoId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DependencyInjection_DeliveryFeatureServices_CanBeResolvedFromContainer()
    {
        // ARRANGE
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Server=localhost;Database=Test;Integrated Security=true;",
                ["BusinessDate:DateFormat"] = "yyyy-MM-dd"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(config);
        services.AddInfrastructure(config);
        services.AddTransient<DeliveryController>();

        var provider = services.BuildServiceProvider();

        // ACT & ASSERT
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetService<DeliveryController>().Should().NotBeNull();
        sp.GetService<IDeliveryOrderDal>().Should().NotBeNull();
        sp.GetService<IDeliveryOrderItemDal>().Should().NotBeNull();
        sp.GetService<IDeliveryInspectionDal>().Should().NotBeNull();
        sp.GetService<IPoDeliveryTrackingDal>().Should().NotBeNull();
        sp.GetService<IDeliveryValidationDal>().Should().NotBeNull();
        sp.GetService<IDeliveryOrderRepo>().Should().NotBeNull();

        sp.GetService<IRequestHandler<DeliveryCreateDraftCommand, DeliveryCreateDraftResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<DeliveryUpdateDraftCommand, DeliveryUpdateDraftResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<DeliveryConfirmCommand, DeliveryConfirmResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<DeliveryDirectConfirmCommand, DeliveryDirectConfirmResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<DeliveryVoidDraftCommand, DeliveryVoidDraftResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<DeliveryGetQuery, DeliveryGetResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<DeliveryValidateSuratJalanQuery, DeliveryValidateSuratJalanResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<DeliveryListByPeriodeQuery, IEnumerable<DeliveryListByPeriodeResponse>>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<DeliveryListPendingPoQuery, IEnumerable<DeliveryPendingPoResponse>>>().Should().NotBeNull();
    }
}
