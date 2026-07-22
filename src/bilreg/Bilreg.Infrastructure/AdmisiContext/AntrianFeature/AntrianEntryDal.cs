using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using PdfSharp.Pdf.Filters;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public interface IAntrianEntryDal :
    IInsert<AntrianEntryDto>,
    IUpdate<AntrianEntryDto>,
    IDelete<IAntrianKey>,
    IListData<AntrianEntryDto, IAntrianKey>
{
    int UpdateFromAnonymousInService(AntrianEntryDto dto);
    int UpdateWaitingToInService(AntrianEntryDto dto);
    int UpdateInServiceToDone(AntrianEntryDto dto);
    void Delete(IAntrianKey key, int noUrut);
    AntrianEntryDto GetData(IAntrianKey key, int noUrut);
    IEnumerable<AntaianEntryOutStandingDto> ListOutStanding();
    void UpdateOutStanding(AntaianEntryOutStandingDto data);
}

public class AntrianEntryDal : IAntrianEntryDal
{
    private readonly DatabaseOptions _opt;

    public AntrianEntryDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(AntrianEntryDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_AntrianEntry(
                AntrianId, NoUrut, PersonName, AntrianStatus,
                PasienTrackerId, CreatedAt, ServedAt, DoneAt,
                ReffId, ReffDesc) 
            VALUES(
                @AntrianId, @NoUrut, @PersonName, @AntrianStatus,
                @PasienTrackerId, @CreatedAt, @ServedAt, @DoneAt,
                @ReffId, @ReffDesc)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", dto.AntrianId, SqlDbType.VarChar); 
        dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);	 
        dp.AddParam("@PersonName", dto.PersonName, SqlDbType.VarChar);
        dp.AddParam("@PasienTrackerId", dto.PasienTrackerId, SqlDbType.VarChar);
        dp.AddParam("@AntrianStatus", dto.AntrianStatus, SqlDbType.Int);	
        dp.AddParam("@CreatedAt", dto.CreatedAt, SqlDbType.DateTime);	 
        dp.AddParam("@ServedAt", dto.ServedAt, SqlDbType.DateTime);	 
        dp.AddParam("@DoneAt", dto.DoneAt, SqlDbType.DateTime);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);
        dp.AddParam("@ReffDesc", dto.ReffDesc, SqlDbType.VarChar);


        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(AntrianEntryDto model)
    {
        const string sql = """
           UPDATE
                BILRG_AntrianEntry
           SET
               PersonName = @PersonName, 
               PasienTrackerId = @PasienTrackerId,
               AntrianStatus = @AntrianStatus,
               CreatedAt = @CreatedAt, 
               ServedAt = @ServedAt, 
               DoneAt = @DoneAt,
               ReffId = @ReffId,
               ReffDesc = @ReffDesc
           WHERE
               AntrianId = @AntrianId 
               AND NoUrut = @NoUrut
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", model.AntrianId, SqlDbType.VarChar); 
        dp.AddParam("@NoUrut", model.NoUrut, SqlDbType.Int);	 
        dp.AddParam("@PersonName", model.PersonName, SqlDbType.VarChar);
        dp.AddParam("@PasienTrackerId", model.PasienTrackerId, SqlDbType.VarChar);
        dp.AddParam("@AntrianStatus", model.AntrianStatus, SqlDbType.Int);	
        dp.AddParam("@CreatedAt", model.CreatedAt, SqlDbType.DateTime);	 
        dp.AddParam("@ServedAt", model.ServedAt, SqlDbType.DateTime);	 
        dp.AddParam("@DoneAt", model.DoneAt, SqlDbType.DateTime);
        dp.AddParam("@ReffId", model.ReffId, SqlDbType.VarChar);
        dp.AddParam("@ReffDesc", model.ReffDesc, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public int UpdateFromAnonymousInService(AntrianEntryDto dto)
    {
        const string sql = """
           UPDATE
                BILRG_AntrianEntry
           SET
               PersonName = @PersonName,
               PasienTrackerId = @PasienTrackerId,
               AntrianStatus = @AntrianStatus,
               CreatedAt = @CreatedAt,
               ServedAt = @ServedAt,
               DoneAt = @DoneAt,
               ReffId = @ReffId,
               ReffDesc = @ReffDesc
           WHERE
               AntrianId = @AntrianId
               AND NoUrut = @NoUrut
               AND PasienTrackerId IN ('', '-')
               AND AntrianStatus = @ExpectedStatus
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", dto.AntrianId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);
        dp.AddParam("@PersonName", dto.PersonName, SqlDbType.VarChar);
        dp.AddParam("@PasienTrackerId", dto.PasienTrackerId, SqlDbType.VarChar);
        dp.AddParam("@AntrianStatus", dto.AntrianStatus, SqlDbType.Int);
        dp.AddParam("@CreatedAt", dto.CreatedAt, SqlDbType.DateTime);
        dp.AddParam("@ServedAt", dto.ServedAt, SqlDbType.DateTime);
        dp.AddParam("@DoneAt", dto.DoneAt, SqlDbType.DateTime);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);
        dp.AddParam("@ReffDesc", dto.ReffDesc, SqlDbType.VarChar);
        dp.AddParam("@ExpectedStatus", (int)AntrianStatusEnum.InService, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public int UpdateWaitingToInService(AntrianEntryDto dto)
    {
        const string sql = """
           UPDATE
                BILRG_AntrianEntry
           SET
               PersonName = @PersonName,
               PasienTrackerId = @PasienTrackerId,
               AntrianStatus = @AntrianStatus,
               CreatedAt = @CreatedAt,
               ServedAt = @ServedAt,
               DoneAt = @DoneAt,
               ReffId = @ReffId,
               ReffDesc = @ReffDesc
           WHERE
               AntrianId = @AntrianId
               AND NoUrut = @NoUrut
               AND AntrianStatus = @ExpectedStatus
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", dto.AntrianId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);
        dp.AddParam("@PersonName", dto.PersonName, SqlDbType.VarChar);
        dp.AddParam("@PasienTrackerId", dto.PasienTrackerId, SqlDbType.VarChar);
        dp.AddParam("@AntrianStatus", dto.AntrianStatus, SqlDbType.Int);
        dp.AddParam("@CreatedAt", dto.CreatedAt, SqlDbType.DateTime);
        dp.AddParam("@ServedAt", dto.ServedAt, SqlDbType.DateTime);
        dp.AddParam("@DoneAt", dto.DoneAt, SqlDbType.DateTime);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);
        dp.AddParam("@ReffDesc", dto.ReffDesc, SqlDbType.VarChar);
        dp.AddParam("@ExpectedStatus", (int)AntrianStatusEnum.Waiting, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public int UpdateInServiceToDone(AntrianEntryDto dto)
    {
        const string sql = """
           UPDATE
                BILRG_AntrianEntry
           SET
               PersonName = @PersonName,
               PasienTrackerId = @PasienTrackerId,
               AntrianStatus = @AntrianStatus,
               CreatedAt = @CreatedAt,
               ServedAt = @ServedAt,
               DoneAt = @DoneAt,
               ReffId = @ReffId,
               ReffDesc = @ReffDesc
           WHERE
               AntrianId = @AntrianId
               AND NoUrut = @NoUrut
               AND PasienTrackerId = @PasienTrackerId
               AND AntrianStatus = @ExpectedStatus
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", dto.AntrianId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);
        dp.AddParam("@PersonName", dto.PersonName, SqlDbType.VarChar);
        dp.AddParam("@PasienTrackerId", dto.PasienTrackerId, SqlDbType.VarChar);
        dp.AddParam("@AntrianStatus", dto.AntrianStatus, SqlDbType.Int);
        dp.AddParam("@CreatedAt", dto.CreatedAt, SqlDbType.DateTime);
        dp.AddParam("@ServedAt", dto.ServedAt, SqlDbType.DateTime);
        dp.AddParam("@DoneAt", dto.DoneAt, SqlDbType.DateTime);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);
        dp.AddParam("@ReffDesc", dto.ReffDesc, SqlDbType.VarChar);
        dp.AddParam("@ExpectedStatus", (int)AntrianStatusEnum.InService, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public void Delete(IAntrianKey key, int noUrut)
    {
        const string sql = """
           DELETE FROM
                BILRG_AntrianEntry
           WHERE
               AntrianId = @AntrianId 
               AND NoUrut = @NoUrut
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", key.AntrianId, SqlDbType.VarChar); 
        dp.AddParam("@NoUrut", noUrut, SqlDbType.Int);	 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IAntrianKey key)
    {
        const string sql = """
           DELETE FROM
                BILRG_AntrianEntry
           WHERE
               AntrianId = @AntrianId 
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", key.AntrianId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    
    public AntrianEntryDto GetData(IAntrianKey key, int noUrut)
    {
        const string sql = """
           SELECT
               AntrianId, NoUrut, PersonName, PasienTrackerId, 
               AntrianStatus, CreatedAt, ServedAt, DoneAt,
               ReffId, ReffDesc
           FROM
                BILRG_AntrianEntry
           WHERE
               AntrianId = @AntrianId 
               AND NoUrut = @NoUrut
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", key.AntrianId, SqlDbType.VarChar); 
        dp.AddParam("@NoUrut", noUrut, SqlDbType.Int);	 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<AntrianEntryDto>(sql, dp);
    }

    public IEnumerable<AntrianEntryDto> ListData(IAntrianKey filter)
    {
        const string sql = """
            SELECT
            AntrianId, NoUrut, PersonName, PasienTrackerId, 
            AntrianStatus, CreatedAt, ServedAt, DoneAt,
            ReffId, ReffDesc
            FROM
                BILRG_AntrianEntry
            WHERE
               AntrianId = @AntrianId 
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", filter.AntrianId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntrianEntryDto>(sql, dp);
    }

    public IEnumerable<AntaianEntryOutStandingDto> ListOutStanding()
    {
        const string sql = """
            SELECT 
                  TOP 50
                  aa.AntrianId, aa.NoUrut, aa.PersonName, aa.ReffId, aa.ReffDesc,
                  ISNULL(bb.BookingId ,'') AS Bok_Ulid,   
                  ISNULL(cc.BookingId ,'') AS Bok_Bh,     
                  ISNULL(dd.fs_kd_booking ,'') AS Bok_Bo, 
                  ISNULL(dd.fs_kd_reg ,'') AS RegId,        
                  ISNULL(dd.fd_tgl_masuk ,'') AS RegDate
            FROM BILRG_AntrianEntry aa
            LEFT JOIN BILRG_BookingExternal bb ON aa.ReffId = bb.BookingId
            LEFT JOIN HIDOK_BookingBridge cc ON bb.ReffId = cc.BookingId
            LEFT JOIN ta_registrasi dd ON cc.KodeTrsBookingRS = dd.fs_kd_booking AND dd.fs_kd_booking <> ''
            WHERE ISNULL(dd.fs_kd_reg, '??') LIKE 'RG%'
            AND aa.ReffDesc = 'BOK'
            ORDER BY dd.fd_tgl_masuk desc, aa.AntrianId, aa.NoUrut
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntaianEntryOutStandingDto>(sql);
    }

    public void UpdateOutStanding(AntaianEntryOutStandingDto data)
    {
        const string sql = """
           UPDATE
                BILRG_AntrianEntry
           SET
               ReffId = @ReffId,
               ReffDesc = @ReffDesc
           WHERE
               AntrianId = @AntrianId 
               AND NoUrut = @NoUrut
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", data.AntrianId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", data.NoUrut, SqlDbType.Int);
        dp.AddParam("@ReffId", data.RegId, SqlDbType.VarChar);
        dp.AddParam("@ReffDesc", "REG", SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
