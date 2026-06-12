//  TODO: jude-dev-trs-billing
// using Bilreg.Domain.AccountingContext.JurnalFeature;
// using Bilreg.Infrastructure.AccountingContext.JurnalFeature;
// using Bilreg.Infrastructure.Shared.Helpers;
// using FluentAssertions;
// using Nuna.Lib.TransactionHelper;
//
// namespace Bilreg.Test.AccountingContext.JurnalFeature;
//
// public class Jurnal2DalTest
// {
//     private readonly Jurnal2Dal _sut = new(ConnStringHelper.GetTestEnv());
//
//     private static Jurnal2Dto Faker()
//         => new Jurnal2Dto(
//             fs_kd_jurnal: "JUR001",
//             fn_urut: 1,
//             fs_kd_rek: "REK001",
//             fs_uraian: "Uraian Jurnal",
//             fn_jurnald: 100000,
//             fn_jurnalk: 0,
//             fs_kd_unit: "UNIT001",
//             fs_kd_jk: "JK1",
//             fs_string00: "STR00",
//             fs_string01: "STR01",
//             fs_string02: "STR02",
//             fs_string03: "STR03",
//             fs_string04: "STR04",
//             fs_string05: "STR05",
//             fs_string06: "STR06",
//             fs_string07: "STR07",
//             fs_string08: "STR08",
//             fs_string09: "STR09",
//             fs_string10: "STR10",
//             fs_string11: "STR11",
//             fn_nilai_jasa: 50000,
//             fn_nilai_obat: 50000
//         );
//
//     private static IJurnalKey FakerKey()
//         => JurnalType.Default with { JurnalId = "JUR001" };
//
//     [Fact]
//     public void InsertTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var dtos = new List<Jurnal2Dto> { Faker() };
//         _sut.Insert(dtos);
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
//         var dtos = new List<Jurnal2Dto> { dto };
//         _sut.Insert(dtos);
//
//         var actual = _sut.ListData(FakerKey());
//         actual.Should().ContainEquivalentOf(dto);
//     }
// }