using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

[CollectionDefinition("Step4CDb", DisableParallelization = true)]
public class Step4CDbCollection : ICollectionFixture<object>;

public sealed class Step4CRealDbWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? _configureTestServices;

    public Step4CRealDbWebApplicationFactory(Action<IServiceCollection>? configureTestServices = null)
        => _configureTestServices = configureTestServices;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ServerName"] = "dev.smart-ics.com",
                ["Database:DbName"] = "HOSPITAL_HPL",
                ["AdmisiRanap:Enabled"] = "true",
                ["Serilog:WriteTo:0:Name"] = "Console",
                ["Serilog:WriteTo:0:Args:serverUrl"] = "",
            });
        });
        builder.ConfigureTestServices(services => _configureTestServices?.Invoke(services));
    }
}

public sealed class FailingRegInapRepo : IRegInapRepo
{
    public void SaveChanges(RegInapModel model)
        => throw new InvalidOperationException("STEP4C_FORCED_REGINAP_FAILURE");

    public MayBe<RegInapModel> LoadEntity(IRegKey key) => MayBe<RegInapModel>.None;

    public void DeleteEntity(IRegKey key)
    {
    }
}

[Collection("Step4CDb")]
public class AdmissionRegistrationStep4CDbTest
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private const string ConnStr =
        "Server=dev.smart-ics.com;Database=HOSPITAL_HPL;User Id=bilregLogin;Password=bilreg123!;";

    private static string CreateJwt()
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("ErF7Zq0praAgc4pV7ajVG4h2rAmP99bvgLMyCVWGkq0="));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "BilregApiServer",
            audience: "BilregApiClient",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, "BilregApiAccessToken"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, "step4c@verify.local"),
                new Claim(ClaimTypes.Name, "Step4CVerifier"),
            ],
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static HttpClient CreateClient(Step4CRealDbWebApplicationFactory factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwt());
        return client;
    }

    private static async Task<T> ReadJSendDataAsync<T>(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(raw);
        response.IsSuccessStatusCode.Should().BeTrue($"HTTP {(int)response.StatusCode}: {raw}");
        doc.RootElement.GetProperty("status").GetString().Should().Be("success");
        return doc.RootElement.GetProperty("data").Deserialize<T>(JsonOpts)!;
    }

    private sealed record ProcessResponse(string RegId, int AdmissionStatus);
    private sealed record CreateOpnameResponse(string OpnameRequestId);
    private sealed record CreateReservationResponse(string ReservationId);
    private sealed record CreatePasienResponse(string PasienId);

    private static async Task<int> ScalarCountAsync(string sql, object param)
    {
        await using var conn = new SqlConnection(ConnStr);
        return await conn.ExecuteScalarAsync<int>(sql, param);
    }

    private static async Task CleanupRegAsync(string? regId, string? opnameId = null, string? reservationId = null, string? requestId = null)
    {
        await using var conn = new SqlConnection(ConnStr);
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        if (!string.IsNullOrWhiteSpace(regId))
        {
            await conn.ExecuteAsync("DELETE FROM ta_registrasi2 WHERE fs_kd_reg=@regId", new { regId }, tx);
            await conn.ExecuteAsync("DELETE FROM ta_reg_history_dokter WHERE fs_kd_reg=@regId", new { regId }, tx);
            await conn.ExecuteAsync("DELETE FROM ta_reg_inap WHERE fs_kd_reg=@regId", new { regId }, tx);
            await conn.ExecuteAsync("DELETE FROM ta_reg_jaminan WHERE fs_kd_reg=@regId", new { regId }, tx);
            await conn.ExecuteAsync("DELETE FROM BILRG_RegAktif WHERE RegId=@regId", new { regId }, tx);
            await conn.ExecuteAsync("DELETE FROM BILRG_BedWaitingList WHERE RegId=@regId", new { regId }, tx);
            await conn.ExecuteAsync("DELETE FROM BILRG_AdmAdmission WHERE RegId=@regId", new { regId }, tx);
            await conn.ExecuteAsync("DELETE FROM ta_registrasi WHERE fs_kd_reg=@regId", new { regId }, tx);
            await conn.ExecuteAsync("DELETE FROM BILRG_AuditLog WHERE EntityId=@regId", new { regId }, tx);
        }

        if (!string.IsNullOrWhiteSpace(opnameId))
        {
            await conn.ExecuteAsync("DELETE FROM BILRG_AuditLog WHERE EntityId=@opnameId", new { opnameId }, tx);
            await conn.ExecuteAsync("DELETE FROM BILRG_AdmOpnameRequest WHERE OpnameRequestId=@opnameId", new { opnameId }, tx);
        }

        if (!string.IsNullOrWhiteSpace(reservationId))
        {
            await conn.ExecuteAsync("DELETE FROM BILRG_AuditLog WHERE EntityId=@reservationId", new { reservationId }, tx);
            await conn.ExecuteAsync("DELETE FROM BILRG_AdmReservation WHERE ReservationId=@reservationId", new { reservationId }, tx);
        }

        if (!string.IsNullOrWhiteSpace(requestId))
            await conn.ExecuteAsync("IF OBJECT_ID('BILRG_AdmCoordinatedCancellationRequest', 'U') IS NOT NULL DELETE FROM BILRG_AdmCoordinatedCancellationRequest WHERE RequestId=@requestId", new { requestId }, tx);

        tx.Commit();
    }

    private static async Task<string> CreateOwnedPatientAsync(HttpClient client)
    {
        var nonce = Guid.NewGuid().ToString("N");
        var nik = DateTime.UtcNow.ToString("yyMMddHHmmss") + Random.Shared.Next(1000, 9999);
        var response = await client.PostAsJsonAsync("api/pasien", new
        {
            pasienName = "Cancellation SQL " + nonce[..8],
            tempatLahir = "Jakarta", tglLahir = "1990-01-01", gender = "L",
            nickName = "SQL", ibuKandung = "Fixture", golDarah = "O",
            alamat1 = "Fixture", alamat2 = "", alamat3 = "", kota = "Jakarta", kodePos = "10110",
            noTelp = "0812" + Random.Shared.Next(10000000, 99999999), noKtp = nik,
        });
        return (await ReadJSendDataAsync<CreatePasienResponse>(response)).PasienId;
    }

    private static async Task CleanupOwnedPatientAsync(string? pasienId)
    {
        if (string.IsNullOrWhiteSpace(pasienId)) return;
        await using var conn = new SqlConnection(ConnStr);
        await conn.ExecuteAsync("DELETE FROM tc_mr_id WHERE fs_mr=@pasienId; DELETE FROM tc_mr_ktp WHERE fs_kd_mr=@pasienId; DELETE FROM tc_mr WHERE fs_mr=@pasienId", new { pasienId });
    }

    private static object RegistrationBody(string dokterId = "DR00000015") => new
    {
        tipeJaminanId = "00000",
        caraMasukDkId = "8",
        prosedurMasukInapId = "IGD",
        rujukanId = "00084",
        dokterId,
        pesertaJaminanId = "",
    };

    private sealed record OwnedOpnameFixture(string PasienId, string OpnameId, string RegId);

    private static async Task<OwnedOpnameFixture> CreateOwnedOpnameRegistrationAsync(HttpClient client)
    {
        var pasienId = await CreateOwnedPatientAsync(client);
        try
        {
            var create = await client.PostAsJsonAsync("api/admisi-ranap/opname-request", new
            {
                pasienId, dokterId = "DR00000015", plannedDate = "2026-07-24",
                clinicalNotes = "owned coordinated cancellation fixture", userId = "sqlverify",
            });
            var opnameId = (await ReadJSendDataAsync<CreateOpnameResponse>(create)).OpnameRequestId;
            var process = await client.PostAsJsonAsync("api/admisi-ranap/admission/from-opname-request", new
            {
                opnameRequestId = opnameId, kelasDkId = "3", bangsalId = "R1", userId = "sqlverify",
                registration = RegistrationBody(),
            });
            return new OwnedOpnameFixture(pasienId, opnameId,
                (await ReadJSendDataAsync<ProcessResponse>(process)).RegId);
        }
        catch
        {
            await CleanupOwnedPatientAsync(pasienId);
            throw;
        }
    }

    private static async Task AddWaitingListAsync(string regId, string pasienId, int status)
    {
        var id = "WL" + Guid.NewGuid().ToString("N")[..10];
        await using var conn = new SqlConnection(ConnStr);
        await conn.ExecuteAsync("""
            INSERT INTO BILRG_BedWaitingList
                (WaitingListId,WaitingListStatus,RegId,PasienId,KelasId,KelasName,BangsalId,BangsalName,Priority,CrtUser,CrtDate,UpdUser,UpdDate,VodUser,VodDate)
            VALUES (@id,@status,@regId,@pasienId,'3','Kelas 3','R1','Bangsal 1',1,'sqlverify',GETUTCDATE(),'sqlverify',GETUTCDATE(),'','3000-01-01')
            """, new { id, status, regId, pasienId });
    }

    private static async Task CancelAsync(IServiceProvider services, string regId, string requestId)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new
            Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases.AdmCoordinatedCancelCmd(
                regId, "real SQL verification", "sqlverify",
                Bilreg.Domain.AdmisiRanapContext.AdmissionFeature.AdmissionStatusEnum.Admitted,
                null, requestId, null, null));
    }

    [Fact]
    public async Task Step4C_OpnamePath_PersistsSharedRegIdIncludingRegInapAndHistory()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);
        var pasienId = await CreateOwnedPatientAsync(client);
        var createOpname = await client.PostAsJsonAsync("api/admisi-ranap/opname-request", new
        {
            pasienId,
            dokterId = "DR00000015",
            plannedDate = "2026-07-20",
            clinicalNotes = "STEP4C opname verify",
            userId = "step4c",
        });
        var opname = await ReadJSendDataAsync<CreateOpnameResponse>(createOpname);
        string? regId = null;
        try
        {
            var process = await client.PostAsJsonAsync("api/admisi-ranap/admission/from-opname-request", new
            {
                opnameRequestId = opname.OpnameRequestId,
                kelasDkId = "3",
                bangsalId = "R1",
                userId = "step4c",
                registration = RegistrationBody(),
            });
            var result = await ReadJSendDataAsync<ProcessResponse>(process);
            regId = result.RegId;
            result.AdmissionStatus.Should().Be(0);

            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AdmAdmission WHERE RegId=@regId", new { regId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_registrasi WHERE fs_kd_reg=@regId AND fs_kd_jenis_reg='1'", new { regId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_reg_inap WHERE fs_kd_reg=@regId AND LTRIM(RTRIM(fs_kd_caramasuk_inap))='IGD'", new { regId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_reg_jaminan WHERE fs_kd_reg=@regId", new { regId })).Should().Be(1);
            (await ScalarCountAsync("""
                SELECT COUNT(*) FROM ta_reg_history_dokter
                WHERE fs_kd_reg=@regId AND LTRIM(RTRIM(fs_kd_dokter))='DR00000015' AND fb_primer=1
                """, new { regId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_RegAktif WHERE RegId=@regId", new { regId })).Should().Be(1);
            // Confirmed rule: Rawat Inap does not create ta_registrasi2 (RJ/IGD only).
            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_registrasi2 WHERE fs_kd_reg=@regId", new { regId })).Should().Be(0);
            (await ScalarCountAsync("""
                SELECT COUNT(*) FROM BILRG_AdmOpnameRequest
                WHERE OpnameRequestId=@opnameId AND OpnameRequestStatus=1 AND FulfilledRegId=@regId
                """, new { opnameId = opname.OpnameRequestId, regId })).Should().Be(1);
            (await ScalarCountAsync("""
                SELECT COUNT(*) FROM BILRG_AuditLog
                WHERE EntityId=@regId AND ActionType='CREATE' AND EntityName='AdmissionModel'
                """, new { regId })).Should().Be(1);
            (await ScalarCountAsync("""
                SELECT COUNT(*) FROM BILRG_AuditLog
                WHERE EntityId=@opnameId AND ActionType='UPDATE' AND EntityName='OpnameRequestModel'
                """, new { opnameId = opname.OpnameRequestId })).Should().BeGreaterThanOrEqualTo(1);

            await using var conn = new SqlConnection(ConnStr);
            var inap = await conn.QuerySingleAsync<(string prosedur, string booking, string sekunder)>(
                """
                SELECT LTRIM(RTRIM(fs_kd_caramasuk_inap)), fs_kd_trs_booking_bed, fs_kd_medis_sekunder
                FROM ta_reg_inap WHERE fs_kd_reg=@regId
                """, new { regId });
            inap.prosedur.Should().Be("IGD");
            inap.booking.Should().Be(" ");
            inap.sekunder.Should().Be(" ");

            var medis = await conn.ExecuteScalarAsync<string>(
                "SELECT LTRIM(RTRIM(fs_kd_medis)) FROM ta_registrasi WHERE fs_kd_reg=@regId", new { regId });
            medis.Should().Be("DR00000015");

            var dbOpts = Options.Create(new DatabaseOptions
            {
                ServerName = "dev.smart-ics.com",
                DbName = "HOSPITAL_HPL",
            });
            var repo = new RegInapRepo(
                new ta_reg_inap_dal(dbOpts),
                new RegHistoryDokterDal(dbOpts),
                new ProsedurMasukInapRepo(new ProsedurMasukInapDal(dbOpts)));
            var loaded = repo.LoadEntity(RegModel.Key(regId!)).Value;
            loaded.RegId.Should().Be(regId);
            loaded.ProsedurMasukInap.ProsedurMasukInapId.Should().Be("IGD");
            loaded.ListDokter.Should().ContainSingle(x =>
                x.IsActive
                && x.DokterRole == DokterRoleEnum.Dpjp
                && x.DpjpResponsibility == DpjpResponsibilityEnum.Primary
                && x.ReleaseDate == null);
            repo.SaveChanges(loaded);
            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_reg_inap WHERE fs_kd_reg=@regId", new { regId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_reg_history_dokter WHERE fs_kd_reg=@regId", new { regId })).Should().Be(1);
        }
        finally
        {
            await CleanupRegAsync(regId, opname.OpnameRequestId);
            await CleanupOwnedPatientAsync(pasienId);
        }
    }

    [Fact]
    public async Task Step4C_ReservationPath_PersistsSharedRegIdIncludingRegInap()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);
        var pasienId = await CreateOwnedPatientAsync(client);
        var createRes = await client.PostAsJsonAsync("api/admisi-ranap/reservation", new
        {
            pasienId,
            plannedDate = "2026-08-01",
            kelasId = "K04",
            bangsalId = "R1",
            userId = "step4c",
        });
        var reservation = await ReadJSendDataAsync<CreateReservationResponse>(createRes);

        var maintain = await client.PutAsJsonAsync($"api/admisi-ranap/reservation/{reservation.ReservationId}", new
        {
            plannedDate = "2026-08-01",
            kelasId = "K04",
            bangsalId = "R1",
            userId = "step4c",
        });
        maintain.IsSuccessStatusCode.Should().BeTrue(await maintain.Content.ReadAsStringAsync());

        string? regId = null;
        try
        {
            var process = await client.PostAsJsonAsync("api/admisi-ranap/admission/from-reservation", new
            {
                reservationId = reservation.ReservationId,
                kelasDkId = "3",
                bangsalId = "R1",
                userId = "step4c",
                registration = RegistrationBody("DR00000053"),
            });
            var result = await ReadJSendDataAsync<ProcessResponse>(process);
            regId = result.RegId;

            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_reg_inap WHERE fs_kd_reg=@regId", new { regId })).Should().Be(1);
            (await ScalarCountAsync("""
                SELECT COUNT(*) FROM BILRG_AdmReservation
                WHERE ReservationId=@reservationId AND ReservationStatus=2 AND RealizedRegId=@regId
                """, new { reservationId = reservation.ReservationId, regId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_RegAktif WHERE RegId=@regId", new { regId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AdmAdmission WHERE RegId=@regId", new { regId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_registrasi2 WHERE fs_kd_reg=@regId", new { regId })).Should().Be(0);
        }
        finally
        {
            await CleanupRegAsync(regId, reservationId: reservation.ReservationId);
            await CleanupOwnedPatientAsync(pasienId);
        }
    }

    [Fact]
    public async Task Step4C_InvalidProsedur_RejectsWithoutResidue()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);
        var pasienId = await CreateOwnedPatientAsync(client);

        var createOpname = await client.PostAsJsonAsync("api/admisi-ranap/opname-request", new
        {
            pasienId,
            dokterId = "DR00000010",
            plannedDate = "2026-07-21",
            clinicalNotes = "STEP4C invalid prosedur",
            userId = "step4c",
        });
        var opname = await ReadJSendDataAsync<CreateOpnameResponse>(createOpname);
        try
        {
            var before = await ScalarCountAsync(
                "SELECT COUNT(*) FROM BILRG_AdmAdmission WHERE PasienId=@pasienId",
                new { pasienId });

            var process = await client.PostAsJsonAsync("api/admisi-ranap/admission/from-opname-request", new
            {
                opnameRequestId = opname.OpnameRequestId,
                kelasDkId = "3",
                bangsalId = "R1",
                userId = "step4c",
                registration = new
                {
                    tipeJaminanId = "00000",
                    caraMasukDkId = "8",
                    prosedurMasukInapId = "ZZZ",
                    rujukanId = "00084",
                    dokterId = "DR00000010",
                    pesertaJaminanId = "",
                },
            });
            var raw = await process.Content.ReadAsStringAsync();
            process.IsSuccessStatusCode.Should().BeFalse();
            raw.Should().Contain("Prosedur");

            (await ScalarCountAsync(
                "SELECT COUNT(*) FROM BILRG_AdmAdmission WHERE PasienId=@pasienId",
                new { pasienId })).Should().Be(before);
            (await ScalarCountAsync("""
                SELECT COUNT(*) FROM BILRG_AdmOpnameRequest
                WHERE OpnameRequestId=@opnameId AND OpnameRequestStatus=0
                """, new { opnameId = opname.OpnameRequestId })).Should().Be(1);
        }
        finally
        {
            await CleanupRegAsync(null, opname.OpnameRequestId);
            await CleanupOwnedPatientAsync(pasienId);
        }
    }

    [Fact]
    public async Task Step4C_RegInapFailure_RollsBackAllCommittedWrites()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory(services =>
        {
            services.RemoveAll<IRegInapRepo>();
            services.AddScoped<IRegInapRepo, FailingRegInapRepo>();
        });
        using var client = CreateClient(factory);
        var pasienId = await CreateOwnedPatientAsync(client);
        var createOpname = await client.PostAsJsonAsync("api/admisi-ranap/opname-request", new
        {
            pasienId,
            dokterId = "DR00000015",
            plannedDate = "2026-07-22",
            clinicalNotes = "STEP4C rollback verify",
            userId = "step4c",
        });
        var opname = await ReadJSendDataAsync<CreateOpnameResponse>(createOpname);
        try
        {
            var process = await client.PostAsJsonAsync("api/admisi-ranap/admission/from-opname-request", new
            {
                opnameRequestId = opname.OpnameRequestId,
                kelasDkId = "3",
                bangsalId = "R1",
                userId = "step4c",
                registration = RegistrationBody(),
            });
            var raw = await process.Content.ReadAsStringAsync();
            process.IsSuccessStatusCode.Should().BeFalse();
            raw.Should().Contain("STEP4C_FORCED_REGINAP_FAILURE");

            (await ScalarCountAsync("""
                SELECT COUNT(*) FROM BILRG_AdmAdmission
                WHERE OpnameRequestId=@opnameId
                """, new { opnameId = opname.OpnameRequestId })).Should().Be(0);
            (await ScalarCountAsync("""
                SELECT COUNT(*) FROM BILRG_AdmOpnameRequest
                WHERE OpnameRequestId=@opnameId AND OpnameRequestStatus=0
                """, new { opnameId = opname.OpnameRequestId })).Should().Be(1);
            (await ScalarCountAsync("""
                SELECT COUNT(*) FROM BILRG_AuditLog
                WHERE EntityId=@opnameId AND ActionType='UPDATE' AND UserId='step4c'
                """, new { opnameId = opname.OpnameRequestId })).Should().Be(0);
            (await ScalarCountAsync(
                "SELECT COUNT(*) FROM BILRG_RegAktif WHERE PasienId=@pasienId",
                new { pasienId })).Should().Be(0);
        }
        finally
        {
            await CleanupRegAsync(null, opname.OpnameRequestId);
            await CleanupOwnedPatientAsync(pasienId);
        }
    }

    [Fact]
    public async Task Step4C_ReprocessFulfilledOpname_IsRejected()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);
        var pasienId = await CreateOwnedPatientAsync(client);

        var createOpname = await client.PostAsJsonAsync("api/admisi-ranap/opname-request", new
        {
            pasienId,
            dokterId = "DR00000010",
            plannedDate = "2026-07-23",
            clinicalNotes = "STEP4C reprocess verify",
            userId = "step4c",
        });
        var opname = await ReadJSendDataAsync<CreateOpnameResponse>(createOpname);
        string? regId = null;
        try
        {
            var first = await client.PostAsJsonAsync("api/admisi-ranap/admission/from-opname-request", new
            {
                opnameRequestId = opname.OpnameRequestId,
                kelasDkId = "3",
                bangsalId = "R2",
                userId = "step4c",
                registration = RegistrationBody("DR00000010"),
            });
            var result = await ReadJSendDataAsync<ProcessResponse>(first);
            regId = result.RegId;

            var second = await client.PostAsJsonAsync("api/admisi-ranap/admission/from-opname-request", new
            {
                opnameRequestId = opname.OpnameRequestId,
                kelasDkId = "3",
                bangsalId = "R2",
                userId = "step4c",
                registration = RegistrationBody("DR00000010"),
            });
            var raw = await second.Content.ReadAsStringAsync();
            second.IsSuccessStatusCode.Should().BeFalse();
            raw.Should().MatchRegex("(?i)Requested|Fulfilled|diproses|status");

            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_reg_inap WHERE fs_kd_reg=@regId", new { regId })).Should().Be(1);
        }
        finally
        {
            await CleanupRegAsync(regId, opname.OpnameRequestId);
            await CleanupOwnedPatientAsync(pasienId);
        }
    }

    [Fact]
    public async Task CancellationSql_OpnameFixture_PersistsVoidRetentionRestoreAndEntitySnapshots()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);
        var pasienId = await CreateOwnedPatientAsync(client);
        string? opnameId = null;
        string? regId = null;
        const string requestId = "CANSQL-OPNAME-01";
        try
        {
            var create = await client.PostAsJsonAsync("api/admisi-ranap/opname-request", new
            {
                pasienId, dokterId = "DR00000015", plannedDate = "2026-07-24",
                clinicalNotes = "owned coordinated cancellation SQL fixture", userId = "sqlverify",
            });
            opnameId = (await ReadJSendDataAsync<CreateOpnameResponse>(create)).OpnameRequestId;
            var process = await client.PostAsJsonAsync("api/admisi-ranap/admission/from-opname-request", new
            {
                opnameRequestId = opnameId, kelasDkId = "3", bangsalId = "R1", userId = "sqlverify",
                registration = RegistrationBody(),
            });
            regId = (await ReadJSendDataAsync<ProcessResponse>(process)).RegId;

            using var scope = factory.Services.CreateScope();
            var cancel = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new
                Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases.AdmCoordinatedCancelCmd(
                    regId, "real SQL verification", "sqlverify",
                    Bilreg.Domain.AdmisiRanapContext.AdmissionFeature.AdmissionStatusEnum.Admitted,
                    null, requestId, null, null));
            cancel.RegistrationVoided.Should().BeTrue();

            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AdmAdmission WHERE RegId=@regId AND AdmissionStatus=4 AND VodUser='sqlverify' AND VodDate <> '3000-01-01'", new { regId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_registrasi WHERE fs_kd_reg=@regId AND fd_tgl_void <> '3000-01-01' AND fs_jam_void <> '' AND fs_kd_petugas_void='sqlverify' AND fd_tgl_keluar='3000-01-01' AND fd_tgl_cancel_out='3000-01-01'", new { regId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_reg_inap WHERE fs_kd_reg=@regId", new { regId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_reg_jaminan WHERE fs_kd_reg=@regId", new { regId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_reg_history_dokter WHERE fs_kd_reg=@regId AND ISNULL(LTRIM(RTRIM(fd_tgl_selesai)),'')=''", new { regId })).Should().Be(0);
            (await ScalarCountAsync("SELECT COUNT(*) FROM ta_reg_history_dokter WHERE fs_kd_reg=@regId", new { regId })).Should().BeGreaterThan(0);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_RegAktif WHERE RegId=@regId", new { regId })).Should().Be(0);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AdmOpnameRequest WHERE OpnameRequestId=@opnameId AND OpnameRequestStatus=0 AND FulfilledRegId='-'", new { opnameId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AuditLog WHERE CorrelationId=@requestId", new { requestId })).Should().Be(5);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AuditLog WHERE CorrelationId=@requestId AND EntityName='RegInapModel' AND OriginalDataJson LIKE '%DoctorHistory%'", new { requestId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AuditLog WHERE CorrelationId=@requestId AND EntityName='RegAktifModel' AND OriginalDataJson LIKE '%RegId%'", new { requestId })).Should().Be(1);
        }
        finally
        {
            await CleanupRegAsync(regId, opnameId, requestId: requestId);
            await CleanupOwnedPatientAsync(pasienId);
        }
    }

    [Theory]
    [InlineData(0)] // Waiting
    [InlineData(1)] // Accepted
    public async Task CancellationSql_ActiveWaitingList_IsRetainedAndCancelled(int waitingStatus)
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);
        OwnedOpnameFixture? fixture = null;
        var requestId = "CANSQL-WL-" + waitingStatus + "-" + Guid.NewGuid().ToString("N")[..12];
        try
        {
            fixture = await CreateOwnedOpnameRegistrationAsync(client);
            await AddWaitingListAsync(fixture.RegId, fixture.PasienId, waitingStatus);

            await CancelAsync(factory.Services, fixture.RegId, requestId);

            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_BedWaitingList WHERE RegId=@regId AND WaitingListStatus=3 AND VodUser='sqlverify' AND VodDate <> '3000-01-01'", new { regId = fixture.RegId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AuditLog WHERE CorrelationId=@requestId AND EntityName='WaitingListModel' AND OriginalDataJson LIKE '%WaitingListStatus%'", new { requestId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_RegAktif WHERE RegId=@regId", new { regId = fixture.RegId })).Should().Be(0);
        }
        finally
        {
            if (fixture is not null)
            {
                await CleanupRegAsync(fixture.RegId, fixture.OpnameId, requestId: requestId);
                await CleanupOwnedPatientAsync(fixture.PasienId);
            }
        }
    }

    [Fact]
    public async Task CancellationSql_BillingItem_BlocksWithoutAnyMutationOrLedger()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);
        OwnedOpnameFixture? fixture = null;
        var requestId = "CANSQL-BILL-" + Guid.NewGuid().ToString("N")[..12];
        var billingId = "CAN" + Guid.NewGuid().ToString("N")[..20];
        try
        {
            fixture = await CreateOwnedOpnameRegistrationAsync(client);
            await using (var conn = new SqlConnection(ConnStr))
                await conn.ExecuteAsync("INSERT INTO ta_trs_billing (fs_kd_trs,fs_kd_reg) VALUES (@billingId,@regId)", new { billingId, regId = fixture.RegId });

            var action = () => CancelAsync(factory.Services, fixture.RegId, requestId);
            (await action.Should().ThrowAsync<Bilreg.Application.AdmisiRanapContext.AdmissionFeature.CoordinatedCancellationException>()).Which.Code
                .Should().Be(Bilreg.Application.AdmisiRanapContext.AdmissionFeature.CoordinatedCancellationErrorCode.RegistrationHasBillingItems);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AdmAdmission WHERE RegId=@regId AND AdmissionStatus=0", new { regId = fixture.RegId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AuditLog WHERE CorrelationId=@requestId", new { requestId })).Should().Be(0);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AdmCoordinatedCancellationRequest WHERE RequestId=@requestId", new { requestId })).Should().Be(0);
        }
        finally
        {
            await using var conn = new SqlConnection(ConnStr);
            await conn.ExecuteAsync("DELETE FROM ta_trs_billing WHERE fs_kd_trs=@billingId", new { billingId });
            if (fixture is not null)
            {
                await CleanupRegAsync(fixture.RegId, fixture.OpnameId, requestId: requestId);
                await CleanupOwnedPatientAsync(fixture.PasienId);
            }
        }
    }

    [Fact]
    public async Task CancellationSql_LegacySource_HasNoSourceRestoreButCancelsCoherently()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);
        OwnedOpnameFixture? fixture = null;
        var requestId = "CANSQL-LEGACY-" + Guid.NewGuid().ToString("N")[..10];
        try
        {
            fixture = await CreateOwnedOpnameRegistrationAsync(client);
            await using (var conn = new SqlConnection(ConnStr))
                await conn.ExecuteAsync("UPDATE BILRG_AdmAdmission SET AdmissionSource=1,OpnameRequestId='-',ReservationId='-' WHERE RegId=@regId", new { regId = fixture.RegId });

            await CancelAsync(factory.Services, fixture.RegId, requestId);

            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AdmAdmission WHERE RegId=@regId AND AdmissionStatus=4", new { regId = fixture.RegId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AuditLog WHERE CorrelationId=@requestId AND ActionType='RESTORE'", new { requestId })).Should().Be(0);
        }
        finally
        {
            if (fixture is not null)
            {
                await CleanupRegAsync(fixture.RegId, fixture.OpnameId, requestId: requestId);
                await CleanupOwnedPatientAsync(fixture.PasienId);
            }
        }
    }

    [Fact]
    public async Task CancellationSql_DurableIdempotency_ReplaysAndRejectsReusedRequestId()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);
        OwnedOpnameFixture? fixture = null;
        var requestId = "CANSQL-IDEMP-" + Guid.NewGuid().ToString("N")[..10];
        var secondRequestId = "CANSQL-IDEMP2-" + Guid.NewGuid().ToString("N")[..9];
        try
        {
            fixture = await CreateOwnedOpnameRegistrationAsync(client);
            await CancelAsync(factory.Services, fixture.RegId, requestId);
            var auditCount = await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AuditLog WHERE CorrelationId=@requestId", new { requestId });

            await CancelAsync(factory.Services, fixture.RegId, requestId);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AuditLog WHERE CorrelationId=@requestId", new { requestId })).Should().Be(auditCount);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AdmCoordinatedCancellationRequest WHERE RequestId=@requestId AND Lifecycle='Completed'", new { requestId })).Should().Be(1);

            using (var scope = factory.Services.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var reused = () => mediator.Send(new Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases.AdmCoordinatedCancelCmd(
                    fixture.RegId, "different reason", "sqlverify", Bilreg.Domain.AdmisiRanapContext.AdmissionFeature.AdmissionStatusEnum.Admitted,
                    null, requestId, null, null));
                (await reused.Should().ThrowAsync<Bilreg.Application.AdmisiRanapContext.AdmissionFeature.CoordinatedCancellationException>()).Which.Code
                    .Should().Be(Bilreg.Application.AdmisiRanapContext.AdmissionFeature.CoordinatedCancellationErrorCode.RequestIdReused);
            }

            await CancelAsync(factory.Services, fixture.RegId, secondRequestId);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AdmCoordinatedCancellationRequest WHERE RequestId=@requestId AND Lifecycle='Completed'", new { requestId = secondRequestId })).Should().Be(1);
        }
        finally
        {
            if (fixture is not null)
            {
                await CleanupRegAsync(fixture.RegId, fixture.OpnameId, requestId: requestId);
                await CleanupRegAsync(null, requestId: secondRequestId);
                await CleanupOwnedPatientAsync(fixture.PasienId);
            }
        }
    }

    [Fact]
    public async Task CancellationSql_TwoIndependentRequests_ProduceOneTransitionAndNoLoserResidue()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);
        OwnedOpnameFixture? fixture = null;
        var first = "CANSQL-CON-A-" + Guid.NewGuid().ToString("N")[..9];
        var second = "CANSQL-CON-B-" + Guid.NewGuid().ToString("N")[..9];
        try
        {
            fixture = await CreateOwnedOpnameRegistrationAsync(client);
            var results = await Task.WhenAll(
                CancelAsync(factory.Services, fixture.RegId, first).ContinueWith(t => t.Exception),
                CancelAsync(factory.Services, fixture.RegId, second).ContinueWith(t => t.Exception));
            results.Count(x => x is null).Should().Be(2);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AdmAdmission WHERE RegId=@regId AND AdmissionStatus=4", new { regId = fixture.RegId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_AuditLog WHERE EntityId=@regId AND ActionType='VOID' AND EntityName='AdmissionModel'", new { regId = fixture.RegId })).Should().Be(1);
            (await ScalarCountAsync("SELECT COUNT(*) FROM BILRG_RegAktif WHERE RegId=@regId", new { regId = fixture.RegId })).Should().Be(0);
        }
        finally
        {
            if (fixture is not null)
            {
                await CleanupRegAsync(fixture.RegId, fixture.OpnameId, requestId: first);
                await CleanupRegAsync(null, requestId: second);
                await CleanupOwnedPatientAsync(fixture.PasienId);
            }
        }
    }
}
