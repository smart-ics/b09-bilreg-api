using System.Data;
using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Dapper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.JourneyFeature;

public sealed partial class JourneyDal
{
    /// <summary>
    /// Loads fact bags for a page with a fixed number of round-trips (does not grow with page size).
    /// Adds exactly 2 round-trips when <paramref name="keys"/> is non-empty (two QueryMultiple batches).
    /// </summary>
    private Dictionary<string, JourneyFactBag> BatchLoadFactBags(
        IDbConnection conn,
        IReadOnlyList<JourneyPageKeyDto> keys,
        ref int roundTrips)
    {
        var result = new Dictionary<string, JourneyFactBag>(StringComparer.OrdinalIgnoreCase);
        if (keys.Count == 0)
            return result;

        var opnIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rsvIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var regIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key.RegId) && key.RegId != "-")
                regIds.Add(key.RegId);

            if (!JourneyIdFactory.TryParse(key.JourneyId, out var prefix, out var domainId))
                continue;

            switch (prefix)
            {
                case JourneyIdFactory.OpnamePrefix:
                    opnIds.Add(domainId);
                    break;
                case JourneyIdFactory.ReservationPrefix:
                    rsvIds.Add(domainId);
                    break;
                case JourneyIdFactory.RegPrefix:
                    regIds.Add(domainId);
                    break;
            }
        }

        var (opnames, reservations, admissions) = QuerySourcesAndAdmissions(conn, opnIds, rsvIds, regIds);
        roundTrips++;

        foreach (var adm in admissions)
        {
            regIds.Add(adm.RegId);
            if (HasId(adm.OpnameRequestId))
                opnIds.Add(adm.OpnameRequestId);
            if (HasId(adm.ReservationId))
                rsvIds.Add(adm.ReservationId);
        }

        foreach (var opn in opnames.Values)
        {
            if (HasId(opn.FulfilledRegId))
                regIds.Add(opn.FulfilledRegId);
        }

        foreach (var rsv in reservations.Values)
        {
            if (HasId(rsv.RealizedRegId))
                regIds.Add(rsv.RealizedRegId);
        }

        var expanded = QueryExpandedFacts(conn, opnIds, rsvIds, regIds);
        roundTrips++;

        foreach (var opn in expanded.Opnames)
            opnames[opn.OpnameRequestId] = opn;
        foreach (var rsv in expanded.Reservations)
            reservations[rsv.ReservationId] = rsv;

        var admissionsByReg = expanded.Admissions
            .Concat(admissions)
            .GroupBy(a => a.RegId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AdmissionDate).First(), StringComparer.OrdinalIgnoreCase);

        var admissionsByOpn = admissionsByReg.Values
            .Where(a => HasId(a.OpnameRequestId))
            .GroupBy(a => a.OpnameRequestId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AdmissionDate).First(), StringComparer.OrdinalIgnoreCase);

        var admissionsByRsv = admissionsByReg.Values
            .Where(a => HasId(a.ReservationId))
            .GroupBy(a => a.ReservationId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AdmissionDate).First(), StringComparer.OrdinalIgnoreCase);

        var wlByReg = expanded.WaitingLists
            .GroupBy(w => w.RegId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<WaitingListRowDto>)g
                    .OrderBy(w => w.UpdDate == VoidSentinel ? w.CrtDate : w.UpdDate)
                    .ThenBy(w => w.WaitingListId, StringComparer.Ordinal)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        var extrasByReg = expanded.RegExtras
            .ToDictionary(e => e.RegId, MapRegExtras, StringComparer.OrdinalIgnoreCase);

        foreach (var key in keys)
        {
            if (!JourneyIdFactory.TryParse(key.JourneyId, out var prefix, out var domainId))
                continue;

            JourneyFactBag? bag = prefix switch
            {
                JourneyIdFactory.OpnamePrefix => FoldOpnameJourney(
                    domainId, key.JourneyId, opnames, admissionsByOpn, admissionsByReg, reservations, wlByReg, extrasByReg),
                JourneyIdFactory.ReservationPrefix => FoldReservationJourney(
                    domainId, key.JourneyId, reservations, admissionsByRsv, admissionsByReg, opnames, wlByReg, extrasByReg),
                JourneyIdFactory.RegPrefix => FoldRegJourney(
                    domainId, admissionsByReg, opnames, reservations, wlByReg, extrasByReg),
                _ => null
            };

            if (bag is not null)
                result[key.JourneyId] = bag;
        }

        return result;
    }

    private (Dictionary<string, OpnameRowDto> Opnames,
        Dictionary<string, ReservationRowDto> Reservations,
        List<AdmissionRowDto> Admissions)
        QuerySourcesAndAdmissions(
            IDbConnection conn,
            IReadOnlyCollection<string> opnIds,
            IReadOnlyCollection<string> rsvIds,
            IReadOnlyCollection<string> regIds)
    {
        var sql = $"""
            {BuildOpnameSelectSql()};
            {BuildReservationSelectSql()};
            {BuildAdmissionSelectSql()};
            """;

        using var multi = conn.QueryMultiple(
            new CommandDefinition(
                sql,
                BuildIdParams(opnIds, rsvIds, regIds),
                commandTimeout: CommandTimeoutSeconds));

        var opnames = (multi.Read<OpnameRowDto>() ?? [])
            .ToDictionary(o => o.OpnameRequestId, StringComparer.OrdinalIgnoreCase);
        var reservations = (multi.Read<ReservationRowDto>() ?? [])
            .ToDictionary(r => r.ReservationId, StringComparer.OrdinalIgnoreCase);
        var admissions = (multi.Read<AdmissionRowDto>() ?? []).ToList();
        return (opnames, reservations, admissions);
    }

    private ExpandedFactBatch QueryExpandedFacts(
        IDbConnection conn,
        IReadOnlyCollection<string> opnIds,
        IReadOnlyCollection<string> rsvIds,
        IReadOnlyCollection<string> regIds)
    {
        var sql = $"""
            {BuildOpnameSelectSql()};
            {BuildReservationSelectSql()};
            {BuildAdmissionSelectSql()};
            {BuildWaitingListSelectSql()};
            {BuildRegExtrasSelectSql()};
            """;

        using var multi = conn.QueryMultiple(
            new CommandDefinition(
                sql,
                BuildIdParams(opnIds, rsvIds, regIds),
                commandTimeout: CommandTimeoutSeconds));

        return new ExpandedFactBatch(
            (multi.Read<OpnameRowDto>() ?? []).ToList(),
            (multi.Read<ReservationRowDto>() ?? []).ToList(),
            (multi.Read<AdmissionRowDto>() ?? []).ToList(),
            (multi.Read<WaitingListRowDto>() ?? []).ToList(),
            (multi.Read<RegExtrasBatchRowDto>() ?? []).ToList());
    }

    private object BuildIdParams(
        IReadOnlyCollection<string> opnIds,
        IReadOnlyCollection<string> rsvIds,
        IReadOnlyCollection<string> regIds) =>
        new
        {
            VodDate = VoidSentinel,
            OpnIds = ToInList(opnIds),
            RsvIds = ToInList(rsvIds),
            RegIds = ToInList(regIds),
            RegIdCsv = string.Join(",", regIds.Where(HasId).Distinct(StringComparer.OrdinalIgnoreCase)),
            HasRegIds = regIds.Any(HasId) ? 1 : 0
        };

    private static string[] ToInList(IReadOnlyCollection<string> ids) =>
        ids.Count == 0 ? ["\0"] : ids.ToArray();

    private static string BuildOpnameSelectSql() =>
        """
        SELECT o.OpnameRequestId, o.OpnameRequestStatus, o.FulfilledRegId, o.PasienId,
               o.DokterId, o.DokterName, o.ClinicalNotes, o.PlannedDate, o.CrtDate, o.UpdDate,
               ISNULL(mr.fs_nm_pasien,'') AS PasienName,
               ISNULL(mr.fs_jns_kelamin,'') AS Gender,
               CASE WHEN ISDATE(mr.fd_tgl_lahir)=1 THEN CAST(mr.fd_tgl_lahir AS DATETIME) ELSE NULL END AS TglLahir
        FROM BILRG_AdmOpnameRequest o
        LEFT JOIN tc_mr mr ON o.PasienId = mr.fs_mr
        WHERE o.VodDate = @VodDate AND o.OpnameRequestId IN @OpnIds
        """;

    private static string BuildReservationSelectSql() =>
        """
        SELECT r.ReservationId, r.ReservationStatus, r.RealizedRegId, r.PasienId,
               ISNULL(r.PasienName,'') AS PasienName, ISNULL(r.Gender,'') AS Gender, r.TglLahir,
               r.KelasId, r.KelasName, r.BangsalId, r.BangsalName, r.PlannedDate, r.CrtDate, r.UpdDate
        FROM BILRG_AdmReservation r
        WHERE r.VodDate = @VodDate AND r.ReservationId IN @RsvIds
        """;

    private static string BuildAdmissionSelectSql() =>
        """
        SELECT a.RegId, a.AdmissionStatus, a.PasienId, a.OpnameRequestId, a.ReservationId,
               a.KelasDkId, a.KelasDkName, a.BangsalId, a.BangsalName, a.AdmissionDate, a.CrtDate, a.UpdDate,
               ISNULL(mr.fs_nm_pasien,'') AS PasienName,
               ISNULL(mr.fs_jns_kelamin,'') AS Gender,
               CASE WHEN ISDATE(mr.fd_tgl_lahir)=1 THEN CAST(mr.fd_tgl_lahir AS DATETIME) ELSE NULL END AS TglLahir
        FROM BILRG_AdmAdmission a
        LEFT JOIN tc_mr mr ON a.PasienId = mr.fs_mr
        WHERE a.VodDate = @VodDate
          AND (
                a.RegId IN @RegIds
                OR a.OpnameRequestId IN @OpnIds
                OR a.ReservationId IN @RsvIds
              )
        """;

    private static string BuildWaitingListSelectSql() =>
        """
        SELECT WaitingListId, RegId, WaitingListStatus, BangsalId, BangsalName,
               KelasId, KelasName, Priority, CrtDate, UpdDate
        FROM BILRG_BedWaitingList
        WHERE VodDate = @VodDate
          AND @HasRegIds = 1
          AND RegId IN @RegIds
        ORDER BY RegId ASC,
                 CASE WHEN UpdDate = @VodDate THEN CrtDate ELSE UpdDate END ASC,
                 WaitingListId ASC
        """;

    private static string BuildRegExtrasSelectSql() =>
        """
        SELECT
            x.RegId,
            ISNULL(reg.fs_kd_tipe_jaminan,'') AS TipeJaminanId,
            ISNULL(tj.fs_nm_tipe_jaminan,'') AS TipeJaminanName,
            ISNULL(reg.fs_kd_kelas,'') AS KelasId,
            ISNULL(kl.fs_nm_kelas,'') AS KelasName,
            ISNULL(COALESCE(NULLIF(LTRIM(RTRIM(hd.fs_kd_dokter)),''), NULLIF(LTRIM(RTRIM(reg.fs_kd_medis)),'')), '') AS DokterId,
            ISNULL(COALESCE(NULLIF(LTRIM(RTRIM(hd.fs_nm_peg)),''), NULLIF(LTRIM(RTRIM(peg.fs_nm_peg)),'')), '') AS DokterName,
            ISNULL(ri.fs_kd_caramasuk_inap,'') AS ProsedurMasukId
        FROM (SELECT DISTINCT LTRIM(RTRIM(value)) AS RegId FROM STRING_SPLIT(@RegIdCsv, ',') WHERE LTRIM(RTRIM(value)) <> '') x
        LEFT JOIN ta_registrasi reg ON reg.fs_kd_reg = x.RegId
        LEFT JOIN ta_tipe_jaminan tj ON reg.fs_kd_tipe_jaminan = tj.fs_kd_tipe_jaminan
        LEFT JOIN ta_kelas kl ON reg.fs_kd_kelas = kl.fs_kd_kelas
        LEFT JOIN ta_reg_inap ri ON ri.fs_kd_reg = x.RegId
        LEFT JOIN (
            SELECT h.fs_kd_reg, h.fs_kd_dokter, ISNULL(p.fs_nm_peg,'') AS fs_nm_peg,
                   ROW_NUMBER() OVER (PARTITION BY h.fs_kd_reg ORDER BY h.fd_tgl_mulai DESC) AS rn
            FROM ta_reg_history_dokter h
            LEFT JOIN td_peg p ON h.fs_kd_dokter = p.fs_kd_peg
            WHERE ISNULL(LTRIM(RTRIM(h.fd_tgl_selesai)),'') = ''
              AND h.fs_kd_reg IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@RegIdCsv, ',') WHERE LTRIM(RTRIM(value)) <> '')
        ) hd ON hd.fs_kd_reg = x.RegId AND hd.rn = 1
        LEFT JOIN td_peg peg ON reg.fs_kd_medis = peg.fs_kd_peg
        WHERE @HasRegIds = 1
        """;

    private JourneyFactBag? FoldOpnameJourney(
        string opnameRequestId,
        string journeyId,
        IReadOnlyDictionary<string, OpnameRowDto> opnames,
        IReadOnlyDictionary<string, AdmissionRowDto> admissionsByOpn,
        IReadOnlyDictionary<string, AdmissionRowDto> admissionsByReg,
        IReadOnlyDictionary<string, ReservationRowDto> reservations,
        IReadOnlyDictionary<string, IReadOnlyList<WaitingListRowDto>> wlByReg,
        IReadOnlyDictionary<string, RegExtras> extrasByReg)
    {
        if (!opnames.TryGetValue(opnameRequestId, out var opn))
            return null;

        AdmissionRowDto? admission = null;
        if (admissionsByOpn.TryGetValue(opnameRequestId, out var bySource))
            admission = bySource;
        else if (HasId(opn.FulfilledRegId) && admissionsByReg.TryGetValue(opn.FulfilledRegId, out var byFulfilled))
            admission = byFulfilled;

        if (admission is not null)
        {
            reservations.TryGetValue(admission.ReservationId ?? "", out var rsv);
            wlByReg.TryGetValue(admission.RegId, out var wl);
            extrasByReg.TryGetValue(admission.RegId, out var extras);
            return AssembleAdmissionBag(
                admission,
                JourneyOriginKind.OpnameRequest,
                opn,
                rsv,
                wl ?? [],
                extras ?? RegExtras.Empty);
        }

        var patient = BuildPatient(opn.PasienId, opn.PasienName, opn.Gender, opn.TglLahir);
        var facts = new JourneyNormalizedFacts(
            JourneyOriginKind.OpnameRequest,
            patient,
            new OpnameRequestJourneyFact(
                opn.OpnameRequestId,
                (OpnameRequestStatusEnum)opn.OpnameRequestStatus,
                NullDash(opn.FulfilledRegId),
                opn.UpdDate == VoidSentinel ? opn.CrtDate : opn.UpdDate),
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

    private JourneyFactBag? FoldReservationJourney(
        string reservationId,
        string journeyId,
        IReadOnlyDictionary<string, ReservationRowDto> reservations,
        IReadOnlyDictionary<string, AdmissionRowDto> admissionsByRsv,
        IReadOnlyDictionary<string, AdmissionRowDto> admissionsByReg,
        IReadOnlyDictionary<string, OpnameRowDto> opnames,
        IReadOnlyDictionary<string, IReadOnlyList<WaitingListRowDto>> wlByReg,
        IReadOnlyDictionary<string, RegExtras> extrasByReg)
    {
        if (!reservations.TryGetValue(reservationId, out var rsv))
            return null;

        AdmissionRowDto? admission = null;
        if (admissionsByRsv.TryGetValue(reservationId, out var bySource))
            admission = bySource;
        else if (HasId(rsv.RealizedRegId) && admissionsByReg.TryGetValue(rsv.RealizedRegId, out var byRealized))
            admission = byRealized;

        if (admission is not null)
        {
            opnames.TryGetValue(admission.OpnameRequestId ?? "", out var opn);
            wlByReg.TryGetValue(admission.RegId, out var wl);
            extrasByReg.TryGetValue(admission.RegId, out var extras);
            return AssembleAdmissionBag(
                admission,
                JourneyOriginKind.Reservation,
                opn,
                rsv,
                wl ?? [],
                extras ?? RegExtras.Empty);
        }

        var patient = BuildPatient(rsv.PasienId, rsv.PasienName, rsv.Gender, rsv.TglLahir);
        var facts = new JourneyNormalizedFacts(
            JourneyOriginKind.Reservation,
            patient,
            null,
            new ReservationJourneyFact(
                rsv.ReservationId,
                (ReservationStatusEnum)rsv.ReservationStatus,
                NullDash(rsv.RealizedRegId),
                rsv.UpdDate == VoidSentinel ? rsv.CrtDate : rsv.UpdDate),
            [],
            []);

        return new JourneyFactBag(
            journeyId,
            facts,
            Named(rsv.KelasId, rsv.KelasName),
            Named(rsv.BangsalId, rsv.BangsalName),
            Priority: null,
            Doctor: null,
            Guarantor: null,
            ClinicalNotes: null,
            ProcedureSummary: null,
            ReferringDoctor: null,
            ReferralOrSourceSummary: "Reservation",
            PolicySummary: null,
            AddressSummary: null);
    }

    private JourneyFactBag? FoldRegJourney(
        string regId,
        IReadOnlyDictionary<string, AdmissionRowDto> admissionsByReg,
        IReadOnlyDictionary<string, OpnameRowDto> opnames,
        IReadOnlyDictionary<string, ReservationRowDto> reservations,
        IReadOnlyDictionary<string, IReadOnlyList<WaitingListRowDto>> wlByReg,
        IReadOnlyDictionary<string, RegExtras> extrasByReg)
    {
        if (!admissionsByReg.TryGetValue(regId, out var admission))
            return null;

        var origin = ResolveOrigin(admission.OpnameRequestId, admission.ReservationId);
        opnames.TryGetValue(admission.OpnameRequestId ?? "", out var opn);
        reservations.TryGetValue(admission.ReservationId ?? "", out var rsv);
        wlByReg.TryGetValue(admission.RegId, out var wl);
        extrasByReg.TryGetValue(admission.RegId, out var extras);
        return AssembleAdmissionBag(
            admission,
            origin,
            opn,
            rsv,
            wl ?? [],
            extras ?? RegExtras.Empty);
    }

    private static RegExtras MapRegExtras(RegExtrasBatchRowDto row) =>
        new(
            Named(row.TipeJaminanId, row.TipeJaminanName),
            Named(row.KelasId, row.KelasName),
            Named(row.DokterId, row.DokterName),
            string.IsNullOrWhiteSpace(row.ProsedurMasukId) ? null : $"Prosedur masuk: {row.ProsedurMasukId}",
            null,
            null);

    private sealed record ExpandedFactBatch(
        IReadOnlyList<OpnameRowDto> Opnames,
        IReadOnlyList<ReservationRowDto> Reservations,
        IReadOnlyList<AdmissionRowDto> Admissions,
        IReadOnlyList<WaitingListRowDto> WaitingLists,
        IReadOnlyList<RegExtrasBatchRowDto> RegExtras);

    private sealed record RegExtrasBatchRowDto(
        string RegId,
        string TipeJaminanId,
        string TipeJaminanName,
        string KelasId,
        string KelasName,
        string DokterId,
        string DokterName,
        string ProsedurMasukId);
}
