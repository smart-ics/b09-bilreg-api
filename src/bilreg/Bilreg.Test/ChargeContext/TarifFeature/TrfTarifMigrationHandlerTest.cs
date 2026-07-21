using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TarifFeature.UseCases;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class TrfTarifMigrationHandlerTest
{
    [Fact]
    public void UT1_GivenImportOnlyMode_WhenEnsurePublish_ThenThrows()
    {
        var guard = CreateGuard(TarifMigrationMode.ImportOnly, allowEmergency: false);

        var act = () => guard.EnsurePublishAllowed();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*publish tidak diperbolehkan*");
    }

    [Fact]
    public void UT2_GivenPublishPrimaryWithoutEmergency_WhenEnsureImport_ThenThrows()
    {
        var guard = CreateGuard(TarifMigrationMode.PublishPrimary, allowEmergency: false);

        var act = () => guard.EnsureImportAllowed(isEmergency: false);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*import tidak diperbolehkan*");
    }

    [Fact]
    public void UT3_GivenPublishPrimaryWithEmergencyFlag_WhenEnsureImportEmergency_ThenAllows()
    {
        var guard = CreateGuard(TarifMigrationMode.PublishPrimary, allowEmergency: true);

        var act = () => guard.EnsureImportAllowed(isEmergency: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void UT4_GivenHybridMode_WhenEnsureImportAndPublish_ThenAllows()
    {
        var guard = CreateGuard(TarifMigrationMode.Hybrid, allowEmergency: false);

        var import = () => guard.EnsureImportAllowed();
        var publish = () => guard.EnsurePublishAllowed();

        import.Should().NotThrow();
        publish.Should().NotThrow();
    }

    [Fact]
    public void UT5_GivenPublishActive_WhenAcquireImport_ThenThrows()
    {
        var gate = new TarifOperationalGate();
        using var publish = gate.Acquire(TarifOperation.Publish);

        var act = () => gate.Acquire(TarifOperation.Import);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sedang berjalan*");
    }

    [Fact]
    public async Task UT6_GivenStatusQuery_WhenHandle_ThenReturnsEffectiveMode()
    {
        var resolver = CreateResolver(TarifMigrationMode.Hybrid, dbOverride: null);

        var stateRepo = new Mock<ITarifOperationalStateRepo>();
        stateRepo.Setup(x => x.GetState()).Returns(new TarifOperationalState(
            null, null, "", null, "", DateTime.Now, ""));

        var projectionRepo = new Mock<ITarifProjectionReadRepo>();
        projectionRepo.Setup(x => x.GetProjectionSummary())
            .Returns(new TarifProjectionSummary(10, 5, 5));
        projectionRepo.Setup(x => x.GetLastPublish())
            .Returns((DateTime.Now, "TPF001", "TPL001"));

        var policyRepo = new Mock<ITarifPolicyRepo>();
        policyRepo.Setup(x => x.ListData(It.IsAny<TarifPolicyListFilter>()))
            .Returns([]);

        var handler = new TrfGetTarifMigrationStatusHandler(
            resolver,
            Options.Create(new TarifMigrationOptions { Mode = TarifMigrationMode.Hybrid }),
            new TarifOperationalGate(),
            stateRepo.Object,
            projectionRepo.Object,
            policyRepo.Object);

        var status = await handler.Handle(new TrfGetTarifMigrationStatusQry(), CancellationToken.None);

        status.EffectiveMode.Should().Be(TarifMigrationMode.Hybrid);
        status.ProjectionSummary.TotalVariantCount.Should().Be(10);
        status.AllowPublish.Should().BeTrue();
        status.AllowImport.Should().BeTrue();
    }

    [Fact]
    public async Task UT7_GivenConsistencyQuery_WhenDuplicatesExist_ThenNotHealthy()
    {
        var handler = new TrfCheckTarifProjectionConsistencyHandler(
            Mock.Of<ITarifProjectionReadRepo>(r =>
                r.GetConsistencyReport() == new TarifProjectionConsistencyReport(2, 0, 0, false)));

        var report = await handler.Handle(
            new TrfCheckTarifProjectionConsistencyQry(),
            CancellationToken.None);

        report.DuplicateVariantKeyCount.Should().Be(2);
        report.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task UT8_GivenExistingBaselinePolicyNo_WhenCreateBaseline_ThenThrows()
    {
        var policyRepo = new Mock<ITarifPolicyRepo>();
        policyRepo
            .Setup(x => x.ListData(It.IsAny<TarifPolicyListFilter>()))
            .Returns([
                new TarifPolicySummaryView(
                    "TPF001", "BASELINE-20260526", "x", DateTime.Now,
                    TarifPolicyStatus.Published, 1)
            ]);

        var handler = CreateBaselineHandler(
            policyRepo: policyRepo.Object,
            projectionRows: [CreateProjectionRow()]);

        var act = async () => await handler.Handle(
            new TrfCreateBaselineTarifPolicyCmd("USR", "BASELINE-20260526"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah ada*");
    }

    [Fact]
    public async Task UT9_GivenProjectionRows_WhenCreateBaseline_ThenUpsertsAndMarksPublished()
    {
        var policyRepo = new Mock<ITarifPolicyRepo>();
        var projectionWriter = new Mock<INilaiTarifProjectionWriter>();
        var publishLogRepo = new Mock<ITarifPublishLogRepo>();
        var operationalState = new Mock<ITarifOperationalStateRepo>();

        policyRepo.Setup(x => x.ListData(It.IsAny<TarifPolicyListFilter>())).Returns([]);
        projectionWriter
            .Setup(x => x.Upsert(It.IsAny<NilaiTarifType>(), It.IsAny<string>()))
            .Returns("NT-EXISTING");

        var handler = CreateBaselineHandler(
            policyRepo: policyRepo.Object,
            projectionWriter: projectionWriter.Object,
            publishLogRepo: publishLogRepo.Object,
            operationalState: operationalState.Object,
            projectionRows: [CreateProjectionRow()]);

        var response = await handler.Handle(
            new TrfCreateBaselineTarifPolicyCmd("USR", "BASELINE-TEST"),
            CancellationToken.None);

        response.VariantCount.Should().Be(1);
        response.PolicyNo.Should().Be("BASELINE-TEST");
        projectionWriter.Verify(
            x => x.Upsert(It.IsAny<NilaiTarifType>(), It.Is<string>(id => id.StartsWith("TPF"))),
            Times.Once);
        publishLogRepo.Verify(x => x.Insert(It.IsAny<TarifPublishLogType>()), Times.Once);
        policyRepo.Verify(x => x.SaveChanges(It.IsAny<TarifPolicyType>()), Times.AtLeast(2));
        operationalState.Verify(
            x => x.RecordBaseline(It.IsAny<string>(), "USR", It.IsAny<DateTime>()),
            Times.Once);
    }

    private static ITarifMigrationGuard CreateGuard(
        TarifMigrationMode mode,
        bool allowEmergency) =>
        new TarifMigrationGuard(CreateResolver(mode, null, allowEmergency));

    private static ITarifMigrationModeResolver CreateResolver(
        TarifMigrationMode configMode,
        TarifMigrationMode? dbOverride = null,
        bool allowEmergency = false)
    {
        var options = Options.Create(new TarifMigrationOptions
        {
            Mode = configMode,
            AllowEmergencyImport = allowEmergency
        });
        var stateRepo = new Mock<ITarifOperationalStateRepo>();
        stateRepo.Setup(x => x.GetState()).Returns(new TarifOperationalState(
            dbOverride, null, "", null, "", DateTime.Now, ""));
        return new TarifMigrationModeResolver(options, stateRepo.Object);
    }

    private static TrfCreateBaselineTarifPolicyHandler CreateBaselineHandler(
        ITarifPolicyRepo? policyRepo = null,
        INilaiTarifProjectionWriter? projectionWriter = null,
        ITarifPublishLogRepo? publishLogRepo = null,
        ITarifOperationalStateRepo? operationalState = null,
        IReadOnlyList<NilaiTarifProjectionRow>? projectionRows = null)
    {
        var rows = projectionRows ?? [];
        var tarifRepo = new Mock<ITarifRepo>();
        var tipeRepo = new Mock<ITipeTarifRepo>();
        var kelasRepo = new Mock<IKelasRepo>();
        var komponenRepo = new Mock<IKomponenRepo>();

        tarifRepo.Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "T01", TarifName = "Tarif" }));
        tipeRepo.Setup(x => x.LoadEntity(It.IsAny<ITipeTarifKey>()))
            .Returns(MayBe.From(TipeTarifType.Default with { TipeTarifId = "TT" }));
        kelasRepo.Setup(x => x.LoadEntity(It.IsAny<IKelasKey>()))
            .Returns(MayBe.From(KelasType.Default with { KelasId = "K1" }));
        komponenRepo.Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([KomponenType.Default with { KomponenId = "KOMP" }]);

        var migrationGuard = new Mock<ITarifMigrationGuard>();
        migrationGuard.Setup(x => x.EnsurePublishAllowed());

        return new TrfCreateBaselineTarifPolicyHandler(
            policyRepo ?? Mock.Of<ITarifPolicyRepo>(),
            publishLogRepo ?? Mock.Of<ITarifPublishLogRepo>(),
            projectionWriter ?? Mock.Of<INilaiTarifProjectionWriter>(),
            Mock.Of<ITarifProjectionReadRepo>(r => r.ListAllProjection() == rows),
            operationalState ?? Mock.Of<ITarifOperationalStateRepo>(),
            migrationGuard.Object,
            new TarifOperationalGate(),
            tarifRepo.Object,
            tipeRepo.Object,
            kelasRepo.Object,
            komponenRepo.Object,
            NullLogger<TrfCreateBaselineTarifPolicyHandler>.Instance, TestTglJamProvider.Instance);
    }

    private static NilaiTarifProjectionRow CreateProjectionRow() =>
        new(
            "NT-001",
            "T01",
            "TT",
            "K1",
            100m,
            "",
            [new NilaiTarifProjectionKomponenRow(1, "KOMP", 100m)]);
}
