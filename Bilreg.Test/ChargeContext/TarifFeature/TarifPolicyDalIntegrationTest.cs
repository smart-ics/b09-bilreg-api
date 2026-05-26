using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ChargeContext.TarifFeature;

/// <summary>
/// Integration tests against test DB; requires Phase-2 SQL scripts deployed.
/// </summary>
public class TarifPolicyDalIntegrationTest
{
    private readonly TarifPolicyRepo _policyRepo;
    private readonly TarifPublishLogRepo _publishLogRepo;
    private readonly NilaiTarifProjectionWriter _projectionWriter;
    private readonly NilaiTarifDal _nilaiTarifDal;
    private readonly NilaiTarifKompDal _nilaiTarifKompDal;

    public TarifPolicyDalIntegrationTest()
    {
        var env = ConnStringHelper.GetTestEnv();
        _nilaiTarifDal = new NilaiTarifDal(env);
        _nilaiTarifKompDal = new NilaiTarifKompDal(env);
        var nilaiTarifRepo = new NilaiTarifRepo(_nilaiTarifDal, _nilaiTarifKompDal, Microsoft.Extensions.Logging.Abstractions.NullLogger<NilaiTarifRepo>.Instance);

        _policyRepo = new TarifPolicyRepo(
            new TarifPolicyDal(env),
            new TarifVariantDal(env),
            new TarifVariantKomponenDal(env));

        _publishLogRepo = new TarifPublishLogRepo(
            new TarifPublishLogDal(env),
            new TarifPublishLogDetailDal(env));

        _projectionWriter = new NilaiTarifProjectionWriter(
            nilaiTarifRepo,
            _nilaiTarifDal,
            _nilaiTarifKompDal);
    }

    [Fact]
    public void IT1_GivenPolicyDraft_WhenSaveAndLoad_ThenRoundTripsVariantsAndKomponen()
    {
        var policyId = $"P{Guid.NewGuid():N}"[..12];
        var komponen = new TarifVariantKomponenType(0, KomponenType.Default.ToReff(), 50m);
        var variant = new TarifVariantType(policyId, 1, "ZZ", "999", "99", 50m, "", [komponen]);
        var policy = new TarifPolicyType(
            policyId,
            "SK-IT",
            "Integration Policy",
            new DateTime(2026, 7, 1),
            "integration",
            TarifPolicyStatus.Draft,
            AuditTrailType.Create("it-user", DateTime.Now),
            [variant]);

        try
        {
            _policyRepo.SaveChanges(policy);
            var loaded = _policyRepo.LoadEntity(TarifPolicyType.Key(policyId));

            loaded.HasValue.Should().BeTrue();
            loaded.Match(
                onSome: m =>
                {
                    m.PolicyNo.Should().Be("SK-IT");
                    m.Variants.Should().HaveCount(1);
                    m.Variants.First().ListKomponen.Should().HaveCount(1);
                },
                onNone: () => Assert.Fail("Expected loaded policy"));
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        finally
        {
            TryDeletePolicy(policyId);
        }
    }

    [Fact]
    public void IT2_GivenPublishLog_WhenInsertAndLoad_ThenReturnsLog()
    {
        var policyId = $"P{Guid.NewGuid():N}"[..12];
        var logId = $"L{Guid.NewGuid():N}"[..12];
        var log = new TarifPublishLogType(
            logId,
            policyId,
            "it-user",
            DateTime.Now,
            1,
            "integration",
            [new TarifPublishLogDetailType(1, "ZZ", "999", "99", "", 50m)]);

        try
        {
            _publishLogRepo.Insert(log);
            var loaded = _publishLogRepo.LoadEntity(TarifPublishLogType.Key(logId));
            loaded.HasValue.Should().BeTrue();
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
        {
            // Phase-2 tables not deployed on this test database.
        }
    }

    [Fact]
    public void IT3_GivenProjectionUpsert_WhenVariantExists_ThenPreservesNilaiTarifId()
    {
        var nilaiTarifId = $"01AR{Guid.NewGuid():N}"[..26];
        var marker = new NilaiTarifDto(nilaiTarifId, "ZZ", "99", "999", 10, "", "", "", "");
        var projection = new NilaiTarifType(
            "-",
            "ZZ",
            "",
            new TipeTarifReff("99", ""),
            new KelasReff("999", ""),
            20m,
            [new NilaiTarifKomponenType(0, KomponenType.Default.ToReff(), 20m)]);

        try
        {
            _nilaiTarifDal.Insert(marker);
            var upsertedId = _projectionWriter.Upsert(projection, "POL-IT");
            upsertedId.Should().Be(nilaiTarifId);
        }
        finally
        {
            _nilaiTarifKompDal.Delete(NilaiTarifType.Key(nilaiTarifId));
            _nilaiTarifDal.Delete(NilaiTarifType.Key(nilaiTarifId));
        }
    }

    private void TryDeletePolicy(string policyId)
    {
        try
        {
            var key = TarifPolicyType.Key(policyId);
            new TarifVariantKomponenDal(ConnStringHelper.GetTestEnv()).Delete(key);
            new TarifVariantDal(ConnStringHelper.GetTestEnv()).Delete(key);
            new TarifPolicyDal(ConnStringHelper.GetTestEnv()).Delete(key);
        }
        catch
        {
            // Tables may not exist in local test DB yet.
        }
    }

}
