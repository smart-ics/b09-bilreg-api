using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.SalesContext.ResepFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.SalesContext.ResepFeature;

[Collection("ResepDatabaseTests")]
public class ResepRepoTest
{
    private readonly ResepRepo _sut;
    private readonly IOptions<DatabaseOptions> _dbOptions;

    public ResepRepoTest()
    {
        _dbOptions = Options.Create(new DatabaseOptions
        {
            ServerName = "dev.smart-ics.com",
            DbName = "HOSPITAL_PKL"
        });
        var resepDal = new ResepDal(_dbOptions);
        var resepBrgDal = new ResepBrgDal(_dbOptions);
        _sut = new ResepRepo(resepDal, resepBrgDal);
    }

    private static string NewTestResepId() => $"KPT{Random.Shared.Next(1000000, 9999999)}";

    private static ResepModel BuildTestResep(string resepId)
    {
        var reg = new RegReff("RG00213695", "337502200016068", "TANAAH");
        var bodyMetric = new BodyMetricType(60m, 165m, 75m);
        var dokter = new DokterReff("DR01", "Dr Spesialis");
        var layanan = new LayananReff("2RJ01", "Poli Umum");
        var layananStok = new LayananReff("4FM01", "Farmasi Rawat Jalan");
        var urgenitas = UrgenitasType.Load("1", "CITO");
        var tipeBrg = new TipeBrgReff("01", "Obat");
        var created = new AuditInfoType("TESTUSR", new DateTime(2026, 10, 12, 10, 30, 0));
        var auditTrail = new AuditTrailType(created, AuditInfoType.Default, AuditInfoType.Default);

        var etiket1 = EtiketType.Load("3x1", "Diminum sesudah makan", 3, 1, "PO", "Sesudah Makan", "07:00, 13:00, 19:00", "-");
        var obat1 = new ResepObatType(
            1,
            new BrgReff("TESTBRG01", "Amoxicillin 500mg"),
            SatuanType.Create("TAB", "Tablet"),
            10,
            1,
            etiket1,
            false);

        var etiketRacik = EtiketType.Load("3x1", "Diminum sesudah makan", 3, 1, "PO", "Sesudah Makan", "-", "-");
        var obatRacik = new ResepObatType(
            2,
            new BrgReff("RCK001", "Racikan Batuk"),
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
            1,
            "Resep Rawat Jalan",
            new DateTime(2026, 10, 15),
            "KP00000001",
            "-",
            auditTrail,
            [obat1, obatRacik]);
    }

    [Fact]
    public void SaveChanges_NewResep_And_LoadEntity_RoundTripsAggregateSuccessfully()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();
        var model = BuildTestResep(resepId);

        _sut.SaveChanges(model);
        var maybeActual = _sut.LoadEntity(ResepModel.Key(resepId));

        maybeActual.HasValue.Should().BeTrue();
        maybeActual.Match(
            onSome: actual =>
            {
                actual.ResepId.Should().Be(resepId);
                actual.Register.RegId.Should().Be("RG00213695");
                actual.Layanan.LayananId.Should().Be("2RJ01");
                actual.LayananStok.LayananId.Should().Be("4FM01");
                actual.Dokter.DokterId.Should().Be("DR01");
                actual.Urgenitas.UrgenitasId.Should().Be("1");
                actual.TglRencanaPeriksa.Date.Should().Be(new DateTime(2026, 10, 15));
                actual.ResepAsalReffId.Should().Be("KP00000001");
                actual.Iter.Should().Be(1);
                actual.Description.Should().Be("Resep Rawat Jalan");
                actual.IsVoid.Should().BeFalse();

                actual.ListObat.Should().HaveCount(2);

                var nonRacik = actual.ListObat.First(x => !x.IsRacik);
                nonRacik.Brg.BrgId.Should().Be("TESTBRG01");
                nonRacik.Qty.Should().Be(10);
                nonRacik.Satuan.SatuanId.Should().Be("TAB");
                nonRacik.Etiket.Route.Should().Be("PO");
                nonRacik.Etiket.MealRelation.Should().Be("Sesudah Makan");
                nonRacik.Etiket.UddTime.Should().Be("07:00, 13:00, 19:00");
                nonRacik.Etiket.Frequency.Should().Be(3);
                nonRacik.Etiket.UnitDose.Should().Be(1);

                var racik = actual.ListObat.First(x => x.IsRacik);
                racik.Brg.BrgId.Should().Be("RCK001");
                racik.Qty.Should().Be(10);
                racik.ListItemRacik.Should().HaveCount(2);

                var rComp1 = racik.ListItemRacik.First(x => x.Brg.BrgId == "BRG02");
                rComp1.Qty.Should().Be(5);
                rComp1.Dosis.Should().Be(250);
                rComp1.DosisTxt.Should().Be("250 mg");

                var rComp2 = racik.ListItemRacik.First(x => x.Brg.BrgId == "BRG03");
                rComp2.Qty.Should().Be(2);
                rComp2.Dosis.Should().Be(2);
                rComp2.DosisTxt.Should().Be("2 mg");
            },
            onNone: () => Assert.Fail("Expected aggregate to load successfully"));
    }

    [Fact]
    public void SaveChanges_ExistingResep_UpdatesAggregateAndVoidStamp()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();
        var model = BuildTestResep(resepId);
        _sut.SaveChanges(model);

        model.Void("VOIDUSR", "Perubahan terapi DPJP", new DateTime(2026, 10, 12, 11, 0, 0));
        _sut.SaveChanges(model);

        var maybeActual = _sut.LoadEntity(ResepModel.Key(resepId));
        maybeActual.HasValue.Should().BeTrue();
        maybeActual.Match(
            onSome: actual =>
            {
                actual.IsVoid.Should().BeTrue();
                actual.VoidReason.Should().Be("Perubahan terapi DPJP");
                actual.AuditTrail.Voided.UserId.Should().Be("VOIDUSR");
                actual.Description.Should().Be("Resep Rawat Jalan");
            },
            onNone: () => Assert.Fail("Expected aggregate to load successfully"));
    }

    [Fact]
    public void DeleteEntity_RemovesAggregateAndChildItems()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();
        var model = BuildTestResep(resepId);
        _sut.SaveChanges(model);

        _sut.DeleteEntity(ResepModel.Key(resepId));

        var maybeActual = _sut.LoadEntity(ResepModel.Key(resepId));
        maybeActual.HasValue.Should().BeFalse();
    }

    [Fact]
    public void ListData_ReturnsResepAggregateListForReg()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();
        var model = BuildTestResep(resepId);
        _sut.SaveChanges(model);

        var list = _sut.ListData(RegModel.Key("RG00213695")).ToList();

        list.Should().Contain(x => x.ResepId == resepId);
        var loaded = list.First(x => x.ResepId == resepId);
        loaded.ListObat.Should().HaveCount(2);
    }

    [Fact]
    public void LoadEntity_NonExistentResep_ReturnsNone()
    {
        using var trans = TransHelper.NewScope();
        var result = _sut.LoadEntity(ResepModel.Key("NON_EXIST_999"));

        result.HasValue.Should().BeFalse();
    }
}
