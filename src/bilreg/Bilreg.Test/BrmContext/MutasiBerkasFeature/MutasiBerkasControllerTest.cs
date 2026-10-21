using Bilreg.Api.Configurations;
using Bilreg.Api.Controllers.BrmContext;
using Bilreg.Application.BrmContext;
using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.BrmContext;
using Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;
using LokasiBerkasDto = Bilreg.Application.BrmContext.MutasiBerkasFeature.LokasiBerkasDto;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly MutasiBerkasController _sut;

    public MutasiBerkasControllerTest()
    {
        _sut = new MutasiBerkasController(_mediatorMock.Object);
    }

    [Fact]
    public void Controller_HasExpectedAttributes()
    {
        typeof(MutasiBerkasController).Should().BeDecoratedWith<ApiControllerAttribute>();
        typeof(MutasiBerkasController).Should().BeDecoratedWith<AuthorizeAttribute>();
        var routeAttributes = typeof(MutasiBerkasController)
            .GetCustomAttributes(typeof(RouteAttribute), false)
            .Cast<RouteAttribute>()
            .Select(r => r.Template)
            .ToList();
        routeAttributes.Should().Contain("api/brm/mutasi-berkas");
        routeAttributes.Should().Contain("api/brm/[controller]");
    }

    [Fact]
    public async Task Kirim_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new MutasiBerkasKirimCommand(
            "RG00000001", "00000001", "DOK01", "POLI01", "2026-10-17", "08:00", "PAGI", "PET01", 1, 0);
        var expectedResponse = new MutasiBerkasKirimResponse("MK00000001", "RG00000001", "00000001", 1, 1m);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Kirim(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PinjamInternal_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new MutasiBerkasPinjamInternalCommand(
            "00000001", "CASEMIX", "PET02", "PET01", "Audit Berkas Medis", "2026-10-17", "09:00");
        var expectedResponse = new MutasiBerkasPinjamInternalResponse(
            "MK00000001", "PINJAM-20261017090000", "00000001", 1, 1m, "CASEMIX", "PET02", "PET01", "Audit Berkas Medis");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.PinjamInternal(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Terima_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new MutasiBerkasTerimaCommand("MK00000001", "PET02");
        var expectedResponse = new MutasiBerkasTerimaResponse("MP00000001", 2, "RG00000001", "00000001", "PET02");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Terima(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferUnit_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new MutasiBerkasTransferUnitCommand(
            "MP00000001", "PET02", "POLI02", "DOK02", "2026-10-17", "10:00", "SIANG", 1, 0);
        var expectedResponse = new MutasiBerkasTransferUnitResponse(
            "MK00000002", MutasiBerkasStatusEnum.Dispatched, "RG00000001", "00000001", "POLI02", "A-005");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.TransferUnit(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Kembali_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new MutasiBerkasKembaliCommand("MP00000001", "PET01", 1, 0);
        var expectedResponse = new MutasiBerkasKembaliResponse(
            "MM00000001", 3, new VolumeBerkasType(1, 0, 1, 0), "RG00000001", "00000001", "PET01");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Kembali(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Hilang_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new MutasiBerkasHilangCommand("MK00000001", "PET01");
        var expectedResponse = new MutasiBerkasHilangResponse("MH00000001", 4, "RG00000001", "00000001", "PET01");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Hilang(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Void_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new MutasiBerkasVoidCommand("MK00000001", "PET01", "Salah input poli");
        var expectedResponse = new MutasiBerkasVoidResponse("MK00000001", 1, 0, "RG00000001", "00000001");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Void(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CetakTracer_GivenValidCommand_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new MutasiBerkasCetakTracerCommand("MK00000001", "PET01");
        var expectedResponse = new MutasiBerkasCetakTracerResponse("MK00000001", 1, "PET01");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.CetakTracer(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Worklist_GivenQueryParams_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string tglPraktek = "2026-10-17";
        const string layananId = "POLI01";
        const string sesiPoli = "PAGI";
        const int status = 1;
        const string gedungLantai = "GEDUNG A";

        var expectedResponse = new List<MutasiBerkasWorklistItemDto>
        {
            new()
            {
                MrId = "00000001",
                PasienName = "Pasien Test",
                MutasiId = "MK00000001",
                Status = 1
            }
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<MutasiBerkasWorklistQuery>(q =>
                q.TglPraktek == tglPraktek &&
                q.LayananId == layananId &&
                q.SesiPoli == sesiPoli &&
                q.Status == status &&
                q.GedungLantai == gedungLantai), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Worklist(tglPraktek, layananId, sesiPoli, status, gedungLantai);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.Is<MutasiBerkasWorklistQuery>(q =>
            q.TglPraktek == tglPraktek &&
            q.LayananId == layananId &&
            q.SesiPoli == sesiPoli &&
            q.Status == status &&
            q.GedungLantai == gedungLantai), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Histori_GivenMr_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string mr = "00000001";
        var expectedResponse = new List<MutasiBerkasHistoriItemDto>
        {
            new()
            {
                MrId = mr,
                MutasiId = "MK00000001",
                Status = 1
            }
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<MutasiBerkasHistoriQuery>(q => q.MrId == mr), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Histori(mr);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.Is<MutasiBerkasHistoriQuery>(q => q.MrId == mr), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Posisi_GivenMr_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string mr = "00000001";
        var expectedResponse = new MutasiBerkasPosisiResponse(
            MutasiId: "MK00000001",
            TrsBuatId: "RG00000001",
            MrId: mr,
            LayananId: "POLI01",
            DokterId: "DOK01",
            PetugasId: "PET01",
            Status: 1,
            StatusDescription: "Dispatched",
            QtyPinjamRi: 1m,
            QtyKembaliRi: 0m,
            QtyPinjamRj: 0m,
            QtyKembaliRj: 0m,
            TotalPinjam: 1m,
            TotalKembali: 0m,
            SisaPinjam: 1m,
            TglMutasi: new DateOnly(2026, 10, 17),
            JamMutasi: new TimeOnly(8, 0)
        );

        _mediatorMock
            .Setup(x => x.Send(It.Is<MutasiBerkasPosisiGetQuery>(q => q.MrId == mr), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Posisi(mr);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.Is<MutasiBerkasPosisiGetQuery>(q => q.MrId == mr), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LokasiRak_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        var expectedResponse = new List<LokasiBerkasDto>
        {
            LokasiBerkasDto.Create("RAK01", "Rak Filing A", "POLI01")
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<LokasiBerkasF12Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse.AsEnumerable());

        // ACT
        var actionResult = await _sut.LokasiRak();

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<LokasiBerkasF12Query>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AddBrmInfrastructure_WhenCalled_RegistersExpectedDependencies()
    {
        // ARRANGE
        var services = new ServiceCollection();

        // ACT
        services.AddBrmInfrastructure();

        // ASSERT
        services.Should().Contain(d => d.ServiceType == typeof(IMutasiBerkasDal)
            && d.ImplementationType == typeof(MutasiBerkasDal)
            && d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d => d.ServiceType == typeof(IVolumeBerkasDal)
            && d.ImplementationType == typeof(VolumeBerkasDal)
            && d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d => d.ServiceType == typeof(ILokasiBerkasDal)
            && d.ImplementationType == typeof(LokasiBerkasDal)
            && d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d => d.ServiceType == typeof(IMutasiBerkasWorklistDal)
            && d.ImplementationType == typeof(MutasiBerkasWorklistDal)
            && d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d => d.ServiceType == typeof(IMutasiBerkasRepo)
            && d.ImplementationType == typeof(MutasiBerkasRepo)
            && d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d => d.ServiceType == typeof(IMutasiBerkasIdGenerator)
            && d.ImplementationType == typeof(MutasiBerkasIdGenerator)
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddBrmApplication_WhenCalled_ReturnsServiceCollection()
    {
        // ARRANGE
        var services = new ServiceCollection();

        // ACT
        var result = services.AddBrmApplication();

        // ASSERT
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void DependencyInjection_FullContainerResolution_CanResolveMutasiBerkasDependencies()
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
        services.AddTransient<MutasiBerkasController>();

        var provider = services.BuildServiceProvider();

        // ACT & ASSERT
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetService<MutasiBerkasController>().Should().NotBeNull();
        sp.GetService<IMutasiBerkasRepo>().Should().NotBeNull();
        sp.GetService<IMutasiBerkasIdGenerator>().Should().NotBeNull();
        sp.GetService<IMutasiBerkasDal>().Should().NotBeNull();
        sp.GetService<IVolumeBerkasDal>().Should().NotBeNull();
        sp.GetService<ILokasiBerkasDal>().Should().NotBeNull();
        sp.GetService<IMutasiBerkasWorklistDal>().Should().NotBeNull();

        // MediatR Handlers
        sp.GetService<IRequestHandler<MutasiBerkasKirimCommand, MutasiBerkasKirimResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBerkasPinjamInternalCommand, MutasiBerkasPinjamInternalResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBerkasTerimaCommand, MutasiBerkasTerimaResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBerkasTransferUnitCommand, MutasiBerkasTransferUnitResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBerkasKembaliCommand, MutasiBerkasKembaliResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBerkasHilangCommand, MutasiBerkasHilangResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBerkasVoidCommand, MutasiBerkasVoidResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBerkasCetakTracerCommand, MutasiBerkasCetakTracerResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBerkasWorklistQuery, IEnumerable<MutasiBerkasWorklistItemDto>>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBerkasHistoriQuery, IEnumerable<MutasiBerkasHistoriItemDto>>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<MutasiBerkasPosisiGetQuery, MutasiBerkasPosisiResponse>>().Should().NotBeNull();
        sp.GetService<IRequestHandler<LokasiBerkasF12Query, IEnumerable<LokasiBerkasDto>>>().Should().NotBeNull();
    }
}
