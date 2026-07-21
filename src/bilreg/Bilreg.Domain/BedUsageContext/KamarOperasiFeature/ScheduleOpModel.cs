using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpModel : IScheduleOpKey
{
    private readonly List<ScheduleOpPpaType> _listPpa;

    #region CREATION
    public ScheduleOpModel(string scheduleOpId, DateTime scheduleDate, AuditTrailType auditTrail,
        OrderOpReff orderOp, PasienReff pasien, UrgencyLevelEnum urgencyLevel, int durasi,
        DateTime tglOp, KamarReff kamarOp, RegReff reg, PpaReff teamLead,
        DateTime startOpDate, DateTime endOpDate, OpCaseStateEnum opState,
        IEnumerable<ScheduleOpPpaType> listPpa)
    {
        ScheduleOpId = scheduleOpId;
        ScheduleOpDate = scheduleDate;
        AuditTrail = auditTrail;
        OrderOp = orderOp;
        Pasien = pasien;
        UrgencyLevel = urgencyLevel;
        Durasi = durasi;
        TglOp = tglOp;
        KamarOp = kamarOp;
        Reg = reg;
        TeamLead = teamLead;
        StartOpDate = startOpDate;
        EndOpDate = endOpDate;
        OrderOpState = opState;
        _listPpa = listPpa.ToList() ?? [];
    }

    public static ScheduleOpModel Default 
        => new ScheduleOpModel("-", new DateTime(3000,1,1), AuditTrailType.Default,
            OrderOpModel.Default.ToReff(), PasienModel.Default.ToReff(),
            UrgencyLevelEnum.Elective, 0,
            new DateTime(3000,1,1), KamarType.Default.ToReff(),
            RegModel.Default.ToReff(), PpaType.Default.ToReff(),
            new DateTime(3000, 1, 1), new DateTime(3000, 1, 1),
            OpCaseStateEnum.Scheduled, []);
    public static IScheduleOpKey Key(string id)
        => new ScheduleOpModel(id, new DateTime(3000,1,1), AuditTrailType.Default,
            OrderOpModel.Default.ToReff(), PasienModel.Default.ToReff(),
            UrgencyLevelEnum.Elective, 0,
            new DateTime(3000,1,1), KamarType.Default.ToReff(),
            RegModel.Default.ToReff(), PpaType.Default.ToReff(),
            new DateTime(3000, 1, 1), new DateTime(3000, 1, 1),
            OpCaseStateEnum.Scheduled, []);

    public static ScheduleOpModel CreateFromOrder(OrderOpModel orderOp, string userId,
        KamarType kamar, PpaType teamLeader, DateTime tglOp, DateTime createdAt = default)
    {
        var newId = Ulid.NewUlid().ToString();
        var audit = new AuditTrailType(new AuditInfoType(userId, createdAt),
            AuditInfoType.Default, AuditInfoType.Default);
        var result = new ScheduleOpModel(newId, createdAt, audit, orderOp.ToReff(),
            orderOp.Pasien, orderOp.UrgencyLevel, orderOp.EstimasiDurasiInMinutes,
            tglOp, kamar.ToReff(), orderOp.Reg, PpaType.Default.ToReff(),
            new DateTime(3000, 1, 1), new DateTime(3000, 1, 1),
            OpCaseStateEnum.Scheduled, []);

        if (string.IsNullOrWhiteSpace(teamLeader.PpaId) ||
            teamLeader.PpaId == "-")
            return result;

        result.AddPpa(teamLeader, userId, createdAt);
        result.AssignLeader(teamLeader, userId, createdAt);
        return result;
    }

    public static ScheduleOpModel CloneFrom(ScheduleOpModel model, DateTime clonedAt = default)
    {
        var newId = Ulid.NewUlid().ToString();
        var audit = new AuditTrailType(new AuditInfoType(model.AuditTrail.Voided.UserId, clonedAt),
            AuditInfoType.Default, AuditInfoType.Default);
        var result = new ScheduleOpModel(newId, clonedAt, audit, model.OrderOp, model.Pasien,
            model.UrgencyLevel, model.Durasi, model.TglOp, model.KamarOp, model.Reg, model.TeamLead,
            model.StartOpDate, model.EndOpDate, model.OrderOpState, model.ListPpa);
        return result;
    }

    #endregion

    #region PROPERTIES
    public string ScheduleOpId { get; init; }
    public DateTime ScheduleOpDate { get; init; }
    public AuditTrailType AuditTrail { get; init; }
    //  order
    public OrderOpReff OrderOp { get; init; }
    public PasienReff Pasien { get; init; }
    public UrgencyLevelEnum UrgencyLevel { get; init; }
    public int Durasi { get; private set; }
    //  schedule
    public DateTime TglOp { get; private set; }
    public KamarReff KamarOp { get; private set; }
    public RegReff Reg { get; private set; }
    public PpaReff TeamLead { get; private set; }

    public DateTime StartOpDate { get; private set; }
    public DateTime EndOpDate { get; private set; }

    public OpCaseStateEnum OrderOpState { get; private set; }

    public IEnumerable<ScheduleOpPpaType> ListPpa => _listPpa;
    #endregion

    #region BEHAVIOUR
    public void SetSchedule(DateTime tglOp, KamarReff kamarOp, int durasi, string userId, DateTime updatedAt = default)
    {
        TglOp = tglOp;
        KamarOp = kamarOp;
        Durasi = durasi;
        AuditTrail.Modif(userId, updatedAt);
    }

    public void AddPpa(PpaType ppa, string userId, DateTime updatedAt = default)
    {
        var profesi = ppa.ListSatTugas.FirstOrDefault()?.SatTugas?.Profesi;
        if (profesi is null)
            throw new ArgumentException("Profesi PPA tidak valid");
        var existing = _listPpa.FirstOrDefault(x => x.Ppa.PpaId == ppa.PpaId);
        if (existing is not null)
            throw new ArgumentException("PPA sudah ada dalam schedule");
        var noUrut = _listPpa.DefaultIfEmpty(ScheduleOpPpaType.Default).Max(x => x.NoUrut);
        noUrut++;
        var newPpaMember = new ScheduleOpPpaType(noUrut, ppa.ToReff(), profesi, GroupSpesialisType.Default);
        _listPpa.Add(newPpaMember);
        AuditTrail.Modif(userId, updatedAt);
    }

    public void RemovePpa(PpaType ppa, string userId, DateTime updatedAt = default)
    {
        _listPpa.RemoveAll(x => x.Ppa.PpaId == ppa.PpaId);
        var newList = new List<ScheduleOpPpaType>();
        var i = 0;
        foreach (var item in _listPpa)
        {
            i++;
            newList.Add(item with { NoUrut = i });
        }
        _listPpa.Clear();
        _listPpa.AddRange(newList);
        AuditTrail.Modif(userId, updatedAt);
    }

    public void AssignLeader(PpaType ppa, string userId, DateTime updatedAt = default)
    {
        // harus dokter
        var profesi = ppa.ListSatTugas.FirstOrDefault()?.SatTugas?.Profesi;
        if (profesi is null)
            throw new ArgumentException("Profesi PPA tidak valid");
        if (profesi != ProfesiType.Dokter)
            throw new ArgumentException("PPA harus dokter");
        //  harus terdaftar di listPpa
        var existing = _listPpa.FirstOrDefault(x => x.Ppa.PpaId == ppa.PpaId);
        if (existing is null)
            throw new ArgumentException("PPA harus terdaftar di member");
        TeamLead = ppa.ToReff();
        AuditTrail.Modif(userId, updatedAt);
    }

    public void CancelSchedule(string userId, DateTime cancelledAt = default)
    {
        AuditTrail.Batal(userId, cancelledAt);
    }

    public ScheduleOpReff ToReff() =>
        new ScheduleOpReff(ScheduleOpId, TglOp);

    #endregion
}

public interface IScheduleOpKey
{
    string ScheduleOpId { get; }
}
