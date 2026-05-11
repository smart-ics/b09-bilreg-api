using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.IgdContext.IgdVisitFeature;

public class IgdVisitModel : IIgdVisitKey
{
    private readonly List<IgdVisitTriageType> _listTriage;
    private readonly List<IgdVisitEventType> _listEvent;

    private const string EMPTY_BED = "-";
    private const string ID_PREFIX = "IGV";

    #region CREATION
    public IgdVisitModel(
        string igdVisitId,
        DateTime daftarDateTime,
        VisitorType visitor,
        PpaReff dokter,
        bool hasTriage,
        IgdVisitTriageType triage,
        AdministrativeStateEnum administrativeState,
        RegReff reg,
        RedirectionType redirection,
        string bedId,
        AuditTrailType auditTrail,
        AuditInfoType dischargeAudit,
        IEnumerable<IgdVisitTriageType> listTriage,
        IEnumerable<IgdVisitEventType> listEvent)
    {
        IgdVisitId = igdVisitId;
        DaftarDateTime = daftarDateTime;
        Visitor = visitor;
        Dokter = dokter;
        HasTriage = hasTriage;
        Triage = triage;
        AdministrativeState = administrativeState;
        Reg = reg;
        Redirection = redirection;
        BedId = bedId;
        AuditTrail = auditTrail;
        DischargeAudit = dischargeAudit;
        _listTriage = listTriage?.ToList() ?? [];
        _listEvent = listEvent?.ToList() ?? [];
    }

    public static IgdVisitModel Default => new(
        igdVisitId: "-",
        daftarDateTime: new DateTime(3000, 1, 1),
        visitor: VisitorType.Default,
        dokter: PpaType.Default.ToReff(),
        hasTriage: false,
        triage: IgdVisitTriageType.Default,
        administrativeState: AdministrativeStateEnum.Daftar,
        reg: RegModel.Default.ToReff(),
        redirection: RedirectionType.Default,
        bedId: EMPTY_BED,
        auditTrail: AuditTrailType.Default,
        dischargeAudit: AuditInfoType.Default,
        listTriage: [],
        listEvent: []);

    public static IIgdVisitKey Key(string id) => new IgdVisitModel(
        igdVisitId: id,
        daftarDateTime: new DateTime(3000, 1, 1),
        visitor: VisitorType.Default,
        dokter: PpaType.Default.ToReff(),
        hasTriage: false,
        triage: IgdVisitTriageType.Default,
        administrativeState: AdministrativeStateEnum.Daftar,
        reg: RegModel.Default.ToReff(),
        redirection: RedirectionType.Default,
        bedId: EMPTY_BED,
        auditTrail: AuditTrailType.Default,
        dischargeAudit: AuditInfoType.Default,
        listTriage: [],
        listEvent: []);

    public static IgdVisitModel Create(VisitorType visitor, AuditInfoType audit)
    {
        Guard.Against.Null(visitor);
        Guard.Against.NullOrWhiteSpace(visitor.VisitorName, nameof(visitor.VisitorName));
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        var newId = NunaId.New(ID_PREFIX);
        var visit = new IgdVisitModel(
            igdVisitId: newId,
            daftarDateTime: audit.Timestamp,
            visitor: visitor,
            dokter: PpaType.Default.ToReff(),
            hasTriage: false,
            triage: IgdVisitTriageType.Default,
            administrativeState: AdministrativeStateEnum.Daftar,
            reg: RegModel.Default.ToReff(),
            redirection: RedirectionType.Default,
            bedId: EMPTY_BED,
            auditTrail: AuditTrailType.Create(audit.UserId, audit.Timestamp),
            dischargeAudit: AuditInfoType.Default,
            listTriage: [],
            listEvent: []);

        visit.Emit(IgdEventEnum.Daftar, audit, $"Visit dibuat untuk {visitor.VisitorName}");
        return visit;
    }
    #endregion

    #region PROPERTIES
    public string IgdVisitId { get; init; }
    public DateTime DaftarDateTime { get; init; }
    public VisitorType Visitor { get; init; }
    public PpaReff Dokter { get; private set; }
    public bool HasTriage { get; private set; }
    public IgdVisitTriageType Triage { get; private set; }
    public AdministrativeStateEnum AdministrativeState { get; private set; }
    public RegReff Reg { get; private set; }
    public RedirectionType Redirection { get; private set; }
    public string BedId { get; private set; }
    public AuditTrailType AuditTrail { get; init; }
    public AuditInfoType DischargeAudit { get; private set; }
    public IEnumerable<IgdVisitTriageType> ListTriage => _listTriage;
    public IEnumerable<IgdVisitEventType> ListEvent => _listEvent;

    public bool HasObserved => BedId != EMPTY_BED && BedId.Length > 0;
    public bool HasReg => Reg.RegId != "-" && Reg.RegId.Length > 0;
    public bool IsDischarged => AdministrativeState == AdministrativeStateEnum.Discharged;
    public bool IsRedirected => AdministrativeState == AdministrativeStateEnum.Redirected;
    public bool IsVoided => AuditTrail.IsVoided;
    public bool IsTerminal => IsDischarged || IsRedirected || IsVoided;
    #endregion

    #region BEHAVIOUR
    public IgdVisitReff ToReff() => new(IgdVisitId, Visitor.VisitorName);

    public void AssignDokter(PpaType dokter, AuditInfoType audit)
    {
        Guard.Against.Null(dokter);
        if (IsTerminal)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah {AdministrativeState}; tidak dapat assign dokter.");
        if (!dokter.IsDokter())
            throw new ArgumentException($"Petugas {dokter.PpaId} bukan dokter");

        Dokter = dokter.ToReff();
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.AssignDokter, audit, $"Dokter aktif: {dokter.PpaName}");
    }

    public void AssessTriage(TriageLevelEnum level, string notes, AuditInfoType audit)
    {
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));
        if (level == TriageLevelEnum.Unknown)
            throw new ArgumentException("Triage level wajib diisi");
        if (IsTerminal)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah {AdministrativeState}; tidak dapat assess triage.");

        var nextNo = _listTriage.Count == 0 ? 1 : _listTriage.Max(x => x.NoTriage) + 1;
        var newTriage = new IgdVisitTriageType(
            NoTriage: nextNo,
            Level: level,
            AssessmentDateTime: audit.Timestamp,
            AssessorUserId: audit.UserId,
            Notes: string.IsNullOrWhiteSpace(notes) ? "-" : notes);

        _listTriage.Add(newTriage);
        Triage = newTriage;
        HasTriage = true;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.AssessTriage, audit, $"Triage {level.ToCode()}");
    }

    public void AssignBed(string bedId, AuditInfoType audit)
    {
        Guard.Against.NullOrWhiteSpace(bedId, nameof(bedId));
        if (IsTerminal)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah {AdministrativeState}; tidak dapat assign bed.");
        if (HasObserved)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah menempati bed {BedId}. Lakukan check-out terlebih dahulu.");
        if (!HasTriage)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} belum melalui triage; assign bed tidak diperbolehkan.");

        BedId = bedId;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.AssignBed, audit, $"Bed {bedId}");
    }

    public void CheckOutBed(AuditInfoType audit)
    {
        if (!HasObserved)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} tidak sedang menempati bed.");

        var prevBed = BedId;
        BedId = EMPTY_BED;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.CheckOut, audit, $"Check-out dari bed {prevBed}");
    }

    public void ClearBed(AuditInfoType audit)
    {
        if (!HasObserved)
            return;

        var prevBed = BedId;
        BedId = EMPTY_BED;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.ClearBed, audit, $"Bed {prevBed} dilepas (cascade discharge)");
    }

    public void AssignRegister(RegModel reg, AuditInfoType audit)
    {
        Guard.Against.Null(reg);
        if (IsTerminal)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah {AdministrativeState}; tidak dapat link register.");
        if (HasReg)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah memiliki RegId {Reg.RegId}.");

        Reg = reg.ToReff();
        AdministrativeState = AdministrativeStateEnum.Registered;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.AssignRegister, audit, $"RegId {reg.RegId} dilinkkan");
    }

    public void RedirectToRawatJalan(string redirectRajalId, string reason, AuditInfoType audit)
    {
        Guard.Against.NullOrWhiteSpace(redirectRajalId, nameof(redirectRajalId));
        if (IsTerminal)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah {AdministrativeState}; tidak dapat redirect.");
        if (HasObserved)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sedang menempati bed; redirect tidak diperbolehkan.");

        Redirection = new RedirectionType(
            RedirectRajalId: redirectRajalId,
            RedirectDateTime: audit.Timestamp,
            Reason: string.IsNullOrWhiteSpace(reason) ? "-" : reason);
        AdministrativeState = AdministrativeStateEnum.Redirected;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.Redirect, audit, $"Redirect ke rawat jalan ({redirectRajalId})");
    }

    public void RecordTindakanEvent(string tindakanId, AuditInfoType audit)
    {
        if (IsTerminal)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah {AdministrativeState}; tidak dapat add tindakan.");
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.AddTindakan, audit, $"Tindakan {tindakanId}");
    }

    public void RecordBhpEvent(string bhpId, AuditInfoType audit)
    {
        if (IsTerminal)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah {AdministrativeState}; tidak dapat add BHP.");
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.AddBhp, audit, $"BHP {bhpId}");
    }

    public void Discharge(AuditInfoType audit)
    {
        if (IsVoided)
            throw new InvalidOperationException($"Visit {IgdVisitId} sudah di-void.");
        if (IsDischarged)
            return;
        if (!HasReg)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} belum memiliki registrasi administratif. Discharge tidak diperbolehkan.");

        AdministrativeState = AdministrativeStateEnum.Discharged;
        DischargeAudit = audit;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.Discharge, audit, "Discharge");
    }

    public void Void(bool hasTindakan, bool hasBhp, AuditInfoType audit)
    {
        if (IsVoided)
            return;
        if (IsDischarged)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah discharge; tidak dapat di-void.");
        if (hasTindakan)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} memiliki transaksi tindakan; void tidak diperbolehkan.");
        if (hasBhp)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} memiliki transaksi BHP; void tidak diperbolehkan.");

        AuditTrail.Batal(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.Void, audit, "Visit di-void");
    }

    public void AttachEvents(IEnumerable<IgdVisitEventType> events)
    {
        _listEvent.Clear();
        _listEvent.AddRange(events);
    }

    public void AttachTriages(IEnumerable<IgdVisitTriageType> triages)
    {
        _listTriage.Clear();
        _listTriage.AddRange(triages);
        var latest = _listTriage.OrderByDescending(x => x.NoTriage).FirstOrDefault();
        if (latest is not null)
        {
            Triage = latest;
            HasTriage = true;
        }
    }
    #endregion

    private void Emit(IgdEventEnum kind, AuditInfoType audit, string notes)
    {
        var nextNo = _listEvent.Count == 0 ? 1 : _listEvent.Max(x => x.NoEvent) + 1;
        _listEvent.Add(new IgdVisitEventType(
            NoEvent: nextNo,
            EventKind: kind,
            EventDateTime: audit.Timestamp,
            UserId: audit.UserId,
            Notes: notes));
    }
}

public record IgdVisitReff(string IgdVisitId, string VisitorName);
