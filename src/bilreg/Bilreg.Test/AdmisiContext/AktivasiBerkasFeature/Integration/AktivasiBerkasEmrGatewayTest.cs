using System.Text.Json;
using Bilreg.Api.Configurations;
using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using Bilreg.Infrastructure.AdmisiContext.AktivasiBerkasFeature.Integration;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AktivasiBerkasFeature.Integration;

public class AktivasiBerkasEmrGatewayTest : IDisposable
{
    private readonly WireMockServer _server;
    private readonly AktivasiBerkasEmrGateway _sut;

    private const string ListDataRoute = "/api/AktifasiBerkas/ListData";
    private const string GetDataRoute = "/api/AktifasiBerkas/GetData";
    private const string SaveRoute = "/api/AktifasiBerkas/Save";
    private const string UpdateRoute = "/api/AktifasiBerkas/Update";
    private const string FindRegisterRoute = "/api/Reg/Find";
    private const string ListReaktifRoute = "/api/AktifasiBerkas/ListReaktif";
    private const string DeactivateRoute = "/api/AktifasiBerkas/Deactivate";
    private const string CleanUpRoute = "/api/AktifasiBerkas/CleanUp";

    public AktivasiBerkasEmrGatewayTest()
    {
        _server = WireMockServer.Start();

        var emr20Options = Options.Create(new Emr20Options
        {
            BaseApiUrl = _server.Urls[0]
        });

        var restClientFactory = new RestClientFactory();
        _sut = new AktivasiBerkasEmrGateway(emr20Options, restClientFactory);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    [Fact]
    public async Task ListData_WhenSuccessful_ReturnsMappedItems()
    {
        // Arrange
        var jsonResponse = """
        [
            {
                "AktifasiBerkasID": "AB-2611-0001",
                "TglJam": "2026-11-09T08:00:00",
                "AktifasiUserrID": "USR-AKT-01",
                "AktifasiUserrName": "Aktivator User",
                "TglAktif": "2026-11-09T00:00:00",
                "ReqUserrID": "USR-REQ-01",
                "ReqUserrName": "Requester User",
                "LayananID": "LYN-001",
                "LayananName": "Poli Penyakit Dalam",
                "ListBerkas": [
                    {
                        "AktifasiBerkasID": "AB-2611-0001",
                        "RegId": "REG-2026-0001",
                        "PasienID": "PAS-001",
                        "PasienName": "Pasien Satu",
                        "DokterID": "DOC-001",
                        "DokterName": "dr. Specialist",
                        "TipeJaminan": "BPJS",
                        "TglJamMasuk": "2026-11-08T09:00:00",
                        "TglDeaktif": "1900-01-01T00:00:00",
                        "DeactifUserrID": ""
                    }
                ]
            }
        ]
        """;

        _server
            .Given(Request.Create()
                .WithPath(ListDataRoute)
                .WithParam("tglYMD1", "2026-11-01")
                .WithParam("tglYMD2", "2026-11-09")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonResponse));

        // Act
        var result = (await _sut.ListData("2026-11-01", "2026-11-09")).ToList();

        // Assert
        result.Should().HaveCount(1);
        var item = result[0];
        item.AktifasiBerkasId.Should().Be("AB-2611-0001");
        item.ReqUserrId.Should().Be("USR-REQ-01");
        item.ReqUserrName.Should().Be("Requester User");
        item.AktifasiUserrId.Should().Be("USR-AKT-01");
        item.AktifasiUserrName.Should().Be("Aktivator User");
        item.LayananId.Should().Be("LYN-001");
        item.LayananName.Should().Be("Poli Penyakit Dalam");
        item.ListBerkas.Should().HaveCount(1);

        var berkas = item.ListBerkas.First();
        berkas.RegId.Should().Be("REG-2026-0001");
        berkas.PasienId.Should().Be("PAS-001");
        berkas.PasienName.Should().Be("Pasien Satu");
    }

    [Fact]
    public async Task ListData_WhenNotFoundOrEmpty_ReturnsEmptyList()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(ListDataRoute)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404));

        // Act
        var result = await _sut.ListData("2026-11-01", "2026-11-09");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ListData_WhenBaseApiUrlInvalid_ReturnsEmptyWithoutHttpCall(string? baseApiUrl)
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        var sut = new AktivasiBerkasEmrGateway(
            Options.Create(new Emr20Options { BaseApiUrl = baseApiUrl! }),
            mockFactory.Object);

        // Act
        var result = await sut.ListData("2026-11-01", "2026-11-09");

        // Assert
        result.Should().BeEmpty();
        mockFactory.Verify(f => f.Create(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetData_WhenFound_ReturnsDetailDto()
    {
        // Arrange
        var jsonResponse = """
        {
            "AktifasiBerkasID": "AB-2611-0002",
            "TglJam": "2026-11-09T10:00:00",
            "AktifasiUserrID": "USR-AKT-02",
            "AktifasiUserrName": "Aktivator Dua",
            "TglAktif": "2026-11-09T00:00:00",
            "ReqUserrID": "USR-REQ-02",
            "ReqUserrName": "Requester Dua",
            "LayananID": "LYN-002",
            "LayananName": "Poli Bedah",
            "ListBerkas": [
                {
                    "AktifasiBerkasID": "AB-2611-0002",
                    "RegId": "REG-2026-0002",
                    "PasienID": "PAS-002",
                    "PasienName": "Pasien Dua",
                    "DokterID": "DOC-002",
                    "DokterName": "dr. Bedah",
                    "TipeJaminan": "UMUM",
                    "TglJamMasuk": "2026-11-07T11:00:00",
                    "TglDeaktif": "1900-01-01T00:00:00",
                    "DeactifUserrID": ""
                }
            ]
        }
        """;

        _server
            .Given(Request.Create()
                .WithPath($"{GetDataRoute}/AB-2611-0002")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonResponse));

        // Act
        var result = await _sut.GetData("AB-2611-0002");

        // Assert
        result.Should().NotBeNull();
        result!.AktifasiBerkasId.Should().Be("AB-2611-0002");
        result.ReqUserrId.Should().Be("USR-REQ-02");
        result.ReqUserrName.Should().Be("Requester Dua");
        result.LayananId.Should().Be("LYN-002");
        result.LayananName.Should().Be("Poli Bedah");
        result.ListBerkas.Should().HaveCount(1);
        result.ListBerkas.First().RegId.Should().Be("REG-2026-0002");
    }

    [Fact]
    public async Task GetData_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath($"{GetDataRoute}/NON-EXISTENT")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404));

        // Act
        var result = await _sut.GetData("NON-EXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetData_WhenIdInvalid_ReturnsNullWithoutHttpCall(string? id)
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        var sut = new AktivasiBerkasEmrGateway(
            Options.Create(new Emr20Options { BaseApiUrl = "http://example.com" }),
            mockFactory.Object);

        // Act
        var result = await sut.GetData(id!);

        // Assert
        result.Should().BeNull();
        mockFactory.Verify(f => f.Create(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Save_WhenSuccessful_ReturnsSuccessMutationResultWithId()
    {
        // Arrange
        var jsonResponse = """
        {
            "AktifasiBerkasID": "AB-2611-0099",
            "TglJam": "2026-11-09T10:30:00",
            "LayananID": "LYN-001"
        }
        """;

        _server
            .Given(Request.Create()
                .WithPath(SaveRoute)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonResponse));

        var saveDto = new AktivasiBerkasSaveDto
        {
            TglJam = "2026-11-09 10:30:00",
            ReqUserrId = "USR-REQ-01",
            TglAktif = "2026-11-09",
            AktifasiUserrId = "USR-AKT-01",
            LayananId = "LYN-001",
            ListRegister = [new AktivasiBerkasRegisterRefDto("REG-001")]
        };

        // Act
        var result = await _sut.Save(saveDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AktifasiBerkasId.Should().Be("AB-2611-0099");
        result.Message.Should().Contain("berhasil");

        var logEntry = _server.LogEntries.Last();
        logEntry.RequestMessage.Body.Should().Contain("\"ReqUserrId\":\"USR-REQ-01\"");
        logEntry.RequestMessage.Body.Should().Contain("\"RegId\":\"REG-001\"");
    }

    [Fact]
    public async Task Save_WhenValidationFails_ReturnsFailureWithoutHttpCall()
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        var sut = new AktivasiBerkasEmrGateway(
            Options.Create(new Emr20Options { BaseApiUrl = "http://example.com" }),
            mockFactory.Object);

        // Act - Null DTO
        var resultNull = await sut.Save(null!);

        // Act - Empty ListRegister
        var emptyRegisterDto = new AktivasiBerkasSaveDto
        {
            ReqUserrId = "USR-01",
            ListRegister = []
        };
        var resultEmpty = await sut.Save(emptyRegisterDto);

        // Assert
        resultNull.Success.Should().BeFalse();
        resultNull.Message.Should().Contain("kosong");
        resultEmpty.Success.Should().BeFalse();
        resultEmpty.Message.Should().Contain("kosong");
        mockFactory.Verify(f => f.Create(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Save_WhenHttpReturnsNonSuccess_ReturnsFailureWithDetails()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(SaveRoute)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Database transaction aborted"));

        var saveDto = new AktivasiBerkasSaveDto
        {
            ReqUserrId = "USR-01",
            ListRegister = [new AktivasiBerkasRegisterRefDto("REG-001")]
        };

        // Act
        var result = await _sut.Save(saveDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("500");
        result.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Save_WhenHttpReturnsBadRequestWithMessageJson_ExtractsEmrErrorMessage()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(SaveRoute)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{ "Message": "Userr invalid" }"""));

        var saveDto = new AktivasiBerkasSaveDto
        {
            ReqUserrId = "DR00139",
            AktifasiUserrId = "SPR001",
            LayananId = "2RJ03",
            ListRegister = [new AktivasiBerkasRegisterRefDto("RG01123803")]
        };

        // Act
        var result = await _sut.Save(saveDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Userr invalid");
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Update_WhenHttpReturnsBadRequestWithMessageJson_ExtractsEmrErrorMessage()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(UpdateRoute)
                .WithParam("aktifasiID", "AB-01")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{ "Message": "Userr invalid" }"""));

        var updateDto = new AktivasiBerkasUpdateDto
        {
            ReqUserrId = "DR00139",
            AktifasiUserrId = "SPR001",
            LayananId = "2RJ03",
            ListRegister = [new AktivasiBerkasRegisterRefDto("RG01123803")]
        };

        // Act
        var result = await _sut.Update("AB-01", updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Userr invalid");
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsSuccessMutationResultWithId()
    {
        // Arrange
        var jsonResponse = """
        {
            "AktifasiBerkasID": "AB-2611-0050",
            "TglJam": "2026-11-09T11:00:00"
        }
        """;

        _server
            .Given(Request.Create()
                .WithPath(UpdateRoute)
                .WithParam("aktifasiID", "AB-2611-0050")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonResponse));

        var updateDto = new AktivasiBerkasUpdateDto
        {
            TglJam = "2026-11-09 11:00:00",
            ReqUserrId = "USR-REQ-02",
            TglAktif = "2026-11-09",
            AktifasiUserrId = "USR-AKT-02",
            LayananId = "LYN-002",
            ListRegister = [new AktivasiBerkasRegisterRefDto("REG-002")]
        };

        // Act
        var result = await _sut.Update("AB-2611-0050", updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AktifasiBerkasId.Should().Be("AB-2611-0050");
        result.Message.Should().Contain("berhasil");
    }

    [Fact]
    public async Task Update_WhenIdOrPayloadInvalid_ReturnsFailureWithoutHttpCall()
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        var sut = new AktivasiBerkasEmrGateway(
            Options.Create(new Emr20Options { BaseApiUrl = "http://example.com" }),
            mockFactory.Object);

        // Act
        var resultEmptyId = await sut.Update("", new AktivasiBerkasUpdateDto());
        var resultNullDto = await sut.Update("AB-01", null!);

        // Assert
        resultEmptyId.Success.Should().BeFalse();
        resultEmptyId.Message.Should().Contain("tidak valid");
        resultNullDto.Success.Should().BeFalse();
        resultNullDto.Message.Should().Contain("kosong");
        mockFactory.Verify(f => f.Create(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task FindRegister_WhenSuccessful_ReturnsMappedRegisterItems()
    {
        // Arrange
        var jsonResponse = """
        [
            {
                "RegID": "REG-2026-1234",
                "BookingID": "BKG-1234",
                "TglReg": "2026-11-05",
                "JamReg": "08:15",
                "KodeMR": "MR-99001",
                "NamaPasien": "Pasien Temuan",
                "KodeTipeJaminan": "TJ-01",
                "NamaTipeJaminan": "Umum",
                "TglVoid": "3000-01-01",
                "JamVoid": "00:00",
                "LayananID": "LYN-001",
                "LayananName": "Poli Umum",
                "KodeJenisReg": 1,
                "KodeMedis": "DOC-001",
                "NamaMedis": "dr. Fulan"
            },
            {
                "RegID": "REG-2026-5678",
                "BookingID": "BKG-5678",
                "TglReg": "2026-11-04",
                "JamReg": "14:20",
                "KodeMR": "MR-99002",
                "NamaPasien": "Pasien Void",
                "KodeTipeJaminan": "TJ-02",
                "NamaTipeJaminan": "BPJS",
                "TglVoid": "2026-11-04",
                "JamVoid": "15:00",
                "LayananID": "LYN-001",
                "LayananName": "Poli Umum",
                "KodeJenisReg": 1,
                "KodeMedis": "DOC-001",
                "NamaMedis": "dr. Fulan"
            }
        ]
        """;

        _server
            .Given(Request.Create()
                .WithPath(FindRegisterRoute)
                .WithParam("keyword", "MR-99001")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonResponse));

        // Act
        var result = (await _sut.FindRegister("MR-99001")).ToList();

        // Assert
        result.Should().HaveCount(2);

        var activeItem = result[0];
        activeItem.RegId.Should().Be("REG-2026-1234");
        activeItem.KodeMr.Should().Be("MR-99001");
        activeItem.NamaPasien.Should().Be("Pasien Temuan");
        activeItem.TglVoid.Should().Be("3000-01-01");
        activeItem.IsVoid.Should().BeFalse();

        var voidItem = result[1];
        voidItem.RegId.Should().Be("REG-2026-5678");
        voidItem.TglVoid.Should().Be("2026-11-04");
        voidItem.IsVoid.Should().BeTrue();
    }

    [Fact]
    public async Task FindRegister_WhenEmr20RealPayload_MapsAllColumnsCorrectly()
    {
        // Arrange - Real payload structure returned by emr20-api/api/Reg/Find
        var jsonResponse = """
        [
            {
                "RegID": "RG01289030",
                "TglMasuk": "2024-01-01",
                "JamMasuk": "11:23:18",
                "Umur": "0 thn 0 bln",
                "PasienID": "337502200257632",
                "PasienName": "NUR HASANAH, NY",
                "TipeJaminanID": "RSI00",
                "TipeJaminanName": "RSI PEKAJANGAN",
                "BookingID": "",
                "TglVoid": "3000-01-01",
                "JamVoid": "00:00:00",
                "UserrIDVoid": "",
                "TglKeluar": "2024-01-01",
                "JamKeluar": "11:31:15",
                "RegIDIbu": "",
                "JenisReg": "0",
                "LayananAkhirID": "4RD01",
                "LayananAkhirName": "RADIOLOGI",
                "NoSep": "",
                "ListLayanan": [
                    {
                        "LayananID": "4RD01",
                        "LayananName": "RADIOLOGI",
                        "KamarID": "-",
                        "KamarName": "-",
                        "DokterName": "HP WINANTA, DR., SP.RAD.",
                        "DokterID": "10541",
                        "SesionPoliID": "1",
                        "AntrianX1ID": ""
                    }
                ]
            },
            {
                "RegID": "RG01240539",
                "TglMasuk": "2023-10-04",
                "JamMasuk": "13:00:00",
                "Umur": "0 thn 0 bln",
                "PasienID": "337502200253056",
                "PasienName": "NUR HASANAH, NY",
                "TipeJaminanID": "PBD00",
                "TipeJaminanName": "JKN PBI APBD",
                "BookingID": "",
                "TglVoid": "2023-10-11",
                "JamVoid": "18:30:23",
                "UserrIDVoid": "ROHMATUL",
                "TglKeluar": "3000-01-01",
                "JamKeluar": "00:00:00",
                "RegIDIbu": "",
                "JenisReg": "0",
                "LayananAkhirID": "2RJ06",
                "LayananAkhirName": "POLI OBSGYN",
                "NoSep": "",
                "ListLayanan": [
                    {
                        "LayananID": "2RJ06",
                        "LayananName": "POLI OBSGYN",
                        "KamarID": "-",
                        "KamarName": "-",
                        "DokterName": "LAFRAN PANCAPUTRANTO, dr.,Sp.OG(K).",
                        "DokterID": "DR0018",
                        "SesionPoliID": "1",
                        "AntrianX1ID": "AN0900268A"
                    }
                ]
            },
            {
                "RegID": "RG01234778",
                "TglMasuk": "2023-09-23",
                "JamMasuk": "08:15:01",
                "Umur": "0 thn 0 bln",
                "PasienID": "337502200253056",
                "PasienName": "NUR HASANAH, NY",
                "TipeJaminanID": "PBD00",
                "TipeJaminanName": "JKN PBI APBD",
                "BookingID": "",
                "TglVoid": "3000-01-01",
                "JamVoid": "00:00:00",
                "UserrIDVoid": "",
                "TglKeluar": "2023-09-28",
                "JamKeluar": "17:26:25",
                "RegIDIbu": "",
                "JenisReg": "1",
                "LayananAkhirID": "3RI02",
                "LayananAkhirName": "ST.ANNA",
                "NoSep": "",
                "ListLayanan": [
                    {
                        "LayananID": "3RI06",
                        "LayananName": "ST.YOSEP",
                        "KamarID": "-",
                        "KamarName": "-",
                        "DokterName": "LAFRAN PANCAPUTRANTO, dr.,Sp.OG(K).",
                        "DokterID": "DR0018",
                        "SesionPoliID": "",
                        "AntrianX1ID": ""
                    }
                ]
            }
        ]
        """;

        _server
            .Given(Request.Create()
                .WithPath(FindRegisterRoute)
                .WithParam("keyword", "nur hasanah")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonResponse));

        // Act
        var result = (await _sut.FindRegister("nur hasanah")).ToList();

        // Assert
        result.Should().HaveCount(3);

        var first = result[0];
        first.RegId.Should().Be("RG01289030");
        first.KodeMr.Should().Be("337502200257632");
        first.NamaPasien.Should().Be("NUR HASANAH, NY");
        first.TglReg.Should().Be("2024-01-01");
        first.JamReg.Should().Be("11:23:18");
        first.KodeTipeJaminan.Should().Be("RSI00");
        first.NamaTipeJaminan.Should().Be("RSI PEKAJANGAN");
        first.LayananName.Should().Be("RADIOLOGI");
        first.LayananAkhirName.Should().Be("RADIOLOGI");
        first.KodeMedis.Should().Be("10541");
        first.NamaMedis.Should().Be("HP WINANTA, DR., SP.RAD.");
        first.KodeJenisReg.Should().Be(0);
        first.IsVoid.Should().BeFalse();

        var second = result[1];
        second.RegId.Should().Be("RG01240539");
        second.TglVoid.Should().Be("2023-10-11");
        second.IsVoid.Should().BeTrue();
        second.UserrIdVoid.Should().Be("ROHMATUL");

        var third = result[2];
        third.RegId.Should().Be("RG01234778");
        third.LayananName.Should().Be("ST.YOSEP");
        third.LayananAkhirId.Should().Be("3RI02");
        third.LayananAkhirName.Should().Be("ST.ANNA");
        third.LayananList.Should().Be("ST.ANNA");
        third.NamaMedis.Should().Be("LAFRAN PANCAPUTRANTO, dr.,Sp.OG(K).");
        third.KodeJenisReg.Should().Be(1);
        third.ListLayanan.Should().HaveCount(1);
        third.ListLayanan.First().LayananName.Should().Be("ST.YOSEP");
    }

    [Fact]
    public async Task FindRegister_WhenDataNotFoundReturned_ReturnsEmptyList()
    {
        // Arrange - Legacy RegBL throws ArgumentException("Data not found") which becomes 400 BadRequest
        _server
            .Given(Request.Create()
                .WithPath(FindRegisterRoute)
                .WithParam("keyword", "NOTFOUND")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithBody("Data not found"));

        // Act
        var result = await _sut.FindRegister("NOTFOUND");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task FindRegister_WhenKeywordInvalid_ReturnsEmptyWithoutHttpCall(string? keyword)
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        var sut = new AktivasiBerkasEmrGateway(
            Options.Create(new Emr20Options { BaseApiUrl = "http://example.com" }),
            mockFactory.Object);

        // Act
        var result = await sut.FindRegister(keyword!);

        // Assert
        result.Should().BeEmpty();
        mockFactory.Verify(f => f.Create(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ListReaktif_WhenSuccessfulWithJSendEnvelope_ReturnsMappedGroupedItems()
    {
        // Arrange
        var jsonResponse = """
        {
            "status": "success",
            "code": "200",
            "data": [
                {
                    "LayananID": "LYN-001",
                    "LayananName": "Poli Penyakit Dalam",
                    "ListBerkas": [
                        {
                            "AktifasiBerkasID": "AB-2611-0001",
                            "RegId": "REG-2026-0001",
                            "PasienID": "PAS-001",
                            "PasienName": "Pasien Satu",
                            "DokterID": "DOC-001",
                            "DokterName": "dr. Specialist",
                            "TipeJaminan": "BPJS",
                            "TglJamMasuk": "2026-11-08T09:00:00",
                            "TglDeaktif": "1900-01-01T00:00:00",
                            "DeactifUserrID": ""
                        }
                    ]
                }
            ]
        }
        """;

        _server
            .Given(Request.Create()
                .WithPath(ListReaktifRoute)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonResponse));

        // Act
        var result = (await _sut.ListReaktif(null)).ToList();

        // Assert
        result.Should().HaveCount(1);
        var group = result[0];
        group.LayananId.Should().Be("LYN-001");
        group.LayananName.Should().Be("Poli Penyakit Dalam");
        group.ListBerkas.Should().HaveCount(1);

        var berkas = group.ListBerkas.First();
        berkas.AktifasiBerkasId.Should().Be("AB-2611-0001");
        berkas.RegId.Should().Be("REG-2026-0001");
        berkas.PasienId.Should().Be("PAS-001");
        berkas.PasienName.Should().Be("Pasien Satu");
        berkas.DokterId.Should().Be("DOC-001");
        berkas.DokterName.Should().Be("dr. Specialist");
        berkas.TipeJaminan.Should().Be("BPJS");
        berkas.TglJamMasuk.Should().Be("2026-11-08T09:00:00");
    }

    [Fact]
    public async Task ListReaktif_WithLayananIdQuery_SendsLayananIdParameter()
    {
        // Arrange
        var jsonResponse = """
        {
            "status": "success",
            "code": "200",
            "data": [
                {
                    "LayananID": "1GD01",
                    "LayananName": "IGD",
                    "ListBerkas": []
                }
            ]
        }
        """;

        _server
            .Given(Request.Create()
                .WithPath(ListReaktifRoute)
                .WithParam("layananID", "1GD01")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonResponse));

        // Act
        var result = (await _sut.ListReaktif("1GD01")).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].LayananId.Should().Be("1GD01");
        result[0].LayananName.Should().Be("IGD");

        var logEntry = _server.LogEntries.Last();
        logEntry.RequestMessage.Query.Should().ContainKey("layananID");
        logEntry.RequestMessage.Query!["layananID"].First().Should().Be("1GD01");
    }

    [Fact]
    public async Task ListReaktif_WhenDirectArray_ReturnsMappedGroupedItems()
    {
        // Arrange - Fallback tolerance if legacy returns plain array
        var jsonResponse = """
        [
            {
                "LayananID": "LYN-002",
                "LayananName": "Poli Bedah",
                "ListBerkas": [
                    {
                        "AktifasiBerkasID": "AB-2611-0002",
                        "RegId": "REG-2026-0002",
                        "PasienID": "PAS-002",
                        "PasienName": "Pasien Dua"
                    }
                ]
            }
        ]
        """;

        _server
            .Given(Request.Create()
                .WithPath(ListReaktifRoute)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonResponse));

        // Act
        var result = (await _sut.ListReaktif(string.Empty)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].LayananId.Should().Be("LYN-002");
        result[0].LayananName.Should().Be("Poli Bedah");
        result[0].ListBerkas.Should().HaveCount(1);
        result[0].ListBerkas.First().RegId.Should().Be("REG-2026-0002");
    }

    [Fact]
    public async Task ListReaktif_WhenNotFoundOrEmpty_ReturnsEmptyList()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(ListReaktifRoute)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404));

        // Act
        var result = await _sut.ListReaktif(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ListReaktif_WhenBaseApiUrlInvalid_ReturnsEmptyWithoutHttpCall(string? baseApiUrl)
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        var sut = new AktivasiBerkasEmrGateway(
            Options.Create(new Emr20Options { BaseApiUrl = baseApiUrl! }),
            mockFactory.Object);

        // Act
        var result = await sut.ListReaktif("LYN-001");

        // Assert
        result.Should().BeEmpty();
        mockFactory.Verify(f => f.Create(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Deactivate_WhenSuccessful_ReturnsSuccessMutationResult()
    {
        // Arrange
        var jsonResponse = """
        {
            "status": "success",
            "code": "200",
            "data": "Register telah di Deaktivasi"
        }
        """;

        _server
            .Given(Request.Create()
                .WithPath(DeactivateRoute)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonResponse));

        // Act
        var result = await _sut.Deactivate("REG-2026-0001", "USR-001");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Register telah di Deaktivasi");

        var logEntry = _server.LogEntries.Last();
        logEntry.RequestMessage.Body.Should().Contain("\"RegID\":\"REG-2026-0001\"");
        logEntry.RequestMessage.Body.Should().Contain("\"UserrID\":\"USR-001\"");
    }

    [Fact]
    public async Task Deactivate_WhenEmptyResponseBody_UsesDefaultStandardMessage()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(DeactivateRoute)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(""));

        // Act
        var result = await _sut.Deactivate("REG-2026-0001", "USR-001");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Register telah di Deaktivasi");
    }

    [Fact]
    public async Task CleanUp_WhenSuccessful_ReturnsSuccessMutationResult()
    {
        // Arrange
        var jsonResponse = """
        {
            "status": "success",
            "code": "200",
            "data": "Seluruh register aktif berhasil dibersihkan"
        }
        """;

        _server
            .Given(Request.Create()
                .WithPath(CleanUpRoute)
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonResponse));

        // Act
        var result = await _sut.CleanUp();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Seluruh register aktif berhasil dibersihkan");

        var logEntry = _server.LogEntries.Last();
        logEntry.RequestMessage.Method.Should().Be("DELETE");
        logEntry.RequestMessage.Path.Should().Be(CleanUpRoute);
    }

    [Fact]
    public async Task CleanUp_WhenSuccessfulWithEmptyBody_ReturnsDefaultSuccessMessage()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(CleanUpRoute)
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(""));

        // Act
        var result = await _sut.CleanUp();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Seluruh register aktif berhasil dibersihkan");
    }

    [Fact]
    public async Task CleanUp_WhenEmrReturns500_ReturnsFailedMutationResult()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath(CleanUpRoute)
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Gagal membersihkan database"));

        // Act
        var result = await _sut.CleanUp();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("500");
        result.StatusCode.Should().Be(500);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CleanUp_WhenBaseApiUrlInvalid_ReturnsFailureWithoutHttpCall(string? baseApiUrl)
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        var sut = new AktivasiBerkasEmrGateway(
            Options.Create(new Emr20Options { BaseApiUrl = baseApiUrl! }),
            mockFactory.Object);

        // Act
        var result = await sut.CleanUp();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("BaseApiUrl");
        mockFactory.Verify(f => f.Create(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Deactivate_WhenEmrFails_ReturnsFailedMutationResult()
    {
        // Arrange
        var jsonResponse = """
        {
            "status": "fail",
            "code": "400",
            "data": "Register sudah dinonaktifkan sebelumnya"
        }
        """;

        _server
            .Given(Request.Create()
                .WithPath(DeactivateRoute)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonResponse));

        // Act
        var result = await _sut.Deactivate("REG-2026-0001", "USR-001");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Register sudah dinonaktifkan sebelumnya");
        result.StatusCode.Should().Be(400);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Deactivate_WhenRegIdInvalid_ReturnsFailureWithoutHttpCall(string? regId)
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        var sut = new AktivasiBerkasEmrGateway(
            Options.Create(new Emr20Options { BaseApiUrl = "http://example.com" }),
            mockFactory.Object);

        // Act
        var result = await sut.Deactivate(regId!, "USR-001");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("RegID tidak valid");
        mockFactory.Verify(f => f.Create(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Deactivate_WhenBaseApiUrlInvalid_ReturnsFailureWithoutHttpCall(string? baseApiUrl)
    {
        // Arrange
        var mockFactory = new Mock<IRestClientFactory>();
        var sut = new AktivasiBerkasEmrGateway(
            Options.Create(new Emr20Options { BaseApiUrl = baseApiUrl! }),
            mockFactory.Object);

        // Act
        var result = await sut.Deactivate("REG-001", "USR-001");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("BaseApiUrl");
        mockFactory.Verify(f => f.Create(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void AddInfrastructure_RegistersIAktivasiBerkasEmrGateway()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Emr20:BaseApiUrl"] = "http://dev.smart-ics.com:8083/emr20-api",
                ["Database:ConnectionString"] = "Server=localhost;Database=test;Trusted_Connection=True;",
                ["BusinessDate:Status"] = "Open"
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAktivasiBerkasEmrGateway));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(AktivasiBerkasEmrGateway));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }
}
