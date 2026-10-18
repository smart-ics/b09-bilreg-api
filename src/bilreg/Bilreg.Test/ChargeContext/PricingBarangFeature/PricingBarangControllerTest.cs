using Bilreg.Api.Controllers.ChargeContext;
using Bilreg.Application;
using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.BrgContext.BrgFeature;
using Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

/// <summary>
/// Unit test controller <see cref="PricingBarangController"/> dan <see cref="TipeHargaController"/>:
/// memverifikasi routing/argument passing, pemetaan respons <see cref="JSendOk"/>, propagasi
/// not-found, serta pembuktian resolusi seluruh komponen fitur dari DI container.
/// </summary>
public class PricingBarangControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly PricingBarangController _pricingSut;
    private readonly TipeHargaController _tipeHargaSut;

    public PricingBarangControllerTest()
    {
        _pricingSut = new PricingBarangController(_mediatorMock.Object);
        _tipeHargaSut = new TipeHargaController(_mediatorMock.Object);
    }

    // ---------------------------------------------------------------------
    // PricingBarangController
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Get_GivenBarangId_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string barangId = "BRG001";
        var expected = new PricingBarangGetByBarangIdResponse(
            barangId,
            12000m,
            MetodeGenHnaEnum.Terakhir,
            [
                new PricingBarangVariantResponse("UM", 0m, 10m, 0m, 0m, 0m, true)
            ]);

        _mediatorMock
            .Setup(x => x.Send(It.Is<PricingBarangGetByBarangIdQuery>(q => q.BarangId == barangId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // ACT
        var actionResult = await _pricingSut.Get(barangId);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(
            x => x.Send(It.Is<PricingBarangGetByBarangIdQuery>(q => q.BarangId == barangId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Get_WhenHandlerThrowsKeyNotFound_WhenExecuted_ThenExceptionPropagatesFor404Mapping()
    {
        // ARRANGE
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PricingBarangGetByBarangIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Pricing barang 'BRG404' tidak ditemukan."));

        // ACT
        var act = async () => await _pricingSut.Get("BRG404");

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Update_GivenRouteIdAndBody_WhenExecuted_ThenSendsCommandWithRouteIdAndReturnsOk()
    {
        // ARRANGE
        const string barangId = "BRG001";
        var body = new PricingBarangUpdateBody(
            12000m,
            MetodeGenHnaEnum.RataRata,
            [new PricingBarangVariantInput("UM", 0m, 12m, 500m, 0m, 0m, true)],
            "USR01");

        PricingBarangUpdateCommand? captured = null;
        var expected = new PricingBarangUpdateResponse(barangId, false, 1, 12000m);
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PricingBarangUpdateCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PricingBarangUpdateResponse>, CancellationToken>(
                (req, _) => captured = req as PricingBarangUpdateCommand)
            .ReturnsAsync(expected);

        // ACT
        var actionResult = await _pricingSut.Update(barangId, body);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        captured.Should().NotBeNull();
        captured!.BarangId.Should().Be(barangId);
        captured.HetNominal.Should().Be(body.HetNominal);
        captured.MetodeGenHna.Should().Be(body.MetodeGenHna);
        captured.Variants.Should().BeEquivalentTo(body.Variants);
        captured.UserId.Should().Be(body.UserId);

        _mediatorMock.Verify(x => x.Send(It.IsAny<PricingBarangUpdateCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Calculate_GivenQuery_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        var query = new CalculateItemPriceQuery("BRG001", "UM", new DateTime(2026, 5, 1), 2, "BOX");
        var expected = new ItemPriceCalculationResultDto(
            "BRG001", "UM", query.TransactionDate, 2, "BOX", 10m,
            1000m, 12m, 0m, false, false, 0m, 1120m, 11200m, 0m, 0m, 0m, 0m);

        _mediatorMock
            .Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // ACT
        var actionResult = await _pricingSut.Calculate(query);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(x => x.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateHna_GivenCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new GenerateHnaBatchCommand(
            ["BRG001"],
            CaraGenerateHnaEnum.HppPlusTax,
            MetodeGenHnaEnum.Terakhir,
            new DateTime(2026, 5, 1),
            "USR01");

        var expected = new GenerateHnaBatchResponse(
            "HN260001A",
            new DateTime(2026, 5, 1),
            new DateTime(2026, 5, 1),
            1,
            [new GenerateHnaBatchItemResponse("BRG001", 1, 1000m, 1000m, 1000m, 1000m, 0m)]);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // ACT
        var actionResult = await _pricingSut.GenerateHna(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRounding_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        var expected = new RoundingPolicyGetResponse(100, RoundingModeEnum.KeAtas);
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoundingPolicyGetQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // ACT
        var actionResult = await _pricingSut.GetRounding();

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(x => x.Send(It.IsAny<RoundingPolicyGetQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRounding_GivenCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new RoundingPolicyUpdateCommand(500, RoundingModeEnum.Normal);
        var expected = new RoundingPolicyUpdateResponse(500, RoundingModeEnum.Normal);
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // ACT
        var actionResult = await _pricingSut.UpdateRounding(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------------------------------------------------------------------
    // TipeHargaController
    // ---------------------------------------------------------------------

    [Fact]
    public async Task TipeHargaList_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        IReadOnlyList<TipeHargaListResponse> expected =
        [
            new TipeHargaListResponse("UM", "Umum", 10m, 1000m, 2000m, 0m, 0m, true)
        ];

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TipeHargaListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // ACT
        var actionResult = await _tipeHargaSut.List();

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expected);

        _mediatorMock.Verify(x => x.Send(It.IsAny<TipeHargaListQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TipeHargaSave_GivenCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new TipeHargaSaveCommand("UM", "Umum", 10m, 1000m, 2000m, 0m, 0m, true);
        var expected = new TipeHargaSaveResponse("UM", false);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // ACT
        var actionResult = await _tipeHargaSut.Save(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------------------------------------------------------------------
    // Dependency Injection wiring
    // ---------------------------------------------------------------------

    [Fact]
    public void AddPricingBarangFeature_WhenCalledOnServiceCollection_ThenRegistersExpectedDependencies()
    {
        // ARRANGE
        var services = new ServiceCollection();

        // ACT
        services.AddPricingBarangFeature();

        // ASSERT
        services.Should().Contain(d => d.ServiceType == typeof(IPricingBarangDal)
            && d.ImplementationType == typeof(PricingBarangDal)
            && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(IPricingBarangItemDal)
            && d.ImplementationType == typeof(PricingBarangItemDal)
            && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(ITipeHargaDal)
            && d.ImplementationType == typeof(TipeHargaDal)
            && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(IHnaBatchDal)
            && d.ImplementationType == typeof(HnaBatchDal)
            && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(IHnaBatchItemDal)
            && d.ImplementationType == typeof(HnaBatchItemDal)
            && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(IRoundingPolicyDal)
            && d.ImplementationType == typeof(RoundingPolicyDal)
            && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(IPurchaseReceiptHnaDal)
            && d.ImplementationType == typeof(PurchaseReceiptHnaDal)
            && d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d => d.ServiceType == typeof(IPricingBarangRepo)
            && d.ImplementationType == typeof(PricingBarangRepo)
            && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(ITipeHargaRepo)
            && d.ImplementationType == typeof(TipeHargaRepo)
            && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(IHnaBatchRepo)
            && d.ImplementationType == typeof(HnaBatchRepo)
            && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(IRoundingPolicyRepo)
            && d.ImplementationType == typeof(RoundingPolicyRepo)
            && d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d => d.ServiceType == typeof(IItemPricingCalculator)
            && d.ImplementationType == typeof(ItemPricingCalculator)
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddPricingBarangFeature_WhenWiredWithBrgFeature_ThenEveryPortAndHandlerResolves()
    {
        // ARRANGE — mirror the host wiring: feature installer + Brg installer (IBrgSatuanDal) +
        // MediatR handler discovery from the Application assembly + a stubbed clock.
        var services = new ServiceCollection();
        services.Configure<DatabaseOptions>(_ => { });
        services.AddBrgFeature();
        services.AddPricingBarangFeature();
        services.AddScoped<ITglJamProvider>(_ => Mock.Of<ITglJamProvider>());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(ApplicationAssemblyAnchor).Assembly));

        var provider = services.BuildServiceProvider();

        // ACT / ASSERT — persistence ports & calculator resolve with their real implementation graph.
        provider.GetRequiredService<IPricingBarangDal>().Should().BeOfType<PricingBarangDal>();
        provider.GetRequiredService<IPricingBarangItemDal>().Should().BeOfType<PricingBarangItemDal>();
        provider.GetRequiredService<ITipeHargaDal>().Should().BeOfType<TipeHargaDal>();
        provider.GetRequiredService<IHnaBatchDal>().Should().BeOfType<HnaBatchDal>();
        provider.GetRequiredService<IHnaBatchItemDal>().Should().BeOfType<HnaBatchItemDal>();
        provider.GetRequiredService<IRoundingPolicyDal>().Should().BeOfType<RoundingPolicyDal>();
        provider.GetRequiredService<IPurchaseReceiptHnaDal>().Should().BeOfType<PurchaseReceiptHnaDal>();
        provider.GetRequiredService<IPurchaseReceiptHnaQuery>().Should().BeOfType<PurchaseReceiptHnaDal>();

        provider.GetRequiredService<IPricingBarangRepo>().Should().BeOfType<PricingBarangRepo>();
        provider.GetRequiredService<ITipeHargaRepo>().Should().BeOfType<TipeHargaRepo>();
        provider.GetRequiredService<IHnaBatchRepo>().Should().BeOfType<HnaBatchRepo>();
        provider.GetRequiredService<IRoundingPolicyRepo>().Should().BeOfType<RoundingPolicyRepo>();
        provider.GetRequiredService<IItemPricingCalculator>().Should().BeOfType<ItemPricingCalculator>();

        // Every new Application handler resolves through the container.
        provider.GetRequiredService<IRequestHandler<PricingBarangUpdateCommand, PricingBarangUpdateResponse>>()
            .Should().NotBeNull();
        provider.GetRequiredService<IRequestHandler<TipeHargaSaveCommand, TipeHargaSaveResponse>>()
            .Should().NotBeNull();
        provider.GetRequiredService<IRequestHandler<RoundingPolicyUpdateCommand, RoundingPolicyUpdateResponse>>()
            .Should().NotBeNull();
        provider.GetRequiredService<IRequestHandler<GenerateHnaBatchCommand, GenerateHnaBatchResponse>>()
            .Should().NotBeNull();
        provider.GetRequiredService<IRequestHandler<PricingBarangGetByBarangIdQuery, PricingBarangGetByBarangIdResponse>>()
            .Should().NotBeNull();
        provider.GetRequiredService<IRequestHandler<TipeHargaListQuery, IReadOnlyList<TipeHargaListResponse>>>()
            .Should().NotBeNull();
        provider.GetRequiredService<IRequestHandler<CalculateItemPriceQuery, ItemPriceCalculationResultDto>>()
            .Should().NotBeNull();
        provider.GetRequiredService<IRequestHandler<RoundingPolicyGetQuery, RoundingPolicyGetResponse>>()
            .Should().NotBeNull();
    }
}
