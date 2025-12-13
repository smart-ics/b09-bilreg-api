using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public interface IBookingExternalDal :
    IInsert<BookingExternalDto>,
    IUpdate<BookingExternalDto>,
    IDelete<IBookingKey>,
    IGetData<BookingExternalDto, IBookingKey>,
    IListData<BookingExtDto, Periode>
{
    BookingExternalDto GetData(string ReffId);
}
public class BookingExternalDal : IBookingExternalDal
{
    private readonly DatabaseOptions _opt;

    public BookingExternalDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public void Insert(BookingExternalDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_BookingExternal (
                BookingId, ExtAppName, ReffId, CheckInQr)
            VALUES (
                @BookingId, @ExtAppName, @ReffId, @CheckInQr)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BookingId", dto.BookingId, SqlDbType.VarChar);
        dp.AddParam("@ExtAppName", dto.ExtAppName, SqlDbType.VarChar);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);
        dp.AddParam("@CheckInQr", dto.CheckInQr, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BookingExternalDto dto)
    {
        const string sql = """
           UPDATE BILRG_BookingExternal 
           SET 
               BookingId = @BookingId, 
               ExtAppName = @ExtAppName, 
               ReffId = @ReffId, 
               CheckInQr = @CheckInQr
           WHERE BookingId = @BookingId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@BookingId", dto.BookingId, SqlDbType.VarChar);
        dp.AddParam("@ExtAppName", dto.ExtAppName, SqlDbType.VarChar);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);
        dp.AddParam("@CheckInQr", dto.CheckInQr, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Delete(IBookingKey key)
    {
        const string sql = """
           DELETE BILRG_BookingExternal 
           WHERE BookingId = @BookingId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@BookingId", key.BookingId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    public BookingExternalDto GetData(IBookingKey key)
    {
        const string sql = """
            SELECT
                aa.BookingId, aa.ExtAppName,  
            	aa.ReffId, aa.CheckInQr 
            FROM
                BILRG_BookingExternal aa
            WHERE
                aa.BookingId = @BookingId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BookingId", key.BookingId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BookingExternalDto>(sql, dp);
    }

    public BookingExternalDto GetData(string reffId)
    {
        const string sql = """
            SELECT
                aa.BookingId, aa.ExtAppName,  
            	aa.ReffId, aa.CheckInQr 
            FROM
                BILRG_BookingExternal aa
            WHERE
                aa.ReffId = @ReffId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ReffId", reffId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BookingExternalDto>(sql, dp);
    }

    public IEnumerable<BookingExtDto> ListData(Periode periode)
    {
        const string sql = """
            SELECT
                aa.BookingId, aa.BookingDate, aa.TglBerobat, aa.DokterId,
                aa.RegId, aa.PasienId, aa.PasienName, 
                aa.LayananId, aa.JamPraktek, aa.NoAntrian,
            	ISNULL(bb.ExtAppName,'') AS ExtAppName,  
            	ISNULL(bb.ReffId,'') AS ReffId,
            	ISNULL(bb.CheckInQr,'') AS CheckInQr, 
            	ISNULL(cc.fs_nm_peg,'') AS DokterName,
                ISNULL(dd.fs_nm_layanan,'') AS LayananName
            FROM
                BILRG_Booking aa
            	LEFT JOIN BILRG_BookingExternal bb ON aa.BookingId = bb.BookingId
            	LEFT JOIN td_peg cc ON aa.DokterId = cc.fs_kd_peg
                LEFT JOIN ta_layanan dd ON aa.LayananId = dd.fs_kd_layanan
            WHERE
            	aa.TglBerobat BETWEEN @Tgl1 AND @Tgl2
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", periode.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", periode.Tgl2, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BookingExtDto>(sql, dp);
    }
}



