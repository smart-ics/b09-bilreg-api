using System.Data.SqlClient;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ApotekContext.QueueFeature;

public record ApotekQueueEntryDto(
    string AntrianId,
    int NoUrut,
    string ServicePointId,
    string QueueLabel,
    int ApotekPhase,
    int CallCountPenerimaan,
    DateTime LastCalledPenerimaanAt,
    int CallCountSerah,
    DateTime LastCalledSerahAt,
    DateTime TakenAt,
    DateTime ServedAt,
    DateTime PreparedAt,
    DateTime DoneAt,
    DateTime WithdrawnAt,
    DateTime LastSkippedAt,
    int LastSkippedLoketType,
    string WithdrawalReason,
    string WithdrawalUserId,
    bool IsPaymentOverridden,
    string PaymentOverrideReason,
    string PaymentOverridePharmacistId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate);

public interface IApotekQueueEntryDal :
    IInsert<ApotekQueueEntryDto>,
    IUpdate<ApotekQueueEntryDto>,
    IDelete<IApotekQueueEntryKey>,
    IGetData<ApotekQueueEntryDto, IApotekQueueEntryKey>
{
    ApotekQueueEntryDto? Get(string antrianId, int noUrut);
    IEnumerable<ApotekQueueEntryDto> ListActiveByDepo(string depoId);
    IEnumerable<ApotekQueueEntryDto> ListByPhase(string depoId, int phase);
}

public class ApotekQueueEntryDal : IApotekQueueEntryDal
{
    private readonly DatabaseOptions _opt;

    public ApotekQueueEntryDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(ApotekQueueEntryDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptQueueEntry (
                AntrianId, NoUrut, ServicePointId, QueueLabel, ApotekPhase,
                CallCountPenerimaan, LastCalledPenerimaanAt, CallCountSerah, LastCalledSerahAt,
                TakenAt, ServedAt, PreparedAt, DoneAt, WithdrawnAt, LastSkippedAt, LastSkippedLoketType,
                WithdrawalReason, WithdrawalUserId, IsPaymentOverridden, PaymentOverrideReason, PaymentOverridePharmacistId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @AntrianId, @NoUrut, @ServicePointId, @QueueLabel, @ApotekPhase,
                @CallCountPenerimaan, @LastCalledPenerimaanAt, @CallCountSerah, @LastCalledSerahAt,
                @TakenAt, @ServedAt, @PreparedAt, @DoneAt, @WithdrawnAt, @LastSkippedAt, @LastSkippedLoketType,
                @WithdrawalReason, @WithdrawalUserId, @IsPaymentOverridden, @PaymentOverrideReason, @PaymentOverridePharmacistId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """, model);
    }

    public void Update(ApotekQueueEntryDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            UPDATE BILRG_AptQueueEntry
            SET ServicePointId = @ServicePointId,
                QueueLabel = @QueueLabel,
                ApotekPhase = @ApotekPhase,
                CallCountPenerimaan = @CallCountPenerimaan,
                LastCalledPenerimaanAt = @LastCalledPenerimaanAt,
                CallCountSerah = @CallCountSerah,
                LastCalledSerahAt = @LastCalledSerahAt,
                TakenAt = @TakenAt,
                ServedAt = @ServedAt,
                PreparedAt = @PreparedAt,
                DoneAt = @DoneAt,
                WithdrawnAt = @WithdrawnAt,
                LastSkippedAt = @LastSkippedAt,
                LastSkippedLoketType = @LastSkippedLoketType,
                WithdrawalReason = @WithdrawalReason,
                WithdrawalUserId = @WithdrawalUserId,
                IsPaymentOverridden = @IsPaymentOverridden,
                PaymentOverrideReason = @PaymentOverrideReason,
                PaymentOverridePharmacistId = @PaymentOverridePharmacistId,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE AntrianId = @AntrianId AND NoUrut = @NoUrut
            """, model);
    }

    public void Delete(IApotekQueueEntryKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(
            "DELETE FROM BILRG_AptQueueEntry WHERE AntrianId = @AntrianId AND NoUrut = @NoUrut",
            new { key.AntrianId, key.NoUrut });
    }

    public ApotekQueueEntryDto GetData(IApotekQueueEntryKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<ApotekQueueEntryDto>(
            "SELECT * FROM BILRG_AptQueueEntry WHERE AntrianId = @AntrianId AND NoUrut = @NoUrut AND VodDate = '3000-01-01'",
            new { key.AntrianId, key.NoUrut })!;
    }

    public ApotekQueueEntryDto? Get(string antrianId, int noUrut)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<ApotekQueueEntryDto>(
            "SELECT * FROM BILRG_AptQueueEntry WHERE AntrianId = @antrianId AND NoUrut = @noUrut AND VodDate = '3000-01-01'",
            new { antrianId, noUrut });
    }

    public IEnumerable<ApotekQueueEntryDto> ListActiveByDepo(string depoId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<ApotekQueueEntryDto>("""
            SELECT q.*
            FROM BILRG_AptQueueEntry q
            INNER JOIN BILRG_AptServicePoint sp ON q.ServicePointId = sp.ServicePointId
            WHERE sp.DepoFarmasiId = @depoId
              AND q.ApotekPhase NOT IN (6, 8, 9)
              AND q.VodDate = '3000-01-01'
            ORDER BY q.NoUrut ASC
            """, new { depoId });
    }

    public IEnumerable<ApotekQueueEntryDto> ListByPhase(string depoId, int phase)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<ApotekQueueEntryDto>("""
            SELECT q.*
            FROM BILRG_AptQueueEntry q
            INNER JOIN BILRG_AptServicePoint sp ON q.ServicePointId = sp.ServicePointId
            WHERE sp.DepoFarmasiId = @depoId
              AND q.ApotekPhase = @phase
              AND q.VodDate = '3000-01-01'
            ORDER BY q.NoUrut ASC
            """, new { depoId, phase });
    }
}

public class ApotekQueueEntryRepo : IApotekQueueEntryRepo
{
    private readonly IApotekQueueEntryDal _dal;

    public ApotekQueueEntryRepo(IApotekQueueEntryDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(ApotekQueueEntryModel model)
    {
        var existing = _dal.GetData(model);
        var now = DateTime.Now;
        var user = string.IsNullOrWhiteSpace(model.WithdrawalUserId) ? "SYSTEM" : model.WithdrawalUserId;

        if (existing is null)
        {
            var dto = new ApotekQueueEntryDto(
                model.AntrianId,
                model.NoUrut,
                model.ServicePointId,
                model.QueueLabel,
                (int)model.ApotekPhase,
                model.CallCountPenerimaan,
                model.LastCalledPenerimaanAt,
                model.CallCountSerah,
                model.LastCalledSerahAt,
                model.TakenAt,
                model.ServedAt,
                model.PreparedAt,
                model.DoneAt,
                model.WithdrawnAt,
                model.LastSkippedAt,
                (int)model.LastSkippedLoketType,
                model.WithdrawalReason,
                model.WithdrawalUserId,
                model.IsPaymentOverridden,
                model.PaymentOverrideReason,
                model.PaymentOverridePharmacistId,
                CrtUser: user,
                CrtDate: now,
                UpdUser: user,
                UpdDate: now,
                VodUser: "",
                VodDate: new DateTime(3000, 1, 1));
            _dal.Insert(dto);
        }
        else
        {
            var dto = new ApotekQueueEntryDto(
                model.AntrianId,
                model.NoUrut,
                model.ServicePointId,
                model.QueueLabel,
                (int)model.ApotekPhase,
                model.CallCountPenerimaan,
                model.LastCalledPenerimaanAt,
                model.CallCountSerah,
                model.LastCalledSerahAt,
                model.TakenAt,
                model.ServedAt,
                model.PreparedAt,
                model.DoneAt,
                model.WithdrawnAt,
                model.LastSkippedAt,
                (int)model.LastSkippedLoketType,
                model.WithdrawalReason,
                model.WithdrawalUserId,
                model.IsPaymentOverridden,
                model.PaymentOverrideReason,
                model.PaymentOverridePharmacistId,
                CrtUser: existing.CrtUser,
                CrtDate: existing.CrtDate,
                UpdUser: user,
                UpdDate: now,
                VodUser: existing.VodUser,
                VodDate: existing.VodDate);
            _dal.Update(dto);
        }
    }

    public MayBe<ApotekQueueEntryModel> LoadEntity(IApotekQueueEntryKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<ApotekQueueEntryModel>.None : MayBe.From(ToModel(dto));
    }

    public IReadOnlyList<ApotekQueueEntryModel> ListActiveByDepo(string depoId)
        => _dal.ListActiveByDepo(depoId).Select(ToModel).ToList();

    public IReadOnlyList<ApotekQueueEntryModel> ListByPhase(string depoId, ApotekPhaseEnum phase)
        => _dal.ListByPhase(depoId, (int)phase).Select(ToModel).ToList();

    private static ApotekQueueEntryModel ToModel(ApotekQueueEntryDto dto)
    {
        return ApotekQueueEntryModel.Rehydrate(
            dto.AntrianId,
            dto.NoUrut,
            dto.ServicePointId,
            dto.QueueLabel,
            (ApotekPhaseEnum)dto.ApotekPhase,
            dto.CallCountPenerimaan,
            dto.LastCalledPenerimaanAt,
            dto.CallCountSerah,
            dto.LastCalledSerahAt,
            dto.TakenAt,
            dto.ServedAt,
            dto.PreparedAt,
            dto.DoneAt,
            dto.WithdrawnAt,
            dto.LastSkippedAt,
            (LoketTypeEnum)dto.LastSkippedLoketType,
            dto.WithdrawalReason,
            dto.WithdrawalUserId,
            dto.IsPaymentOverridden,
            dto.PaymentOverrideReason,
            dto.PaymentOverridePharmacistId);
    }
}
