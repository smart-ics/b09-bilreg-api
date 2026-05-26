using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TarifFeature.UseCases;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class TrfPublishTarifPolicyHandlerTest
{
    private readonly Mock<ITarifPolicyRepo> _policyRepoMock = new();
    private readonly Mock<ITarifPublishLogRepo> _publishLogRepoMock = new();
    private readonly Mock<INilaiTarifProjectionWriter> _projectionWriterMock = new();
    private readonly Mock<ITarifRepo> _tarifRepoMock = new();
    private readonly Mock<ITipeTarifRepo> _tipeTarifRepoMock = new();
    private readonly Mock<IKelasRepo> _kelasRepoMock = new();
    private readonly Mock<IKomponenRepo> _komponenRepoMock = new();
    private readonly TrfPublishTarifPolicyHandler _handler;

    public TrfPublishTarifPolicyHandlerTest()
    {
        SetupMastersExist();
        _handler = new TrfPublishTarifPolicyHandler(
            _policyRepoMock.Object,
            _publishLogRepoMock.Object,
            _projectionWriterMock.Object,
            _tarifRepoMock.Object,
            _tipeTarifRepoMock.Object,
            _kelasRepoMock.Object,
            _komponenRepoMock.Object,
            NullLogger<TrfPublishTarifPolicyHandler>.Instance);
    }

    [Fact]
    public async Task UT1_GivenDraftPolicy_WhenPublish_ThenInsertsLogUpsertsAndMarksPublished()
    {
        var policy = CreateDraftPolicy();
        SetupPolicyLoad(policy);
        _projectionWriterMock
            .Setup(x => x.Upsert(It.IsAny<NilaiTarifType>(), It.IsAny<string>()))
            .Returns("NT-001");

        var response = await _handler.Handle(
            new TrfPublishTarifPolicyCmd(policy.TarifPolicyId, "publisher", "note"),
            CancellationToken.None);

        response.PolicyStatus.Should().Be(TarifPolicyStatus.Published);
        response.VariantCount.Should().Be(1);
        response.Variants.Should().ContainSingle(v => v.NilaiTarifId == "NT-001");
        response.PublishLogId.Should().NotBeNullOrWhiteSpace();

        _projectionWriterMock.Verify(
            x => x.Upsert(It.IsAny<NilaiTarifType>(), policy.TarifPolicyId),
            Times.Once);
        _publishLogRepoMock.Verify(
            x => x.Insert(It.Is<TarifPublishLogType>(l =>
                l.TarifPolicyId == policy.TarifPolicyId &&
                l.VariantCount == 1 &&
                l.Details.Count() == 1 &&
                l.Details.First().NilaiTarifId == "NT-001")),
            Times.Once);
        _policyRepoMock.Verify(
            x => x.SaveChanges(It.Is<TarifPolicyType>(p =>
                p.PolicyStatus == TarifPolicyStatus.Published &&
                p.Variants.Single().PublishedNilaiTarifId == "NT-001")),
            Times.Once);
    }

    [Fact]
    public async Task UT2_GivenMissingPolicy_WhenPublish_ThenThrowsKeyNotFound()
    {
        _policyRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifPolicyKey>()))
            .Returns(MayBe<TarifPolicyType>.None);

        var act = () => _handler.Handle(
            new TrfPublishTarifPolicyCmd("MISSING", "publisher"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*MISSING*");
    }

    [Fact]
    public async Task UT3_GivenUpsertFails_WhenPublish_ThenWrapsAndDoesNotPersist()
    {
        var policy = CreateDraftPolicy();
        SetupPolicyLoad(policy);
        _projectionWriterMock
            .Setup(x => x.Upsert(It.IsAny<NilaiTarifType>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("db failure"));

        var act = () => _handler.Handle(
            new TrfPublishTarifPolicyCmd(policy.TarifPolicyId, "publisher"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*transaction rolled back*");
        _publishLogRepoMock.Verify(x => x.Insert(It.IsAny<TarifPublishLogType>()), Times.Never);
        _policyRepoMock.Verify(x => x.SaveChanges(It.IsAny<TarifPolicyType>()), Times.Never);
    }

    [Fact]
    public async Task UT4_GivenPublishedPolicy_WhenRepublish_ThenKeepsPublishedAndRefreshesProjection()
    {
        var publishedVariant = new TarifVariantType(
            "POL001", 1, "T01", "K1", "01", 100m, "NT-OLD",
            [new TarifVariantKomponenType(1, KomponenType.Default.ToReff(), 100m)]);
        var policy = new TarifPolicyType(
            "POL001", "SK", "Name", DateTime.Now, "",
            TarifPolicyStatus.Published,
            AuditTrailType.Create("pub", DateTime.Now),
            [publishedVariant]);

        SetupPolicyLoad(policy);
        _projectionWriterMock
            .Setup(x => x.Upsert(It.IsAny<NilaiTarifType>(), It.IsAny<string>()))
            .Returns("NT-REFRESH");

        var response = await _handler.Handle(
            new TrfPublishTarifPolicyCmd(policy.TarifPolicyId, "republisher"),
            CancellationToken.None);

        response.PolicyStatus.Should().Be(TarifPolicyStatus.Published);
        response.Variants.Single().NilaiTarifId.Should().Be("NT-REFRESH");
        _policyRepoMock.Verify(
            x => x.SaveChanges(It.Is<TarifPolicyType>(p =>
                p.PolicyStatus == TarifPolicyStatus.Published &&
                p.Variants.Single().PublishedNilaiTarifId == "NT-REFRESH")),
            Times.Once);
    }

    [Fact]
    public async Task UT5_GivenEmptyPolicy_WhenPublish_ThenThrowsInvalidOperation()
    {
        var policy = TarifPolicyType.Create("SK", "Name", DateTime.Now, "", "user");
        SetupPolicyLoad(policy);

        var act = () => _handler.Handle(
            new TrfPublishTarifPolicyCmd(policy.TarifPolicyId, "publisher"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak memiliki variant*");
        _projectionWriterMock.Verify(x => x.Upsert(It.IsAny<NilaiTarifType>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UT6_GivenDuplicateVariants_WhenPublish_ThenThrowsInvalidOperation()
    {
        var line = new TarifVariantKomponenType(1, KomponenType.Default.ToReff(), 50m);
        var v1 = new TarifVariantType("POL001", 1, "T01", "K1", "01", 50m, "", [line]);
        var v2 = new TarifVariantType("POL001", 2, "T01", "K1", "01", 50m, "", [line]);
        var policy = new TarifPolicyType(
            "POL001", "SK", "N", DateTime.Now, "",
            TarifPolicyStatus.Draft,
            AuditTrailType.Create("u", DateTime.Now),
            [v1, v2]);
        SetupPolicyLoad(policy);

        var act = () => _handler.Handle(
            new TrfPublishTarifPolicyCmd(policy.TarifPolicyId, "publisher"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*duplikat*");
    }

    [Fact]
    public async Task UT7_GivenArchivedPolicy_WhenPublish_ThenThrowsInvalidOperation()
    {
        var policy = CreateDraftPolicy() with { PolicyStatus = TarifPolicyStatus.Archived };
        SetupPolicyLoad(policy);

        var act = () => _handler.Handle(
            new TrfPublishTarifPolicyCmd(policy.TarifPolicyId, "publisher"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak diperbolehkan*");
    }

    // Billing / tindakan consumers are unchanged — publish does not call PaymentContext or tindakan repos.

    private void SetupPolicyLoad(TarifPolicyType policy)
    {
        _policyRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITarifPolicyKey>(k => k.TarifPolicyId == policy.TarifPolicyId)))
            .Returns(MayBe.From(policy));
    }

    private void SetupMastersExist()
    {
        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "T01", TarifName = "Tarif" }));
        _tipeTarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITipeTarifKey>()))
            .Returns(MayBe.From(TipeTarifType.Default with { TipeTarifId = "01" }));
        _kelasRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IKelasKey>()))
            .Returns(MayBe.From(KelasType.Default with { KelasId = "K1" }));
        _komponenRepoMock
            .Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([KomponenType.Default]);
    }

    private static TarifPolicyType CreateDraftPolicy()
    {
        var komponen = new TarifVariantKomponenType(1, KomponenType.Default.ToReff(), 100m);
        var variant = new TarifVariantType("POL001", 1, "T01", "K1", "01", 100m, "", [komponen]);
        return new TarifPolicyType(
            "POL001",
            "SK-001",
            "Policy",
            new DateTime(2026, 6, 1),
            "",
            TarifPolicyStatus.Draft,
            AuditTrailType.Create("user1", DateTime.Now),
            [variant]);
    }
}
