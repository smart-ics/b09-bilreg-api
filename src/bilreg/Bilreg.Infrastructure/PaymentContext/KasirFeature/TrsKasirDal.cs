using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public class TrsKasirDal : ITrsKasirDal
{
    private readonly DatabaseOptions _opt;

    public TrsKasirDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TrsKasirDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_TrsKasir (
                TrsKasirId, TglTrs, JamTrs, TglJamTrs, JenisTrs, ShiftId, KasirId,
                RegId, PasienId, PasienName, OrderRef, Keterangan, TotalNilai,
                StatusTrs, TglVoid, JamVoid, PetugasVoid, AlasanVoid,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            ) VALUES (
                @TrsKasirId, @TglTrs, @JamTrs, @TglJamTrs, @JenisTrs, @ShiftId, @KasirId,
                @RegId, @PasienId, @PasienName, @OrderRef, @Keterangan, @TotalNilai,
                @StatusTrs, @TglVoid, @JamVoid, @PetugasVoid, @AlasanVoid,
                @crttgl, @crtjam, @crtusr, @updtgl, @updjam, @updusr
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TrsKasirId", dto.TrsKasirId, SqlDbType.VarChar);
        dp.AddParam("@TglTrs", dto.TglTrs, SqlDbType.VarChar);
        dp.AddParam("@JamTrs", dto.JamTrs, SqlDbType.VarChar);
        dp.AddParam("@TglJamTrs", dto.TglJamTrs, SqlDbType.VarChar);
        dp.AddParam("@JenisTrs", dto.JenisTrs, SqlDbType.VarChar);
        dp.AddParam("@ShiftId", dto.ShiftId, SqlDbType.VarChar);
        dp.AddParam("@KasirId", dto.KasirId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@OrderRef", dto.OrderRef ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@Keterangan", dto.Keterangan ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TotalNilai", dto.TotalNilai, SqlDbType.Decimal);
        dp.AddParam("@StatusTrs", dto.StatusTrs, SqlDbType.VarChar);
        dp.AddParam("@TglVoid", dto.TglVoid, SqlDbType.VarChar);
        dp.AddParam("@JamVoid", dto.JamVoid, SqlDbType.VarChar);
        dp.AddParam("@PetugasVoid", dto.PetugasVoid ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@AlasanVoid", dto.AlasanVoid ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@crttgl", dto.crttgl, SqlDbType.VarChar);
        dp.AddParam("@crtjam", dto.crtjam, SqlDbType.VarChar);
        dp.AddParam("@crtusr", dto.crtusr, SqlDbType.VarChar);
        dp.AddParam("@updtgl", dto.updtgl, SqlDbType.VarChar);
        dp.AddParam("@updjam", dto.updjam, SqlDbType.VarChar);
        dp.AddParam("@updusr", dto.updusr, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TrsKasirDto dto)
    {
        const string sql = """
            UPDATE BILRG_TrsKasir
            SET TglTrs = @TglTrs,
                JamTrs = @JamTrs,
                TglJamTrs = @TglJamTrs,
                JenisTrs = @JenisTrs,
                ShiftId = @ShiftId,
                KasirId = @KasirId,
                RegId = @RegId,
                PasienId = @PasienId,
                PasienName = @PasienName,
                OrderRef = @OrderRef,
                Keterangan = @Keterangan,
                TotalNilai = @TotalNilai,
                StatusTrs = @StatusTrs,
                TglVoid = @TglVoid,
                JamVoid = @JamVoid,
                PetugasVoid = @PetugasVoid,
                AlasanVoid = @AlasanVoid,
                updtgl = @updtgl,
                updjam = @updjam,
                updusr = @updusr
            WHERE TrsKasirId = @TrsKasirId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TrsKasirId", dto.TrsKasirId, SqlDbType.VarChar);
        dp.AddParam("@TglTrs", dto.TglTrs, SqlDbType.VarChar);
        dp.AddParam("@JamTrs", dto.JamTrs, SqlDbType.VarChar);
        dp.AddParam("@TglJamTrs", dto.TglJamTrs, SqlDbType.VarChar);
        dp.AddParam("@JenisTrs", dto.JenisTrs, SqlDbType.VarChar);
        dp.AddParam("@ShiftId", dto.ShiftId, SqlDbType.VarChar);
        dp.AddParam("@KasirId", dto.KasirId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@OrderRef", dto.OrderRef ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@Keterangan", dto.Keterangan ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TotalNilai", dto.TotalNilai, SqlDbType.Decimal);
        dp.AddParam("@StatusTrs", dto.StatusTrs, SqlDbType.VarChar);
        dp.AddParam("@TglVoid", dto.TglVoid, SqlDbType.VarChar);
        dp.AddParam("@JamVoid", dto.JamVoid, SqlDbType.VarChar);
        dp.AddParam("@PetugasVoid", dto.PetugasVoid ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@AlasanVoid", dto.AlasanVoid ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@updtgl", dto.updtgl, SqlDbType.VarChar);
        dp.AddParam("@updjam", dto.updjam, SqlDbType.VarChar);
        dp.AddParam("@updusr", dto.updusr, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TrsKasirDto GetData(ITrsKasirKey key)
    {
        const string sql = """
            SELECT
                TrsKasirId, TglTrs, JamTrs, TglJamTrs, JenisTrs, ShiftId, KasirId,
                RegId, PasienId, PasienName, OrderRef, Keterangan, TotalNilai,
                StatusTrs, TglVoid, JamVoid, PetugasVoid, AlasanVoid,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            FROM BILRG_TrsKasir
            WHERE TrsKasirId = @TrsKasirId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TrsKasirId", key.TrsKasirId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TrsKasirDto>(sql, dp);
    }

    public void InsertPayments(IEnumerable<TrsKasirPaymentDto> payments)
    {
        var list = payments?.Select(p => new
        {
            p.PaymentRowId,
            p.TrsKasirId,
            p.NoUrut,
            p.MetodeBayar,
            KodeJenisPayment = p.KodeJenisPayment ?? string.Empty,
            NamaMetodeBayar = p.NamaMetodeBayar ?? string.Empty,
            CoaId = p.CoaId ?? string.Empty,
            p.NilaiBayar,
            p.NilaiKembalian,
            NilaiNetto = p.NilaiNetto != 0 ? p.NilaiNetto : (p.NilaiBayar - p.NilaiKembalian),
            NomorKartu = p.NomorKartu ?? string.Empty,
            NomorReferensi = p.NomorReferensi ?? string.Empty,
            NamaBank = p.NamaBank ?? string.Empty
        }).ToList() ?? [];

        if (list.Count == 0)
            return;

        const string sql = """
            INSERT INTO BILRG_TrsKasirPayment (
                PaymentRowId, TrsKasirId, NoUrut, MetodeBayar,
                KodeJenisPayment, NamaMetodeBayar, CoaId,
                NilaiBayar, NilaiKembalian, NilaiNetto,
                NomorKartu, NomorReferensi, NamaBank
            ) VALUES (
                @PaymentRowId, @TrsKasirId, @NoUrut, @MetodeBayar,
                @KodeJenisPayment, @NamaMetodeBayar, @CoaId,
                @NilaiBayar, @NilaiKembalian, @NilaiNetto,
                @NomorKartu, @NomorReferensi, @NamaBank
            )
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, list);
    }

    public void DeletePayments(ITrsKasirKey key)
    {
        const string sql = """
            DELETE FROM BILRG_TrsKasirPayment
            WHERE TrsKasirId = @TrsKasirId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TrsKasirId", key.TrsKasirId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<TrsKasirPaymentDto> ListPayments(ITrsKasirKey key)
    {
        const string sql = """
            SELECT
                PaymentRowId, TrsKasirId, NoUrut, MetodeBayar,
                KodeJenisPayment, NamaMetodeBayar, CoaId,
                NilaiBayar, NilaiKembalian, NilaiNetto,
                NomorKartu, NomorReferensi, NamaBank
            FROM BILRG_TrsKasirPayment
            WHERE TrsKasirId = @TrsKasirId
            ORDER BY NoUrut ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TrsKasirId", key.TrsKasirId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TrsKasirPaymentDto>(sql, dp);
    }

    public IEnumerable<TrsKasirDto> ListByShift(string shiftId)
    {
        const string sql = """
            SELECT
                TrsKasirId, TglTrs, JamTrs, TglJamTrs, JenisTrs, ShiftId, KasirId,
                RegId, PasienId, PasienName, OrderRef, Keterangan, TotalNilai,
                StatusTrs, TglVoid, JamVoid, PetugasVoid, AlasanVoid,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            FROM BILRG_TrsKasir
            WHERE ShiftId = @ShiftId
            ORDER BY TglJamTrs ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ShiftId", shiftId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TrsKasirDto>(sql, dp);
    }

    public IEnumerable<TrsKasirDto> ListByReg(string regId)
    {
        const string sql = """
            SELECT
                TrsKasirId, TglTrs, JamTrs, TglJamTrs, JenisTrs, ShiftId, KasirId,
                RegId, PasienId, PasienName, OrderRef, Keterangan, TotalNilai,
                StatusTrs, TglVoid, JamVoid, PetugasVoid, AlasanVoid,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            FROM BILRG_TrsKasir
            WHERE RegId = @RegId
            ORDER BY TglJamTrs DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TrsKasirDto>(sql, dp);
    }

    public IEnumerable<TrsKasirDto> ListByTgl(string tglTrs)
    {
        const string sql = """
            SELECT
                TrsKasirId, TglTrs, JamTrs, TglJamTrs, JenisTrs, ShiftId, KasirId,
                RegId, PasienId, PasienName, OrderRef, Keterangan, TotalNilai,
                StatusTrs, TglVoid, JamVoid, PetugasVoid, AlasanVoid,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            FROM BILRG_TrsKasir
            WHERE TglTrs = @TglTrs
            ORDER BY TglJamTrs ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TglTrs", tglTrs, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TrsKasirDto>(sql, dp);
    }
}
