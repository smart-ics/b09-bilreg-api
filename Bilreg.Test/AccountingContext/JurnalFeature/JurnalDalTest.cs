//  TODO: jude-dev-trs-billing
// using Bilreg.Domain.AccountingContext.JurnalFeature;
// using Bilreg.Infrastructure.AccountingContext.JurnalFeature;
// using Bilreg.Infrastructure.Shared.Helpers;
// using FluentAssertions;
// using Nuna.Lib.TransactionHelper;
//
// namespace Bilreg.Test.AccountingContext.JurnalFeature;
//
// public class JurnalDalTest
// {
//     private readonly JurnalDal _sut = new(ConnStringHelper.GetTestEnv());
//
//     private static JurnalDto Faker()
//         => new JurnalDto(
//             fs_kd_jurnal: "JUR001",
//             fd_tgl_jurnal: "2025-01-01",
//             fs_jam_jurnal: "10:00:00",
//             fs_kd_petugas: "USR001",
//             fs_keterangan: "Jurnal Testing",
//             fs_no_bukti1: "INV001",
//             fs_no_bukti2: "BK001",
//             fs_no_bukti3: "REF001",
//             fs_kd_reg: "REG001",
//             fs_kd_mr: "MR001",
//             fs_nm_pasien: "John Doe"
//         );
//
//     private static IJurnalKey FakerKey()
//         => JurnalType.Default with { JurnalId = "JUR001" };
//
//     [Fact]
//     public void InsertTest()
//     {
//         using var trans = TransHelper.NewScope();
//         _sut.Insert(Faker());
//     }
//
//     [Fact]
//     public void UpdateTest()
//     {
//         using var trans = TransHelper.NewScope();
//         _sut.Update(Faker());
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
//     public void GetDataTest()
//     {
//         using var trans = TransHelper.NewScope();
//         _sut.Insert(Faker());
//         var actual = _sut.GetData(FakerKey());
//         actual.Should().BeEquivalentTo(Faker(),
//             opt => opt.Excluding(x => x.fs_nm_pasien));
//     }
// }