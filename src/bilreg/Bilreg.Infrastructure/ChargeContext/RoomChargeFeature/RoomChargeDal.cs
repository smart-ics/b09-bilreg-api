using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public class RoomChargeDal : IRoomChargeDal
{
    private const string SelectColumns = """
        aa.RoomChargeId, aa.PakaiBedId, aa.RegId, aa.PasienId,
        aa.BangsalId, aa.KamarId, aa.BedId,
        aa.ChargeCycleDate, aa.TimeCharge, aa.ChargeTriggerType,
        aa.PhysicalClassId, aa.PayerClassId, aa.BillingClassId,
        aa.IsTitipKelas, aa.IsNaikKelas, aa.DifferentialAmount,
        aa.IsRoomingIn, aa.MotherPakaiBedId,
        aa.ChargedUnits, aa.BaseTariff, aa.DiscountAmount, aa.TotalAmount,
        aa.RoomChargeStatus, aa.BillingItemRefId, aa.ReversalOfRoomChargeId, aa.ReversalReason,
        aa.CreatedBy, aa.CreatedAt, aa.UpdatedBy, aa.UpdatedAt
        """;

    private readonly DatabaseOptions _opt;

    public RoomChargeDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(RoomChargeDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_RnaRoomCharge (
                RoomChargeId, PakaiBedId, RegId, PasienId,
                BangsalId, KamarId, BedId,
                ChargeCycleDate, TimeCharge, ChargeTriggerType,
                PhysicalClassId, PayerClassId, BillingClassId,
                IsTitipKelas, IsNaikKelas, DifferentialAmount,
                IsRoomingIn, MotherPakaiBedId,
                ChargedUnits, BaseTariff, DiscountAmount, TotalAmount,
                RoomChargeStatus, BillingItemRefId, ReversalOfRoomChargeId, ReversalReason,
                CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
            VALUES (
                @RoomChargeId, @PakaiBedId, @RegId, @PasienId,
                @BangsalId, @KamarId, @BedId,
                @ChargeCycleDate, @TimeCharge, @ChargeTriggerType,
                @PhysicalClassId, @PayerClassId, @BillingClassId,
                @IsTitipKelas, @IsNaikKelas, @DifferentialAmount,
                @IsRoomingIn, @MotherPakaiBedId,
                @ChargedUnits, @BaseTariff, @DiscountAmount, @TotalAmount,
                @RoomChargeStatus, @BillingItemRefId, @ReversalOfRoomChargeId, @ReversalReason,
                @CreatedBy, @CreatedAt, @UpdatedBy, @UpdatedAt)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public void Update(RoomChargeDto dto)
    {
        const string sql = """
            UPDATE BILRG_RnaRoomCharge
            SET
                PakaiBedId = @PakaiBedId,
                RegId = @RegId,
                PasienId = @PasienId,
                BangsalId = @BangsalId,
                KamarId = @KamarId,
                BedId = @BedId,
                ChargeCycleDate = @ChargeCycleDate,
                TimeCharge = @TimeCharge,
                ChargeTriggerType = @ChargeTriggerType,
                PhysicalClassId = @PhysicalClassId,
                PayerClassId = @PayerClassId,
                BillingClassId = @BillingClassId,
                IsTitipKelas = @IsTitipKelas,
                IsNaikKelas = @IsNaikKelas,
                DifferentialAmount = @DifferentialAmount,
                IsRoomingIn = @IsRoomingIn,
                MotherPakaiBedId = @MotherPakaiBedId,
                ChargedUnits = @ChargedUnits,
                BaseTariff = @BaseTariff,
                DiscountAmount = @DiscountAmount,
                TotalAmount = @TotalAmount,
                RoomChargeStatus = @RoomChargeStatus,
                BillingItemRefId = @BillingItemRefId,
                ReversalOfRoomChargeId = @ReversalOfRoomChargeId,
                ReversalReason = @ReversalReason,
                UpdatedBy = @UpdatedBy,
                UpdatedAt = @UpdatedAt
            WHERE RoomChargeId = @RoomChargeId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public RoomChargeDto? GetData(IRoomChargeKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return GetData(key.RoomChargeId);
    }

    public RoomChargeDto? GetData(string roomChargeId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RnaRoomCharge aa
            WHERE aa.RoomChargeId = @RoomChargeId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RoomChargeId", roomChargeId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RoomChargeDto>(sql, dp);
    }

    public RoomChargeDto? GetByPakaiBedAndDate(string pakaiBedId, string chargeCycleDate)
    {
        var sql = $"""
            SELECT TOP 1 {SelectColumns}
            FROM BILRG_RnaRoomCharge aa
            WHERE aa.PakaiBedId = @PakaiBedId
              AND aa.ChargeCycleDate = @ChargeCycleDate
              AND aa.RoomChargeStatus <> 'Reversed'
            ORDER BY aa.CreatedAt DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", pakaiBedId, SqlDbType.VarChar);
        dp.AddParam("@ChargeCycleDate", chargeCycleDate, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<RoomChargeDto>(sql, dp);
        return list?.FirstOrDefault();
    }

    public IEnumerable<RoomChargeDto> ListDataByReg(string regId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RnaRoomCharge aa
            WHERE aa.RegId = @RegId
            ORDER BY aa.ChargeCycleDate ASC, aa.TimeCharge ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RoomChargeDto>(sql, dp) ?? [];
    }

    public IEnumerable<RoomChargeDto> ListActiveByBangsalAndDate(string bangsalId, string chargeCycleDate)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_RnaRoomCharge aa
            WHERE aa.BangsalId = @BangsalId
              AND aa.ChargeCycleDate = @ChargeCycleDate
              AND aa.RoomChargeStatus <> 'Reversed'
            ORDER BY aa.TimeCharge ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BangsalId", bangsalId, SqlDbType.VarChar);
        dp.AddParam("@ChargeCycleDate", chargeCycleDate, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RoomChargeDto>(sql, dp) ?? [];
    }

    private static DynamicParameters MapParams(RoomChargeDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@RoomChargeId", dto.RoomChargeId, SqlDbType.VarChar);
        dp.AddParam("@PakaiBedId", dto.PakaiBedId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@BangsalId", dto.BangsalId, SqlDbType.VarChar);
        dp.AddParam("@KamarId", dto.KamarId, SqlDbType.VarChar);
        dp.AddParam("@BedId", dto.BedId, SqlDbType.VarChar);

        dp.AddParam("@ChargeCycleDate", dto.ChargeCycleDate, SqlDbType.VarChar);
        dp.AddParam("@TimeCharge", dto.TimeCharge, SqlDbType.DateTime2);
        dp.AddParam("@ChargeTriggerType", dto.ChargeTriggerType, SqlDbType.VarChar);

        dp.AddParam("@PhysicalClassId", dto.PhysicalClassId, SqlDbType.VarChar);
        dp.AddParam("@PayerClassId", dto.PayerClassId, SqlDbType.VarChar);
        dp.AddParam("@BillingClassId", dto.BillingClassId, SqlDbType.VarChar);
        dp.AddParam("@IsTitipKelas", dto.IsTitipKelas, SqlDbType.Bit);
        dp.AddParam("@IsNaikKelas", dto.IsNaikKelas, SqlDbType.Bit);
        dp.AddParam("@DifferentialAmount", dto.DifferentialAmount, SqlDbType.Decimal);

        dp.AddParam("@IsRoomingIn", dto.IsRoomingIn, SqlDbType.Bit);
        dp.AddParam("@MotherPakaiBedId", dto.MotherPakaiBedId, SqlDbType.VarChar);

        dp.AddParam("@ChargedUnits", dto.ChargedUnits, SqlDbType.Decimal);
        dp.AddParam("@BaseTariff", dto.BaseTariff, SqlDbType.Decimal);
        dp.AddParam("@DiscountAmount", dto.DiscountAmount, SqlDbType.Decimal);
        dp.AddParam("@TotalAmount", dto.TotalAmount, SqlDbType.Decimal);

        dp.AddParam("@RoomChargeStatus", dto.RoomChargeStatus, SqlDbType.VarChar);
        dp.AddParam("@BillingItemRefId", dto.BillingItemRefId, SqlDbType.VarChar);
        dp.AddParam("@ReversalOfRoomChargeId", dto.ReversalOfRoomChargeId, SqlDbType.VarChar);
        dp.AddParam("@ReversalReason", dto.ReversalReason, SqlDbType.VarChar);

        dp.AddParam("@CreatedBy", dto.CreatedBy, SqlDbType.VarChar);
        dp.AddParam("@CreatedAt", dto.CreatedAt, SqlDbType.DateTime2);
        dp.AddParam("@UpdatedBy", dto.UpdatedBy, SqlDbType.VarChar);
        dp.AddParam("@UpdatedAt", dto.UpdatedAt, SqlDbType.DateTime2);

        return dp;
    }
}
