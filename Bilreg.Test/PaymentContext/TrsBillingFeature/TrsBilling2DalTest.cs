//  TODO: jude-dev-trs-billing
// using Bilreg.Domain.PaymentContext.TrsBillingFeature;
// using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
// using Bilreg.Infrastructure.Shared.Helpers;
// using FluentAssertions;
// using Nuna.Lib.TransactionHelper;
//
// namespace Bilreg.Test.PaymentContext.TrsBillingFeature;
//
// public class TrsBilling2DalTest
// {
//     private readonly TrsBilling2Dal _sut = new(ConnStringHelper.GetTestEnv());
//     
//     private static TaTrsBilling2Dto Faker()
//         => new TaTrsBilling2Dto(
//             fs_kd_trs: "A",
//             fn_no_urut: 1,
//             fs_kd_detil_tarif: "DT0",
//             fs_kd_grup_rek: "GR",
//             fs_kd_trs_bayar: "BY001",
//             fs_kd_jenis_bayar: "JB001",
//             fn_trs_p: 100000,
//             fn_trs_n: 0,
//             fs_kd_petugas_medis: "MED001",
//             fs_kd_petugas_kasir: "KSR001",
//             fd_tgl_bayar: "2025-01-01",
//             fs_jam_bayar: "10:00:00",
//             fs_kd_rek_ppdp: "PPDP001",
//             fs_kd_rek_pdpt: "PDPT001",
//             fs_kd_rek_pdpt_lain: "PDPTL001",
//             fs_kd_rek_disc: "DISC001",
//             fs_kd_rek_persediaan: "PERS001",
//             fs_kd_rek_tax: "TAX001",
//             fs_kd_rek_retur: "RTR001",
//             fs_nm_detil_tarif: "Nama Detil Tarif",
//             fs_nm_grup_rek: "Nama Grup Rek",
//             fs_nm_peg_medis: "Nama Peg Medis",
//             fs_nm_peg_kasir: "Nama Peg Kasir"
//         );
//     
//     private static ITrsBillingKey FakerKey()
//         => TrsBillingType.Default with { TrsBillingId = "A" };
//     
//     [Fact]
//     public void InsertTest()
//     {
//         using var trans = TransHelper.NewScope();
//         _sut.Insert([Faker()]);
//     }
//     
//     [Fact]
//     public void DeleteTest()
//     {
//         using var trans = TransHelper.NewScope();
//         _sut.Delete(FakerKey());
//     }
//     
//     [Fact]
//     public void ListDataTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var dto = Faker();
//         _sut.Insert([dto]);
//         var actual = _sut.ListData(FakerKey());
//         actual.Should().ContainEquivalentOf(dto,
//             opt => opt
//                 .Excluding(x => x.fs_nm_detil_tarif)
//                 .Excluding(x => x.fs_nm_grup_rek)
//                 .Excluding(x => x.fs_nm_peg_medis)
//                 .Excluding(x => x.fs_nm_peg_kasir));
//     }
// }