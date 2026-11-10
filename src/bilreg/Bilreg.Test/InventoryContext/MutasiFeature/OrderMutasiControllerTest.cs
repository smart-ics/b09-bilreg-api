using System.Text.Json;
using Bilreg.Api.Controllers.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class OrderMutasiControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly OrderMutasiController _sut;

    public OrderMutasiControllerTest()
    {
        _sut = new OrderMutasiController(_mediatorMock.Object);
    }

    [Fact]
    public void AddItem_DeserializeFromJson_WithoutOrderMutasiId_MapsCorrectly()
    {
        // Arrange
        var json = """
        {
            "brgId": "BRG01",
            "qty": 5,
            "satuanId": "SAT01",
            "layananOrderId": "LAY01",
            "userId": "USR01"
        }
        """;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<OrderMutasiAddItemCommand>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.BrgId.Should().Be("BRG01");
        result.Qty.Should().Be(5);
        result.SatuanId.Should().Be("SAT01");
        result.LayananOrderId.Should().Be("LAY01");
        result.UserId.Should().Be("USR01");
        result.OrderMutasiId.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_DeserializeFromJson_WithOrderMutasiId_MapsCorrectly()
    {
        // Arrange
        var json = """
        {
            "orderMutasiId": "ORD001",
            "brgId": "BRG02",
            "qty": 12.5,
            "satuanId": "SAT02",
            "layananOrderId": "LAY02",
            "userId": "USR02"
        }
        """;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<OrderMutasiAddItemCommand>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.OrderMutasiId.Should().Be("ORD001");
        result.BrgId.Should().Be("BRG02");
        result.Qty.Should().Be(12.5m);
        result.SatuanId.Should().Be("SAT02");
        result.LayananOrderId.Should().Be("LAY02");
        result.UserId.Should().Be("USR02");
    }

    [Fact]
    public async Task AddItem_SendsCommand_ReturnsOk()
    {
        // Arrange
        var cmd = new OrderMutasiAddItemCommand("B01", 10, "S01", "L01", "USER01");

        // Act
        var result = await _sut.AddItem(cmd);

        // Assert
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateItemQty_SendsCommand_ReturnsOk()
    {
        // Arrange
        var cmd = new OrderMutasiUpdateItemQtyCommand("ORD01", "B01", 15, "USER01");

        // Act
        var result = await _sut.UpdateItemQty(cmd);

        // Assert
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RemoveItem_SendsCommand_ReturnsOk()
    {
        // Arrange
        var cmd = new OrderMutasiRemoveItemCommand("ORD01", "B01", "USER01");

        // Act
        var result = await _sut.RemoveItem(cmd);

        // Assert
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Submit_SendsCommand_ReturnsOk()
    {
        // Arrange
        var cmd = new OrderMutasiSubmitCommand("ORD01", "L02", "USER01", OrderMutasiPriorityEnum.Cito, "Urgente", false);

        // Act
        var result = await _sut.Submit(cmd);

        // Assert
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Approve_SendsCommand_ReturnsOk()
    {
        // Arrange
        var cmd = new OrderMutasiApproveCommand("ORD01", "USER02");

        // Act
        var result = await _sut.Approve(cmd);

        // Assert
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Reject_SendsCommand_ReturnsOk()
    {
        // Arrange
        var cmd = new OrderMutasiRejectCommand("ORD01", "Ditolak", "USER02");

        // Act
        var result = await _sut.Reject(cmd);

        // Assert
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Cancel_SendsCommand_ReturnsOk()
    {
        // Arrange
        var cmd = new OrderMutasiCancelCommand("ORD01", "Batal permintaan", "USER01");

        // Act
        var result = await _sut.Cancel(cmd);

        // Assert
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ForceClose_SendsCommand_ReturnsOk()
    {
        // Arrange
        var cmd = new OrderMutasiForceCloseCommand("ORD01", "Stok sisa tidak tersedia lagi", "USER02");

        // Act
        var result = await _sut.ForceClose(cmd);

        // Assert
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetOrder_SendsQuery_ReturnsOkWithResponse()
    {
        // Arrange
        var queryResponse = new OrderMutasiGetResponse(
            "ORD01", "2026-10-27 10:00:00", "Submitted", "Rutin",
            new LayananReff("L01", "Poli A"), new LayananReff("L02", "Gudang B"),
            new OrderMutasiApprovalGetResponse("-", "-"),
            new OrderMutasiApprovalGetResponse("-", "-"),
            "-", []);

        _mediatorMock.Setup(x => x.Send(It.Is<OrderMutasiGetQuery>(q => q.OrderMutasiId == "ORD01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResponse);

        // Act
        var result = await _sut.GetOrder("ORD01");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(queryResponse);
    }

    [Fact]
    public async Task GetOrderDraft_SendsQuery_ReturnsOkWithResponse()
    {
        // Arrange
        var draftResponse = new OrderMutasiDraftStateGetResponse(
            "DFT01", "2026-10-27 08:00:00", "Draft",
            new LayananReff("L01", "Poli A"), new LayananReff("-", "-"),
            "-", []);

        _mediatorMock.Setup(x => x.Send(It.Is<OrderMutasiDraftStateGetQuery>(q => q.LayananOrderId == "L01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftResponse);

        // Act
        var result = await _sut.GetOrderDraft("L01");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(draftResponse);
    }

    [Fact]
    public async Task ListByLayanan_SendsQuery_ReturnsOkWithResponse()
    {
        // Arrange
        var listResponse = new List<OrderMutasiListByLayananResponse>
        {
            new("ORD01", "2026-10-27 10:00:00", "Submitted", "Rutin",
                new LayananReff("L01", "Poli A"), new LayananReff("L02", "Gudang B"), "Catatan")
        };

        _mediatorMock.Setup(x => x.Send(It.Is<OrderMutasiListByLayananQuery>(q => q.LayananOrderId == "L01" && q.TglYmd1 == "2026-10-01" && q.TglYmd2 == "2026-10-31"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(listResponse);

        // Act
        var result = await _sut.ListByLayanan("L01", "2026-10-01", "2026-10-31");

        // Assert
        var okResult2 = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend2 = okResult2.Value.Should().BeOfType<JSendOk>().Subject;
        jsend2.data.Should().Be(listResponse);
    }

    [Fact]
    public async Task ListSubmitted_SendsQuery_ReturnsOkWithResponse()
    {
        // Arrange
        var listResponse = new List<OrderMutasiSubmittedListResponse>
        {
            new("ORD01", "2026-10-27 10:00:00", "Submitted", "Cito",
                new LayananReff("L01", "Poli A"), new LayananReff("L02", "Gudang B"), "Urgente")
        };

        _mediatorMock.Setup(x => x.Send(It.Is<OrderMutasiSubmittedListQuery>(q => q.TglYmd1 == "2026-10-01" && q.TglYmd2 == "2026-10-31"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(listResponse);

        // Act
        var result = await _sut.ListSubmitted("2026-10-01", "2026-10-31");

        // Assert
        var okResult3 = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend3 = okResult3.Value.Should().BeOfType<JSendOk>().Subject;
        jsend3.data.Should().Be(listResponse);
    }

    [Fact]
    public async Task ListApproved_SendsQuery_ReturnsOkWithResponse()
    {
        // Arrange
        var listResponse = new List<OrderMutasiApprovedListResponse>
        {
            new("ORD01", "2026-10-27 10:00:00", "Approved", "Rutin",
                new LayananReff("L01", "Poli A"), new LayananReff("L02", "Gudang B"), "Siap kirim")
        };

        _mediatorMock.Setup(x => x.Send(It.Is<OrderMutasiApprovedListQuery>(q => q.TglYmd1 == "2026-10-01" && q.TglYmd2 == "2026-10-31"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(listResponse);

        // Act
        var result = await _sut.ListApproved("2026-10-01", "2026-10-31");

        // Assert
        var okResult4 = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend4 = okResult4.Value.Should().BeOfType<JSendOk>().Subject;
        jsend4.data.Should().Be(listResponse);
    }
}
