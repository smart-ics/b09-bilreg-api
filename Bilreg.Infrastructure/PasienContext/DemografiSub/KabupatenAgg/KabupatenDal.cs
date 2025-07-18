// using System.Data;
// using System.Data.SqlClient;
// using Bilreg.Application.PasienContext.DemografiSub.KabupatenAgg;
// using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
// using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
// using Bilreg.Infrastructure.Helpers;
// using Dapper;
// using FluentAssertions;
// using Microsoft.Extensions.Options;
// using Nuna.Lib.DataAccessHelper;
// using Nuna.Lib.TransactionHelper;
// using Xunit;
//
// namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KabupatenAgg;
//
// public class KabupatenDal : IKabupatenDal
// {
//     private readonly DatabaseOptions _opt;
//
//     public KabupatenDal(IOptions<DatabaseOptions> opt)
//     {
//         _opt = opt.Value;
//     }
//
//     public void Insert(KabupatenType type)
//     {
//         const string sql = @"
//             INSERT INTO ta_kabupaten
//                 (fs_kd_kabupaten, fs_nm_kabupaten, fs_kd_propinsi)
//             VALUES 
//                 (@fs_kd_kabupaten, @fs_nm_kabupaten, @fs_kd_propinsi)";
//
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_kabupaten", type.KabupatenId, SqlDbType.VarChar);
//         dp.AddParam("@fs_nm_kabupaten", type.KabupatenName, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_propinsi", type.Propinsi.PropinsiId, SqlDbType.VarChar);
//
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         conn.Execute(sql, dp);
//     }
//
//     public void Update(KabupatenType type)
//     {
//         const string sql = @"
//             UPDATE 
//                 ta_kabupaten
//             SET 
//                 fs_nm_kabupaten  = @fs_nm_kabupaten,
//                 fs_kd_propinsi = @fs_kd_propinsi
//             WHERE 
//                 fs_kd_kabupaten = @fs_kd_kabupaten";
//
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_kabupaten", type.KabupatenId, SqlDbType.VarChar);
//         dp.AddParam("@fs_nm_kabupaten", type.KabupatenName, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_propinsi", type.Propinsi.PropinsiId, SqlDbType.VarChar);
//
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         conn.Execute(sql, dp);
//     }
//
//     public void Delete(IKabupatenKey key)
//     {
//         const string sql = @"
//             DELETE FROM 
//                 ta_kabupaten
//             WHERE 
//                 fs_kd_kabupaten = @fs_kd_kabupaten";
//
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_kabupaten", key.KabupatenId, SqlDbType.VarChar);
//
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         conn.Execute(sql, dp);
//     }
//
//     public KabupatenType GetData(IKabupatenKey key)
//     {
//         const string sql = @"
//             SELECT 
//                 aa.fs_kd_kabupaten AS KabupatenId, 
//                 aa.fs_nm_kabupaten AS KabupatenNAme, 
//                 aa.fs_kd_propinsi AS PropinsiId,
//                 ISNULL(bb.fs_nm_propinsi, '') AS PropinsiName 
//             FROM 
//                 ta_kabupaten aa
//                 LEFT JOIN ta_propinsi bb ON aa.fs_kd_propinsi = bb.fs_kd_propinsi
//             WHERE 
//                 fs_kd_kabupaten = @fs_kd_kabupaten";
//
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_kabupaten", key.KabupatenId, SqlDbType.VarChar);
//
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         var result = conn.ReadSingle<KabupatenDto>(sql, dp);
//         return result?.ToModel();
//     }
//
//     public IEnumerable<KabupatenType> ListData(IPropinsiKey filter)
//     {
//         const string sql = @"
//             SELECT 
//                 aa.fs_kd_kabupaten AS KabupatenId, 
//                 aa.fs_nm_kabupaten AS KabupatenNAme, 
//                 aa.fs_kd_propinsi AS PropinsiId,
//                 ISNULL(bb.fs_nm_propinsi, '') AS PropinsiName 
//             FROM 
//                 ta_kabupaten aa
//                 LEFT JOIN ta_propinsi bb ON aa.fs_kd_propinsi = bb.fs_kd_propinsi
//             WHERE 
//                 aa.fs_kd_propinsi = @fs_kd_propinsi";
//
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_propinsi", filter.PropinsiId, SqlDbType.VarChar);
//
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         var result = conn.Read<KabupatenDto>(sql, dp);
//         return result.Select(x => x.ToModel());
//     }
// }
//
// public class KabupatenDalTest
// {
//     private readonly KabupatenDal _sut;
//
//     public KabupatenDalTest()
//     {
//         _sut = new KabupatenDal(ConnStringHelper.GetTestEnv());
//     }
//
//     [Fact]
//     public void InsertTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var kab = new KabupatenType("A", "B", PropinsiType.Default);
//         _sut.Insert(kab);
//     }
//
//     [Fact]
//     public void UpdateTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var kab = new KabupatenType("A", "B", PropinsiType.Default);
//         _sut.Update(kab);
//     }
//
//     [Fact]
//     public void DeleteTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var kab = new KabupatenType("A", "B", PropinsiType.Default);
//         _sut.Insert(kab);
//         _sut.Delete(kab);
//     }
//     
//     [Fact]
//     public void GetDataTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var kab = new KabupatenType("A", "B", PropinsiType.Default);
//         _sut.Insert(kab);
//         var actual = _sut.GetData(kab);
//         actual.Should().BeEquivalentTo(kab);
//     }
//
//     [Fact]
//     public void ListDataTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var kab = new KabupatenType("A", "B", PropinsiType.Default);
//         _sut.Insert(kab);
//         var actual = _sut.GetData(kab);
//         actual.Should().BeEquivalentTo(kab, opt =>
//             opt.Excluding(x => x.Propinsi.PropinsiName));
//     }
// }