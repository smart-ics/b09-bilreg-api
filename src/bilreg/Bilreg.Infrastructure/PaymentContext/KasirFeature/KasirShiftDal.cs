using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public class KasirShiftDal : IKasirShiftDal
{
    private readonly DatabaseOptions _opt;

    public KasirShiftDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KasirShiftDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_KasirShift (
                ShiftId, TglShift, KodeShift, KasirId, Loket, TglJamBuka,
                SaldoAwal, TglJamTutup, SaldoAkhirFisik, TotalTerimaSistem,
                TotalKeluarSistem, StatusShift,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            ) VALUES (
                @ShiftId, @TglShift, @KodeShift, @KasirId, @Loket, @TglJamBuka,
                @SaldoAwal, @TglJamTutup, @SaldoAkhirFisik, @TotalTerimaSistem,
                @TotalKeluarSistem, @StatusShift,
                @crttgl, @crtjam, @crtusr, @updtgl, @updjam, @updusr
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ShiftId", dto.ShiftId, SqlDbType.VarChar);
        dp.AddParam("@TglShift", dto.TglShift, SqlDbType.VarChar);
        dp.AddParam("@KodeShift", dto.KodeShift, SqlDbType.VarChar);
        dp.AddParam("@KasirId", dto.KasirId, SqlDbType.VarChar);
        dp.AddParam("@Loket", dto.Loket ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TglJamBuka", dto.TglJamBuka, SqlDbType.VarChar);
        dp.AddParam("@SaldoAwal", dto.SaldoAwal, SqlDbType.Decimal);
        dp.AddParam("@TglJamTutup", dto.TglJamTutup, SqlDbType.VarChar);
        dp.AddParam("@SaldoAkhirFisik", dto.SaldoAkhirFisik, SqlDbType.Decimal);
        dp.AddParam("@TotalTerimaSistem", dto.TotalTerimaSistem, SqlDbType.Decimal);
        dp.AddParam("@TotalKeluarSistem", dto.TotalKeluarSistem, SqlDbType.Decimal);
        dp.AddParam("@StatusShift", dto.StatusShift, SqlDbType.VarChar);
        dp.AddParam("@crttgl", dto.crttgl, SqlDbType.VarChar);
        dp.AddParam("@crtjam", dto.crtjam, SqlDbType.VarChar);
        dp.AddParam("@crtusr", dto.crtusr, SqlDbType.VarChar);
        dp.AddParam("@updtgl", dto.updtgl, SqlDbType.VarChar);
        dp.AddParam("@updjam", dto.updjam, SqlDbType.VarChar);
        dp.AddParam("@updusr", dto.updusr, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KasirShiftDto dto)
    {
        const string sql = """
            UPDATE BILRG_KasirShift
            SET TglShift = @TglShift,
                KodeShift = @KodeShift,
                KasirId = @KasirId,
                Loket = @Loket,
                TglJamBuka = @TglJamBuka,
                SaldoAwal = @SaldoAwal,
                TglJamTutup = @TglJamTutup,
                SaldoAkhirFisik = @SaldoAkhirFisik,
                TotalTerimaSistem = @TotalTerimaSistem,
                TotalKeluarSistem = @TotalKeluarSistem,
                StatusShift = @StatusShift,
                updtgl = @updtgl,
                updjam = @updjam,
                updusr = @updusr
            WHERE ShiftId = @ShiftId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ShiftId", dto.ShiftId, SqlDbType.VarChar);
        dp.AddParam("@TglShift", dto.TglShift, SqlDbType.VarChar);
        dp.AddParam("@KodeShift", dto.KodeShift, SqlDbType.VarChar);
        dp.AddParam("@KasirId", dto.KasirId, SqlDbType.VarChar);
        dp.AddParam("@Loket", dto.Loket ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TglJamBuka", dto.TglJamBuka, SqlDbType.VarChar);
        dp.AddParam("@SaldoAwal", dto.SaldoAwal, SqlDbType.Decimal);
        dp.AddParam("@TglJamTutup", dto.TglJamTutup, SqlDbType.VarChar);
        dp.AddParam("@SaldoAkhirFisik", dto.SaldoAkhirFisik, SqlDbType.Decimal);
        dp.AddParam("@TotalTerimaSistem", dto.TotalTerimaSistem, SqlDbType.Decimal);
        dp.AddParam("@TotalKeluarSistem", dto.TotalKeluarSistem, SqlDbType.Decimal);
        dp.AddParam("@StatusShift", dto.StatusShift, SqlDbType.VarChar);
        dp.AddParam("@updtgl", dto.updtgl, SqlDbType.VarChar);
        dp.AddParam("@updjam", dto.updjam, SqlDbType.VarChar);
        dp.AddParam("@updusr", dto.updusr, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KasirShiftDto GetData(IKasirShiftKey key)
    {
        const string sql = """
            SELECT
                ShiftId, TglShift, KodeShift, KasirId, Loket, TglJamBuka,
                SaldoAwal, TglJamTutup, SaldoAkhirFisik, TotalTerimaSistem,
                TotalKeluarSistem, StatusShift,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            FROM BILRG_KasirShift
            WHERE ShiftId = @ShiftId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ShiftId", key.ShiftId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<KasirShiftDto>(sql, dp);
    }

    public KasirShiftDto? GetActiveShift(string kasirId)
    {
        const string sql = """
            SELECT TOP 1
                ShiftId, TglShift, KodeShift, KasirId, Loket, TglJamBuka,
                SaldoAwal, TglJamTutup, SaldoAkhirFisik, TotalTerimaSistem,
                TotalKeluarSistem, StatusShift,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            FROM BILRG_KasirShift
            WHERE KasirId = @KasirId AND StatusShift = 'BUKA'
            ORDER BY TglJamBuka DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@KasirId", kasirId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<KasirShiftDto>(sql, dp);
    }

    public IEnumerable<KasirShiftDto> ListByTgl(string tglShift)
    {
        const string sql = """
            SELECT
                ShiftId, TglShift, KodeShift, KasirId, Loket, TglJamBuka,
                SaldoAwal, TglJamTutup, SaldoAkhirFisik, TotalTerimaSistem,
                TotalKeluarSistem, StatusShift,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            FROM BILRG_KasirShift
            WHERE TglShift = @TglShift
            ORDER BY TglJamBuka ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TglShift", tglShift, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KasirShiftDto>(sql, dp);
    }
}
