using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public interface IBookingDal :
    IInsert<BookingDto>, 
    IUpdate<BookingDto>, 
    IDelete<IBookingKey>, 
    IGetData<BookingDto, IBookingKey>,
    IListData<BookingDto, Periode>
{
    IEnumerable<BookingDto> ListPerTglBerobat(Periode periode);
}

public class BookingDal : IBookingDal
{
    private readonly DatabaseOptions _opt;

    public BookingDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BookingDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_Booking (
                BookingId, BookingDate,
                PasienName, TglLahir, Gender, Alamat, PasienId, RegId,
                TglBerobat, JamPraktek, LayananId, DokterId, NoAntrian,
                NoPeserta, NoReffKontrol, ReffId, 
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @BookingId, @BookingDate,
                @PasienName, @TglLahir, @Gender, @Alamat, @PasienId, @RegId,
                @TglBerobat, @JamPraktek, @LayananId, @DokterId, @NoAntrian,
                @NoPeserta, @NoReffKontrol, @ReffId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BookingId", dto.BookingId, SqlDbType.VarChar);
        dp.AddParam("@BookingDate", dto.BookingDate, SqlDbType.DateTime);
        
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@TglLahir", dto.TglLahir, SqlDbType.DateTime);
        dp.AddParam("@Gender", dto.Gender, SqlDbType.VarChar);
        dp.AddParam("@Alamat", dto.Alamat, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        
        dp.AddParam("@TglBerobat", dto.TglBerobat, SqlDbType.DateTime);
        dp.AddParam("@JamPraktek", dto.JamPraktek, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@DokterId", dto.DokterId, SqlDbType.VarChar);
        dp.AddParam("@NoAntrian", dto.NoAntrian, SqlDbType.Int);

        dp.AddParam("@NoPeserta", dto.NoPeserta, SqlDbType.VarChar);
        dp.AddParam("@NoReffKontrol", dto.NoReffKontrol, SqlDbType.VarChar);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);
        
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BookingDto dto)
    {
        const string sql = """
           UPDATE BILRG_Booking 
           SET 
               BookingDate = @BookingDate,
               PasienName = @PasienName,
               TglLahir = @TglLahir,
               Gender = @Gender,
               Alamat = @Alamat,
               PasienId = @PasienId,
               RegId = @RegId,
               TglBerobat = @TglBerobat,
               JamPraktek = @JamPraktek,
               LayananId = @LayananId,
               DokterId = @DokterId,
               NoAntrian = @NoAntrian,
               NoPesertaa = @NoPeserta,
               NoReffKontrol = @NoReffKontrol,
               ReffId = @ReffId,
               CrtUser = @CrtUser,
               CrtDate = @CrtDate,
               UpdUser = @UpdUser,
               UpdDate = @UpdDate,
               VodUser = @VodUser,
               VodDate = @VodDate
           WHERE BookingId = @BookingId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@BookingId", dto.BookingId, SqlDbType.VarChar);
        dp.AddParam("@BookingDate", dto.BookingDate, SqlDbType.DateTime);
        
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@TglLahir", dto.TglLahir, SqlDbType.DateTime);
        dp.AddParam("@Gender", dto.Gender, SqlDbType.VarChar);
        dp.AddParam("@Alamat", dto.Alamat, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        
        dp.AddParam("@TglBerobat", dto.TglBerobat, SqlDbType.DateTime);
        dp.AddParam("@JamPraktek", dto.JamPraktek, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@DokterId", dto.DokterId, SqlDbType.VarChar);
        dp.AddParam("@NoAntrian", dto.NoAntrian, SqlDbType.Int);

        dp.AddParam("@NoPeserta", dto.NoPeserta, SqlDbType.VarChar);
        dp.AddParam("@NoReffKontrol", dto.NoReffKontrol, SqlDbType.VarChar);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);

        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
    
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IBookingKey key)
    {
        const string sql = """
           DELETE BILRG_Booking 
           WHERE BookingId = @BookingId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@BookingId", key.BookingId, SqlDbType.VarChar);
    
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public BookingDto GetData(IBookingKey key)
    {
        const string sql = """
            SELECT
                aa.BookingId, aa.BookingDate,
                aa.PasienName, aa.TglLahir, aa.Gender, aa.Alamat, aa.PasienId, aa.RegId,
                aa.TglBerobat, aa.JamPraktek, aa.LayananId, aa.DokterId, aa.NoAntrian,
                aa.NoPeserta, aa.NoReffKontrol, aa.ReffId, 
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
                ISNULL(bb.fs_nm_layanan, '') AS LayananName,
                ISNULL(cc.fs_nm_peg, '') AS DokterName
            FROM
                BILRG_Booking aa
                LEFT JOIN ta_layanan bb ON aa.LayananId = bb.fs_kd_layanan
                LEFT JOIN td_peg cc ON aa.DokterId = cc.fs_kd_peg
            WHERE
                aa.BookingId = @BookingId
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@BookingId", key.BookingId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BookingDto>(sql, dp);
    }

    public IEnumerable<BookingDto> ListData(Periode filter)
    {
        const string sql = """
           SELECT
               aa.BookingId, aa.BookingDate,
               aa.PasienName, aa.TglLahir, aa.Gender, aa.Alamat, aa.PasienId, aa.RegId,
               aa.TglBerobat, aa.JamPraktek, aa.LayananId, aa.DokterId, aa.NoAntrian,
               aa.NoPeserta, aa.NoReffKontrol, aa.ReffId,
               aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
               ISNULL(bb.fs_nm_layanan, '') AS LayananName,
               ISNULL(cc.fs_nm_peg, '') AS DokterName
           FROM
               BILRG_Booking aa
               LEFT JOIN ta_layanan bb ON aa.LayananId = bb.fs_kd_layanan
               LEFT JOIN td_peg cc ON aa.DokterId = cc.fs_kd_peg
           WHERE
               aa.BookingDate BETWEEN @Tgl1 AND @Tgl2
               AND aa.VodDate = @VodDate
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime);
        dp.AddParam("@VodDate", new DateTime(3000,1,1), SqlDbType.DateTime);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BookingDto>(sql, dp);
    }

    public IEnumerable<BookingDto> ListPerTglBerobat(Periode filter)
    {
        const string sql = """
            SELECT
               aa.BookingId, aa.BookingDate,
               aa.PasienName, aa.TglLahir, aa.Gender, aa.Alamat, aa.PasienId, aa.RegId,
               aa.TglBerobat, aa.JamPraktek, aa.LayananId, aa.DokterId, aa.NoAntrian,
               aa.NoPeserta, aa.NoReffKontrol, aa.ReffId,
               aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
               ISNULL(bb.fs_nm_layanan, '') AS LayananName,
               ISNULL(cc.fs_nm_peg, '') AS DokterName
            FROM
               BILRG_Booking aa
               LEFT JOIN ta_layanan bb ON aa.LayananId = bb.fs_kd_layanan
               LEFT JOIN td_peg cc ON aa.DokterId = cc.fs_kd_peg
            WHERE
               aa.TglBerobat BETWEEN @Tgl1 AND @Tgl2
               AND aa.VodDate = @VodDate
            """;
            
        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime);
        dp.AddParam("@VodDate", new DateTime(3000,1,1), SqlDbType.DateTime);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BookingDto>(sql, dp);
    }
}