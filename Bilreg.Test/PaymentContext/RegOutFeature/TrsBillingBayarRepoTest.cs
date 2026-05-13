using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.PaymentContext.RegOutFeature;

public class TrsBillingBayarRepoTest
{
    private readonly TrsBillingBayarRepo _sut;
    private readonly TrsBilling2Dal _dal;

    public TrsBillingBayarRepoTest()
    {
        _dal = new TrsBilling2Dal(ConnStringHelper.GetTestEnv());
        _sut = new TrsBillingBayarRepo(_dal);
    }

    //[Fact]
    //public void SaveChanges_GivenTrsBilling2JasaType_WhenInsert_ThenDataPersisted()
    //{
    //    // === ARRANGE ===
    //    using var trans = TransHelper.NewScope();

    //    var billing = new TrsBilling2JasaType(
    //        TrsBillingId: "BILL001", NoUrut: 1, PaymentId: "RO0001",
    //        PaymentDate: DateTime.Parse("2024-01-15 10:30:00"),
    //        NilaiBilling: new("KAS", 100000, 0),
    //        Ppa: new("MED01", "Dr. A"),
    //        Kasir: PegType.Create("KAS01", "Kasir 1"),
    //        Komponen: new("JASA01", "Jasa Medis"),
    //        Rekening: new("", "", ""));

    //    // === ACT ===
    //    _sut.SaveChanges(new[] { billing });
    //    var result = _sut.ListData(RegModel.Key("RG0001"));

    //    // === ASSERT ===
    //    result.Should().ContainSingle(x => x.PaymentId == "RO0001");
    //}

    //[Fact]
    //public void DeleteByPaymentId_GivenExistingData_WhenDelete_ThenDataRemoved()
    //{
    //    // === ARRANGE ===
    //    using var trans = TransHelper.NewScope();

    //    var billing = new TrsBilling2ObatType(
    //        TrsBillingId: "BILL002", NoUrut: 1, PaymentId: "RO0002",
    //        PaymentDate: DateTime.Parse("2024-01-15 11:00:00"),
    //        NilaiBilling: new("KAS", 50000, 0),
    //        Kasir: PegType.Create("KAS02", "Kasir 2"),
    //        GroupRek: new("GR01", "Grup Obat"),
    //        Rekening: new("", "", "", "", "", "", ""));

    //    _sut.SaveChanges(new[] { billing });

    //    // === ACT ===
    //    _sut.DeleteByPaymentId("RO0002");
    //    var result = _sut.ListData(RegModel.Key("RG0002"));

    //    // === ASSERT ===
    //    result.Should().NotContain(x => x.PaymentId == "RO0002");
    //}
}