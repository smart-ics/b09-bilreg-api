using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.WardCockpitFeature;

public class WardCockpitDal : IWardCockpitDal
{
    private const string OVERVIEW_QUERY = """
        -- 1. Bangsal Header
        SELECT TOP 1
            b.fs_kd_bangsal AS BangsalId,
            b.fs_nm_bangsal AS BangsalName,
            ISNULL(k.fs_kd_kelas, '') AS KelasId,
            ISNULL(k.fs_nm_kelas, '') AS KelasName
        FROM ta_bangsal b
        LEFT JOIN ta_kamar km ON b.fs_kd_bangsal = km.fs_kd_bangsal
        LEFT JOIN ta_kelas k ON km.fs_kd_kelas = k.fs_kd_kelas
        WHERE b.fs_kd_bangsal = @BangsalId;

        -- 2. Rooms, Beds and Occupancy
        SELECT 
            km.fs_kd_kamar AS RoomId,
            km.fs_nm_kamar AS RoomName,
            ISNULL(kls_kamar.fs_nm_kelas, '') AS KelasKamar,
            bd.fs_kd_bed AS BedId,
            bd.fs_nm_bed AS BedCode,
            bd.fb_aktif AS IsBedActive,
            -- Occupancy (from active ta_trs_bed)
            occ.fs_kd_trs AS PakaiBedId,
            occ.fs_kd_reg AS RegId,
            ISNULL(reg.fs_mr, '') AS MrNo,
            ISNULL(pmr.fs_nm_pasien, '') AS NamaPasien,
            ISNULL(pmr.fs_jns_kelamin, '') AS Gender,
            pmr.fd_tgl_lahir AS TglLahir,
            occ.fd_tgl_in AS TglIn,
            occ.fs_jam_in AS JamIn,
            ISNULL(peg.fs_nm_peg, '') AS DpjpName,
            ISNULL(tj.fs_nm_tipe_jaminan, '') AS PenjaminName,
            ISNULL(kls_reg.fs_nm_kelas, '') AS BillingKelasName,
            ISNULL(occ.fs_kd_kelas, '') AS TransaksiKelasId,
            ISNULL(reg.fs_kd_kelas, '') AS RegKelasId,
            ISNULL(alok.PakaiBedPurpose, 0) AS PakaiBedPurpose
        FROM ta_kamar km
        INNER JOIN ta_bed bd ON km.fs_kd_kamar = bd.fs_kd_kamar
        LEFT JOIN ta_kelas kls_kamar ON km.fs_kd_kelas = kls_kamar.fs_kd_kelas
        LEFT JOIN ta_trs_bed occ ON bd.fs_kd_bed = occ.fs_kd_bed
            AND (occ.fd_tgl_out = '' OR occ.fd_tgl_out = '3000-01-01' OR occ.fd_tgl_out IS NULL)
            AND (occ.fd_tgl_void = '' OR occ.fd_tgl_void = '3000-01-01' OR occ.fd_tgl_void IS NULL)
        LEFT JOIN ta_registrasi reg ON occ.fs_kd_reg = reg.fs_kd_reg
        LEFT JOIN tc_mr pmr ON reg.fs_mr = pmr.fs_mr
        LEFT JOIN td_peg peg ON reg.fs_kd_medis = peg.fs_kd_peg
        LEFT JOIN ta_tipe_jaminan tj ON reg.fs_kd_tipe_jaminan = tj.fs_kd_tipe_jaminan
        LEFT JOIN ta_kelas kls_reg ON reg.fs_kd_kelas = kls_reg.fs_kd_kelas
        LEFT JOIN BILRG_RnaPakaiBedAlokasi alok ON occ.fs_kd_trs = alok.PakaiBedId
        WHERE km.fs_kd_bangsal = @BangsalId
        ORDER BY km.fs_kd_kamar, bd.fs_kd_bed;

        -- 3. Bed Operational State
        SELECT 
            bo.BedId,
            bo.CurrentReadiness,
            bo.IsBlocked,
            bo.CurrentRestrictionType,
            bo.CurrentBlockerReason,
            bo.UpdDate
        FROM BILRG_RnaBedOperational bo
        WHERE bo.BangsalId = @BangsalId;

        -- 4. Waiting List
        SELECT 
            wl.WaitingListId,
            wl.RegId,
            wl.PasienId AS MrNo,
            ISNULL(pmr.fs_nm_pasien, wl.PasienId) AS NamaPasien,
            ISNULL(pmr.fs_jns_kelamin, '') AS Gender,
            pmr.fd_tgl_lahir AS TglLahir,
            wl.ClinicalUrgency,
            wl.ArrivalSourceType,
            ISNULL(wl.ReferringUnitName, '') AS ReferringUnitName,
            wl.KelasName AS RequestedKelasName,
            wl.PlannedBedId,
            wl.SpecialRequirements,
            wl.CrtDate
        FROM BILRG_BedWaitingList wl
        LEFT JOIN tc_mr pmr ON wl.PasienId = pmr.fs_mr
        WHERE wl.BangsalId = @BangsalId 
          AND wl.WaitingListStatus IN (0, 1)
        ORDER BY wl.ClinicalUrgency DESC, wl.CrtDate ASC;
        """;

    private const string BED_DETAIL_QUERY = """
        -- 1. Bed Info & Current Occupancy
        SELECT 
            bd.fs_kd_bed AS BedId,
            bd.fs_nm_bed AS BedCode,
            km.fs_nm_kamar AS RoomName,
            ISNULL(kls.fs_nm_kelas, '') AS KelasKamar,
            -- Current Occupancy
            occ.fs_kd_trs AS PakaiBedId,
            occ.fs_kd_reg AS RegId,
            ISNULL(reg.fs_mr, '') AS MrNo,
            ISNULL(pmr.fs_nm_pasien, '') AS NamaPasien,
            ISNULL(pmr.fs_jns_kelamin, '') AS Gender,
            pmr.fd_tgl_lahir AS TglLahir,
            occ.fd_tgl_in AS TglIn,
            occ.fs_jam_in AS JamIn,
            ISNULL(occ.fn_tarif, 0) AS TarifPerHari,
            ISNULL(reg.fs_kd_tipe_jaminan, '') AS TipeJaminanId,
            ISNULL(tj.fs_nm_tipe_jaminan, '') AS PenjaminName,
            ISNULL(reg.fs_kd_medis, '') AS DokterId,
            ISNULL(peg.fs_nm_peg, '') AS DpjpName,
            ISNULL(kls_reg.fs_nm_kelas, '') AS BillingKelasName
        FROM ta_bed bd
        INNER JOIN ta_kamar km ON bd.fs_kd_kamar = km.fs_kd_kamar
        LEFT JOIN ta_kelas kls ON km.fs_kd_kelas = kls.fs_kd_kelas
        LEFT JOIN ta_trs_bed occ ON bd.fs_kd_bed = occ.fs_kd_bed
            AND (occ.fd_tgl_out = '' OR occ.fd_tgl_out = '3000-01-01' OR occ.fd_tgl_out IS NULL)
            AND (occ.fd_tgl_void = '' OR occ.fd_tgl_void = '3000-01-01' OR occ.fd_tgl_void IS NULL)
        LEFT JOIN ta_registrasi reg ON occ.fs_kd_reg = reg.fs_kd_reg
        LEFT JOIN tc_mr pmr ON reg.fs_mr = pmr.fs_mr
        LEFT JOIN td_peg peg ON reg.fs_kd_medis = peg.fs_kd_peg
        LEFT JOIN ta_tipe_jaminan tj ON reg.fs_kd_tipe_jaminan = tj.fs_kd_tipe_jaminan
        LEFT JOIN ta_kelas kls_reg ON reg.fs_kd_kelas = kls_reg.fs_kd_kelas
        WHERE bd.fs_kd_bed = @BedId;

        -- 2. Patient Card No (BPJS / ASKES / etc)
        SELECT TOP 1 
            NoID 
        FROM tc_mr_id 
        WHERE fs_mr = (
            SELECT TOP 1 reg.fs_mr
            FROM ta_bed bd
            INNER JOIN ta_trs_bed occ ON bd.fs_kd_bed = occ.fs_kd_bed
                AND (occ.fd_tgl_out = '' OR occ.fd_tgl_out = '3000-01-01' OR occ.fd_tgl_out IS NULL)
                AND (occ.fd_tgl_void = '' OR occ.fd_tgl_void = '3000-01-01' OR occ.fd_tgl_void IS NULL)
            INNER JOIN ta_registrasi reg ON occ.fs_kd_reg = reg.fs_kd_reg
            WHERE bd.fs_kd_bed = @BedId
        )
        ORDER BY CASE WHEN JenisID = 'BPJS' THEN 1 WHEN JenisID = 'ASKES' THEN 2 ELSE 3 END;

        -- 3. Tab Tindakan
        SELECT 
            t.TindakanDate AS TglJam,
            t.TarifName AS ItemTindakan,
            t.CrtUser AS Petugas,
            t.Total AS Biaya
        FROM BILRG_Tindakan t
        WHERE t.PakaiBedId = (
            SELECT TOP 1 occ.fs_kd_trs
            FROM ta_bed bd
            INNER JOIN ta_trs_bed occ ON bd.fs_kd_bed = occ.fs_kd_bed
                AND (occ.fd_tgl_out = '' OR occ.fd_tgl_out = '3000-01-01' OR occ.fd_tgl_out IS NULL)
                AND (occ.fd_tgl_void = '' OR occ.fd_tgl_void = '3000-01-01' OR occ.fd_tgl_void IS NULL)
            WHERE bd.fs_kd_bed = @BedId
        )
        AND t.VodDate = '3000-01-01'
        ORDER BY t.TindakanDate DESC;

        -- 4. Tab Riwayat Mutasi Bed Pasien Terkait
        SELECT 
            b.fd_tgl_in AS TglIn,
            b.fs_jam_in AS JamIn,
            b.fd_tgl_out AS TglOut,
            b.fs_jam_out AS JamOut,
            ISNULL(p_in.fs_nm_peg, b.fs_kd_petugas) AS PetugasIn,
            ISNULL(p_out.fs_nm_peg, b.fs_kd_petugas_out) AS PetugasOut,
            km.fs_nm_kamar AS RoomName,
            bd.fs_nm_bed AS BedCode
        FROM ta_trs_bed b
        INNER JOIN ta_bed bd ON b.fs_kd_bed = bd.fs_kd_bed
        INNER JOIN ta_kamar km ON bd.fs_kd_kamar = km.fs_kd_kamar
        LEFT JOIN td_peg p_in ON b.fs_kd_petugas = p_in.fs_kd_peg
        LEFT JOIN td_peg p_out ON b.fs_kd_petugas_out = p_out.fs_kd_peg
        WHERE b.fs_kd_reg = (
            SELECT TOP 1 occ.fs_kd_reg
            FROM ta_bed bd
            INNER JOIN ta_trs_bed occ ON bd.fs_kd_bed = occ.fs_kd_bed
                AND (occ.fd_tgl_out = '' OR occ.fd_tgl_out = '3000-01-01' OR occ.fd_tgl_out IS NULL)
                AND (occ.fd_tgl_void = '' OR occ.fd_tgl_void = '3000-01-01' OR occ.fd_tgl_void IS NULL)
            WHERE bd.fs_kd_bed = @BedId
        )
        AND (b.fd_tgl_void = '' OR b.fd_tgl_void = '3000-01-01' OR b.fd_tgl_void IS NULL)
        ORDER BY b.fd_tgl_in ASC, b.fs_jam_in ASC;
        """;

    private readonly DatabaseOptions _opt;

    public WardCockpitDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public async Task<WardCockpitOverviewDto?> GetOverviewAsync(string bangsalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bangsalId))
            return null;

        var dp = new DynamicParameters();
        dp.AddParam("@BangsalId", bangsalId.Trim(), SqlDbType.VarChar);

        await using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        await conn.OpenAsync(cancellationToken);

        var cmd = new CommandDefinition(
            OVERVIEW_QUERY,
            dp,
            cancellationToken: cancellationToken);

        await using var multi = await conn.QueryMultipleAsync(cmd);

        var bangsalRow = await multi.ReadFirstOrDefaultAsync<RawBangsalRow>();
        if (bangsalRow == null)
            return null;

        var roomBedRows = (await multi.ReadAsync<RawRoomBedRow>()).ToList();
        var operationalRows = (await multi.ReadAsync<RawBedOperationalRow>()).ToList();
        var waitingListRows = (await multi.ReadAsync<RawWaitingListRow>()).ToList();

        return BuildOverview(bangsalRow, roomBedRows, operationalRows, waitingListRows, DateTime.Now);
    }

    public WardCockpitOverviewDto? GetOverview(string bangsalId)
    {
        return GetOverviewAsync(bangsalId).GetAwaiter().GetResult();
    }

    public async Task<WardCockpitBedDetailDto?> GetBedDetailAsync(string bedId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bedId))
            return null;

        var dp = new DynamicParameters();
        dp.AddParam("@BedId", bedId.Trim(), SqlDbType.VarChar);

        await using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        await conn.OpenAsync(cancellationToken);

        var cmd = new CommandDefinition(
            BED_DETAIL_QUERY,
            dp,
            cancellationToken: cancellationToken);

        await using var multi = await conn.QueryMultipleAsync(cmd);

        var bedRow = await multi.ReadFirstOrDefaultAsync<RawBedDetailRow>();
        if (bedRow == null)
            return null;

        var noKartu = await multi.ReadFirstOrDefaultAsync<string>();
        var tindakanRows = (await multi.ReadAsync<RawTindakanRow>()).ToList();
        var mutasiRows = (await multi.ReadAsync<RawMutasiRow>()).ToList();

        return BuildBedDetail(bedRow, noKartu, tindakanRows, mutasiRows, DateTime.Now);
    }

    public WardCockpitBedDetailDto? GetBedDetail(string bedId)
    {
        return GetBedDetailAsync(bedId).GetAwaiter().GetResult();
    }

    internal static WardCockpitOverviewDto BuildOverview(
        RawBangsalRow bangsalRow,
        IEnumerable<RawRoomBedRow> roomBedRows,
        IEnumerable<RawBedOperationalRow> operationalRows,
        IEnumerable<RawWaitingListRow> waitingListRows,
        DateTime? currentTime = null)
    {
        var now = currentTime ?? DateTime.Now;
        var opMap = operationalRows.ToDictionary(x => x.BedId, x => x);

        var bedList = new List<WardCockpitBedDto>();
        var roomGroups = roomBedRows.GroupBy(x => x.RoomId);
        var roomDtos = new List<WardCockpitRoomDto>();

        foreach (var rg in roomGroups)
        {
            var first = rg.First();
            var currentRoomBeds = new List<WardCockpitBedDto>();

            foreach (var b in rg)
            {
                opMap.TryGetValue(b.BedId, out var opRow);
                var opState = MapOperationalState(opRow);
                var isOccupied = !string.IsNullOrWhiteSpace(b.PakaiBedId);
                var status = ResolveBedStatus(isOccupied, opState);

                WardCockpitOccupancyDto? currentOccupancy = null;
                if (isOccupied)
                {
                    var tglJamMasuk = ParseDateTime(b.TglIn, b.JamIn, now);
                    var losDays = Math.Max(0, (now.Date - tglJamMasuk.Date).Days);
                    var isTitipKelas = !string.IsNullOrEmpty(b.TransaksiKelasId) &&
                                       !string.IsNullOrEmpty(b.RegKelasId) &&
                                       b.TransaksiKelasId != b.RegKelasId;
                    var isRawatGabung = b.PakaiBedPurpose == 4;

                    currentOccupancy = new WardCockpitOccupancyDto(
                        PakaiBedId: b.PakaiBedId!,
                        RegId: b.RegId ?? string.Empty,
                        MrNo: b.MrNo ?? string.Empty,
                        NamaPasien: b.NamaPasien ?? string.Empty,
                        Gender: b.Gender ?? string.Empty,
                        Umur: CalculateAge(b.TglLahir, now),
                        TglJamMasuk: tglJamMasuk,
                        LosDays: losDays,
                        DpjpName: b.DpjpName ?? string.Empty,
                        PenjaminName: b.PenjaminName ?? string.Empty,
                        BillingKelasName: string.IsNullOrWhiteSpace(b.BillingKelasName) ? first.KelasKamar : b.BillingKelasName,
                        IsTitipKelas: isTitipKelas,
                        IsRawatGabung: isRawatGabung
                    );
                }

                var bedDto = new WardCockpitBedDto(
                    BedId: b.BedId,
                    BedCode: b.BedCode,
                    Status: status,
                    CurrentOccupancy: currentOccupancy,
                    OperationalState: opState
                );

                currentRoomBeds.Add(bedDto);
                bedList.Add(bedDto);
            }

            var genderRestriction = ResolveGenderRestriction(currentRoomBeds);

            roomDtos.Add(new WardCockpitRoomDto(
                RoomId: first.RoomId,
                RoomName: first.RoomName,
                KelasKamar: first.KelasKamar,
                GenderRestriction: genderRestriction,
                Beds: currentRoomBeds
            ));
        }

        var waitingListDtos = waitingListRows.Select(wl => new WardCockpitWaitingListDto(
            WaitingListId: wl.WaitingListId,
            RegId: wl.RegId,
            MrNo: wl.MrNo,
            NamaPasien: wl.NamaPasien,
            Gender: wl.Gender,
            Umur: CalculateAge(wl.TglLahir, now),
            ClinicalUrgency: MapUrgency(wl.ClinicalUrgency),
            ReferralSource: MapReferralSource(wl.ArrivalSourceType, wl.ReferringUnitName),
            RequestedKelasName: wl.RequestedKelasName,
            PlannedBedId: wl.PlannedBedId,
            ElapsedWaitMinutes: Math.Max(0, (int)(now - wl.CrtDate).TotalMinutes),
            SpecialRequirements: wl.SpecialRequirements
        )).ToList();

        var totalBed = bedList.Count;
        var occupied = bedList.Count(x => x.Status == "Occupied");
        var available = bedList.Count(x => x.Status == "Available");
        var cleaning = bedList.Count(x => x.Status == "Cleaning");
        var maintenance = bedList.Count(x => x.Status == "Maintenance");
        var borPercentage = totalBed > 0 ? Math.Round((double)occupied / totalBed * 100, 1) : 0.0;

        var kpi = new WardCockpitKpiDto(
            TotalBed: totalBed,
            Occupied: occupied,
            BorPercentage: borPercentage,
            Available: available,
            Cleaning: cleaning,
            Maintenance: maintenance,
            WaitingListCount: waitingListDtos.Count
        );

        var bangsalDto = new WardCockpitBangsalDto(
            BangsalId: bangsalRow.BangsalId,
            BangsalName: bangsalRow.BangsalName,
            KelasId: bangsalRow.KelasId,
            KelasName: bangsalRow.KelasName
        );

        return new WardCockpitOverviewDto(
            Bangsal: bangsalDto,
            Kpi: kpi,
            Rooms: roomDtos,
            WaitingList: waitingListDtos
        );
    }

    private static WardCockpitOperationalStateDto MapOperationalState(RawBedOperationalRow? op)
    {
        if (op == null)
        {
            return new WardCockpitOperationalStateDto(
                ReadinessStatus: "Ready",
                BlockerType: "None",
                DirtySince: null,
                MaintenanceNote: null);
        }

        var readiness = op.CurrentReadiness switch
        {
            1 => "Ready",
            2 => "CleaningRequired",
            3 => "CleaningInProgress",
            4 => "Blocked",
            5 => "OutOfService",
            _ => "Ready"
        };

        var blocker = op.CurrentRestrictionType switch
        {
            1 => "Cleaning",
            2 => "Inspection",
            3 => "Maintenance",
            4 => "Safety",
            5 => "Operational",
            _ => "None"
        };

        DateTime? dirtySince = (readiness is "CleaningRequired" or "CleaningInProgress" || blocker is "Cleaning")
            ? op.UpdDate
            : null;

        string? note = (op.IsBlocked || op.CurrentRestrictionType > 1) && !string.IsNullOrWhiteSpace(op.CurrentBlockerReason)
            ? op.CurrentBlockerReason
            : null;

        return new WardCockpitOperationalStateDto(readiness, blocker, dirtySince, note);
    }

    private static string ResolveBedStatus(bool isOccupied, WardCockpitOperationalStateDto opState)
    {
        if (isOccupied)
            return "Occupied";

        if (opState.ReadinessStatus is "CleaningRequired" or "CleaningInProgress" || opState.BlockerType is "Cleaning")
            return "Cleaning";

        if (opState.ReadinessStatus is "Blocked" or "OutOfService" ||
            opState.BlockerType is "Maintenance" or "Inspection" or "Safety" or "Operational")
            return "Maintenance";

        return "Available";
    }

    private static string ResolveGenderRestriction(IEnumerable<WardCockpitBedDto> beds)
    {
        var occupants = beds
            .Where(b => b.Status == "Occupied" && b.CurrentOccupancy != null)
            .Select(b => b.CurrentOccupancy!.Gender.Trim().ToUpperInvariant())
            .Where(g => !string.IsNullOrEmpty(g))
            .ToList();

        if (occupants.Count == 0)
            return "Unrestricted";

        if (occupants.All(g => g is "L" or "MALE" or "LAKI-LAKI"))
            return "Male";

        if (occupants.All(g => g is "P" or "FEMALE" or "PEREMPUAN"))
            return "Female";

        return "Unrestricted";
    }

    private static string CalculateAge(DateTime? birthDate, DateTime now)
    {
        if (birthDate == null || birthDate.Value.Year >= 3000 || birthDate.Value.Year < 1900)
            return "-";

        var years = now.Year - birthDate.Value.Year;
        if (birthDate.Value.Date > now.AddYears(-years).Date)
            years--;

        return $"{Math.Max(0, years)} Thn";
    }

    private static DateTime ParseDateTime(string? tgl, string? jam, DateTime defaultTime)
    {
        if (string.IsNullOrWhiteSpace(tgl) || tgl == "3000-01-01")
            return defaultTime;

        var datePart = tgl.Trim();
        var timePart = string.IsNullOrWhiteSpace(jam) ? "00:00:00" : jam.Trim();
        if (DateTime.TryParse($"{datePart} {timePart}", out var dt))
            return dt;

        if (DateTime.TryParse(datePart, out var d))
            return d;

        return defaultTime;
    }

    private static string MapUrgency(int urgency) => urgency switch
    {
        2 => "Cito",
        1 => "Urgent",
        _ => "Elective"
    };

    private static string MapReferralSource(int sourceType, string? unitName)
    {
        if (!string.IsNullOrWhiteSpace(unitName))
            return unitName;

        return sourceType switch
        {
            0 => "IGD",
            1 => "Poliklinik",
            2 => "Admisi",
            3 => "Transfer",
            _ => "IGD"
        };
    }

    internal static WardCockpitBedDetailDto BuildBedDetail(
        RawBedDetailRow bedRow,
        string? noKartu,
        IEnumerable<RawTindakanRow> tindakanRows,
        IEnumerable<RawMutasiRow> mutasiRows,
        DateTime? currentTime = null)
    {
        var now = currentTime ?? DateTime.Now;

        var bedInfo = new WardCockpitBedDetailBedInfoDto(
            BedId: bedRow.BedId,
            BedCode: bedRow.BedCode,
            RoomName: bedRow.RoomName,
            KelasKamar: bedRow.KelasKamar
        );

        var isOccupied = !string.IsNullOrWhiteSpace(bedRow.PakaiBedId);
        if (!isOccupied)
        {
            return new WardCockpitBedDetailDto(
                BedInfo: bedInfo,
                Occupancy: null,
                TabTindakan: [],
                TabRoomCharge: null,
                TabRiwayatMutasi: []
            );
        }

        var tglJamMasuk = ParseDateTime(bedRow.TglIn, bedRow.JamIn, now);
        var losDays = Math.Max(0, (now.Date - tglJamMasuk.Date).Days);
        var genderStr = bedRow.Gender?.Trim().ToUpperInvariant() switch
        {
            "L" or "MALE" or "LAKI-LAKI" => "Laki-laki",
            "P" or "FEMALE" or "PEREMPUAN" => "Perempuan",
            _ => bedRow.Gender ?? string.Empty
        };

        var patient = new WardCockpitBedDetailPatientDto(
            MrNo: bedRow.MrNo ?? string.Empty,
            NamaPasien: bedRow.NamaPasien ?? string.Empty,
            Gender: genderStr,
            TglLahir: bedRow.TglLahir.HasValue && bedRow.TglLahir.Value.Year < 3000 ? bedRow.TglLahir.Value.ToString("yyyy-MM-dd") : string.Empty,
            Umur: CalculateAgeFull(bedRow.TglLahir, now)
        );

        var penjamin = new WardCockpitBedDetailPenjaminDto(
            TipeJaminanId: bedRow.TipeJaminanId ?? string.Empty,
            TipeJaminanName: bedRow.PenjaminName ?? string.Empty,
            NoKartu: noKartu ?? string.Empty
        );

        var dpjp = new WardCockpitBedDetailDpjpDto(
            DokterId: bedRow.DokterId ?? string.Empty,
            DokterName: bedRow.DpjpName ?? string.Empty
        );

        var occupancy = new WardCockpitBedDetailOccupancyDto(
            PakaiBedId: bedRow.PakaiBedId!,
            RegId: bedRow.RegId ?? string.Empty,
            Patient: patient,
            Penjamin: penjamin,
            Dpjp: dpjp,
            CheckInTime: tglJamMasuk,
            LosDays: losDays,
            BillingKelas: bedRow.BillingKelasName ?? string.Empty
        );

        var tabTindakan = (tindakanRows ?? [])
            .Select(t => new WardCockpitBedDetailTindakanDto(
                TglJam: t.TglJam,
                ItemTindakan: t.ItemTindakan,
                Petugas: t.Petugas,
                Biaya: t.Biaya
            ))
            .ToList();

        var akumulasiHari = Math.Max(1, losDays);
        var tarifPerHari = bedRow.TarifPerHari;
        var estimasiTotalSewa = tarifPerHari * akumulasiHari;

        var tabRoomCharge = new WardCockpitBedDetailRoomChargeDto(
            TarifPerHari: tarifPerHari,
            AkumulasiHari: akumulasiHari,
            EstimasiTotalSewa: estimasiTotalSewa,
            CutOffTime: "12:00"
        );

        var mutasiList = new List<WardCockpitBedDetailRiwayatMutasiDto>();
        foreach (var m in mutasiRows ?? [])
        {
            var tglJamIn = ParseDateTime(m.TglIn, m.JamIn, now);
            mutasiList.Add(new WardCockpitBedDetailRiwayatMutasiDto(
                TglJam: tglJamIn,
                Kegiatan: $"Check-in {m.RoomName} ({m.BedCode})",
                Petugas: m.PetugasIn
            ));

            if (!string.IsNullOrWhiteSpace(m.TglOut) && m.TglOut != "3000-01-01")
            {
                var tglJamOut = ParseDateTime(m.TglOut, m.JamOut, now);
                mutasiList.Add(new WardCockpitBedDetailRiwayatMutasiDto(
                    TglJam: tglJamOut,
                    Kegiatan: $"Pindah/Keluar dari {m.RoomName} ({m.BedCode})",
                    Petugas: m.PetugasOut ?? m.PetugasIn
                ));
            }
        }

        return new WardCockpitBedDetailDto(
            BedInfo: bedInfo,
            Occupancy: occupancy,
            TabTindakan: tabTindakan,
            TabRoomCharge: tabRoomCharge,
            TabRiwayatMutasi: mutasiList
        );
    }

    private static string CalculateAgeFull(DateTime? birthDate, DateTime now)
    {
        if (birthDate == null || birthDate.Value.Year >= 3000 || birthDate.Value.Year < 1900)
            return "-";

        var years = now.Year - birthDate.Value.Year;
        if (birthDate.Value.Date > now.AddYears(-years).Date)
            years--;

        return $"{Math.Max(0, years)} Tahun";
    }
}
