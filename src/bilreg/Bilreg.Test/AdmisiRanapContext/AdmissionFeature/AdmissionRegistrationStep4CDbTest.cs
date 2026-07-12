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

    private static async Task<int> ScalarCountAsync(string sql, object param)
    {
        await using var conn = new SqlConnection(ConnStr);
        return await conn.ExecuteScalarAsync<int>(sql, param);
    }

    private static async Task CleanupRegAsync(string? regId, string? opnameId = null, string? reservationId = null)
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

        tx.Commit();
    }

    private static object RegistrationBody(string dokterId = "DR00000015") => new
    {
        tipeJaminanId = "00000",
        caraMasukDkId = "8",
        prosedurMasukInapId = "IGD",
        rujukanId = "00084",
        dokterId,
        layananId = "RI001",
        karcisId = "02",
        pesertaJaminanId = "",
    };

    [Fact]
    public async Task Step4C_OpnamePath_PersistsSharedRegIdIncludingRegInapAndHistory()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);

        const string pasienId = "347137300000057";
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
        }
    }

    [Fact]
    public async Task Step4C_ReservationPath_PersistsSharedRegIdIncludingRegInap()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);

        const string pasienId = "347137300000037";
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
        }
    }

    [Fact]
    public async Task Step4C_InvalidProsedur_RejectsWithoutResidue()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);

        var createOpname = await client.PostAsJsonAsync("api/admisi-ranap/opname-request", new
        {
            pasienId = "347137300000014",
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
                new { pasienId = "347137300000014" });

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
                    layananId = "RI001",
                    karcisId = "02",
                    pesertaJaminanId = "",
                },
            });
            var raw = await process.Content.ReadAsStringAsync();
            process.IsSuccessStatusCode.Should().BeFalse();
            raw.Should().Contain("Prosedur");

            (await ScalarCountAsync(
                "SELECT COUNT(*) FROM BILRG_AdmAdmission WHERE PasienId=@pasienId",
                new { pasienId = "347137300000014" })).Should().Be(before);
            (await ScalarCountAsync("""
                SELECT COUNT(*) FROM BILRG_AdmOpnameRequest
                WHERE OpnameRequestId=@opnameId AND OpnameRequestStatus=0
                """, new { opnameId = opname.OpnameRequestId })).Should().Be(1);
        }
        finally
        {
            await CleanupRegAsync(null, opname.OpnameRequestId);
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

        const string pasienId = "347137300000057";
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
        }
    }

    [Fact]
    public async Task Step4C_ReprocessFulfilledOpname_IsRejected()
    {
        await using var factory = new Step4CRealDbWebApplicationFactory();
        using var client = CreateClient(factory);

        var createOpname = await client.PostAsJsonAsync("api/admisi-ranap/opname-request", new
        {
            pasienId = "347137300000014",
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
        }
    }
}
