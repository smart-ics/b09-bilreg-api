using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.BedUsageContext.BhpRanapFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.BhpRanapFeature;

public class BhpRanapDalTest
{
    private readonly BhpRanapDal _sut = new(ConnStringHelper.GetTestEnv());

    private static BhpRanapDto Faker(string id = "TBHR0000000001")
        => new(
            BhpRanapId: id,
            PakaiBedId: "BED001",
            RegId: "REG001",
            LayananId: "3RI02",
            BhpItemId: "BH001",
            BhpItemName: "Spuit 10cc Disposible",
            Qty: 2,
            Price: 7500m,
            CrtUser: "SPR001",
            CrtDate: new DateTime(2026, 2, 11, 10, 0, 0),
            VodUser: string.Empty,
            VodDate: new DateTime(3000, 1, 1),
            VodKet: string.Empty);

    [Fact]
    public void InsertAndGetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker("TBHR_TEST_01");
        _sut.Insert(dto);

        var loaded = _sut.GetData(BhpRanapModel.Key("TBHR_TEST_01"));
        loaded.Should().NotBeNull();
        loaded.BhpRanapId.Should().Be("TBHR_TEST_01");
        loaded.BhpItemName.Should().Be("Spuit 10cc Disposible");
        loaded.Qty.Should().Be(2);
        loaded.Price.Should().Be(7500m);
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker("TBHR_TEST_02");
        _sut.Insert(dto);

        var updated = dto with { Qty = 5, Price = 8000m, VodUser = "SPR002", VodKet = "Batal sebagian" };
        _sut.Update(updated);

        var loaded = _sut.GetData(BhpRanapModel.Key("TBHR_TEST_02"));
        loaded.Qty.Should().Be(5);
        loaded.Price.Should().Be(8000m);
        loaded.VodUser.Should().Be("SPR002");
        loaded.VodKet.Should().Be("Batal sebagian");
    }
}
