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
    int UpdateConditional(BilrgTataRekeningDto dto, int expectedVersion);
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
                RegId, Status, PetugasVerif, DischargeDate,
                FinVerifStatus, FinVerifPetugas, FinVerifDate,
                IsAllocated, SettlementInitiated, Version)
            VALUES(
                @RegId, @Status, @PetugasVerif, @DischargeDate,
                @FinVerifStatus, @FinVerifPetugas, @FinVerifDate,
                @IsAllocated, @SettlementInitiated, @Version)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(model));
    }

    public void Update(BilrgTataRekeningDto model)
    {
        const string sql = """
            UPDATE
                BILRG_TataRekening
            SET
                Status = @Status,
                PetugasVerif = @PetugasVerif,
                DischargeDate = @DischargeDate,
                FinVerifStatus = @FinVerifStatus,
                FinVerifPetugas = @FinVerifPetugas,
                FinVerifDate = @FinVerifDate,
                IsAllocated = @IsAllocated,
                SettlementInitiated = @SettlementInitiated,
                Version = Version + 1
            WHERE
                RegId = @RegId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(model));
    }

    public int UpdateConditional(BilrgTataRekeningDto dto, int expectedVersion)
    {
        const string sql = """
            UPDATE BILRG_TataRekening
            SET Status = @Status,
                PetugasVerif = @PetugasVerif,
                DischargeDate = @DischargeDate,
                FinVerifStatus = @FinVerifStatus,
                FinVerifPetugas = @FinVerifPetugas,
                FinVerifDate = @FinVerifDate,
                IsAllocated = @IsAllocated,
                SettlementInitiated = @SettlementInitiated,
                Version = Version + 1
            WHERE RegId = @RegId
              AND Version = @ExpectedVersion
            """;

        var dp = BuildParams(dto);
        dp.AddParam("@ExpectedVersion", expectedVersion, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
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
                RegId,
                Status,
                PetugasVerif,
                DischargeDate AS FinalizationDate,
                FinVerifStatus,
                FinVerifPetugas,
                FinVerifDate,
                IsAllocated,
                SettlementInitiated,
                Version
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

    private static DynamicParameters BuildParams(BilrgTataRekeningDto model)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", model.RegId, SqlDbType.VarChar);
        dp.AddParam("@Status", model.Status, SqlDbType.Int);
        dp.AddParam("@PetugasVerif", model.PetugasVerif, SqlDbType.VarChar);
        dp.AddParam("@DischargeDate", model.FinalizationDate, SqlDbType.DateTime);
        dp.AddParam("@FinVerifStatus", model.FinVerifStatus, SqlDbType.Int);
        dp.AddParam("@FinVerifPetugas", model.FinVerifPetugas, SqlDbType.VarChar);
        dp.AddParam("@FinVerifDate", model.FinVerifDate, SqlDbType.DateTime);
        dp.AddParam("@IsAllocated", model.IsAllocated, SqlDbType.Bit);
        dp.AddParam("@SettlementInitiated", model.SettlementInitiated, SqlDbType.Bit);
        dp.AddParam("@Version", model.Version, SqlDbType.Int);
        return dp;
    }
}
