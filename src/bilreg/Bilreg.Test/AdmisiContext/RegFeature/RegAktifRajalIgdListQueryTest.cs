using System.Net;
using System.Text.Json;
using Bilreg.Api.AdmisiContext.AntrianFeature;
using Bilreg.Api.Configurations;
using Bilreg.Api.Controllers.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RegFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Param;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegAktifRajalIgdListQueryTest
{
    [Fact]
    public void Repo_ListDataRajalIgd_WhenDalReturnsDtos_MapsModelsCorrectlyAndDoesNotCallFullListData()
    {
        var dalMock = new Mock<IRegAktifDal>(MockBehavior.Strict);
        var kodeRsMock = new Mock<IGetKodeRsService>(MockBehavior.Strict);
        var dtos = new List<RegAktifDto>
        {
            new(
                RegId: "RG20261107002",
                RegDate: new DateTime(2026, 11, 7),
                PasienId: "00028231",
                JenisReg: "0",
                LayananId: "POLI01",
                DokterId: "DR01",
                TipeJaminanId: "BPJS",
                PasienName: "Siti Aminah",
                TglLahir: "1992-04-15",
                Gender: "P",
                LayananName: "Poli Penyakit Dalam",
                DokterName: "dr. Budi, Sp.PD",
                TipeJaminanName: "BPJS Kesehatan",
                StatusTataRekening: 0),
            new(
                RegId: "RG20261107001",
                RegDate: new DateTime(2026, 11, 7),
                PasienId: "00019999",
                JenisReg: "3",
                LayananId: "IGD01",
                DokterId: "DR02",
                TipeJaminanId: "UMUM",
                PasienName: "Andi Pratama",
                TglLahir: "1988-09-21",
                Gender: "L",
                LayananName: "IGD Umum",
                DokterName: "dr. Citra",
                TipeJaminanName: "Umum / Pribadi",
                StatusTataRekening: 2)
        };

        dalMock.Setup(x => x.ListDataRajalIgd()).Returns(dtos);

        var sut = new RegAktifRepo(dalMock.Object, kodeRsMock.Object);

        var actual = sut.ListDataRajalIgd().ToList();

        actual.Should().HaveCount(2);

        actual[0].RegId.Should().Be("RG20261107002");
        actual[0].RegDate.Should().Be(new DateOnly(2026, 11, 7));
        actual[0].JenisReg.Should().Be(JenisRegEnum.RegJalan);
        actual[0].Pasien.Should().Be(new PasienReff("00028231", "Siti Aminah", new DateOnly(1992, 4, 15), "P"));
        actual[0].Layanan.Should().Be(new LayananReff("POLI01", "Poli Penyakit Dalam"));
        actual[0].Dokter.Should().Be(new PpaReff("DR01", "dr. Budi, Sp.PD"));
        actual[0].TipeJaminan.Should().Be(new TipeJaminanReff("BPJS", "BPJS Kesehatan"));
        actual[0].StatusTataRekening.Should().Be(0);

        actual[1].RegId.Should().Be("RG20261107001");
        actual[1].RegDate.Should().Be(new DateOnly(2026, 11, 7));
        actual[1].JenisReg.Should().Be(JenisRegEnum.Darurat);
        actual[1].Pasien.Should().Be(new PasienReff("00019999", "Andi Pratama", new DateOnly(1988, 9, 21), "L"));
        actual[1].Layanan.Should().Be(new LayananReff("IGD01", "IGD Umum"));
        actual[1].Dokter.Should().Be(new PpaReff("DR02", "dr. Citra"));
        actual[1].TipeJaminan.Should().Be(new TipeJaminanReff("UMUM", "Umum / Pribadi"));
        actual[1].StatusTataRekening.Should().Be(2);

        dalMock.Verify(x => x.ListDataRajalIgd(), Times.Once);
        dalMock.VerifyNoOtherCalls();
        kodeRsMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Repo_ListDataRajalIgd_WhenDalReturnsNull_ReturnsEmptyCollection()
    {
        var dalMock = new Mock<IRegAktifDal>(MockBehavior.Strict);
        var kodeRsMock = new Mock<IGetKodeRsService>(MockBehavior.Strict);
        dalMock.Setup(x => x.ListDataRajalIgd()).Returns((IEnumerable<RegAktifDto>)null!);

        var sut = new RegAktifRepo(dalMock.Object, kodeRsMock.Object);

        var actual = sut.ListDataRajalIgd();

        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
        dalMock.Verify(x => x.ListDataRajalIgd(), Times.Once);
    }

    [Fact]
    public void Repo_ListDataRajalIgd_WhenDalReturnsEmpty_ReturnsEmptyCollection()
    {
        var dalMock = new Mock<IRegAktifDal>(MockBehavior.Strict);
        var kodeRsMock = new Mock<IGetKodeRsService>(MockBehavior.Strict);
        dalMock.Setup(x => x.ListDataRajalIgd()).Returns([]);

        var sut = new RegAktifRepo(dalMock.Object, kodeRsMock.Object);

        var actual = sut.ListDataRajalIgd();

        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
        dalMock.Verify(x => x.ListDataRajalIgd(), Times.Once);
    }

    [Fact]
    public async Task Handler_GivenRajalAndIgdModelsAcrossBillingStatuses_MapsAll15FieldsCorrectly()
    {
        var repoMock = new Mock<IRegAktifRepo>(MockBehavior.Strict);
        var models = new List<RegAktifModel>
        {
            BuildModel("RG004", new DateOnly(2026, 11, 7), "00010001", "Pasien Open", new DateOnly(1995, 1, 10), "P",
                JenisRegEnum.RegJalan, "LYN01", "Poli Anak", "DR01", "dr. Ana", "BPJS", "BPJS Kesehatan", statusTataRekening: 0),
            BuildModel("RG003", new DateOnly(2026, 11, 7), "00010002", "Pasien Close", new DateOnly(1980, 6, 20), "L",
                JenisRegEnum.Darurat, "IGD01", "IGD", "DR02", "dr. Bima", "UMUM", "Umum", statusTataRekening: 1),
            BuildModel("RG002", new DateOnly(2026, 11, 6), "00010003", "Pasien Finalisasi", new DateOnly(1975, 12, 5), "P",
                JenisRegEnum.RegJalan, "LYN02", "Poli Jantung", "DR03", "dr. Candra", "ASUR", "Asuransi Swasta", statusTataRekening: 2),
            BuildModel("RG001", new DateOnly(2026, 11, 5), "00010004", "Pasien Lunas", new DateOnly(2001, 3, 30), "L",
                JenisRegEnum.Darurat, "IGD01", "IGD", "DR02", "dr. Bima", "BPJS", "BPJS Kesehatan", statusTataRekening: 3)
        };

        repoMock.Setup(x => x.ListDataRajalIgd()).Returns(models);

        var sut = new RegAktifRajalIgdListHandler(repoMock.Object);

        var actual = (await sut.Handle(new RegAktifRajalIgdListQuery(), CancellationToken.None)).ToList();

        actual.Should().HaveCount(4);
        actual[0].Should().Be(new RegAktifRajalIgdListResponse(
            RegId: "RG004",
            RegDate: "2026-11-07",
            PasienId: "00010001",
            PasienName: "Pasien Open",
            TglLahir: "1995-01-10",
            Gender: "P",
            JenisReg: "0",
            JenisRegString: "RegJalan",
            LayananId: "LYN01",
            LayananName: "Poli Anak",
            DokterId: "DR01",
            DokterName: "dr. Ana",
            TipeJaminanId: "BPJS",
            TipeJaminanName: "BPJS Kesehatan",
            StatusTataRekening: 0));

        actual[1].JenisReg.Should().Be("3");
        actual[1].JenisRegString.Should().Be("Darurat");
        actual[1].StatusTataRekening.Should().Be(1);

        actual[2].RegDate.Should().Be("2026-11-06");
        actual[2].TglLahir.Should().Be("1975-12-05");
        actual[2].TipeJaminanId.Should().Be("ASUR");
        actual[2].TipeJaminanName.Should().Be("Asuransi Swasta");
        actual[2].StatusTataRekening.Should().Be(2);

        actual[3].RegDate.Should().Be("2026-11-05");
        actual[3].TglLahir.Should().Be("2001-03-30");
        actual[3].StatusTataRekening.Should().Be(3);

        repoMock.Verify(x => x.ListDataRajalIgd(), Times.Once);
        repoMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handler_WhenRepoReturnsNull_ReturnsEmptyCollection()
    {
        var repoMock = new Mock<IRegAktifRepo>(MockBehavior.Strict);
        repoMock.Setup(x => x.ListDataRajalIgd()).Returns((IEnumerable<RegAktifModel>)null!);

        var sut = new RegAktifRajalIgdListHandler(repoMock.Object);

        var actual = await sut.Handle(new RegAktifRajalIgdListQuery(), CancellationToken.None);

        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
        repoMock.Verify(x => x.ListDataRajalIgd(), Times.Once);
    }

    [Fact]
    public void Controller_HasExpectedClassAndEndpointAttributes_WhilePreservingLegacyEndpoints()
    {
        typeof(RegController).Should().BeDecoratedWith<ApiControllerAttribute>();
        typeof(RegController).Should().BeDecoratedWith<AuthorizeAttribute>();
        typeof(RegController).Should().BeDecoratedWith<RouteAttribute>(a => a.Template == "api/[controller]");

        var rajalIgdMethod = typeof(RegController).GetMethod(nameof(RegController.ListAktifRajalIgd));
        rajalIgdMethod.Should().NotBeNull();
        rajalIgdMethod!.Should().BeDecoratedWith<HttpGetAttribute>();
        rajalIgdMethod.Should().BeDecoratedWith<RouteAttribute>(a => a.Template == "aktif/rajal-igd");

        var listAktifMethod = typeof(RegController).GetMethod(nameof(RegController.ListAktif), Type.EmptyTypes);
        listAktifMethod.Should().NotBeNull();
        listAktifMethod!.Should().BeDecoratedWith<HttpGetAttribute>();
        listAktifMethod.Should().BeDecoratedWith<RouteAttribute>(a => a.Template == "listAktif");

        var listByJenisRegMethod = typeof(RegController).GetMethod(nameof(RegController.ListAktifByJenisReg));
        listByJenisRegMethod.Should().NotBeNull();
        listByJenisRegMethod!.Should().BeDecoratedWith<HttpGetAttribute>();
        listByJenisRegMethod.Should().BeDecoratedWith<RouteAttribute>(a => a.Template == "aktif/{jenisReg}/jenisReg");
    }

    [Fact]
    public async Task Controller_ListAktifRajalIgd_SendsQueryAndReturnsJSendOk()
    {
        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        var workstationResolverMock = new Mock<IAdmissionQueueWorkstationResolver>(MockBehavior.Strict);
        var expected = new List<RegAktifRajalIgdListResponse>
        {
            new(
                RegId: "RG20261107001",
                RegDate: "2026-11-07",
                PasienId: "00028231",
                PasienName: "Siti Aminah",
                TglLahir: "1992-04-15",
                Gender: "P",
                JenisReg: "0",
                JenisRegString: "RegJalan",
                LayananId: "POLI01",
                LayananName: "Poli Penyakit Dalam",
                DokterId: "DR01",
                DokterName: "dr. Budi",
                TipeJaminanId: "BPJS",
                TipeJaminanName: "BPJS Kesehatan",
                StatusTataRekening: 1)
        };

        mediatorMock
            .Setup(x => x.Send(It.IsAny<RegAktifRajalIgdListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new RegController(mediatorMock.Object, workstationResolverMock.Object);

        var actionResult = await sut.ListAktifRajalIgd();

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expected);

        mediatorMock.Verify(x => x.Send(It.IsAny<RegAktifRajalIgdListQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        mediatorMock.VerifyNoOtherCalls();
        workstationResolverMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EndpointRouting_GetAktifRajalIgd_ResolvesToDedicatedActionWithoutAmbiguityWithAktifPasienId()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<RegAktifRajalIgdListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new RegAktifRajalIgdListResponse(
                    "RG01", "2026-11-07", "00028231", "Siti Aminah", "1992-04-15", "P",
                    "0", "RegJalan", "POLI01", "Poli Umum", "DR01", "dr. Budi", "BPJS", "BPJS Kesehatan", 2)
            ]);
        mediatorMock
            .Setup(x => x.Send(It.IsAny<RegAktifByMrListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(mediatorMock.Object);
        builder.Services.AddSingleton(Mock.Of<IAdmissionQueueWorkstationResolver>());
        builder.Services.AddControllers().AddApplicationPart(typeof(RegController).Assembly);

        await using var app = builder.Build();
        app.MapControllers().AllowAnonymous();
        await app.StartAsync();

        using var client = app.GetTestClient();

        var rajalIgdResponse = await client.GetAsync("/api/reg/aktif/rajal-igd");
        rajalIgdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await rajalIgdResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("success");
        var firstItem = doc.RootElement.GetProperty("data")[0];
        firstItem.GetProperty("regId").GetString().Should().Be("RG01");
        firstItem.GetProperty("tglLahir").GetString().Should().Be("1992-04-15");
        firstItem.GetProperty("gender").GetString().Should().Be("P");
        firstItem.GetProperty("tipeJaminanId").GetString().Should().Be("BPJS");
        firstItem.GetProperty("tipeJaminanName").GetString().Should().Be("BPJS Kesehatan");
        firstItem.GetProperty("statusTataRekening").GetInt32().Should().Be(2);

        mediatorMock.Verify(x => x.Send(It.IsAny<RegAktifRajalIgdListQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        mediatorMock.Verify(x => x.Send(It.IsAny<RegAktifByMrListQuery>(), It.IsAny<CancellationToken>()), Times.Never);

        var byMrResponse = await client.GetAsync("/api/reg/aktif/00028231");
        byMrResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        mediatorMock.Verify(
            x => x.Send(It.Is<RegAktifByMrListQuery>(q => q.PasienId == "00028231"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void DependencyInjection_RegAktifRajalIgdServices_CanBeResolvedFromContainer()
    {
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

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetService<IRegAktifDal>().Should().BeOfType<RegAktifDal>();
        sp.GetService<IRegAktifRepo>().Should().BeOfType<RegAktifRepo>();
        sp.GetService<IRequestHandler<RegAktifRajalIgdListQuery, IEnumerable<RegAktifRajalIgdListResponse>>>()
            .Should().BeOfType<RegAktifRajalIgdListHandler>();
    }

    private static RegAktifModel BuildModel(
        string regId,
        DateOnly regDate,
        string pasienId,
        string pasienName,
        DateOnly tglLahir,
        string gender,
        JenisRegEnum jenisReg,
        string layananId,
        string layananName,
        string dokterId,
        string dokterName,
        string tipeJaminanId,
        string tipeJaminanName,
        int statusTataRekening) =>
        new(
            regId,
            regDate,
            new PasienReff(pasienId, pasienName, tglLahir, gender),
            jenisReg,
            new LayananReff(layananId, layananName),
            new PpaReff(dokterId, dokterName),
            new TipeJaminanReff(tipeJaminanId, tipeJaminanName),
            statusTataRekening);
}
