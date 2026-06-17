using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public interface IBilrgTataRekeningDal :
    IInsert<BilrgTataRekeningDto>,
    IUpdate<BilrgTataRekeningDto>,
    IDelete<IRegKey>,
    IGetData<BilrgTataRekeningDto, IRegKey>
{
}

public class BilrgTataRekeningDal : IBilrgTataRekeningDal
{
    private readonly DatabaseOptions _opt;

    public BilrgTataRekeningDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BilrgTataRekeningDto model)
    {
        const string sql = """
            INSERT INTO BILRG_TataRekening(
                RegId, Status, PetugasVerif, DischargeDate)
            VALUES(
                @RegId, @Status, @PetugasVerif, @DischargeDate)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", model.RegId, SqlDbType.VarChar);
        dp.AddParam("@Status", model.Status, SqlDbType.Int);
        dp.AddParam("@PetugasVerif", model.PetugasVerif, SqlDbType.VarChar);
        dp.AddParam("@DischargeDate", model.DischargeDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BilrgTataRekeningDto model)
    {
        const string sql = """
            UPDATE
                BILRG_TataRekening
            SET
                Status = @Status,
                PetugasVerif = @PetugasVerif,
                DischargeDate = @DischargeDate
            WHERE
                RegId = @RegId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", model.RegId, SqlDbType.VarChar);
        dp.AddParam("@Status", model.Status, SqlDbType.Int);
        dp.AddParam("@PetugasVerif", model.PetugasVerif, SqlDbType.VarChar);
        dp.AddParam("@DischargeDate", model.DischargeDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IRegKey key)
    {
        const string sql = """
            DELETE FROM
                BILRG_TataRekening
            WHERE
                RegId = @RegId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", key.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public BilrgTataRekeningDto GetData(IRegKey key)
    {
        const string sql = """
            SELECT
                RegId, Status, PetugasVerif, DischargeDate
            FROM
                BILRG_TataRekening
            WHERE
                RegId = @RegId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", key.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BilrgTataRekeningDto>(sql, dp);
    }
}
