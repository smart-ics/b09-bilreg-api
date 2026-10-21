using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpModel : IScheduleOpKey
{
    private readonly List<ScheduleOpPpaType> _listPpa;
    private readonly List<ScheduleOpHistType> _listHistory;

    #region CREATION
    public ScheduleOpModel(string scheduleOpId, DateTime scheduleDate, AuditTrailType auditTrail,
        OrderOpReff orderOp, PasienReff pasien, UrgencyLevelEnum urgencyLevel, int durasi,
        DateTime tglOp, KamarReff kamarOp, RegReff reg, PpaReff teamLead,
        DateTime startOpDate, DateTime endOpDate, OpCaseStateEnum opState,
        IEnumerable<ScheduleOpPpaType> listPpa,
        string alasanBatal = "",
        int turnoverBuffer = 30,
        IEnumerable<ScheduleOpHistType> listHistory = null)
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
        _listPpa = listPpa?.OrderBy(x => x.NoUrut).ToList() ?? [];
        AlasanBatal = alasanBatal ?? string.Empty;
        TurnoverBuffer = turnoverBuffer <= 0 ? 30 : turnoverBuffer;
        _listHistory = listHistory?.ToList() ?? [];

        EnsureTeamLeadInvariant();
    }

    public static ScheduleOpModel Default 
        => new ScheduleOpModel("-", new DateTime(3000, 1, 1), AuditTrailType.Default,
            OrderOpModel.Default.ToReff(), PasienModel.Default.ToReff(),
            UrgencyLevelEnum.Elective, 0,
            new DateTime(3000, 1, 1), KamarType.Default.ToReff(),
            RegModel.Default.ToReff(), PpaType.Default.ToReff(),
            new DateTime(3000, 1, 1), new DateTime(3000, 1, 1),
            OpCaseStateEnum.Scheduled, [], "", 30, []);

    public static IScheduleOpKey Key(string id)
        => new ScheduleOpModel(id, new DateTime(3000, 1, 1), AuditTrailType.Default,
            OrderOpModel.Default.ToReff(), PasienModel.Default.ToReff(),
            UrgencyLevelEnum.Elective, 0,
            new DateTime(3000, 1, 1), KamarType.Default.ToReff(),
            RegModel.Default.ToReff(), PpaType.Default.ToReff(),
            new DateTime(3000, 1, 1), new DateTime(3000, 1, 1),
            OpCaseStateEnum.Scheduled, [], "", 30, []);

    public static ScheduleOpModel CreateFromOrder(OrderOpModel orderOp, string userId,
        KamarType kamar, PpaType dokterOperator, DateTime tglOp, DateTime createdAt = default)
    {
        if (dokterOperator is null || string.IsNullOrWhiteSpace(dokterOperator.PpaId) || dokterOperator.PpaId == "-")
            throw new ArgumentException("Dokter Operator Utama wajib ditentukan.");

        var profesi = dokterOperator.ListSatTugas.FirstOrDefault()?.SatTugas?.Profesi;
        if (profesi is null || profesi != ProfesiType.Dokter)
            throw new ArgumentException("PPA yang ditugaskan sebagai Operator Utama harus berprofesi Dokter.");

        var newId = Ulid.NewUlid().ToString();
        var audit = new AuditTrailType(new AuditInfoType(userId, createdAt),
            AuditInfoType.Default, AuditInfoType.Default);
        var firstPpaMember = new ScheduleOpPpaType(1, dokterOperator.ToReff(), profesi,
            dokterOperator.GroupSpesialis ?? GroupSpesialisType.Default);

        var result = new ScheduleOpModel(newId, createdAt, audit, orderOp.ToReff(),
            orderOp.Pasien, orderOp.UrgencyLevel, orderOp.EstimasiDurasiInMinutes,
            tglOp, kamar.ToReff(), orderOp.Reg, dokterOperator.ToReff(),
            new DateTime(3000, 1, 1), new DateTime(3000, 1, 1),
            OpCaseStateEnum.Scheduled, [firstPpaMember], "", 30, []);

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

    public string AlasanBatal { get; private set; }
    public int TurnoverBuffer { get; private set; }

    public IEnumerable<ScheduleOpPpaType> ListPpa => _listPpa;
    public IEnumerable<ScheduleOpHistType> ListHistory => _listHistory;
    #endregion

    #region BEHAVIOUR
    public void SetSchedule(DateTime tglOp, KamarReff kamarOp, int durasi, string userId, DateTime updatedAt = default, int turnoverBuffer = 30)
    {
        TglOp = tglOp;
        KamarOp = kamarOp;
        Durasi = durasi;
        TurnoverBuffer = turnoverBuffer > 0 ? turnoverBuffer : 30;
        AuditTrail.Modif(userId, updatedAt);
    }

    public void Reschedule(
        DateTime newTglOp,
        KamarReff newKamar,
        int newDurasi,
        int newTurnoverBuffer,
        PpaReff newTeamLead,
        string alasanPerubahan,
        bool isOverride,
        string overrideReason,
        string userId,
        DateTime occurredAt)
    {
        Guard.Against.NullOrWhiteSpace(alasanPerubahan, nameof(alasanPerubahan));

        var nextNoUrut = _listHistory.Count + 1;
        var hist = new ScheduleOpHistType(
            nextNoUrut,
            TglOp,
            newTglOp,
            Durasi,
            newDurasi,
            KamarOp,
            newKamar,
            TeamLead,
            newTeamLead,
            alasanPerubahan,
            isOverride,
            overrideReason ?? string.Empty,
            userId,
            occurredAt,
            "RESCHEDULE");
        _listHistory.Add(hist);

        TglOp = newTglOp;
        KamarOp = newKamar;
        Durasi = newDurasi;
        TurnoverBuffer = newTurnoverBuffer;
        TeamLead = newTeamLead;

        if (newTeamLead != null && !newTeamLead.IsDefault && !string.IsNullOrWhiteSpace(newTeamLead.PpaId) && newTeamLead.PpaId != "-")
        {
            EnsureTeamLeadInvariant();
        }

        AuditTrail.Modif(userId, occurredAt);
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
        if (ppa.PpaId == TeamLead.PpaId)
            throw new ArgumentException("Dokter Operator Utama aktif tidak dapat dihapus dari tim operasi. Alihkan peran TeamLead terlebih dahulu.");

        _listPpa.RemoveAll(x => x.Ppa.PpaId == ppa.PpaId);
        EnsureTeamLeadInvariant();
        AuditTrail.Modif(userId, updatedAt);
    }

    public void AssignLeader(PpaType ppa, string alasanPerubahan, string userId, DateTime updatedAt = default, bool isOverride = false, string overrideReason = "")
    {
        Guard.Against.NullOrWhiteSpace(alasanPerubahan, nameof(alasanPerubahan));

        // harus dokter
        var profesi = ppa.ListSatTugas.FirstOrDefault()?.SatTugas?.Profesi;
        if (profesi is null)
            throw new ArgumentException("Profesi PPA tidak valid");
        if (profesi != ProfesiType.Dokter)
            throw new ArgumentException("PPA harus dokter");

        // guard pre-condition invarian bisnis
        if (ppa.PpaId == TeamLead.PpaId)
            throw new ArgumentException("Dokter Operator Utama baru sama dengan Dokter Operator Utama yang aktif.");

        // jika belum ada di listPpa, daftarkan
        var existingIndex = _listPpa.FindIndex(x => x.Ppa.PpaId == ppa.PpaId);
        if (existingIndex < 0)
        {
            var groupSpesialis = ppa.GroupSpesialis ?? GroupSpesialisType.Default;
            _listPpa.Add(new ScheduleOpPpaType(_listPpa.Count + 1, ppa.ToReff(), profesi, groupSpesialis));
        }

        var nextNoUrut = _listHistory.Count + 1;
        var hist = new ScheduleOpHistType(
            nextNoUrut,
            TglOp, TglOp,
            Durasi, Durasi,
            KamarOp, KamarOp,
            TeamLead, ppa.ToReff(),
            alasanPerubahan,
            isOverride,
            overrideReason ?? string.Empty,
            userId, updatedAt,
            "ASSIGN_LEADER");
        _listHistory.Add(hist);

        TeamLead = ppa.ToReff();
        EnsureTeamLeadInvariant();
        AuditTrail.Modif(userId, updatedAt);
    }

    private void EnsureTeamLeadInvariant()
    {
        if (_listPpa.Count == 0)
            return;

        if (TeamLead != null && !string.IsNullOrWhiteSpace(TeamLead.PpaId) && TeamLead.PpaId != "-")
        {
            var leadIndex = _listPpa.FindIndex(x => x.Ppa.PpaId == TeamLead.PpaId);
            if (leadIndex < 0)
            {
                _listPpa.Insert(0, new ScheduleOpPpaType(1, TeamLead, ProfesiType.Dokter, GroupSpesialisType.Default));
            }
            else if (leadIndex > 0)
            {
                var leadItem = _listPpa[leadIndex];
                _listPpa.RemoveAt(leadIndex);
                _listPpa.Insert(0, leadItem);
            }
        }

        // Penomoran ulang terurut rapat: NoUrut 1 untuk lead, 2..N untuk anggota lain
        for (var i = 0; i < _listPpa.Count; i++)
        {
            _listPpa[i] = _listPpa[i] with { NoUrut = i + 1 };
        }
    }

    public void CancelSchedule(string alasanBatal, string userId, DateTime cancelledAt)
    {
        Guard.Against.NullOrWhiteSpace(alasanBatal, nameof(alasanBatal));
        AlasanBatal = alasanBatal;
        AuditTrail.Batal(userId, cancelledAt);
    }

    public void CancelSchedule(string userId, DateTime cancelledAt = default)
    {
        CancelSchedule("-", userId, cancelledAt);
    }

    public ScheduleOpReff ToReff() =>
        new ScheduleOpReff(ScheduleOpId, TglOp);

    #endregion
}

public interface IScheduleOpKey
{
    string ScheduleOpId { get; }
}
