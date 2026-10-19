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
using Xunit;

namespace Bilreg.Test.SalesContext.ResepFeature;

public class ResepListByRegHandlerTest
{
    private readonly Mock<IResepRepo> _resepRepoMock = new();

    private ResepListByRegHandler CreateHandler() => new(_resepRepoMock.Object);

    private static ResepModel CreateSampleResep(
        string resepId,
        string regId = "RG00213695",
        bool isVoided = false,
        string voidReason = "-")
    {
        var reg = new RegReff(regId, "PAS001", "John Doe");
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

        var etiket = EtiketType.Load("3x1", "3 kali sehari 1 tablet sesudah makan", 3, 1m, "PO", "Sesudah Makan", "07:00, 13:00, 19:00", "Habiskan");
        var obat = new ResepObatType(
            1,
            new BrgReff("TESTBRG01", "Amoxicillin 500mg"),
            SatuanType.Create("TAB", "Tablet"),
            10,
            1,
            etiket,
            false);

        return new ResepModel(
            resepId,
            reg,
            bodyMetric,
            dokter,
            layanan,
            layananStok,
            urgenitas,
            tipeBrg,
            1,
            "Resep Catatan",
            new DateTime(2026, 10, 15),
            "-",
            voidReason,
            auditTrail,
            [obat]);
    }

    [Fact]
    public async Task GivenValidRegId_WhenHandle_ThenReturnsActivePrescriptionsWithFullHeaderAndItemSummary()
    {
        // Arrange
        const string regId = "RG00213695";
        var resep = CreateSampleResep("KP261012001", regId);
        _resepRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns([resep]);

        var handler = CreateHandler();
        var query = new ResepListByRegQuery(regId);

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        var item = result[0];
        item.ResepId.Should().Be("KP261012001");
        item.RegId.Should().Be(regId);
        item.PasienId.Should().Be("PAS001");
        item.PasienName.Should().Be("John Doe");
        item.DokterId.Should().Be("DR01");
        item.DokterName.Should().Be("dr. Spesialis");
        item.LayananId.Should().Be("2RJ01");
        item.LayananName.Should().Be("Poli Umum");
        item.LayananStokId.Should().Be("4FM01");
        item.LayananStokName.Should().Be("Farmasi Rawat Jalan");
        item.UrgenitasId.Should().Be("1");
        item.UrgenitasName.Should().Be("CITO");
        item.TipeBrgId.Should().Be("01");
        item.TipeBrgName.Should().Be("Obat");
        item.Iter.Should().Be(1);
        item.Description.Should().Be("Resep Catatan");
        item.TglRencanaPeriksa.Should().Be(new DateTime(2026, 10, 15));
        item.IsVoid.Should().BeFalse();
        item.RingkasanItem.Should().Be("Amoxicillin 500mg (10 Tablet)");

        // Item level verification
        item.ListObat.Should().HaveCount(1);
        var obat = item.ListObat[0];
        obat.NoUrut.Should().Be(1);
        obat.BrgId.Should().Be("TESTBRG01");
        obat.BrgName.Should().Be("Amoxicillin 500mg");
        obat.SatuanId.Should().Be("TAB");
        obat.SatuanName.Should().Be("Tablet");
        obat.Qty.Should().Be(10m);
        obat.Iter.Should().Be(1);
        obat.Frequency.Should().Be(3);
        obat.UnitDose.Should().Be(1m);
        obat.Route.Should().Be("PO");
        obat.MealRelation.Should().Be("Sesudah Makan");
        obat.UddTime.Should().Be("07:00, 13:00, 19:00");
        obat.IsRacik.Should().BeFalse();
    }

    [Fact]
    public async Task GivenRegWithActiveAndVoidResep_WhenActiveOnlyDefault_ThenFiltersOutVoidResep()
    {
        // Arrange
        const string regId = "RG00213695";
        var activeResep = CreateSampleResep("KP001", regId, isVoided: false);
        var voidResep = CreateSampleResep("KP002", regId, isVoided: true, voidReason: "Batal");
        _resepRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns([activeResep, voidResep]);

        var handler = CreateHandler();
        var query = new ResepListByRegQuery(regId);

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].ResepId.Should().Be("KP001");
        result[0].IsVoid.Should().BeFalse();
    }

    [Fact]
    public async Task GivenRegWithActiveAndVoidResep_WhenActiveOnlyFalse_ThenReturnsAllResepIncludingVoid()
    {
        // Arrange
        const string regId = "RG00213695";
        var activeResep = CreateSampleResep("KP001", regId, isVoided: false);
        var voidResep = CreateSampleResep("KP002", regId, isVoided: true, voidReason: "Batal");
        _resepRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns([activeResep, voidResep]);

        var handler = CreateHandler();
        var query = new ResepListByRegQuery(regId, ActiveOnly: false);

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(x => x.ResepId == "KP001" && !x.IsVoid);
        result.Should().Contain(x => x.ResepId == "KP002" && x.IsVoid);
    }

    [Fact]
    public async Task GivenRegWithNoResep_WhenHandle_ThenReturnsEmptyList()
    {
        // Arrange
        const string regId = "RG_EMPTY";
        _resepRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns([]);

        var handler = CreateHandler();
        var query = new ResepListByRegQuery(regId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
