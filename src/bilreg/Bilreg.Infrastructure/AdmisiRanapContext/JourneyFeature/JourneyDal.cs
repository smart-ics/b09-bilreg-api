using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.JourneyFeature;

/// <summary>
/// Release 1 journey read projection. Leaves OperationalWorklist untouched.
/// SQL owns one-row-per-journey roots, stage CASE (parity with B1), facets, and cursor paging.
/// Page enrichment uses fixed-batch fact hydration; detail runs B1 then allowed-action evaluation.
/// </summary>
public sealed partial class JourneyDal : IJourneyDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);
    private const int CommandTimeoutSeconds = 60;

    /// <summary>Fixed list round-trips when the page is non-empty: facet+page batch, sources+admissions, wl+reg extras.</summary>
    public const int FixedListRoundTripsWithPage = 3;

    /// <summary>Fixed list round-trips when the page is empty: facet+page batch only.</summary>
    public const int FixedListRoundTripsEmptyPage = 1;

    private readonly DatabaseOptions _opt;
    private readonly ILogger<JourneyDal> _logger;
    private readonly IRegistrationCancellationEligibilityRepo? _cancellationEligibility;
    private DateOnly _businessDate;

    public JourneyListDiagnostics? LastListDiagnostics { get; private set; }

    public JourneyDal(
        IOptions<DatabaseOptions> opt,
        ILogger<JourneyDal>? logger = null,
        IRegistrationCancellationEligibilityRepo? cancellationEligibility = null)
    {
        _opt = opt.Value;
        _logger = logger ?? NullLogger<JourneyDal>.Instance;
        _cancellationEligibility = cancellationEligibility;
    }

    public async Task<JourneyListResult> ListAsync(
        JourneyListFilter filter,
        DateTime businessNow = default,
        CancellationToken cancellationToken = default)
    {
        var asOf = businessNow;
        _businessDate = DateOnly.FromDateTime(businessNow);
        var pageSize = filter.PageSize <= 0 ? 50 : Math.Min(filter.PageSize, 200);
        var swTotal = System.Diagnostics.Stopwatch.StartNew();
        var roundTrips = 0;

        await using var conn = await OpenAsync(cancellationToken);
        var dp = BuildCommonParams(filter);
        dp.AddParam("@PageSize", pageSize + 1, SqlDbType.Int);

        if (JourneyListCursor.TryDecode(filter.Cursor, out var cursorSortAt, out var cursorJourneyId))
        {
            dp.AddParam("@CursorSortAt", cursorSortAt, SqlDbType.DateTime);
            dp.AddParam("@CursorJourneyId", cursorJourneyId, SqlDbType.VarChar);
            dp.AddParam("@HasCursor", 1, SqlDbType.Int);
        }
        else
        {
            dp.AddParam("@CursorSortAt", VoidSentinel, SqlDbType.DateTime);
            dp.AddParam("@CursorJourneyId", "", SqlDbType.VarChar);
            dp.AddParam("@HasCursor", 0, SqlDbType.Int);
        }

        var batchSql = BuildFacetAndPageSql(filter);

        var swFacetPage = System.Diagnostics.Stopwatch.StartNew();
        using var multi = await conn.QueryMultipleAsync(
            new CommandDefinition(batchSql, dp, commandTimeout: CommandTimeoutSeconds, cancellationToken: cancellationToken));
        roundTrips++;

        var facets = (await multi.ReadAsync<FacetRowDto>() ?? []).ToList();
        var pageKeys = (await multi.ReadAsync<JourneyPageKeyDto>() ?? []).ToList();
        swFacetPage.Stop();

        string? nextCursor = null;
        if (pageKeys.Count > pageSize)
        {
            var lastKept = pageKeys[pageSize - 1];
            nextCursor = JourneyListCursor.Encode(lastKept.SortAt, lastKept.JourneyId);
            pageKeys = pageKeys.Take(pageSize).ToList();
        }

        var totalMatches = facets.Sum(f => f.Cnt);

        var swBatch = System.Diagnostics.Stopwatch.StartNew();
        var bagsByJourneyId = await BatchLoadFactBagsAsync(conn, pageKeys, cancellationToken);
        if (pageKeys.Count > 0)
            roundTrips += 2;
        swBatch.Stop();

        var swResolve = System.Diagnostics.Stopwatch.StartNew();
        var items = new List<JourneyListItem>(pageKeys.Count);
        foreach (var key in pageKeys)
        {
            if (!bagsByJourneyId.TryGetValue(key.JourneyId, out var bag))
                continue;

            var resolution = Release1JourneyStageResolver.Resolve(bag.Facts);
            items.Add(MapListItem(bag, resolution, key.SortAt, asOf));
        }
        swResolve.Stop();

        swTotal.Stop();
        LastListDiagnostics = new JourneyListDiagnostics(
            roundTrips,
            swFacetPage.ElapsedMilliseconds,
            swBatch.ElapsedMilliseconds,
            swResolve.ElapsedMilliseconds,
            swTotal.ElapsedMilliseconds,
            items.Count);

        _logger.LogInformation(
            "JourneyDal.List scope={Scope} totalMatches={Total} page={Page} ms={Ms} facetPageMs={Fp} batchMs={Batch} resolveMs={Res} roundTrips={Rt} placementUnavailable={Pu} reconciliation={Rec}",
            filter.Scope,
            totalMatches,
            items.Count,
            swTotal.ElapsedMilliseconds,
            swFacetPage.ElapsedMilliseconds,
            swBatch.ElapsedMilliseconds,
            swResolve.ElapsedMilliseconds,
            roundTrips,
            facets.FirstOrDefault(f => f.SqlStage == (int)JourneyOperationalStage.PlacementStatusUnavailable)?.Cnt ?? 0,
            facets.FirstOrDefault(f => f.SqlStage == (int)JourneyOperationalStage.NeedsReconciliation)?.Cnt ?? 0);

        return new JourneyListResult(
            items,
            facets
                .Select(f => new JourneyStageFacet((JourneyOperationalStage)f.SqlStage, f.Cnt))
                .OrderBy(f => f.Stage)
                .ToList(),
            totalMatches,
            nextCursor,
            asOf,
            JourneyProjectionVersions.Release1);
    }

    public JourneyListResult List(JourneyListFilter filter, DateTime businessNow = default) =>
        ListAsync(filter, businessNow, CancellationToken.None).GetAwaiter().GetResult();

    public async Task<JourneyDetailWorkspace?> GetByJourneyIdAsync(
        string journeyId,
        DateTime businessNow = default,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(journeyId))
            return null;

        var asOf = businessNow;
        _businessDate = DateOnly.FromDateTime(businessNow);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await using var conn = await OpenAsync(cancellationToken);
        var bag = await LoadFactBagAsync(conn, journeyId.Trim(), cancellationToken);
        if (bag is null)
            return null;

        var resolution = Release1JourneyStageResolver.Resolve(bag.Facts);
        var detail = MapDetail(bag, resolution, asOf, hasBillingItems: false);
        sw.Stop();
        _logger.LogInformation(
            "JourneyDal.GetByJourneyId journeyId={JourneyId} stage={Stage} ms={Ms} recon={Recon}",
            journeyId,
            resolution.Stage,
            sw.ElapsedMilliseconds,
            resolution.ReconciliationIssues.Count);
        return detail;
    }

    public JourneyDetailWorkspace? GetByJourneyId(string journeyId, DateTime businessNow = default) =>
        GetByJourneyIdAsync(journeyId, businessNow, CancellationToken.None).GetAwaiter().GetResult();

    public async Task<JourneyLegacyResolution?> ResolveLegacyRecordAsync(
        string recordType,
        string recordId,
        DateTime businessNow = default,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recordType) || string.IsNullOrWhiteSpace(recordId))
            return null;

        var type = recordType.Trim().ToLowerInvariant();
        var id = recordId.Trim();
        JourneyDetailWorkspace? detail = type switch
        {
            "opnamerequest" or "opname" or "opn" => await GetByJourneyIdAsync(JourneyIdFactory.FromOpnameRequest(id), businessNow, cancellationToken),
            "reservation" or "rsv" => await GetByJourneyIdAsync(JourneyIdFactory.FromReservation(id), businessNow, cancellationToken),
            "admission" or "registration" or "reg" => await GetByJourneyIdAsync(JourneyIdFactory.FromRegId(id), businessNow, cancellationToken),
            "waitinglist" or "wl" or "wtl" => await ResolveWaitingListAsync(id, businessNow, cancellationToken),
            _ => null
        };

        if (detail is null)
            return null;

        var ambiguous = detail.ReconciliationIssues.Count > 0;
        return new JourneyLegacyResolution(
            ambiguous ? null : detail.JourneyId,
            ambiguous,
            detail.ReconciliationIssues);
    }

    public JourneyLegacyResolution? ResolveLegacyRecord(string recordType, string recordId, DateTime businessNow = default) =>
        ResolveLegacyRecordAsync(recordType, recordId, businessNow, CancellationToken.None).GetAwaiter().GetResult();

    private async Task<JourneyDetailWorkspace?> ResolveWaitingListAsync(
        string waitingListId,
        DateTime businessNow,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await OpenAsync(cancellationToken);
        var regId = await conn.ExecuteScalarAsync<string?>(new CommandDefinition(
            """
            SELECT RegId
            FROM BILRG_BedWaitingList
            WHERE VodDate = @VodDate AND WaitingListId = @WaitingListId
            """,
            new { VodDate = VoidSentinel, WaitingListId = waitingListId },
            commandTimeout: CommandTimeoutSeconds,
            cancellationToken: cancellationToken));

        return string.IsNullOrWhiteSpace(regId)
            ? null
            : await GetByJourneyIdAsync(JourneyIdFactory.FromRegId(regId), businessNow, cancellationToken);
    }

    private JourneyDetailWorkspace? ResolveWaitingList(string waitingListId, DateTime businessNow) =>
        ResolveWaitingListAsync(waitingListId, businessNow, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Single-journey fact load (detail path and batch-parity oracle). Prefer <see cref="BatchLoadFactBagsAsync"/> for lists.
    /// </summary>
    internal async Task<JourneyFactBag?> LoadFactBagForTestsAsync(string journeyId, CancellationToken cancellationToken = default)
    {
        await using var conn = await OpenAsync(cancellationToken);
        return await LoadFactBagAsync(conn, journeyId, cancellationToken);
    }

    internal JourneyFactBag? LoadFactBagForTests(string journeyId) =>
        LoadFactBagForTestsAsync(journeyId).GetAwaiter().GetResult();

    private static JourneyListItem MapListItem(
        JourneyFactBag bag,
        Release1JourneyResolution resolution,
        DateTime sortAt,
        DateTime asOf)
    {
        var waitingSince = resolution.CurrentCondition.Since;
        long? waitingSeconds = waitingSince.HasValue
            ? (long)Math.Max(0, (asOf - waitingSince.Value).TotalSeconds)
            : null;

        return new JourneyListItem(
            resolution.Identity.JourneyId,
            resolution.Identity.RegId,
            resolution.Identity.OriginKind,
            bag.Facts.Patient,
            resolution.Stage,
            resolution.CurrentCondition,
            resolution.NextTask,
            bag.CareClass,
            bag.TargetBangsal,
            bag.Priority,
            waitingSince,
            waitingSeconds,
            bag.Doctor,
            bag.Guarantor,
            resolution.AttentionFlags,
            resolution.ReconciliationIssues,
            sortAt,
            JourneyProjectionVersions.Release1,
            asOf);
    }

    private JourneyDetailWorkspace MapDetail(
        JourneyFactBag bag,
        Release1JourneyResolution resolution,
        DateTime asOf,
        bool? hasBillingItems = false)
    {
        var patientContact = bag.Facts.Patient is null
            ? null
            : new JourneyPatientContactInfo(
                bag.Facts.Patient.PatientId,
                bag.Facts.Patient.MedicalRecordNumber,
                bag.Facts.Patient.DisplayName,
                bag.Facts.Patient.Sex,
                bag.Facts.Patient.BirthDate,
                bag.Facts.Patient.AgeDisplay,
                bag.Facts.Patient.ContactSummary,
                bag.AddressSummary,
                bag.RegIbuId,
                bag.NamaIbu);

        Func<string, bool>? billingCheck = hasBillingItems.HasValue
            ? _ => hasBillingItems.Value
            : _ => false;

        var allowed = JourneyAllowedActionEvaluator.Evaluate(
            resolution.CandidateAdmisiActions,
            resolution.Identity.RegId,
            billingCheck);

        return new JourneyDetailWorkspace(
            resolution.Identity.JourneyId,
            resolution.Identity,
            bag.Facts.Patient,
            resolution.Stage,
            resolution.CurrentCondition,
            resolution.NextTask,
            resolution.CandidateAdmisiActions,
            allowed,
            resolution.Timeline,
            new JourneyDetailInformation(
                patientContact,
                new JourneyGuarantorCareClassInfo(bag.Guarantor, bag.CareClass, bag.PolicySummary),
                new JourneyClinicalInfo(
                    bag.ClinicalNotes,
                    bag.ProcedureSummary,
                    bag.Doctor,
                    bag.ReferringDoctor,
                    bag.ReferralOrSourceSummary,
                    bag.JenisInap,
                    bag.ProsedurMasuk,
                    bag.CaraMasuk),
                resolution.PlacementAvailability,
                new JourneyHandoverInfo(resolution.HandoverSummary, bag.TargetBangsal, bag.Priority)),
            resolution.SystemAudit,
            resolution.ReconciliationIssues,
            asOf,
            JourneyProjectionVersions.Release1);
    }

    private async Task<JourneyFactBag?> LoadFactBagAsync(
        IDbConnection conn,
        string journeyId,
        CancellationToken cancellationToken = default)
    {
        if (!JourneyIdFactory.TryParse(journeyId, out var prefix, out var domainId))
            return null;

        return prefix switch
        {
            JourneyIdFactory.OpnamePrefix => await LoadOpnameJourneyAsync(conn, domainId, journeyId, cancellationToken),
            JourneyIdFactory.ReservationPrefix => await LoadReservationJourneyAsync(conn, domainId, journeyId, cancellationToken),
            JourneyIdFactory.RegPrefix => await LoadRegJourneyAsync(conn, domainId, journeyId, cancellationToken),
            _ => null
        };
    }

    private JourneyFactBag? LoadFactBag(IDbConnection conn, string journeyId) =>
        LoadFactBagAsync(conn, journeyId, CancellationToken.None).GetAwaiter().GetResult();

    private async Task<JourneyFactBag?> LoadOpnameJourneyAsync(
        IDbConnection conn,
        string opnameRequestId,
        string journeyId,
        CancellationToken cancellationToken = default)
    {
        var opn = await conn.QueryFirstOrDefaultAsync<OpnameRowDto>(
            new CommandDefinition(
                """
                SELECT ord.InpatientOrderId AS OpnameRequestId,
                       CASE WHEN ord.OrderStatus = 2 THEN 1 WHEN ord.OrderStatus = 3 THEN 2 ELSE 0 END AS OpnameRequestStatus,
                       ord.FulfilledRegId, ord.PasienId,
                       ord.DokterId, ord.DokterName, ord.ClinicalNotes, ord.PlannedDate, ord.CrtDate, ord.UpdDate,
                       ISNULL(NULLIF(ord.PasienName,''), ISNULL(mr.fs_nm_pasien,'')) AS PasienName,
                       ISNULL(NULLIF(ord.Gender,''), ISNULL(mr.fs_jns_kelamin,'')) AS Gender,
                       CASE WHEN ord.TglLahir <> '3000-01-01' THEN ord.TglLahir WHEN ISDATE(mr.fd_tgl_lahir)=1 THEN CAST(mr.fd_tgl_lahir AS DATETIME) ELSE NULL END AS TglLahir
                FROM BILRG_AdmInpatientOrder ord
                LEFT JOIN tc_mr mr ON ord.PasienId = mr.fs_mr
                WHERE ord.VodDate = @VodDate AND ord.OrderSource IN (1, 2) AND ord.InpatientOrderId = @Id
                """,
                new { VodDate = VoidSentinel, Id = opnameRequestId },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken));
        if (opn is null)
            return null;

        // Prefer registered episode when Admission links this source.
        var admission = await FindAdmissionByOpnameAsync(conn, opnameRequestId, opn.FulfilledRegId, cancellationToken);
        if (admission is not null)
            return await LoadAdmissionBagAsync(conn, admission, JourneyOriginKind.OpnameRequest, opn, reservation: null, cancellationToken);

        var patient = BuildPatient(opn.PasienId, opn.PasienName, opn.Gender, opn.TglLahir);
        var facts = new JourneyNormalizedFacts(
            JourneyOriginKind.OpnameRequest,
            patient,
            new OpnameRequestJourneyFact(
                opn.OpnameRequestId,
                (OpnameRequestStatusEnum)opn.OpnameRequestStatus,
                NullDash(opn.FulfilledRegId),
                opn.UpdDate == VoidSentinel ? opn.CrtDate : opn.UpdDate,
                opn.CrtDate),
            null,
            [],
            []);

        return new JourneyFactBag(
            journeyId,
            facts,
            CareClass: null,
            TargetBangsal: null,
            Priority: null,
            Doctor: Named(opn.DokterId, opn.DokterName),
            Guarantor: null,
            ClinicalNotes: opn.ClinicalNotes,
            ProcedureSummary: null,
            ReferringDoctor: null,
            ReferralOrSourceSummary: "Opname Request",
            PolicySummary: null,
            AddressSummary: null);
    }

    private JourneyFactBag? LoadOpnameJourney(IDbConnection conn, string opnameRequestId, string journeyId) =>
        LoadOpnameJourneyAsync(conn, opnameRequestId, journeyId, CancellationToken.None).GetAwaiter().GetResult();

    private async Task<JourneyFactBag?> LoadReservationJourneyAsync(
        IDbConnection conn,
        string reservationId,
        string journeyId,
        CancellationToken cancellationToken = default)
    {
        var rsv = await conn.QueryFirstOrDefaultAsync<ReservationRowDto>(
            new CommandDefinition(
                """
                SELECT ord.InpatientOrderId AS ReservationId, ord.OrderStatus AS ReservationStatus,
                       ord.FulfilledRegId AS RealizedRegId, ord.PasienId,
                       ISNULL(NULLIF(ord.PasienName,''), ISNULL(mr.fs_nm_pasien,'')) AS PasienName,
                       ISNULL(NULLIF(ord.Gender,''), ISNULL(mr.fs_jns_kelamin,'')) AS Gender,
                       CASE WHEN ord.TglLahir <> '3000-01-01' THEN ord.TglLahir WHEN ISDATE(mr.fd_tgl_lahir)=1 THEN CAST(mr.fd_tgl_lahir AS DATETIME) ELSE '3000-01-01' END AS TglLahir,
                       ord.KelasId, ord.KelasName, ord.BangsalId, ord.BangsalName, ord.PlannedDate, ord.CrtDate, ord.UpdDate,
                       ord.DokterId, ord.DokterName, ord.TipeJaminanId, ord.TipeJaminanName, ord.ClinicalNotes
                FROM BILRG_AdmInpatientOrder ord
                LEFT JOIN tc_mr mr ON ord.PasienId = mr.fs_mr
                WHERE ord.VodDate = @VodDate AND ord.OrderSource IN (3, 4) AND ord.InpatientOrderId = @Id
                """,
                new { VodDate = VoidSentinel, Id = reservationId },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken));
        if (rsv is null)
            return null;

        var admission = await FindAdmissionByReservationAsync(conn, reservationId, rsv.RealizedRegId, cancellationToken);
        if (admission is not null)
            return await LoadAdmissionBagAsync(conn, admission, JourneyOriginKind.Reservation, opname: null, rsv, cancellationToken);

        var patient = BuildPatient(rsv.PasienId, rsv.PasienName, rsv.Gender, rsv.TglLahir);
        var facts = new JourneyNormalizedFacts(
            JourneyOriginKind.Reservation,
            patient,
            null,
            new ReservationJourneyFact(
                rsv.ReservationId,
                (ReservationStatusEnum)rsv.ReservationStatus,
                NullDash(rsv.RealizedRegId),
                rsv.UpdDate == VoidSentinel ? rsv.CrtDate : rsv.UpdDate,
                rsv.CrtDate),
            [],
            []);

        return new JourneyFactBag(
            journeyId,
            facts,
            Named(rsv.KelasId, rsv.KelasName),
            Named(rsv.BangsalId, rsv.BangsalName),
            Priority: null,
            Doctor: Named(rsv.DokterId, rsv.DokterName),
            Guarantor: Named(rsv.TipeJaminanId, rsv.TipeJaminanName),
            ClinicalNotes: string.IsNullOrWhiteSpace(rsv.ClinicalNotes) ? null : rsv.ClinicalNotes,
            ProcedureSummary: null,
            ReferringDoctor: null,
            ReferralOrSourceSummary: "Reservation",
            PolicySummary: null,
            AddressSummary: null);
    }

    private JourneyFactBag? LoadReservationJourney(IDbConnection conn, string reservationId, string journeyId) =>
        LoadReservationJourneyAsync(conn, reservationId, journeyId, CancellationToken.None).GetAwaiter().GetResult();

    private async Task<JourneyFactBag?> LoadRegJourneyAsync(
        IDbConnection conn,
        string regId,
        string journeyId,
        CancellationToken cancellationToken = default)
    {
        var admission = await conn.QueryFirstOrDefaultAsync<AdmissionRowDto>(
            new CommandDefinition(
                BuildLoadRegJourneySql(),
                new { VodDate = VoidSentinel, Id = regId },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken));
        if (admission is null)
            return null;

        var origin = ResolveOrigin(admission.OpnameRequestId, admission.ReservationId);
        OpnameRowDto? opn = null;
        ReservationRowDto? rsv = null;
        if (HasId(admission.OpnameRequestId))
            opn = await LoadOpnameRowAsync(conn, admission.OpnameRequestId, cancellationToken);
        if (HasId(admission.ReservationId))
            rsv = await LoadReservationRowAsync(conn, admission.ReservationId, cancellationToken);

        return await LoadAdmissionBagAsync(conn, admission, origin, opn, rsv, cancellationToken);
    }

    private JourneyFactBag? LoadRegJourney(IDbConnection conn, string regId, string journeyId) =>
        LoadRegJourneyAsync(conn, regId, journeyId, CancellationToken.None).GetAwaiter().GetResult();

    private async Task<JourneyFactBag> LoadAdmissionBagAsync(
        IDbConnection conn,
        AdmissionRowDto admission,
        JourneyOriginKind origin,
        OpnameRowDto? opname,
        ReservationRowDto? reservation,
        CancellationToken cancellationToken = default)
    {
        var wlRows = await LoadWaitingListRowsAsync(conn, admission.RegId, cancellationToken);
        var regExtras = await LoadRegistrationExtrasAsync(conn, admission.RegId, cancellationToken);
        return AssembleAdmissionBag(admission, origin, opname, reservation, wlRows, regExtras);
    }

    private JourneyFactBag LoadAdmissionBag(
        IDbConnection conn,
        AdmissionRowDto admission,
        JourneyOriginKind origin,
        OpnameRowDto? opname,
        ReservationRowDto? reservation) =>
        LoadAdmissionBagAsync(conn, admission, origin, opname, reservation, CancellationToken.None).GetAwaiter().GetResult();

    internal JourneyFactBag AssembleAdmissionBag(
        AdmissionRowDto admission,
        JourneyOriginKind origin,
        OpnameRowDto? opname,
        ReservationRowDto? reservation,
        IReadOnlyList<WaitingListRowDto> wlRows,
        RegExtras regExtras)
    {
        var waitingLists = wlRows
            .Select(r => new WaitingListJourneyFact(
                r.WaitingListId,
                r.RegId,
                (WaitingListStatusEnum)r.WaitingListStatus,
                r.UpdDate == VoidSentinel ? r.CrtDate : r.UpdDate,
                NullDash(r.BangsalId),
                r.BangsalName))
            .ToList();

        var patient = BuildPatient(
            admission.PasienId,
            admission.PasienName,
            admission.Gender,
            admission.TglLahir);

        OpnameRequestJourneyFact? opnFact = opname is null
            ? null
            : new OpnameRequestJourneyFact(
                opname.OpnameRequestId,
                (OpnameRequestStatusEnum)opname.OpnameRequestStatus,
                NullDash(opname.FulfilledRegId),
                opname.UpdDate == VoidSentinel ? opname.CrtDate : opname.UpdDate,
                opname.CrtDate);

        ReservationJourneyFact? rsvFact = reservation is null
            ? null
            : new ReservationJourneyFact(
                reservation.ReservationId,
                (ReservationStatusEnum)reservation.ReservationStatus,
                NullDash(reservation.RealizedRegId),
                reservation.UpdDate == VoidSentinel ? reservation.CrtDate : reservation.UpdDate,
                reservation.CrtDate);

        if (origin == JourneyOriginKind.OpnameRequest && opnFact is null && HasId(admission.OpnameRequestId))
            opnFact = new OpnameRequestJourneyFact(
                admission.OpnameRequestId,
                OpnameRequestStatusEnum.Fulfilled,
                admission.RegId,
                admission.AdmissionDate,
                admission.AdmissionDate);

        if (origin == JourneyOriginKind.Reservation && rsvFact is null && HasId(admission.ReservationId))
            rsvFact = new ReservationJourneyFact(
                admission.ReservationId,
                ReservationStatusEnum.Realized,
                admission.RegId,
                admission.AdmissionDate,
                admission.AdmissionDate);

        var facts = new JourneyNormalizedFacts(
            origin,
            patient,
            opnFact,
            rsvFact,
            [
                new AdmissionJourneyFact(
                    admission.RegId,
                    (AdmissionStatusEnum)admission.AdmissionStatus,
                    NullDash(admission.OpnameRequestId),
                    NullDash(admission.ReservationId),
                    admission.AdmissionDate,
                    NullDash(admission.BangsalId),
                    admission.BangsalName,
                    admission.CrtDate,
                    admission.UpdDate == VoidSentinel ? null : admission.UpdDate)
            ],
            waitingLists,
            HasOrphanRegistration: false);

        var activeWlRow = wlRows
            .Where(w => w.WaitingListStatus is 0 or 1)
            .OrderByDescending(w => w.UpdDate == VoidSentinel ? w.CrtDate : w.UpdDate)
            .FirstOrDefault();
        var latestWlRow = wlRows
            .OrderByDescending(w => w.UpdDate == VoidSentinel ? w.CrtDate : w.UpdDate)
            .FirstOrDefault();

        var careClass = Named(admission.KelasDkId, admission.KelasDkName)
                        ?? Named(reservation?.KelasId, reservation?.KelasName)
                        ?? Named(activeWlRow?.KelasId, activeWlRow?.KelasName)
                        ?? Named(latestWlRow?.KelasId, latestWlRow?.KelasName)
                        ?? regExtras.CareClass;

        var targetBangsal = Named(activeWlRow?.BangsalId, activeWlRow?.BangsalName)
                            ?? Named(latestWlRow?.BangsalId, latestWlRow?.BangsalName)
                            ?? Named(admission.BangsalId, admission.BangsalName)
                            ?? Named(reservation?.BangsalId, reservation?.BangsalName);

        var priority = activeWlRow?.Priority ?? latestWlRow?.Priority;
        var doctor = regExtras.Doctor
                     ?? Named(opname?.DokterId, opname?.DokterName);

        return new JourneyFactBag(
            JourneyIdFactory.Derive(facts, JourneyIntegrityValidator.Validate(facts)),
            facts,
            careClass,
            targetBangsal,
            priority,
            doctor,
            regExtras.Guarantor,
            opname?.ClinicalNotes,
            regExtras.ProcedureSummary,
            null,
            origin switch
            {
                JourneyOriginKind.OpnameRequest => "Opname Request",
                JourneyOriginKind.Reservation => "Reservation",
                _ => "Direct/Legacy Admission"
            },
            regExtras.PolicySummary,
            regExtras.AddressSummary,
            regExtras.JenisInap,
            regExtras.ProsedurMasuk,
            regExtras.CaraMasuk,
            regExtras.RegIbuId,
            regExtras.NamaIbu);
    }

    private async Task<AdmissionRowDto?> FindAdmissionByOpnameAsync(
        IDbConnection conn,
        string opnameRequestId,
        string? fulfilledRegId,
        CancellationToken cancellationToken = default)
    {
        var bySource = await conn.QueryFirstOrDefaultAsync<AdmissionRowDto>(
            new CommandDefinition(
                BuildFindAdmissionByOpnameSql(),
                new { VodDate = VoidSentinel, OpnameRequestId = opnameRequestId },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken));
        if (bySource is not null)
            return bySource;

        if (!HasId(fulfilledRegId))
            return null;

        return await conn.QueryFirstOrDefaultAsync<AdmissionRowDto>(
            new CommandDefinition(
                BuildFindAdmissionByOpnameFallbackSql(),
                new { VodDate = VoidSentinel, RegId = fulfilledRegId },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken));
    }

    private AdmissionRowDto? FindAdmissionByOpname(IDbConnection conn, string opnameRequestId, string? fulfilledRegId) =>
        FindAdmissionByOpnameAsync(conn, opnameRequestId, fulfilledRegId, CancellationToken.None).GetAwaiter().GetResult();

    private async Task<AdmissionRowDto?> FindAdmissionByReservationAsync(
        IDbConnection conn,
        string reservationId,
        string? realizedRegId,
        CancellationToken cancellationToken = default)
    {
        var bySource = await conn.QueryFirstOrDefaultAsync<AdmissionRowDto>(
            new CommandDefinition(
                BuildFindAdmissionByReservationSql(),
                new { VodDate = VoidSentinel, ReservationId = reservationId },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken));
        if (bySource is not null)
            return bySource;

        if (!HasId(realizedRegId))
            return null;

        return await conn.QueryFirstOrDefaultAsync<AdmissionRowDto>(
            new CommandDefinition(
                BuildFindAdmissionByReservationFallbackSql(),
                new { VodDate = VoidSentinel, RegId = realizedRegId },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken));
    }

    private AdmissionRowDto? FindAdmissionByReservation(IDbConnection conn, string reservationId, string? realizedRegId) =>
        FindAdmissionByReservationAsync(conn, reservationId, realizedRegId, CancellationToken.None).GetAwaiter().GetResult();

    private async Task<OpnameRowDto?> LoadOpnameRowAsync(
        IDbConnection conn,
        string id,
        CancellationToken cancellationToken = default) =>
        await conn.QueryFirstOrDefaultAsync<OpnameRowDto>(
            new CommandDefinition(
                """
                SELECT ord.InpatientOrderId AS OpnameRequestId,
                       CASE WHEN ord.OrderStatus = 2 THEN 1 WHEN ord.OrderStatus = 3 THEN 2 ELSE 0 END AS OpnameRequestStatus,
                       ord.FulfilledRegId, ord.PasienId,
                       ord.DokterId, ord.DokterName, ord.ClinicalNotes, ord.PlannedDate, ord.CrtDate, ord.UpdDate,
                       ISNULL(NULLIF(ord.PasienName,''), ISNULL(mr.fs_nm_pasien,'')) AS PasienName,
                       ISNULL(NULLIF(ord.Gender,''), ISNULL(mr.fs_jns_kelamin,'')) AS Gender,
                       CASE WHEN ord.TglLahir <> '3000-01-01' THEN ord.TglLahir WHEN ISDATE(mr.fd_tgl_lahir)=1 THEN CAST(mr.fd_tgl_lahir AS DATETIME) ELSE NULL END AS TglLahir
                FROM BILRG_AdmInpatientOrder ord
                LEFT JOIN tc_mr mr ON ord.PasienId = mr.fs_mr
                WHERE ord.VodDate = @VodDate AND ord.OrderSource IN (1, 2) AND ord.InpatientOrderId = @Id
                """,
                new { VodDate = VoidSentinel, Id = id },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken));

    private OpnameRowDto? LoadOpnameRow(IDbConnection conn, string id) =>
        LoadOpnameRowAsync(conn, id, CancellationToken.None).GetAwaiter().GetResult();

    private async Task<ReservationRowDto?> LoadReservationRowAsync(
        IDbConnection conn,
        string id,
        CancellationToken cancellationToken = default) =>
        await conn.QueryFirstOrDefaultAsync<ReservationRowDto>(
            new CommandDefinition(
                """
                SELECT ord.InpatientOrderId AS ReservationId, ord.OrderStatus AS ReservationStatus,
                       ord.FulfilledRegId AS RealizedRegId, ord.PasienId,
                       ISNULL(NULLIF(ord.PasienName,''), ISNULL(mr.fs_nm_pasien,'')) AS PasienName,
                       ISNULL(NULLIF(ord.Gender,''), ISNULL(mr.fs_jns_kelamin,'')) AS Gender,
                       CASE WHEN ord.TglLahir <> '3000-01-01' THEN ord.TglLahir WHEN ISDATE(mr.fd_tgl_lahir)=1 THEN CAST(mr.fd_tgl_lahir AS DATETIME) ELSE '3000-01-01' END AS TglLahir,
                       ord.KelasId, ord.KelasName, ord.BangsalId, ord.BangsalName, ord.PlannedDate, ord.CrtDate, ord.UpdDate,
                       ord.DokterId, ord.DokterName, ord.TipeJaminanId, ord.TipeJaminanName, ord.ClinicalNotes
                FROM BILRG_AdmInpatientOrder ord
                LEFT JOIN tc_mr mr ON ord.PasienId = mr.fs_mr
                WHERE ord.VodDate = @VodDate AND ord.OrderSource IN (3, 4) AND ord.InpatientOrderId = @Id
                """,
                new { VodDate = VoidSentinel, Id = id },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken));

    private ReservationRowDto? LoadReservationRow(IDbConnection conn, string id) =>
        LoadReservationRowAsync(conn, id, CancellationToken.None).GetAwaiter().GetResult();

    private async Task<IReadOnlyList<WaitingListJourneyFact>> LoadWaitingListsAsync(
        IDbConnection conn,
        string regId,
        CancellationToken cancellationToken = default) =>
        (await LoadWaitingListRowsAsync(conn, regId, cancellationToken))
            .Select(r => new WaitingListJourneyFact(
                r.WaitingListId,
                r.RegId,
                (WaitingListStatusEnum)r.WaitingListStatus,
                r.UpdDate == VoidSentinel ? r.CrtDate : r.UpdDate,
                NullDash(r.BangsalId),
                r.BangsalName))
            .ToList();

    private IReadOnlyList<WaitingListJourneyFact> LoadWaitingLists(IDbConnection conn, string regId) =>
        LoadWaitingListsAsync(conn, regId, CancellationToken.None).GetAwaiter().GetResult();

    private async Task<IReadOnlyList<WaitingListRowDto>> LoadWaitingListRowsAsync(
        IDbConnection conn,
        string regId,
        CancellationToken cancellationToken = default) =>
        ((await conn.QueryAsync<WaitingListRowDto>(
            new CommandDefinition(
                """
                SELECT WaitingListId, RegId, WaitingListStatus, BangsalId, BangsalName,
                       KelasId, KelasName, Priority, CrtDate, UpdDate
                FROM BILRG_BedWaitingList
                WHERE VodDate = @VodDate AND RegId = @RegId
                ORDER BY CASE WHEN UpdDate = @VodDate THEN CrtDate ELSE UpdDate END ASC, WaitingListId ASC
                """,
                new { VodDate = VoidSentinel, RegId = regId },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken))) ?? []).ToList();

    private IReadOnlyList<WaitingListRowDto> LoadWaitingListRows(IDbConnection conn, string regId) =>
        LoadWaitingListRowsAsync(conn, regId, CancellationToken.None).GetAwaiter().GetResult();

    private async Task<RegExtras> LoadRegistrationExtrasAsync(
        IDbConnection conn,
        string regId,
        CancellationToken cancellationToken = default)
    {
        var row = await conn.QueryFirstOrDefaultAsync<RegExtrasRowDto>(
            new CommandDefinition(
                """
                SELECT
                    ISNULL(reg.fs_kd_tipe_jaminan,'') AS TipeJaminanId,
                    ISNULL(tj.fs_nm_tipe_jaminan,'') AS TipeJaminanName,
                    ISNULL(reg.fs_kd_kelas,'') AS KelasId,
                    ISNULL(kl.fs_nm_kelas,'') AS KelasName,
                    ISNULL(COALESCE(NULLIF(LTRIM(RTRIM(hd.fs_kd_dokter)),''), NULLIF(LTRIM(RTRIM(reg.fs_kd_medis)),'')), '') AS DokterId,
                    ISNULL(COALESCE(NULLIF(LTRIM(RTRIM(hd.fs_nm_peg)),''), NULLIF(LTRIM(RTRIM(peg.fs_nm_peg)),'')), '') AS DokterName,
                    ISNULL(reg.fs_kd_jenis_inap,'') AS JenisInapId,
                    ISNULL(ji.fs_nm_jenis_inap,'') AS JenisInapName,
                    ISNULL(ri.fs_kd_caramasuk_inap,'') AS ProsedurMasukId,
                    ISNULL(cmi.fs_nm_caramasuk_inap,'') AS ProsedurMasukName,
                    ISNULL(reg.fs_kd_cara_masuk_dk,'') AS CaraMasukId,
                    ISNULL(cmd.fs_nm_cara_masuk_dk,'') AS CaraMasukName,
                    ISNULL(reg.fs_reg_ibu,'') AS RegIbuId,
                    ISNULL(ibuMr.fs_nm_pasien,'') AS NamaIbu
                FROM (SELECT @RegId AS RegId) x
                LEFT JOIN ta_registrasi reg ON reg.fs_kd_reg = x.RegId
                LEFT JOIN ta_tipe_jaminan tj ON reg.fs_kd_tipe_jaminan = tj.fs_kd_tipe_jaminan
                LEFT JOIN ta_kelas kl ON reg.fs_kd_kelas = kl.fs_kd_kelas
                LEFT JOIN ta_reg_inap ri ON ri.fs_kd_reg = x.RegId
                LEFT JOIN ta_jenis_inap ji ON reg.fs_kd_jenis_inap = ji.fs_kd_jenis_inap
                LEFT JOIN ta_caramasuk_inap cmi ON ri.fs_kd_caramasuk_inap = cmi.fs_kd_caramasuk_inap
                LEFT JOIN ta_cara_masuk_dk cmd ON reg.fs_kd_cara_masuk_dk = cmd.fs_kd_cara_masuk_dk
                LEFT JOIN ta_registrasi ibuReg ON reg.fs_reg_ibu = ibuReg.fs_kd_reg
                LEFT JOIN tc_mr ibuMr ON ibuReg.fs_mr = ibuMr.fs_mr
                LEFT JOIN (
                    SELECT TOP 1 h.fs_kd_reg, h.fs_kd_dokter, ISNULL(p.fs_nm_peg,'') AS fs_nm_peg
                    FROM ta_reg_history_dokter h
                    LEFT JOIN td_peg p ON h.fs_kd_dokter = p.fs_kd_peg
                    WHERE h.fs_kd_reg = @RegId
                      AND ISNULL(LTRIM(RTRIM(h.fd_tgl_selesai)),'') = ''
                    ORDER BY h.fd_tgl_mulai DESC
                ) hd ON hd.fs_kd_reg = x.RegId
                LEFT JOIN td_peg peg ON reg.fs_kd_medis = peg.fs_kd_peg
                """,
                new { RegId = regId },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken));

        if (row is null)
            return RegExtras.Empty;

        var effectiveRegIbu = (string.IsNullOrWhiteSpace(row.RegIbuId) || row.RegIbuId.All(c => c == '0' || c == '-')) ? null : row.RegIbuId.Trim();
        var effectiveNamaIbu = string.IsNullOrWhiteSpace(row.NamaIbu) ? null : row.NamaIbu.Trim();

        return new RegExtras(
            Named(row.TipeJaminanId, row.TipeJaminanName),
            Named(row.KelasId, row.KelasName),
            Named(row.DokterId, row.DokterName),
            string.IsNullOrWhiteSpace(row.ProsedurMasukId) ? null : $"Prosedur masuk: {row.ProsedurMasukId}",
            null,
            null,
            Named(row.JenisInapId, row.JenisInapName),
            Named(row.ProsedurMasukId, row.ProsedurMasukName),
            Named(row.CaraMasukId, row.CaraMasukName),
            effectiveRegIbu,
            effectiveNamaIbu);
    }

    private RegExtras LoadRegistrationExtras(IDbConnection conn, string regId) =>
        LoadRegistrationExtrasAsync(conn, regId, CancellationToken.None).GetAwaiter().GetResult();

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        await conn.OpenAsync(cancellationToken);
        return conn;
    }

    private SqlConnection Open() =>
        OpenAsync(CancellationToken.None).GetAwaiter().GetResult();

    private DynamicParameters BuildCommonParams(JourneyListFilter filter)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);
        dp.AddParam("@ScopeActive", filter.Scope == JourneyListScope.Active ? 1 : 0, SqlDbType.Int);
        dp.AddParam("@StageFilter", filter.Stage.HasValue ? (int)filter.Stage.Value : -1, SqlDbType.Int);
        dp.AddParam("@HasStageFilter", filter.Stage.HasValue ? 1 : 0, SqlDbType.Int);

        if (!string.IsNullOrWhiteSpace(filter.DokterId))
            dp.AddParam("@DokterId", filter.DokterId.Trim(), SqlDbType.VarChar);
        else
            dp.AddParam("@DokterId", "", SqlDbType.VarChar);

        if (!string.IsNullOrWhiteSpace(filter.BangsalId))
            dp.AddParam("@BangsalId", filter.BangsalId.Trim(), SqlDbType.VarChar);
        else
            dp.AddParam("@BangsalId", "", SqlDbType.VarChar);

        if (!string.IsNullOrWhiteSpace(filter.KelasId))
            dp.AddParam("@KelasId", filter.KelasId.Trim(), SqlDbType.VarChar);
        else
            dp.AddParam("@KelasId", "", SqlDbType.VarChar);

        if (!string.IsNullOrWhiteSpace(filter.TipeJaminanId))
            dp.AddParam("@TipeJaminanId", filter.TipeJaminanId.Trim(), SqlDbType.VarChar);
        else
            dp.AddParam("@TipeJaminanId", "", SqlDbType.VarChar);

        dp.AddParam("@HasPriority", filter.Priority.HasValue ? 1 : 0, SqlDbType.Int);
        dp.AddParam("@Priority", filter.Priority ?? 0, SqlDbType.Int);

        if (filter.DateFrom.HasValue)
            dp.AddParam("@DateFrom", filter.DateFrom.Value, SqlDbType.DateTime);
        else
            dp.AddParam("@DateFrom", new DateTime(1900, 1, 1), SqlDbType.DateTime);

        if (filter.DateTo.HasValue)
            dp.AddParam("@DateToEnd", filter.DateTo.Value.Date.AddDays(1), SqlDbType.DateTime);
        else
            dp.AddParam("@DateToEnd", new DateTime(9999, 12, 31), SqlDbType.DateTime);

        dp.AddParam("@HasDateFrom", filter.DateFrom.HasValue ? 1 : 0, SqlDbType.Int);
        dp.AddParam("@HasDateTo", filter.DateTo.HasValue ? 1 : 0, SqlDbType.Int);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            dp.AddParam("@SearchPattern", $"%{filter.SearchTerm.Trim()}%", SqlDbType.VarChar);
        else
            dp.AddParam("@SearchPattern", "", SqlDbType.VarChar);

        dp.AddParam("@HasSearch", string.IsNullOrWhiteSpace(filter.SearchTerm) ? 0 : 1, SqlDbType.Int);
        dp.AddParam("@HasDokter", string.IsNullOrWhiteSpace(filter.DokterId) ? 0 : 1, SqlDbType.Int);
        dp.AddParam("@HasBangsal", string.IsNullOrWhiteSpace(filter.BangsalId) ? 0 : 1, SqlDbType.Int);
        dp.AddParam("@HasKelas", string.IsNullOrWhiteSpace(filter.KelasId) ? 0 : 1, SqlDbType.Int);
        dp.AddParam("@HasTipeJaminan", string.IsNullOrWhiteSpace(filter.TipeJaminanId) ? 0 : 1, SqlDbType.Int);

        return dp;
    }

    private static string BuildFacetAndPageSql(JourneyListFilter filter) =>
        $"""
        {BuildFacetSql(filter)};

        {BuildPageSql(filter)};
        """;

    private static string BuildFacetSql(JourneyListFilter filter) =>
        $"""
        {BuildJourneyCte()}
        SELECT j.SqlStage, COUNT(*) AS Cnt
        FROM JourneyProjected j
        WHERE {BuildNonStageWhere(filter)}
        GROUP BY j.SqlStage
        """;

    private static string BuildPageSql(JourneyListFilter filter) =>
        $"""
        {BuildJourneyCte()}
        SELECT TOP (@PageSize) j.JourneyId, j.SortAt, j.SqlStage, j.OriginKind, j.RegId
        FROM JourneyProjected j
        WHERE {BuildNonStageWhere(filter)}
          AND (@HasStageFilter = 0 OR j.SqlStage = @StageFilter)
          AND (
                @HasCursor = 0
                OR j.SortAt < @CursorSortAt
                OR (j.SortAt = @CursorSortAt AND j.JourneyId < @CursorJourneyId)
              )
        ORDER BY j.SortAt DESC, j.JourneyId DESC
        """;

    private static string BuildNonStageWhere(JourneyListFilter filter) =>
        """
        (@ScopeActive = 0 OR j.SqlStage NOT IN (5, 6))
          AND (@HasDokter = 0 OR j.DokterId = @DokterId)
          AND (@HasBangsal = 0 OR j.BangsalId = @BangsalId)
          AND (@HasKelas = 0 OR j.KelasId = @KelasId)
          AND (@HasTipeJaminan = 0 OR j.TipeJaminanId = @TipeJaminanId)
          AND (@HasPriority = 0 OR j.Priority = @Priority)
          AND (@HasDateFrom = 0 OR j.SortAt >= @DateFrom)
          AND (@HasDateTo = 0 OR j.SortAt < @DateToEnd)
          AND (
                @HasSearch = 0
                OR j.PasienName LIKE @SearchPattern
                OR j.PasienId LIKE @SearchPattern
                OR j.JourneyId LIKE @SearchPattern
                OR j.RegId LIKE @SearchPattern
                OR j.DokterName LIKE @SearchPattern
                OR j.TipeJaminanName LIKE @SearchPattern
                OR j.BangsalName LIKE @SearchPattern
              )
        """;

    /// <summary>
    /// Journey-root CTE. Does not join Reservation to Opname Request.
    /// Does not require ta_registrasi2.
    /// </summary>
    internal static string BuildJourneyCte() =>
        $"""
        WITH WlAgg AS (
            SELECT
                w.RegId,
                COUNT(*) AS WlCount,
                SUM(CASE WHEN w.WaitingListStatus IN (0,1) THEN 1 ELSE 0 END) AS ActiveCount,
                MAX(CASE WHEN w.WaitingListStatus = 0 THEN 1 ELSE 0 END) AS HasWaiting,
                MAX(CASE WHEN w.WaitingListStatus = 1 THEN 1 ELSE 0 END) AS HasAccepted,
                MAX(CASE WHEN w.WaitingListStatus = 2 THEN 1 ELSE 0 END) AS HasClosed,
                MAX(w.Priority) AS Priority,
                MAX(CASE WHEN w.WaitingListStatus IN (0,1) THEN w.BangsalId END) AS ActiveBangsalId,
                MAX(CASE WHEN w.WaitingListStatus IN (0,1) THEN w.BangsalName END) AS ActiveBangsalName,
                MAX(CASE WHEN w.WaitingListStatus IN (0,1) THEN
                    CASE WHEN w.UpdDate = @VodDate THEN w.CrtDate ELSE w.UpdDate END END) AS ActiveStatusAt,
                MAX(CASE WHEN w.UpdDate = @VodDate THEN w.CrtDate ELSE w.UpdDate END) AS LastWlAt,
                MAX(w.KelasId) AS WlKelasId,
                MAX(w.KelasName) AS WlKelasName
            FROM BILRG_BedWaitingList w
            WHERE w.VodDate = @VodDate
            GROUP BY w.RegId
        ),
        OrderMulti AS (
            SELECT InpatientOrderId
            FROM BILRG_AdmAdmission
            WHERE VodDate = @VodDate AND InpatientOrderId NOT IN ('','-')
            GROUP BY InpatientOrderId
            HAVING COUNT(*) > 1
        ),
        ProspectiveOrders AS (
            SELECT
                CAST(
                    CASE 
                        WHEN o.OrderSource IN (1, 2) THEN 'opn:' + o.InpatientOrderId
                        ELSE 'rsv:' + o.InpatientOrderId
                    END AS VARCHAR(64)
                ) AS JourneyId,
                CASE 
                    WHEN o.OrderSource IN (1, 2) THEN 0
                    ELSE 1
                END AS OriginKind,
                CAST(NULL AS VARCHAR(10)) AS RegId,
                CASE 
                    WHEN o.OrderSource IN (1, 2) THEN o.InpatientOrderId
                    ELSE CAST(NULL AS VARCHAR(12))
                END AS SourceOpnameRequestId,
                CASE 
                    WHEN o.OrderSource IN (3, 4) THEN o.InpatientOrderId
                    ELSE CAST(NULL AS VARCHAR(12))
                END AS SourceReservationId,
                o.PasienId,
                ISNULL(NULLIF(o.PasienName,''), ISNULL(mr.fs_nm_pasien,'')) AS PasienName,
                o.DokterId,
                o.DokterName,
                o.KelasId,
                o.KelasName,
                o.BangsalId,
                o.BangsalName,
                o.TipeJaminanId,
                o.TipeJaminanName,
                CAST(NULL AS INT) AS Priority,
                CASE 
                    WHEN o.OrderSource IN (1, 2) THEN 
                        CASE WHEN o.UpdDate = @VodDate THEN o.CrtDate ELSE o.UpdDate END
                    ELSE 
                        CASE WHEN o.UpdDate = @VodDate THEN o.PlannedDate ELSE o.UpdDate END
                END AS SortAt,
                CASE
                    WHEN o.OrderStatus = 2 THEN 1
                    ELSE 0
                END AS HasBlockingIntegrity,
                0 AS AdmissionCancelled,
                CASE WHEN o.OrderStatus = 3 THEN 1 ELSE 0 END AS ProspectiveSourceCancelled,
                0 AS AdmissionCompletedWithoutAuthority,
                CAST(NULL AS INT) AS ActiveWaitingListStatus,
                0 AS HasClosedHandover,
                0 AS HasActiveAdmission,
                CASE WHEN o.OrderSource IN (1, 2) AND o.OrderStatus IN (0, 1) THEN 1 ELSE 0 END AS ProspectiveOpnameRequested,
                CASE WHEN o.OrderSource IN (3, 4) AND o.OrderStatus IN (0, 1) THEN 1 ELSE 0 END AS ProspectiveReservationReady
            FROM BILRG_AdmInpatientOrder o
            LEFT JOIN tc_mr mr ON o.PasienId = mr.fs_mr
            WHERE o.VodDate = @VodDate
              AND o.OrderSource IN (1, 2, 3, 4)
              AND NOT EXISTS (
                    SELECT 1 FROM BILRG_AdmAdmission a
                    WHERE a.VodDate = @VodDate 
                      AND a.InpatientOrderId = o.InpatientOrderId)
        ),
        AdmissionRoots AS (
            SELECT
                CAST(
                    CASE
                        WHEN (
                            (a.InpatientOrderId NOT IN ('','-') AND om.InpatientOrderId IS NOT NULL)
                            OR ISNULL(wl.ActiveCount,0) > 1
                            OR a.AdmissionStatus = 3
                            OR (a.AdmissionStatus = 4 AND ISNULL(wl.ActiveCount,0) > 0)
                            OR (
                                a.AdmissionStatus IN (0,1,2)
                                AND (
                                    (o.OpnameRequestId IS NOT NULL AND o.OpnameRequestStatus = 2)
                                    OR (rs.ReservationId IS NOT NULL AND rs.ReservationStatus = 3)
                                )
                            )
                            OR (
                                o.OpnameRequestId IS NOT NULL AND o.OpnameRequestStatus = 1
                                AND (
                                    ISNULL(NULLIF(LTRIM(RTRIM(o.FulfilledRegId)),''),'-') <> a.RegId
                                    OR a.InpatientOrderId <> o.OpnameRequestId
                                )
                            )
                            OR (
                                rs.ReservationId IS NOT NULL AND rs.ReservationStatus = 2
                                AND (
                                    ISNULL(NULLIF(LTRIM(RTRIM(rs.RealizedRegId)),''),'-') <> a.RegId
                                    OR a.InpatientOrderId <> rs.ReservationId
                                )
                            )
                        ) THEN 'reg:' + a.RegId
                        WHEN a.InpatientOrderId NOT IN ('','-') AND o.OpnameRequestId IS NOT NULL
                            THEN 'opn:' + a.InpatientOrderId
                        WHEN a.InpatientOrderId NOT IN ('','-') AND rs.ReservationId IS NOT NULL
                            THEN 'rsv:' + a.InpatientOrderId
                        ELSE 'reg:' + a.RegId
                    END AS VARCHAR(64)
                ) AS JourneyId,
                CASE
                    WHEN a.InpatientOrderId NOT IN ('','-') AND o.OpnameRequestId IS NOT NULL THEN 0
                    WHEN a.InpatientOrderId NOT IN ('','-') AND rs.ReservationId IS NOT NULL THEN 1
                    ELSE 2
                END AS OriginKind,
                a.RegId,
                o.OpnameRequestId AS SourceOpnameRequestId,
                rs.ReservationId AS SourceReservationId,
                a.PasienId,
                ISNULL(COALESCE(NULLIF(LTRIM(RTRIM(mr.fs_nm_pasien)),''), NULLIF(LTRIM(RTRIM(a.PasienId)),'')), '') AS PasienName,
                ISNULL(COALESCE(NULLIF(LTRIM(RTRIM(hd.DokterId)),''), NULLIF(LTRIM(RTRIM(reg.fs_kd_medis)),''), NULLIF(LTRIM(RTRIM(o.DokterId)),'')), '') AS DokterId,
                ISNULL(COALESCE(NULLIF(LTRIM(RTRIM(hd.DokterName)),''), NULLIF(LTRIM(RTRIM(peg.fs_nm_peg)),''), NULLIF(LTRIM(RTRIM(o.DokterName)),'')), '') AS DokterName,
                COALESCE(NULLIF(a.KelasDkId,'-'), NULLIF(wl.WlKelasId,'-'), NULLIF(rs.KelasId,'-'), ISNULL(reg.fs_kd_kelas,'')) AS KelasId,
                COALESCE(NULLIF(a.KelasDkName,''), NULLIF(wl.WlKelasName,''), NULLIF(rs.KelasName,''), '') AS KelasName,
                COALESCE(NULLIF(wl.ActiveBangsalId,'-'), NULLIF(a.BangsalId,'-'), NULLIF(rs.BangsalId,'-'), '') AS BangsalId,
                COALESCE(NULLIF(wl.ActiveBangsalName,''), NULLIF(a.BangsalName,''), NULLIF(rs.BangsalName,''), '') AS BangsalName,
                ISNULL(reg.fs_kd_tipe_jaminan,'') AS TipeJaminanId,
                ISNULL(tj.fs_nm_tipe_jaminan,'') AS TipeJaminanName,
                wl.Priority,
                COALESCE(wl.ActiveStatusAt, wl.LastWlAt, a.AdmissionDate, a.CrtDate) AS SortAt,
                CASE
                    WHEN (a.InpatientOrderId NOT IN ('','-') AND om.InpatientOrderId IS NOT NULL) THEN 1
                    WHEN ISNULL(wl.ActiveCount,0) > 1 THEN 1
                    WHEN a.AdmissionStatus = 3 THEN 1
                    WHEN (a.AdmissionStatus = 4 AND ISNULL(wl.ActiveCount,0) > 0) THEN 1
                    WHEN (
                        a.AdmissionStatus IN (0,1,2)
                        AND (
                            (o.OpnameRequestId IS NOT NULL AND o.OpnameRequestStatus = 2)
                            OR (rs.ReservationId IS NOT NULL AND rs.ReservationStatus = 3)
                        )
                    ) THEN 1
                    WHEN (
                        o.OpnameRequestId IS NOT NULL AND o.OpnameRequestStatus = 1
                        AND (
                            ISNULL(NULLIF(LTRIM(RTRIM(o.FulfilledRegId)),''),'-') <> a.RegId
                            OR a.InpatientOrderId <> o.OpnameRequestId
                        )
                    ) THEN 1
                    WHEN (
                        rs.ReservationId IS NOT NULL AND rs.ReservationStatus = 2
                        AND (
                            ISNULL(NULLIF(LTRIM(RTRIM(rs.RealizedRegId)),''),'-') <> a.RegId
                            OR a.InpatientOrderId <> rs.ReservationId
                        )
                    ) THEN 1
                    ELSE 0
                END AS HasBlockingIntegrity,
                CASE WHEN a.AdmissionStatus = 4 THEN 1 ELSE 0 END AS AdmissionCancelled,
                0 AS ProspectiveSourceCancelled,
                CASE WHEN a.AdmissionStatus = 3 THEN 1 ELSE 0 END AS AdmissionCompletedWithoutAuthority,
                CASE
                    WHEN ISNULL(wl.ActiveCount,0) > 1 THEN NULL
                    WHEN ISNULL(wl.HasWaiting,0) = 1 THEN 0
                    WHEN ISNULL(wl.HasAccepted,0) = 1 THEN 1
                    ELSE NULL
                END AS ActiveWaitingListStatus,
                CASE WHEN ISNULL(wl.HasClosed,0) = 1 THEN 1 ELSE 0 END AS HasClosedHandover,
                CASE WHEN a.AdmissionStatus IN (0,1,2) THEN 1 ELSE 0 END AS HasActiveAdmission,
                0 AS ProspectiveOpnameRequested,
                0 AS ProspectiveReservationReady
            FROM BILRG_AdmAdmission a
            LEFT JOIN tc_mr mr ON a.PasienId = mr.fs_mr
            LEFT JOIN (
                SELECT 
                    InpatientOrderId AS OpnameRequestId,
                    CASE WHEN OrderStatus = 2 THEN 1 WHEN OrderStatus = 3 THEN 2 ELSE 0 END AS OpnameRequestStatus,
                    FulfilledRegId,
                    DokterId,
                    DokterName
                FROM BILRG_AdmInpatientOrder
                WHERE VodDate = @VodDate AND OrderSource IN (1, 2)
            ) o ON a.InpatientOrderId <> '-' AND a.InpatientOrderId = o.OpnameRequestId
            LEFT JOIN (
                SELECT 
                    InpatientOrderId AS ReservationId,
                    OrderStatus AS ReservationStatus,
                    FulfilledRegId AS RealizedRegId,
                    KelasId,
                    KelasName,
                    BangsalId,
                    BangsalName
                FROM BILRG_AdmInpatientOrder
                WHERE VodDate = @VodDate AND OrderSource IN (3, 4)
            ) rs ON a.InpatientOrderId <> '-' AND a.InpatientOrderId = rs.ReservationId
            LEFT JOIN WlAgg wl ON wl.RegId = a.RegId
            LEFT JOIN OrderMulti om ON om.InpatientOrderId = a.InpatientOrderId
            LEFT JOIN ta_registrasi reg ON reg.fs_kd_reg = a.RegId
            LEFT JOIN ta_tipe_jaminan tj ON reg.fs_kd_tipe_jaminan = tj.fs_kd_tipe_jaminan
            LEFT JOIN td_peg peg ON reg.fs_kd_medis = peg.fs_kd_peg
            OUTER APPLY (
                SELECT TOP 1 h.fs_kd_dokter AS DokterId, ISNULL(p.fs_nm_peg, '') AS DokterName
                FROM ta_reg_history_dokter h
                LEFT JOIN td_peg p ON h.fs_kd_dokter = p.fs_kd_peg
                WHERE h.fs_kd_reg = a.RegId
                  AND ISNULL(LTRIM(RTRIM(h.fd_tgl_selesai)),'') = ''
                ORDER BY h.fd_tgl_mulai DESC
            ) hd
            WHERE a.VodDate = @VodDate
              AND (@ScopeActive = 0 OR a.AdmissionStatus <> 4)
        ),
        JourneyUnion AS (
            SELECT * FROM ProspectiveOrders
            UNION ALL
            SELECT * FROM AdmissionRoots
        ),
        JourneyProjected AS (
            SELECT
                u.*,
                ({Release1JourneySqlStageExpression.SqlCaseExpression.Replace("j.", "u.")}) AS SqlStage
            FROM JourneyUnion u
        )
        """;

    private static JourneyOriginKind ResolveOrigin(string? opnameRequestId, string? reservationId)
    {
        var hasOpn = HasId(opnameRequestId);
        var hasRsv = HasId(reservationId);
        if (hasOpn && !hasRsv)
            return JourneyOriginKind.OpnameRequest;
        if (hasRsv && !hasOpn)
            return JourneyOriginKind.Reservation;
        return JourneyOriginKind.DirectOrLegacyAdmission;
    }

    private JourneyPatientSummary BuildPatient(
        string pasienId,
        string? name,
        string? gender,
        DateTime? tglLahir)
    {
        DateOnly? birth = null;
        string? age = null;
        if (tglLahir.HasValue && tglLahir.Value.Year > 1900 && tglLahir.Value.Year < 3000)
        {
            birth = DateOnly.FromDateTime(tglLahir.Value);
            var today = _businessDate;
            var years = today.Year - birth.Value.Year;
            if (birth.Value > today.AddYears(-years))
                years--;
            age = $"{years} th";
        }

        return new JourneyPatientSummary(
            pasienId,
            pasienId,
            string.IsNullOrWhiteSpace(name) ? pasienId : name,
            string.IsNullOrWhiteSpace(gender) ? null : gender,
            birth,
            age);
    }

    private static JourneyIdName? Named(string? id, string? name) =>
        HasId(id) ? new JourneyIdName(id, string.IsNullOrWhiteSpace(name) ? id : name) : null;

    private static bool HasId(string? id) =>
        !string.IsNullOrWhiteSpace(id) && id != "-";

    private static string NullDash(string? id) =>
        HasId(id) ? id! : "-";

    internal sealed record JourneyFactBag(
        string JourneyId,
        JourneyNormalizedFacts Facts,
        JourneyIdName? CareClass,
        JourneyIdName? TargetBangsal,
        int? Priority,
        JourneyIdName? Doctor,
        JourneyIdName? Guarantor,
        string? ClinicalNotes,
        string? ProcedureSummary,
        JourneyIdName? ReferringDoctor,
        string? ReferralOrSourceSummary,
        string? PolicySummary,
        string? AddressSummary,
        JourneyIdName? JenisInap = null,
        JourneyIdName? ProsedurMasuk = null,
        JourneyIdName? CaraMasuk = null,
        string? RegIbuId = null,
        string? NamaIbu = null);

    internal sealed record RegExtras(
        JourneyIdName? Guarantor,
        JourneyIdName? CareClass,
        JourneyIdName? Doctor,
        string? ProcedureSummary,
        string? PolicySummary,
        string? AddressSummary,
        JourneyIdName? JenisInap = null,
        JourneyIdName? ProsedurMasuk = null,
        JourneyIdName? CaraMasuk = null,
        string? RegIbuId = null,
        string? NamaIbu = null)
    {
        public static RegExtras Empty { get; } = new(null, null, null, null, null, null);
    }

    private sealed record FacetRowDto(int SqlStage, int Cnt);

    private sealed record JourneyPageKeyDto(
        string JourneyId,
        DateTime SortAt,
        int SqlStage,
        int OriginKind,
        string? RegId);

    internal sealed record OpnameRowDto(
        string OpnameRequestId,
        int OpnameRequestStatus,
        string FulfilledRegId,
        string PasienId,
        string DokterId,
        string DokterName,
        string ClinicalNotes,
        DateTime PlannedDate,
        DateTime CrtDate,
        DateTime UpdDate,
        string PasienName,
        string Gender,
        DateTime? TglLahir);

    internal sealed record ReservationRowDto(
        string ReservationId,
        int ReservationStatus,
        string RealizedRegId,
        string PasienId,
        string PasienName,
        string Gender,
        DateTime TglLahir,
        string KelasId,
        string KelasName,
        string BangsalId,
        string BangsalName,
        DateTime PlannedDate,
        DateTime CrtDate,
        DateTime UpdDate,
        string DokterId = "",
        string DokterName = "",
        string TipeJaminanId = "",
        string TipeJaminanName = "",
        string ClinicalNotes = "");

    internal sealed record AdmissionRowDto(
        string RegId,
        int AdmissionStatus,
        string PasienId,
        string OpnameRequestId,
        string ReservationId,
        string KelasDkId,
        string KelasDkName,
        string BangsalId,
        string BangsalName,
        DateTime AdmissionDate,
        DateTime CrtDate,
        DateTime UpdDate,
        string PasienName,
        string Gender,
        DateTime? TglLahir);

    internal sealed record WaitingListRowDto(
        string WaitingListId,
        string RegId,
        int WaitingListStatus,
        string BangsalId,
        string BangsalName,
        string KelasId,
        string KelasName,
        int Priority,
        DateTime CrtDate,
        DateTime UpdDate);

    internal static string BuildFindAdmissionByOpnameSql() =>
        """
        SELECT a.RegId, a.AdmissionStatus, a.PasienId, 
               a.InpatientOrderId AS OpnameRequestId,
               '-' AS ReservationId,
               a.KelasDkId, a.KelasDkName, a.BangsalId, a.BangsalName, a.AdmissionDate, a.CrtDate, a.UpdDate,
               ISNULL(mr.fs_nm_pasien,'') AS PasienName,
               ISNULL(mr.fs_jns_kelamin,'') AS Gender,
               CASE WHEN ISDATE(mr.fd_tgl_lahir)=1 THEN CAST(mr.fd_tgl_lahir AS DATETIME) ELSE NULL END AS TglLahir
        FROM BILRG_AdmAdmission a
        LEFT JOIN tc_mr mr ON a.PasienId = mr.fs_mr
        WHERE a.VodDate = @VodDate AND a.InpatientOrderId = @OpnameRequestId
        ORDER BY a.AdmissionDate DESC
        """;

    internal static string BuildFindAdmissionByOpnameFallbackSql() =>
        """
        SELECT a.RegId, a.AdmissionStatus, a.PasienId, 
               a.InpatientOrderId AS OpnameRequestId,
               '-' AS ReservationId,
               a.KelasDkId, a.KelasDkName, a.BangsalId, a.BangsalName, a.AdmissionDate, a.CrtDate, a.UpdDate,
               ISNULL(mr.fs_nm_pasien,'') AS PasienName,
               ISNULL(mr.fs_jns_kelamin,'') AS Gender,
               CASE WHEN ISDATE(mr.fd_tgl_lahir)=1 THEN CAST(mr.fd_tgl_lahir AS DATETIME) ELSE NULL END AS TglLahir
        FROM BILRG_AdmAdmission a
        LEFT JOIN tc_mr mr ON a.PasienId = mr.fs_mr
        WHERE a.VodDate = @VodDate AND a.RegId = @RegId
        """;

    internal static string BuildFindAdmissionByReservationSql() =>
        """
        SELECT a.RegId, a.AdmissionStatus, a.PasienId, 
               '-' AS OpnameRequestId,
               a.InpatientOrderId AS ReservationId,
               a.KelasDkId, a.KelasDkName, a.BangsalId, a.BangsalName, a.AdmissionDate, a.CrtDate, a.UpdDate,
               ISNULL(mr.fs_nm_pasien,'') AS PasienName,
               ISNULL(mr.fs_jns_kelamin,'') AS Gender,
               CASE WHEN ISDATE(mr.fd_tgl_lahir)=1 THEN CAST(mr.fd_tgl_lahir AS DATETIME) ELSE NULL END AS TglLahir
        FROM BILRG_AdmAdmission a
        LEFT JOIN tc_mr mr ON a.PasienId = mr.fs_mr
        WHERE a.VodDate = @VodDate AND a.InpatientOrderId = @ReservationId
        ORDER BY a.AdmissionDate DESC
        """;

    internal static string BuildFindAdmissionByReservationFallbackSql() =>
        """
        SELECT a.RegId, a.AdmissionStatus, a.PasienId, 
               '-' AS OpnameRequestId,
               a.InpatientOrderId AS ReservationId,
               a.KelasDkId, a.KelasDkName, a.BangsalId, a.BangsalName, a.AdmissionDate, a.CrtDate, a.UpdDate,
               ISNULL(mr.fs_nm_pasien,'') AS PasienName,
               ISNULL(mr.fs_jns_kelamin,'') AS Gender,
               CASE WHEN ISDATE(mr.fd_tgl_lahir)=1 THEN CAST(mr.fd_tgl_lahir AS DATETIME) ELSE NULL END AS TglLahir
        FROM BILRG_AdmAdmission a
        LEFT JOIN tc_mr mr ON a.PasienId = mr.fs_mr
        WHERE a.VodDate = @VodDate AND a.RegId = @RegId
        """;

    internal static string BuildLoadRegJourneySql() =>
        """
        SELECT a.RegId, a.AdmissionStatus, a.PasienId, 
               CASE 
                   WHEN a.InpatientOrderId NOT IN ('','-') AND io.OrderSource IN (1, 2) THEN a.InpatientOrderId
                   ELSE '-'
               END AS OpnameRequestId,
               CASE 
                   WHEN a.InpatientOrderId NOT IN ('','-') AND io.OrderSource IN (3, 4) THEN a.InpatientOrderId
                   ELSE '-'
               END AS ReservationId,
               a.KelasDkId, a.KelasDkName, a.BangsalId, a.BangsalName, a.AdmissionDate, a.CrtDate, a.UpdDate,
               ISNULL(mr.fs_nm_pasien,'') AS PasienName,
               ISNULL(mr.fs_jns_kelamin,'') AS Gender,
               CASE WHEN ISDATE(mr.fd_tgl_lahir)=1 THEN CAST(mr.fd_tgl_lahir AS DATETIME) ELSE NULL END AS TglLahir
        FROM BILRG_AdmAdmission a
        LEFT JOIN tc_mr mr ON a.PasienId = mr.fs_mr
        LEFT JOIN BILRG_AdmInpatientOrder io ON a.InpatientOrderId = io.InpatientOrderId AND io.VodDate = @VodDate
        WHERE a.VodDate = @VodDate AND a.RegId = @Id
        """;

    internal sealed record RegExtrasRowDto(
        string TipeJaminanId,
        string TipeJaminanName,
        string KelasId,
        string KelasName,
        string DokterId,
        string DokterName,
        string JenisInapId,
        string JenisInapName,
        string ProsedurMasukId,
        string ProsedurMasukName,
        string CaraMasukId,
        string CaraMasukName,
        string RegIbuId = "",
        string NamaIbu = "")
    {
        public RegExtrasRowDto() : this("", "", "", "", "", "", "", "", "", "", "", "", "", "") { }
    }

}
