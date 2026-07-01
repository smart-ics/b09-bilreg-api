using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TarifFeature.UseCases;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class TrfTarifPolicyAdminHandlerTest
{
    private static TrfVariantKomponenInput Komp(string id, decimal nilai) => new(id, nilai);

    private static TarifVariantKomponenType Line(int noUrut, decimal nilai) =>
        new(noUrut, KomponenType.Default.ToReff(), nilai);

    private static TarifPolicyType CreateDraftWithOneVariant()
    {
        var policy = TarifPolicyType.Create("SK-001", "Policy Test", new DateTime(2026, 6, 1), "desc", "user1");
        return policy.AddVariant("T01", "K1", "01", 100m, [Line(1, 60m), Line(2, 40m)]);
    }

    private readonly Mock<ITarifPolicyRepo> _policyRepoMock = new();
    private readonly Mock<ITarifPublishLogRepo> _publishLogRepoMock = new();

    [Fact]
    public async Task UT1_GivenValidRequest_WhenCreate_ThenSavesDraftPolicy()
    {
        TarifPolicyType? saved = null;
        _policyRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<TarifPolicyType>()))
            .Callback<TarifPolicyType>(p => saved = p);

        var handler = new TrfCreateTarifPolicyHandler(_policyRepoMock.Object);
        var response = await handler.Handle(
            new TrfCreateTarifPolicyCmd("SK-NEW", "New", new DateTime(2026, 7, 1), "", "user1"),
            CancellationToken.None);

        response.PolicyStatus.Should().Be(TarifPolicyStatus.Draft);
        saved.Should().NotBeNull();
        saved!.PolicyNo.Should().Be("SK-NEW");
        _policyRepoMock.Verify(x => x.SaveChanges(It.IsAny<TarifPolicyType>()), Times.Once);
    }

    [Fact]
    public async Task UT2_GivenDraftPolicy_WhenUpdateVariant_ThenSavesUpdatedPolicy()
    {
        var policy = CreateDraftWithOneVariant();
        SetupPolicyLoad(policy);
        TarifPolicyType? saved = null;
        _policyRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<TarifPolicyType>()))
            .Callback<TarifPolicyType>(p => saved = p);

        var handler = new TrfUpdateTarifPolicyVariantHandler(_policyRepoMock.Object);
        await handler.Handle(
            new TrfUpdateTarifPolicyVariantCmd(
                policy.TarifPolicyId,
                1,
                "T01",
                "K1",
                "01",
                [Komp("K1", 100m)],
                "user1"),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.Variants.Single().Nilai.Should().Be(100m);
    }

    [Fact]
    public async Task UT3_GivenDuplicateVariant_WhenAddVariant_ThenThrows()
    {
        var policy = CreateDraftWithOneVariant();
        SetupPolicyLoad(policy);

        var handler = new TrfAddTarifPolicyVariantHandler(_policyRepoMock.Object);
        var act = async () => await handler.Handle(
            new TrfAddTarifPolicyVariantCmd(
                policy.TarifPolicyId,
                "T01",
                "K1",
                "01",
                [Komp("K1", 50m)],
                "user1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*duplikat*");
    }

    [Fact]
    public async Task UT4_GivenSourcePolicy_WhenCopy_ThenSavesIndependentDraft()
    {
        var source = CreateDraftWithOneVariant();
        SetupPolicyLoad(source);
        TarifPolicyType? saved = null;
        _policyRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<TarifPolicyType>()))
            .Callback<TarifPolicyType>(p => saved = p);

        var handler = new TrfCopyTarifPolicyHandler(_policyRepoMock.Object);
        var response = await handler.Handle(
            new TrfCopyTarifPolicyCmd(source.TarifPolicyId, "SK-COPY", "Copy", "user2"),
            CancellationToken.None);

        response.TarifPolicyId.Should().NotBe(source.TarifPolicyId);
        response.PolicyStatus.Should().Be(TarifPolicyStatus.Draft);
        response.VariantCount.Should().Be(1);
        saved!.TarifPolicyId.Should().Be(response.TarifPolicyId);
    }

    [Fact]
    public async Task UT5_GivenDraftPolicy_WhenMassAdjust_ThenScalesNilai()
    {
        var policy = CreateDraftWithOneVariant();
        SetupPolicyLoad(policy);
        TarifPolicyType? saved = null;
        _policyRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<TarifPolicyType>()))
            .Callback<TarifPolicyType>(p => saved = p);

        var handler = new TrfMassAdjustTarifPolicyHandler(_policyRepoMock.Object);
        await handler.Handle(
            new TrfMassAdjustTarifPolicyCmd(
                policy.TarifPolicyId,
                "ALL",
                "PERCENTAGE",
                10m,
                "user1"),
            CancellationToken.None);

        saved!.Variants.Single().Nilai.Should().Be(110m);
    }

    [Fact]
    public async Task UT6_GivenPublishedPolicy_WhenUpdateMetadata_ThenThrows()
    {
        var published = CreateDraftWithOneVariant().MarkPublished("pub");
        SetupPolicyLoad(published);

        var handler = new TrfUpdateTarifPolicyHandler(_policyRepoMock.Object);
        var act = async () => await handler.Handle(
            new TrfUpdateTarifPolicyCmd(
                published.TarifPolicyId,
                "X",
                "Y",
                DateTime.Now,
                "",
                "user1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak diperbolehkan*");
    }

    [Fact]
    public async Task UT7_GivenDraftPolicy_WhenReview_ThenReviewed()
    {
        var policy = CreateDraftWithOneVariant();
        SetupPolicyLoad(policy);
        TarifPolicyType? saved = null;
        _policyRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<TarifPolicyType>()))
            .Callback<TarifPolicyType>(p => saved = p);

        var handler = new TrfReviewTarifPolicyHandler(_policyRepoMock.Object);
        var response = await handler.Handle(
            new TrfReviewTarifPolicyCmd(policy.TarifPolicyId, "supervisor"),
            CancellationToken.None);

        response.PolicyStatus.Should().Be(TarifPolicyStatus.Reviewed);
        saved!.PolicyStatus.Should().Be(TarifPolicyStatus.Reviewed);
    }

    [Fact]
    public async Task UT8_GivenPolicyWithLogs_WhenListPublishLog_ThenReturnsEntries()
    {
        var policy = CreateDraftWithOneVariant();
        SetupPolicyLoad(policy);

        var log = new TarifPublishLogType(
            "TPL001",
            policy.TarifPolicyId,
            "publisher",
            DateTime.Now,
            1,
            "note",
            [new TarifPublishLogDetailType(1, "T01", "K1", "01", "NT-001", 100m)]);

        _publishLogRepoMock
            .Setup(x => x.ListByPolicy(It.IsAny<ITarifPolicyKey>()))
            .Returns([log]);

        var handler = new TrfListTarifPolicyPublishLogHandler(
            _policyRepoMock.Object,
            _publishLogRepoMock.Object);

        var result = await handler.Handle(
            new TrfListTarifPolicyPublishLogQry(policy.TarifPolicyId),
            CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].PublishLogId.Should().Be("TPL001");
        result[0].Details.Should().ContainSingle(d => d.NilaiTarifId == "NT-001");
    }

    [Fact]
    public async Task UT9_GivenMissingPolicy_WhenGet_ThenThrowsKeyNotFound()
    {
        _policyRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifPolicyKey>()))
            .Returns(MayBe<TarifPolicyType>.None);

        var handler = new TrfGetTarifPolicyHandler(
            _policyRepoMock.Object,
            new Mock<IKomponenRepo>().Object);

        var act = async () => await handler.Handle(
            new TrfGetTarifPolicyQry("MISSING"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private void SetupPolicyLoad(TarifPolicyType policy)
    {
        _policyRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITarifPolicyKey>(k => k.TarifPolicyId == policy.TarifPolicyId)))
            .Returns(MayBe.From(policy));
    }
}
