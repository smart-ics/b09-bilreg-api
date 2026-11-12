using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Application.SalesContext.ResepFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.SalesContext.ResepFeature;

public class ResepGetHandlerTest
{
    private readonly Mock<IResepRepo> _resepRepoMock = new();

    private ResepGetHandler CreateHandler() => new(_resepRepoMock.Object);

    private static ResepModel CreateSampleResep(
        string resepId = "KP261012001",
        string voidReason = "-",
        bool isVoided = false)
    {
        var reg = new RegReff("RG00213695", "PAS001", "John Doe");
        var bodyMetric = new BodyMetricType(65m, 170m, 80m);
        var dokter = new DokterReff("DR01", "dr. Spesialis");
        var layanan = new LayananReff("2RJ01", "Poli Umum");
        var layananStok = new LayananReff("4FM01", "Farmasi Rawat Jalan");
        var urgenitas = UrgenitasType.Load("1", "CITO");
        var tipeBrg = new TipeBrgReff("01", "Obat");
        var created = new AuditInfoType("DR_TEST", new DateTime(2026, 10, 12, 8, 30, 0));
        var voided = isVoided
            ? new AuditInfoType("USER_VOID", new DateTime(2026, 10, 12, 11, 0, 0))
            : AuditInfoType.Default;
        var auditTrail = new AuditTrailType(created, AuditInfoType.Default, voided);

        var etiket1 = EtiketType.Load("3x1", "3 kali sehari 1 tablet sesudah makan", 3, 1m, "PO", "Sesudah Makan", "07:00, 13:00, 19:00", "Habiskan");
        var obat1 = new ResepObatType(
            1,
            new BrgReff("TESTBRG01", "Amoxicillin 500mg"),
            SatuanType.Create("TAB", "Tablet"),
            10,
            1,
            etiket1,
            false);

        var etiketRacik = EtiketType.Load("3x1", "3 kali sehari 1 bungkus sesudah makan", 3, 1m, "PO", "Sesudah Makan", "-", "-");
        var obatRacik = new ResepObatType(
            2,
            new BrgReff("RCK001", "Puyer Batuk Dewasa"),
            SatuanType.Create("BKS", "Bungkus"),
            10,
            0,
            etiketRacik,
            true);
        obatRacik.AddItemRacik(
            BrgObatType.Default with { BrgId = "BRG02", BrgName = "Paracetamol 500mg" },
            SatuanType.Create("TAB", "Tablet"),
            5,
            250,
            "250 mg");
        obatRacik.AddItemRacik(
            BrgObatType.Default with { BrgId = "BRG03", BrgName = "CTM 4mg" },
            SatuanType.Create("TAB", "Tablet"),
            2,
            2,
            "2 mg");

        return new ResepModel(
            resepId,
            reg,
            bodyMetric,
            dokter,
            layanan,
            layananStok,
            urgenitas,
            tipeBrg,
            2,
            "Catatan klinis resep",
            new DateTime(2026, 10, 15),
            "KP261001001",
            voidReason,
            auditTrail,
            [obat1, obatRacik]);
    }

    [Fact]
    public async Task GivenExistingResep_WhenHandle_ThenReturnsCompleteResepGetResponse()
    {
        // Arrange
        const string resepId = "KP261012001";
        var sampleResep = CreateSampleResep(resepId);
        _resepRepoMock.Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == resepId)))
            .Returns(MayBe.From(sampleResep));

        var handler = CreateHandler();
        var query = new ResepGetQuery(resepId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ResepId.Should().Be(resepId);
        result.RegId.Should().Be("RG00213695");
        result.PasienId.Should().Be("PAS001");
        result.PasienName.Should().Be("John Doe");
        result.BodyWeight.Should().Be(65m);
        result.BodyHeight.Should().Be(170m);
        result.LingkarPinggang.Should().Be(80m);
        result.DokterId.Should().Be("DR01");
        result.DokterName.Should().Be("dr. Spesialis");
        result.LayananId.Should().Be("2RJ01");
        result.LayananName.Should().Be("Poli Umum");
        result.LayananStokId.Should().Be("4FM01");
        result.LayananStokName.Should().Be("Farmasi Rawat Jalan");
        result.UrgenitasId.Should().Be("1");
        result.UrgenitasName.Should().Be("CITO");
        result.TipeBrgId.Should().Be("01");
        result.TipeBrgName.Should().Be("Obat");
        result.Iter.Should().Be(2);
        result.Description.Should().Be("Catatan klinis resep");
        result.TglRencanaPeriksa.Should().Be(new DateTime(2026, 10, 15));
        result.ResepAsalReffId.Should().Be("KP261001001");
        result.VoidReason.Should().Be("-");
        result.IsVoid.Should().BeFalse();

        // Items verification
        result.ListObat.Should().HaveCount(2);

        // Item 1 (Single medicine)
        var item1 = result.ListObat[0];
        item1.NoUrut.Should().Be(1);
        item1.BrgId.Should().Be("TESTBRG01");
        item1.BrgName.Should().Be("Amoxicillin 500mg");
        item1.SatuanId.Should().Be("TAB");
        item1.SatuanName.Should().Be("Tablet");
        item1.Qty.Should().Be(10m);
        item1.Iter.Should().Be(1);
        item1.Signa.Should().Be("3x1");
        item1.Instruction.Should().Be("3 kali sehari 1 tablet sesudah makan");
        item1.Frequency.Should().Be(3);
        item1.UnitDose.Should().Be(1m);
        item1.Route.Should().Be("PO");
        item1.MealRelation.Should().Be("Sesudah Makan");
        item1.UddTime.Should().Be("07:00, 13:00, 19:00");
        item1.Note.Should().Be("Habiskan");
        item1.IsRacik.Should().BeFalse();
        item1.ListItemRacik.Should().BeEmpty();

        // Item 2 (Racikan)
        var item2 = result.ListObat[1];
        item2.NoUrut.Should().Be(2);
        item2.BrgId.Should().Be("RCK001");
        item2.BrgName.Should().Be("Puyer Batuk Dewasa");
        item2.SatuanId.Should().Be("BKS");
        item2.SatuanName.Should().Be("Bungkus");
        item2.Qty.Should().Be(10m);
        item2.Iter.Should().Be(0);
        item2.Frequency.Should().Be(3);
        item2.UnitDose.Should().Be(1m);
        item2.Route.Should().Be("PO");
        item2.MealRelation.Should().Be("Sesudah Makan");
        item2.IsRacik.Should().BeTrue();
        item2.ListItemRacik.Should().HaveCount(2);

        var rck1 = item2.ListItemRacik[0];
        rck1.NoUrut.Should().Be(1);
        rck1.BrgId.Should().Be("BRG02");
        rck1.BrgName.Should().Be("Paracetamol 500mg");
        rck1.SatuanId.Should().Be("TAB");
        rck1.Qty.Should().Be(5m);
        rck1.Dosis.Should().Be(250m);
        rck1.DosisTxt.Should().Be("250 mg");

        var rck2 = item2.ListItemRacik[1];
        rck2.NoUrut.Should().Be(2);
        rck2.BrgId.Should().Be("BRG03");
        rck2.BrgName.Should().Be("CTM 4mg");
        rck2.SatuanId.Should().Be("TAB");
        rck2.Qty.Should().Be(2m);
        rck2.Dosis.Should().Be(2m);
        rck2.DosisTxt.Should().Be("2 mg");
    }

    [Fact]
    public async Task GivenNonExistentResepId_WhenHandle_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        const string nonExistentId = "KP999999999";
        _resepRepoMock.Setup(x => x.LoadEntity(It.IsAny<IResepKey>()))
            .Returns(MayBe<ResepModel>.None);

        var handler = CreateHandler();
        var query = new ResepGetQuery(nonExistentId);

        // Act
        var act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*Resep with Id: {nonExistentId} not found.*");
    }

    [Fact]
    public async Task GivenVoidedResep_WhenHandle_ThenReturnsResepWithVoidStatusAndReason()
    {
        // Arrange
        const string resepId = "KP261012999";
        const string voidReason = "Salah dosis oleh DPJP";
        var sampleResep = CreateSampleResep(resepId, voidReason, isVoided: true);
        _resepRepoMock.Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == resepId)))
            .Returns(MayBe.From(sampleResep));

        var handler = CreateHandler();
        var query = new ResepGetQuery(resepId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ResepId.Should().Be(resepId);
        result.IsVoid.Should().BeTrue();
        result.VoidReason.Should().Be(voidReason);
    }
}
