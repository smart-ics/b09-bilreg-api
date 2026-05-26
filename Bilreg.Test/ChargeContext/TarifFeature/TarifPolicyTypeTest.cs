using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class TarifPolicyTypeTest
{
    private static TarifVariantKomponenType Line(int noUrut, decimal nilai) =>
        new(noUrut, KomponenType.Default.ToReff(), nilai);

    private static TarifPolicyType CreateDraftWithOneVariant()
    {
        var policy = TarifPolicyType.Create("SK-001", "Policy Test", new DateTime(2026, 6, 1), "desc", "user1");
        return policy.AddVariant("T01", "K1", "01", 100m, [Line(1, 60m), Line(2, 40m)]);
    }

    [Fact]
    public void DT1_GivenCreate_WhenCalled_ThenDraftWithEmptyVariants()
    {
        var policy = TarifPolicyType.Create("SK-NEW", "New Policy", new DateTime(2026, 7, 1), "", "user1");

        policy.PolicyStatus.Should().Be(TarifPolicyStatus.Draft);
        policy.TarifPolicyId.Should().NotBeNullOrWhiteSpace();
        policy.TarifPolicyId.Length.Should().BeLessOrEqualTo(12);
        policy.Variants.Should().BeEmpty();
    }

    [Fact]
    public void DT2_GivenDuplicateVariant_WhenAddVariant_ThenThrows()
    {
        var policy = CreateDraftWithOneVariant();

        var act = () => policy.AddVariant("T01", "K1", "01", 50m, [Line(1, 50m)]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*duplikat*");
    }

    [Fact]
    public void DT3_GivenEmptyPolicy_WhenValidateForPublish_ThenThrows()
    {
        var policy = TarifPolicyType.Create("SK-EMPTY", "Empty", DateTime.Now, "", "user1");

        var act = () => policy.ValidateForPublish();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak memiliki variant*");
    }

    [Fact]
    public void DT4_GivenVariantWithBadKomponenSum_WhenConstructed_ThenThrows()
    {
        var komponen = new TarifVariantKomponenType(1, KomponenType.Default.ToReff(), 50m);

        var act = () => new TarifVariantType("POL001", 1, "T01", "K1", "01", 100m, "", [komponen]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*jumlah komponen*");
    }

    [Fact]
    public void DT5_GivenVariantWithNoKomponen_WhenValidateForPublish_ThenThrows()
    {
        var variant = new TarifVariantType("POL001", 1, "T01", "K1", "01", 0m, "", []);
        var policy = new TarifPolicyType(
            "POL001", "SK", "Name", DateTime.Now, "",
            TarifPolicyStatus.Draft,
            AuditTrailType.Create("u", DateTime.Now),
            [variant]);

        var act = () => policy.ValidateForPublish();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*minimal satu baris komponen*");
    }

    [Fact]
    public void DT6_GivenDraft_WhenMarkReviewed_ThenReviewed()
    {
        var policy = CreateDraftWithOneVariant();

        var reviewed = policy.MarkReviewed("supervisor");

        reviewed.PolicyStatus.Should().Be(TarifPolicyStatus.Reviewed);
    }

    [Fact]
    public void DT7_GivenReviewed_WhenMarkReviewed_ThenThrows()
    {
        var policy = CreateDraftWithOneVariant().MarkReviewed("supervisor");

        var act = () => policy.MarkReviewed("supervisor");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*harus Draft*");
    }

    [Fact]
    public void DT8_GivenPublished_WhenAddVariant_ThenThrows()
    {
        var published = CreateDraftWithOneVariant().MarkPublished("pub");

        var act = () => published.AddVariant("T02", "K2", "02", 10m, [Line(1, 10m)]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak diperbolehkan*");
    }

    [Fact]
    public void DT9_GivenPublished_WhenMassAdjust_ThenThrows()
    {
        var published = CreateDraftWithOneVariant().MarkPublished("pub");

        var act = () => published.MassAdjust(10m, "user1");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DT10_GivenSourcePolicy_WhenCopyFrom_ThenIndependentDraft()
    {
        var publishedVariant = new TarifVariantType(
            "POL001", 1, "T01", "K1", "01", 100m, "NT-OLD",
            [Line(1, 60m), Line(2, 40m)]);
        var source = new TarifPolicyType(
            "POL001", "SK-001", "Source", new DateTime(2026, 6, 1), "",
            TarifPolicyStatus.Published,
            AuditTrailType.Create("pub", DateTime.Now),
            [publishedVariant]);

        var copy = TarifPolicyType.CopyFrom(source, "SK-COPY", "Copied Policy", "user2");

        copy.TarifPolicyId.Should().NotBe(source.TarifPolicyId);
        copy.PolicyStatus.Should().Be(TarifPolicyStatus.Draft);
        copy.Variants.Should().HaveCount(1);
        copy.Variants.Single().PublishedNilaiTarifId.Should().BeEmpty();
        copy.Variants.Single().Nilai.Should().Be(100m);
    }

    [Fact]
    public void DT11_GivenValidDraft_WhenMarkPublished_ThenPublished()
    {
        var policy = CreateDraftWithOneVariant();

        var published = policy.MarkPublished("publisher");

        published.PolicyStatus.Should().Be(TarifPolicyStatus.Published);
    }

    [Fact]
    public void DT12_GivenDraft_WhenMassAdjust_ThenScalesVariantNilai()
    {
        var policy = CreateDraftWithOneVariant();

        var adjusted = policy.MassAdjust(10m, "user1");

        var variant = adjusted.Variants.Single();
        variant.Nilai.Should().Be(110m);
        variant.ListKomponen.Sum(x => x.Nilai).Should().Be(110m);
    }

    [Fact]
    public void DT13_GivenVariant_WhenSetKomponenLines_ThenHeaderMatchesSum()
    {
        var variant = TarifVariantType.Create("POL001", 1, "T01", "K1", "01", 100m, [Line(1, 100m)]);

        var updated = variant.SetKomponenLines([Line(1, 70m), Line(2, 30m)]);

        updated.Nilai.Should().Be(100m);
        updated.ListKomponen.Should().HaveCount(2);
    }

    [Fact]
    public void DT14_GivenVariant_WhenToPublishedSnapshot_ThenSetsNilaiTarifId()
    {
        var variant = TarifVariantType.Create("POL001", 1, "T01", "K1", "01", 100m, [Line(1, 100m)]);

        var snapshot = variant.ToPublishedSnapshot("01AR00000000000000000042");

        snapshot.PublishedNilaiTarifId.Should().Be("01AR00000000000000000042");
    }

    [Fact]
    public void DT15_GivenPublishedPolicy_WhenValidateForRepublish_ThenSucceeds()
    {
        var published = CreateDraftWithOneVariant().MarkPublished("pub");

        var act = () => published.ValidateForRepublish();

        act.Should().NotThrow();
    }

    [Fact]
    public void DT16_GivenDraft_WhenValidateForRepublish_ThenThrows()
    {
        var policy = CreateDraftWithOneVariant();

        var act = () => policy.ValidateForRepublish();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*hanya dapat di-republish*");
    }
}
