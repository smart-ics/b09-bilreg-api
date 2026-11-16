using Bilreg.Api.Configurations;
using Bilreg.Api.Controllers.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.InventoryContext.MutasiFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class MutasiBrgControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly MutasiBrgController _sut;

    public MutasiBrgControllerTest()
    {
        _sut = new MutasiBrgController(_mediatorMock.Object);
    }

    [Fact]
    public void Controller_HasExpectedAttributes()
    {
        typeof(MutasiBrgController).Should().BeDecoratedWith<ApiControllerAttribute>();
        typeof(MutasiBrgController).Should().BeDecoratedWith<RouteAttribute>(a => a.Template == "api/[controller]");
    }

    [Fact]
    public async Task CreateDraft_GivenCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new MutasiBrgCreateDraftCommand("L01", "L02", "ORD1", "Catatan", "USR1");
        var expectedResponse = new MutasiBrgCreateDraftResponse("MT2600001A");
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.CreateDraft(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddItem_GivenCommand_WhenExecuted_ThenSendsCommandAndReturnsOkDone()
    {
        // ARRANGE
        var cmd = new MutasiBrgAddItemCommand("MT2600001A", "BRG01", "B01", "2027-01-01", 10m, "SAT1", "USR1");

        // ACT
        var result = await _sut.AddItem(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveItem_GivenCommand_WhenExecuted_ThenSendsCommandAndReturnsOkDone()
    {
        // ARRANGE
        var cmd = new MutasiBrgRemoveItemCommand("MT2600001A", 1, "USR1");

        // ACT
        var result = await _sut.RemoveItem(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_GivenCommand_WhenExecuted_ThenSendsCommandAndReturnsOkDone()
    {
        // ARRANGE
        var cmd = new MutasiBrgDispatchCommand("MT2600001A", "USR1");

        // ACT
        var result = await _sut.Dispatch(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_GivenCommand_WhenExecuted_ThenSendsCommandAndReturnsOkDone()
    {
        // ARRANGE
        var cmd = new MutasiBrgCancelCommand("MT2600001A", "USR1", "Alasan pembatalan");

        // ACT
        var result = await _sut.Cancel(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_GivenId_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string id = "MT2600001A";
        var expectedResponse = new MutasiBrgGetResponse(
            MutasiBrgId: id,
            MutasiBrgDate: "2026-11-10 10:00:00",
            State: "Draft",
            LayananAsal: new LayananReff("L01", "Gudang"),
            LayananTujuan: new LayananReff("L02", "Apotek"),
            OrderMutasiId: "",
            DispatchNote: "",
            IssuerUserId: "",
            ListItem: new List<MutasiBrgGetItemResponse>());

        _mediatorMock
            .Setup(x => x.Send(It.Is<MutasiBrgGetQuery>(q => q.MutasiBrgId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.Get(id);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(It.Is<MutasiBrgGetQuery>(q => q.MutasiBrgId == id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_GivenDateRange_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string tgl1 = "2026-11-01";
        const string tgl2 = "2026-11-10";
        var expectedList = new List<MutasiBrgHeaderView>
        {
            new("MT2600001A", new DateTime(2026, 11, 10), MutasiBrgStateEnum.Draft, new LayananReff("L01", "Gudang"), new LayananReff("L02", "Apotek"), "", "", "")
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<MutasiBrgListQuery>(q => q.Tgl1 == tgl1 && q.Tgl2 == tgl2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // ACT
        var result = await _sut.List(tgl1, tgl2);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedList);
        _mediatorMock.Verify(x => x.Send(It.Is<MutasiBrgListQuery>(q => q.Tgl1 == tgl1 && q.Tgl2 == tgl2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListInTransit_GivenLayananTujuanId_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string tujuanId = "L02";
        var expectedList = new List<MutasiBrgHeaderView>
        {
            new("MT2600001A", new DateTime(2026, 11, 10), MutasiBrgStateEnum.Dispatched, new LayananReff("L01", "Gudang"), new LayananReff("L02", "Apotek"), "", "", "")
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<MutasiBrgInTransitListQuery>(q => q.LayananTujuanId == tujuanId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // ACT
        var result = await _sut.ListInTransit(tujuanId);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedList);
        _mediatorMock.Verify(x => x.Send(It.Is<MutasiBrgInTransitListQuery>(q => q.LayananTujuanId == tujuanId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DependencyInjection_MutasiBrgServices_CanBeResolvedFromContainer()
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
        services.AddTransient<MutasiBrgController>();

        var provider = services.BuildServiceProvider();

        // ACT & ASSERT
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetService<MutasiBrgController>().Should().NotBeNull();
        sp.GetService<IMutasiBrgDal>().Should().NotBeNull();
        sp.GetService<IMutasiBrgItemDal>().Should().NotBeNull();
        sp.GetService<IMutasiBrgRepo>().Should().NotBeNull();
        sp.GetService<IMutasiBrgMovementService>().Should().NotBeNull();
        sp.GetService<ISqlDatabase>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBrgCreateDraftCommand, MutasiBrgCreateDraftResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBrgAddItemCommand>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBrgRemoveItemCommand>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBrgDispatchCommand>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBrgCancelCommand>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBrgGetQuery, MutasiBrgGetResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBrgListQuery, IEnumerable<MutasiBrgHeaderView>>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBrgInTransitListQuery, IEnumerable<MutasiBrgHeaderView>>>().Should().NotBeNull();
    }
}
