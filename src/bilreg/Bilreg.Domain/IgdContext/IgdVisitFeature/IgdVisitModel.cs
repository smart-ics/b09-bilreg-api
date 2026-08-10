using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
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
        TriageMethodEnum triageMethod,
        TriageColorEnum triageColor,
        DateTime lastTriageAt,
        DateTime nextReTriageAt,
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
        TriageMethod = triageMethod;
        TriageColor = triageColor;
        LastTriageAt = lastTriageAt;
        NextReTriageAt = nextReTriageAt;
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
        triageMethod: TriageMethodEnum.Unknown,
        triageColor: TriageColorEnum.Unknown,
        lastTriageAt: new DateTime(3000, 1, 1),
        nextReTriageAt: new DateTime(3000, 1, 1),
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
        triageMethod: TriageMethodEnum.Unknown,
        triageColor: TriageColorEnum.Unknown,
        lastTriageAt: new DateTime(3000, 1, 1),
        nextReTriageAt: new DateTime(3000, 1, 1),
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
            triageMethod: TriageMethodEnum.Unknown,
            triageColor: TriageColorEnum.Unknown,
            lastTriageAt: new DateTime(3000, 1, 1),
            nextReTriageAt: new DateTime(3000, 1, 1),
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
    public VisitorType Visitor { get; private set; }
    public PpaReff Dokter { get; private set; }
    public bool HasTriage { get; private set; }
    public IgdVisitTriageType Triage { get; private set; }
    public AdministrativeStateEnum AdministrativeState { get; private set; }
    public RegReff Reg { get; private set; }
    public RedirectionType Redirection { get; private set; }
    public string BedId { get; private set; }
    public TriageMethodEnum TriageMethod { get; private set; }
    public TriageColorEnum TriageColor { get; private set; }
    public DateTime LastTriageAt { get; private set; }
    public DateTime NextReTriageAt { get; private set; }
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
    public bool HasNextReTriage => NextReTriageAt != new DateTime(3000, 1, 1);
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
            Method: TriageMethodEnum.Ats,
            Level: level,
            Color: level switch
            {
                TriageLevelEnum.Ats1 or TriageLevelEnum.Ats2 => TriageColorEnum.Red,
                TriageLevelEnum.Ats3 => TriageColorEnum.Yellow,
                TriageLevelEnum.Ats4 or TriageLevelEnum.Ats5 => TriageColorEnum.Green,
                _ => TriageColorEnum.Unknown
            },
            AirwaysScore: 0,
            BreathingScore: 0,
            BloodCirculationScore: 0,
            GcsEyeScore: 0,
            GcsMotorScore: 0,
            GcsVoiceScore: 0,
            IsManualOverrideBlack: false,
            OverrideByUserId: "-",
            OverrideReason: "-",
            OverrideDateTime: new DateTime(3000, 1, 1),
            AssessmentDateTime: audit.Timestamp,
            AssessorUserId: audit.UserId,
            Notes: string.IsNullOrWhiteSpace(notes) ? "-" : notes);

        _listTriage.Add(newTriage);
        Triage = newTriage;
        HasTriage = true;
        TriageMethod = newTriage.Method;
        TriageColor = newTriage.Color;
        LastTriageAt = newTriage.AssessmentDateTime;
        NextReTriageAt = level.ToReAssessmentInterval() is { } interval
            ? LastTriageAt.Add(interval)
            : new DateTime(3000, 1, 1);
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.AssessTriage, audit, $"Triage {level.ToCode()}");
    }

    public void AssessTriage(
        AtsAssessmentType assessment,
        TriageMethodEnum method,
        TriageLevelEnum level,
        TriageColorEnum color,
        string notes,
        bool isManualOverrideBlack,
        string overrideByUserId,
        string overrideReason,
        DateTime? nextReTriageAt,
        AuditInfoType audit)
    {
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));
        if (level == TriageLevelEnum.Unknown)
            throw new ArgumentException("Triage level wajib diisi");
        if (method == TriageMethodEnum.Unknown)
            throw new ArgumentException("Triage method wajib diisi");
        if (IsTerminal)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah {AdministrativeState}; tidak dapat assess triage.");

        var nextNo = _listTriage.Count == 0 ? 1 : _listTriage.Max(x => x.NoTriage) + 1;
        var overrideTime = isManualOverrideBlack ? audit.Timestamp : new DateTime(3000, 1, 1);
        var newTriage = new IgdVisitTriageType(
            NoTriage: nextNo,
            Method: method,
            Level: level,
            Color: isManualOverrideBlack ? TriageColorEnum.Black : color,
            AirwaysScore: assessment.AirwaysScore,
            BreathingScore: assessment.BreathingScore,
            BloodCirculationScore: assessment.BloodCirculationScore,
            GcsEyeScore: assessment.GcsEyeScore,
            GcsMotorScore: assessment.GcsMotorScore,
            GcsVoiceScore: assessment.GcsVoiceScore,
            IsManualOverrideBlack: isManualOverrideBlack,
            OverrideByUserId: isManualOverrideBlack ? overrideByUserId : "-",
            OverrideReason: isManualOverrideBlack
                ? (string.IsNullOrWhiteSpace(overrideReason) ? "-" : overrideReason)
                : "-",
            OverrideDateTime: overrideTime,
            AssessmentDateTime: audit.Timestamp,
            AssessorUserId: audit.UserId,
            Notes: string.IsNullOrWhiteSpace(notes) ? "-" : notes);

        _listTriage.Add(newTriage);
        Triage = newTriage;
        HasTriage = true;
        TriageMethod = newTriage.Method;
        TriageColor = newTriage.Color;
        LastTriageAt = newTriage.AssessmentDateTime;
        NextReTriageAt = nextReTriageAt ?? new DateTime(3000, 1, 1);
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.AssessTriage, audit, $"Triage {newTriage.Level.ToCode()}");
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

    public void TransferBed(string toBedId, string reason, string notes, AuditInfoType audit)
    {
        Guard.Against.NullOrWhiteSpace(toBedId, nameof(toBedId));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        notes ??= string.Empty;

        if (IsTerminal)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah {AdministrativeState}; tidak dapat transfer bed.");
        if (!HasObserved)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} tidak sedang menempati bed; transfer bed tidak diperbolehkan.");
        if (toBedId == BedId)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah di bed {BedId}; pilih bed tujuan lain.");

        var fromBed = BedId;
        BedId = toBedId;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);

        var segment = $"{reason}: {fromBed} → {toBedId}";
        if (!string.IsNullOrWhiteSpace(notes))
            segment = $"{segment}; {notes.Trim()}";

        const int maxNotes = 200;
        if (segment.Length > maxNotes)
            segment = segment[..maxNotes];

        Emit(IgdEventEnum.TransferBed, audit, segment);
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

    public void ReplaceRegister(RegModel newReg, AuditInfoType audit)
    {
        Guard.Against.Null(newReg);
        if (IsTerminal)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah {AdministrativeState}; tidak dapat replace register.");
        if(!HasReg)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} belum Assign Register; Gunakan Assign Register.");
        if (Reg.RegId == newReg.RegId)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} Register lama {Reg.RegId} sama dengan Register baru {newReg.RegId}; Akses ditolak.");
        
        var oldReg = Reg;
        var visitor = new VisitorType(newReg.Pasien.PasienName, newReg.Pasien.Gender, newReg.Pasien.TglLahir, "-");
        
        Reg = newReg.ToReff();
        Visitor = visitor;
        AdministrativeState = AdministrativeStateEnum.Registered;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.ReplaceRegister, audit, $"RegId {oldReg.RegId} diganti dengan {newReg.RegId}");

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

    public void RecordTindakanEvent(TindakanIgdModel tindakan, AuditInfoType audit)
    {
        if (IsTerminal)
            throw new InvalidOperationException(
                $"Visit {IgdVisitId} sudah {AdministrativeState}; tidak dapat add tindakan.");

        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.AddTindakan, audit, $"{tindakan.Aktifitas.ToString()} {tindakan.TindakanIgdId} {tindakan.Descriptions}");
    }

    public void RecordVoidTindakanEvent(TindakanIgdModel tdk, AuditInfoType audit)
    {
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
        Emit(IgdEventEnum.VoidTindakan, audit, $"{tdk.Aktifitas.ToString()} {tdk.TindakanIgdId} {tdk.Descriptions}");
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
            TriageMethod = latest.Method;
            TriageColor = latest.Color;
            LastTriageAt = latest.AssessmentDateTime;
            NextReTriageAt = latest.Level.ToReAssessmentInterval() is { } interval
                ? latest.AssessmentDateTime.Add(interval)
                : new DateTime(3000, 1, 1);
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
