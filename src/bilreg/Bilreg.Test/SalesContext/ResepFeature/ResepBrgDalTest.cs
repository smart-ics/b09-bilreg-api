using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Infrastructure.SalesContext.ResepFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.SalesContext.ResepFeature;

[Collection("ResepDatabaseTests")]
public class ResepBrgDalTest
{
    private readonly ResepBrgDal _sut;
    private readonly IOptions<DatabaseOptions> _dbOptions;

    public ResepBrgDalTest()
    {
        _dbOptions = Options.Create(new DatabaseOptions
        {
            ServerName = "dev.smart-ics.com",
            DbName = "HOSPITAL_PKL"
        });
        _sut = new ResepBrgDal(_dbOptions);
    }

    private static string NewTestResepId() => $"KPT{Random.Shared.Next(1000000, 9999999)}";

    [Fact]
    public void Insert_And_ListData_SingleMedication_PersistsAllColumnsCorrectly()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();

        var item = new ResepBrgDto(
            ResepId: resepId,
            NoUrut: 1,
            BrgId: "TESTBRG01",
            BrgName: "Amoxicillin 500mg Test",
            Qty: 10,
            SatuanId: "TAB",
            Iter: 1,
            IsRacik: false,
            IsKomponen: false,
            RacikId: string.Empty,
            Dosis: 0,
            DosisTxt: string.Empty,
            Signa: "3x1",
            Etiket: "Diminum sesudah makan",
            Frequency: 3,
            UnitDose: 1,
            SatuanName: "Tablet",
            Route: "PO",
            MealRelation: "Sesudah Makan",
            UddTime: "07:00, 13:00, 19:00",
            IsVoid: false);

        _sut.Insert([item]);
        var actual = _sut.ListData(ResepModel.Key(resepId)).ToList();

        actual.Should().HaveCount(1);
        var read = actual[0];
        read.ResepId.Should().Be(resepId);
        read.NoUrut.Should().Be(1);
        read.BrgId.Should().Be("TESTBRG01");
        read.BrgName.Should().Be("Amoxicillin 500mg Test");
        read.Qty.Should().Be(10);
        read.SatuanId.Should().Be("TAB");
        read.Iter.Should().Be(1);
        read.IsRacik.Should().BeFalse();
        read.IsKomponen.Should().BeFalse();
        read.RacikId.Should().BeEmpty();
        read.Signa.Should().Be("3x1");
        read.Etiket.Should().Be("Diminum sesudah makan");
        read.Frequency.Should().Be(3);
        read.UnitDose.Should().Be(1);
        read.Route.Should().Be("PO");
        read.MealRelation.Should().Be("Sesudah Makan");
        read.UddTime.Should().Be("07:00, 13:00, 19:00");
        read.IsVoid.Should().BeFalse();
    }

    [Fact]
    public void Insert_And_ListData_RacikanWithComponents_PersistsHierarchy()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();

        var racikParent = new ResepBrgDto(
            ResepId: resepId,
            NoUrut: 1,
            BrgId: "RCK001",
            BrgName: "Racikan Batuk Pilek",
            Qty: 10,
            SatuanId: "BKS",
            Iter: 0,
            IsRacik: true,
            IsKomponen: false,
            RacikId: string.Empty,
            Dosis: 0,
            DosisTxt: string.Empty,
            Signa: "3x1 bungkus",
            Etiket: "Sesudah makan",
            Frequency: 3,
            UnitDose: 1,
            SatuanName: "Bungkus",
            Route: "PO",
            MealRelation: "Sesudah Makan",
            UddTime: string.Empty,
            IsVoid: false);

        var comp1 = new ResepBrgDto(
            ResepId: resepId,
            NoUrut: 2,
            BrgId: "BRG_PARACETAMOL",
            BrgName: "   Paracetamol 500mg",
            Qty: 5,
            SatuanId: "TAB",
            Iter: 0,
            IsRacik: false,
            IsKomponen: true,
            RacikId: "RCK001",
            Dosis: 250,
            DosisTxt: "250 mg",
            Signa: string.Empty,
            Etiket: string.Empty,
            Frequency: 0,
            UnitDose: 0,
            SatuanName: "Tablet",
            Route: string.Empty,
            MealRelation: string.Empty,
            UddTime: string.Empty,
            IsVoid: false);

        var comp2 = new ResepBrgDto(
            ResepId: resepId,
            NoUrut: 3,
            BrgId: "BRG_CTM",
            BrgName: "   CTM 4mg",
            Qty: 2,
            SatuanId: "TAB",
            Iter: 0,
            IsRacik: false,
            IsKomponen: true,
            RacikId: "RCK001",
            Dosis: 2,
            DosisTxt: "2 mg",
            Signa: string.Empty,
            Etiket: string.Empty,
            Frequency: 0,
            UnitDose: 0,
            SatuanName: "Tablet",
            Route: string.Empty,
            MealRelation: string.Empty,
            UddTime: string.Empty,
            IsVoid: false);

        _sut.Insert([racikParent, comp1, comp2]);
        var actual = _sut.ListData(ResepModel.Key(resepId)).ToList();

        actual.Should().HaveCount(3);

        var p = actual[0];
        p.NoUrut.Should().Be(1);
        p.IsRacik.Should().BeTrue();
        p.IsKomponen.Should().BeFalse();
        p.BrgId.Should().Be("RCK001");
        p.Route.Should().Be("PO");

        var c1 = actual[1];
        c1.NoUrut.Should().Be(2);
        c1.IsRacik.Should().BeFalse();
        c1.IsKomponen.Should().BeTrue();
        c1.RacikId.Should().Be("RCK001");
        c1.Dosis.Should().Be(250);
        c1.DosisTxt.Should().Be("250 mg");

        var c2 = actual[2];
        c2.NoUrut.Should().Be(3);
        c2.IsRacik.Should().BeFalse();
        c2.IsKomponen.Should().BeTrue();
        c2.RacikId.Should().Be("RCK001");
        c2.Dosis.Should().Be(2);
        c2.DosisTxt.Should().Be("2 mg");
    }

    [Fact]
    public void Delete_RemovesAllItemsForResepId()
    {
        using var trans = TransHelper.NewScope();
        var resepId = NewTestResepId();

        var item = new ResepBrgDto(
            ResepId: resepId,
            NoUrut: 1,
            BrgId: "TESTBRG01",
            BrgName: "Test Drug",
            Qty: 10,
            SatuanId: "TAB",
            Iter: 0,
            IsRacik: false,
            IsKomponen: false,
            RacikId: string.Empty,
            Dosis: 0,
            DosisTxt: string.Empty,
            Signa: "1x1",
            Etiket: "Pagi",
            Frequency: 1,
            UnitDose: 1,
            SatuanName: "Tablet");

        _sut.Insert([item]);
        _sut.ListData(ResepModel.Key(resepId)).Should().HaveCount(1);

        _sut.Delete(ResepModel.Key(resepId));
        _sut.ListData(ResepModel.Key(resepId)).Should().BeEmpty();
    }

    [Fact]
    public void Insert_EmptyList_DoesNotThrow()
    {
        using var trans = TransHelper.NewScope();
        var act = () => _sut.Insert([]);
        act.Should().NotThrow();
    }
}
