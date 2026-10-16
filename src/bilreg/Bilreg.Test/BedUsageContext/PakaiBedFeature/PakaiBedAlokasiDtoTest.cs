using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;
using FluentAssertions;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedAlokasiDtoTest
{
    private static readonly DateTime ProposedAt = new(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FromModelToModel_ActiveAllocation_PreservesAggregateAndFacts()
    {
        var source = CreateActive();

        var dto = PakaiBedAlokasiDto.FromModel(source);
        var result = dto.ToModel(
            source.ListTransition.Select(PakaiBedTransisiDto.FromModel).Select(x => x.ToModel()),
            source.ListCorrection.Select(PakaiBedKoreksiDto.FromModel).Select(x => x.ToModel()));

        result.Should().BeEquivalentTo(source);
        result.StartedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        result.ListTransition.Should().OnlyContain(x => x.OccurredAt.Kind == DateTimeKind.Utc);
    }

    [Fact]
    public void ToModel_EmptyStartedAtSentinel_ReturnsNull()
    {
        var source = PakaiBedAlokasiModel.Propose(
            "REG-1", "P-1", "BG", "KMR-1", "BED-1", "WTL-1", "REQ-1",
            PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
            "USR-1", ProposedAt, ProposedAt.AddMinutes(1), "Proposal");

        var result = PakaiBedAlokasiDto.FromModel(source).ToModel([], []);

        result.StartedAt.Should().BeNull();
    }

    [Fact]
    public void FromModelToModel_DualClassTitipKelas_PreservesFields()
    {
        var source = PakaiBedAlokasiModel.Propose(
            "REG-1", "P-1", "BG", "KMR-1", "BED-1", "WTL-1", "REQ-1",
            PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
            "USR-1", ProposedAt, ProposedAt.AddMinutes(1), "Proposal",
            physicalKelasId: "001",
            billingKelasId: "002",
            tipeKamarId: "1",
            tarif: 500000m,
            isTitipKelas: true,
            isNaikKelas: false);

        var dto = PakaiBedAlokasiDto.FromModel(source);

        dto.PhysicalKelasId.Should().Be("001");
        dto.BillingKelasId.Should().Be("002");
        dto.TipeKamarId.Should().Be("1");
        dto.Tarif.Should().Be(500000m);
        dto.IsTitipKelas.Should().BeTrue();
        dto.IsNaikKelas.Should().BeFalse();
        dto.MotherPakaiBedId.Should().BeEmpty();

        var result = dto.ToModel([], []);
        result.PhysicalKelasId.Should().Be("001");
        result.BillingKelasId.Should().Be("002");
        result.TipeKamarId.Should().Be("1");
        result.Tarif.Should().Be(500000m);
        result.IsTitipKelas.Should().BeTrue();
        result.IsNaikKelas.Should().BeFalse();
    }

    [Fact]
    public void FromModelToModel_DualClassNaikKelas_PreservesFields()
    {
        var source = PakaiBedAlokasiModel.Propose(
            "REG-1", "P-1", "BG", "KMR-1", "BED-1", "WTL-1", "REQ-1",
            PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
            "USR-1", ProposedAt, ProposedAt.AddMinutes(1), "Proposal",
            physicalKelasId: "001",
            billingKelasId: "003",
            tipeKamarId: "2",
            tarif: 750000m,
            isTitipKelas: false,
            isNaikKelas: true);

        var dto = PakaiBedAlokasiDto.FromModel(source);
        dto.PhysicalKelasId.Should().Be("001");
        dto.BillingKelasId.Should().Be("003");
        dto.TipeKamarId.Should().Be("2");
        dto.Tarif.Should().Be(750000m);
        dto.IsTitipKelas.Should().BeFalse();
        dto.IsNaikKelas.Should().BeTrue();

        var result = dto.ToModel(
            source.ListTransition.Select(PakaiBedTransisiDto.FromModel).Select(x => x.ToModel()),
            source.ListCorrection.Select(PakaiBedKoreksiDto.FromModel).Select(x => x.ToModel()));
        result.Should().BeEquivalentTo(source);
    }

    [Fact]
    public void FromModelToModel_RoomingInBaby_PreservesMotherPakaiBedIdAndZeroTarif()
    {
        var source = PakaiBedAlokasiModel.Propose(
            "REG-BABY", "P-BABY", "BG", "KMR-1", "BED-1", "WTL-BABY", "REQ-BABY",
            PakaiBedPurposeEnum.RoomingIn, OccupantRoleEnum.Associated,
            "USR-1", ProposedAt, ProposedAt.AddMinutes(1), "Rooming-in Proposal",
            physicalKelasId: "001",
            billingKelasId: "001",
            tipeKamarId: "1",
            tarif: 0m,
            motherPakaiBedId: "PKB-MOM-001");

        var dto = PakaiBedAlokasiDto.FromModel(source);
        dto.MotherPakaiBedId.Should().Be("PKB-MOM-001");
        dto.Tarif.Should().Be(0m);
        dto.IsTitipKelas.Should().BeFalse();
        dto.IsNaikKelas.Should().BeFalse();

        var result = dto.ToModel([], []);
        result.MotherPakaiBedId.Should().Be("PKB-MOM-001");
        result.Tarif.Should().Be(0m);
        result.IsRoomingIn.Should().BeTrue();
    }

    private static PakaiBedAlokasiModel CreateActive() =>
        PakaiBedAlokasiModel.Propose(
                "REG-1", "P-1", "BG", "KMR-1", "BED-1", "WTL-1", "REQ-1",
                PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
                "USR-1", ProposedAt, ProposedAt.AddMinutes(1), "Proposal",
                physicalKelasId: "001",
                billingKelasId: "001",
                tipeKamarId: "1",
                tarif: 350000m)
            .Assign("USR-2", ProposedAt.AddMinutes(5), ProposedAt.AddMinutes(6),
                "Assignment", "EVIDENCE-1");
}
