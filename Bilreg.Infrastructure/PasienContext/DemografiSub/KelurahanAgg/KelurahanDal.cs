// using System.Data;
// using System.Data.SqlClient;
// using Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;
// using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
// using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
// using Bilreg.Infrastructure.Helpers;
// using Dapper;
// using FluentAssertions;
// using Microsoft.Extensions.Options;
// using Nuna.Lib.DataAccessHelper;
// using Nuna.Lib.TransactionHelper;
// using Xunit;
//
// namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KelurahanAgg;
//
// public class KelurahanDal: IKelurahanDal
// {
//     private readonly DatabaseOptions _opt;
//
//     public KelurahanDal(IOptions<DatabaseOptions> opt)
//     {
//         _opt = opt.Value;
//     }
//
//     public void Insert(KelurahanModel model)
//     {
//         const string sql = @"
//             INSERT INTO ta_kelurahan 
//                 (fs_kd_kelurahan, fs_nm_kelurahan, fs_kd_kecamatan, fs_kd_pos)
//             VALUES 
//                 (@fs_kd_kelurahan, @fs_nm_kelurahan, @fs_kd_kecamatan, @fs_kd_pos)";
//
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_kelurahan", model.KelurahanId, SqlDbType.VarChar);
//         dp.AddParam("@fs_nm_kelurahan", model.KelurahanName, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_kecamatan", model.Kecamatan.KecamatanId, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_pos", model.KodePos, SqlDbType.VarChar);
//         
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         conn.Execute(sql, dp);
//     }
//
//     public void Update(KelurahanModel model)
//     {
//         const string sql = @"
//             UPDATE 
//                 ta_kelurahan
//             SET 
//                 fs_nm_kelurahan = @fs_nm_kelurahan,
//                 fs_kd_kecamatan = @fs_kd_kecamatan,
//                 fs_kd_pos = @fs_kd_pos
//             WHERE 
//                 fs_kd_kelurahan = @fs_kd_kelurahan";
//
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_kelurahan", model.KelurahanId, SqlDbType.VarChar);
//         dp.AddParam("@fs_nm_kelurahan", model.KelurahanName, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_kecamatan", model.Kecamatan.KecamatanId, SqlDbType.VarChar);
//         dp.AddParam("@fs_kd_pos", model.KodePos, SqlDbType.VarChar);
//         
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         conn.Execute(sql, dp);
//     }
//
//     public void Delete(IKelurahanKey key)
//     {
//         const string sql = @"
//             DELETE FROM
//                 ta_kelurahan
//             WHERE 
//                 fs_kd_kelurahan = @fs_kd_kelurahan";
//
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_kelurahan", key.KelurahanId, SqlDbType.VarChar);
//         
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         conn.Execute(sql, dp);
//     }
//
//     public KelurahanModel GetData(IKelurahanKey key)
//     {
//         const string sql = @"
//             SELECT 
//                 aa.fs_kd_kelurahan AS KelurahanId, 
//                 aa.fs_nm_kelurahan AS KelurahanName, 
//                 aa.fs_kd_pos AS KodePos, 
//                 aa.fs_kd_kecamatan AS KecamatanId,
//                 ISNULL(bb.fs_nm_kecamatan, '') AS KecamatanName, 
//                 ISNULL(bb.fs_kd_kabupaten, '') AS KabupatenId,
//                 ISNULL(cc.fs_nm_kabupaten, '') AS KabupatenName, 
//                 ISNULL(cc.fs_kd_propinsi, '') AS PropinsiId,
//                 ISNULL(dd.fs_nm_propinsi, '') AS PropinsiName
//             FROM 
//                 ta_kelurahan aa
//                 LEFT JOIN ta_kecamatan bb ON aa.fs_kd_kecamatan = bb.fs_kd_kecamatan
//                 LEFT JOIN ta_kabupaten cc ON bb.fs_kd_kabupaten = cc.fs_kd_kabupaten
//                 LEFT JOIN ta_propinsi dd ON cc.fs_kd_propinsi = dd.fs_kd_propinsi
//             WHERE 
//                 aa.fs_kd_kelurahan = @fs_kd_kelurahan";
//         
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_kelurahan", key.KelurahanId, SqlDbType.VarChar);
//         
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         var result = conn.ReadSingle<KelurahanDto>(sql, dp);
//         return result?.ToModel()!;
//     }
//
//     public IEnumerable<KelurahanModel> ListData(IKecamatanKey filter)
//     {
//         const string sql = @"
//             SELECT 
//                 aa.fs_kd_kelurahan AS KelurahanId, 
//                 aa.fs_nm_kelurahan AS KelurahanName, 
//                 aa.fs_kd_pos AS KodePos, 
//                 aa.fs_kd_kecamatan AS KecamatanId,
//                 ISNULL(bb.fs_nm_kecamatan, '') AS KecamatanName, 
//                 ISNULL(bb.fs_kd_kabupaten, '') AS KabupatenId,
//                 ISNULL(cc.fs_nm_kabupaten, '') AS KabupatenName, 
//                 ISNULL(cc.fs_kd_propinsi, '') AS PropinsiId,
//                 ISNULL(dd.fs_nm_propinsi, '') AS PropinsiName
//             FROM 
//                 ta_kelurahan aa
//                 LEFT JOIN ta_kecamatan bb ON aa.fs_kd_kecamatan = bb.fs_kd_kecamatan
//                 LEFT JOIN ta_kabupaten cc ON bb.fs_kd_kabupaten = cc.fs_kd_kabupaten
//                 LEFT JOIN ta_propinsi dd ON cc.fs_kd_propinsi = dd.fs_kd_propinsi
//             WHERE 
//                 aa.fs_kd_kecamatan = @fs_kd_kecamatan";
//
//         var dp = new DynamicParameters();
//         dp.AddParam("@fs_kd_kecamatan", filter.KecamatanId, SqlDbType.VarChar);
//
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         var result = conn.Read<KelurahanDto>(sql, dp);
//         return result?.Select(x => x.ToModel())!;
//     }
//
//     public IEnumerable<KelurahanModel> ListData(string filter)
//     {
//         const string sql = @"
//             SELECT 
//                 aa.fs_kd_kelurahan AS KelurahanId, 
//                 aa.fs_nm_kelurahan AS KelurahanName, 
//                 aa.fs_kd_pos AS KodePos, 
//                 aa.fs_kd_kecamatan AS KecamatanId,
//                 ISNULL(bb.fs_nm_kecamatan, '') AS KabupatenName, 
//                 ISNULL(bb.fs_kd_kabupaten, '') AS KabupatenId,
//                 ISNULL(cc.fs_nm_kabupaten, '') AS KabupatenName, 
//                 ISNULL(cc.fs_kd_propinsi, '') AS PropinsiId,
//                 ISNULL(dd.fs_nm_propinsi, '') AS PropinsiName
//             FROM 
//                 ta_kelurahan aa
//                 LEFT JOIN ta_kecamatan bb ON aa.fs_kd_kecamatan = bb.fs_kd_kecamatan
//                 LEFT JOIN ta_kabupaten cc ON bb.fs_kd_kabupaten = cc.fs_kd_kabupaten
//                 LEFT JOIN ta_propinsi dd ON cc.fs_kd_propinsi = dd.fs_kd_propinsi
//             WHERE 
//                 aa.fs_nm_kelurahan LIKE @keyword ";
//
//         var parameters = new { keyword = $"%{filter}%" };
//         
//         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
//         var result = conn.Query<KelurahanDto>(sql, parameters);
//         return result?.Select(x => x.ToModel())!;
//     }
// }
//
// public class KelurahanDalTest
// {
//     private readonly KelurahanDal _sut;
//
//     public KelurahanDalTest()
//     {
//         _sut = new KelurahanDal(ConnStringHelper.GetTestEnv());
//     }
//
//     [Fact]
//     public void InsertTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var kelurahan = new KelurahanModel("A", "B", "C", KecamatanType.Default);
//         _sut.Insert(kelurahan);
//     }
//
//     [Fact]
//     public void UpdateTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var kelurahan = new KelurahanModel("A", "B", "C", KecamatanType.Default);
//
//         _sut.Update(kelurahan);
//     }
//
//     [Fact]
//     public void DeleteTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var kelurahan = new KelurahanModel("A", "B", "C", KecamatanType.Default);
//         
//         _sut.Delete(kelurahan);
//     }
//
//     [Fact]
//     public void GetDataTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var expected = new KelurahanModel("A", "B", "C", KecamatanType.Default);
//         _sut.Insert(expected);
//         
//         var actual = _sut.GetData(expected);
//         actual.Should().BeEquivalentTo(expected, opt => opt.Excluding(y => y.Kecamatan.Kabupaten.Propinsi));
//     }
//
//     [Fact]
//     public void ListDataTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var expected = new KelurahanModel("A", "B", "C", KecamatanType.Default);
//         
//         _sut.Insert(expected);
//         
//         var actual = _sut.ListData(KecamatanType.Default);
//         _ = actual.Select(x => x.Should().BeEquivalentTo(expected));
//     }
//     
//     [Fact]
//     public void SearchDataTest()
//     {
//         using var trans = TransHelper.NewScope();
//         
//         const string keyword = "B";
//         var expected = new KelurahanModel("A", "ABC", "C", KecamatanType.Default);
//
//         _sut.Insert(expected);
//         
//         var actual = _sut.ListData(keyword);
//         _ = actual.Select(x => x.Should().BeEquivalentTo(expected));
//     }
// } 