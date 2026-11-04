using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

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
            COALESCE(
                NULLIF(NULLIF(kls_reg.fs_nm_kelas, 'NON KELAS'), ''),
                NULLIF(wl.KelasName, ''),
                kls_kamar.fs_nm_kelas,
                ''
            ) AS BillingKelasName,
            ISNULL(occ.fs_kd_kelas, '') AS TransaksiKelasId,
            COALESCE(
                NULLIF(NULLIF(reg.fs_kd_kelas, '000'), '-'),
                NULLIF(wl.KelasId, ''),
                occ.fs_kd_kelas,
                ''
            ) AS RegKelasId,
            CASE WHEN baby.BabyRegId IS NOT NULL THEN 4 ELSE 0 END AS PakaiBedPurpose,
            baby.BabyName,
            baby.BabyRegId,
            baby.BabyMrNo,
            reg.fs_kd_jenis_inap AS KdJenisInap
        FROM ta_kamar km
        INNER JOIN ta_bed bd ON km.fs_kd_kamar = bd.fs_kd_kamar
        LEFT JOIN ta_kelas kls_kamar ON km.fs_kd_kelas = kls_kamar.fs_kd_kelas
        LEFT JOIN (
            SELECT 
                tb.fs_kd_bed,
                tb.fs_kd_trs,
                tb.fs_kd_reg,
                tb.fd_tgl_in,
                tb.fs_jam_in,
                tb.fn_tarif,
                tb.fs_kd_kelas,
                ROW_NUMBER() OVER (
                    PARTITION BY tb.fs_kd_bed 
                    ORDER BY 
                        CASE WHEN tb.fn_tarif > 0 THEN 1 ELSE 2 END,
                        tb.fd_tgl_in ASC,
                        tb.fs_jam_in ASC
                ) AS rn
            FROM ta_trs_bed tb
            WHERE (tb.fd_tgl_out = '' OR tb.fd_tgl_out = '3000-01-01' OR tb.fd_tgl_out IS NULL)
              AND (tb.fd_tgl_void = '' OR tb.fd_tgl_void = '3000-01-01' OR tb.fd_tgl_void IS NULL)
        ) occ ON bd.fs_kd_bed = occ.fs_kd_bed AND occ.rn = 1
        LEFT JOIN ta_registrasi reg ON occ.fs_kd_reg = reg.fs_kd_reg
        LEFT JOIN tc_mr pmr ON reg.fs_mr = pmr.fs_mr
        LEFT JOIN td_peg peg ON reg.fs_kd_medis = peg.fs_kd_peg
        LEFT JOIN ta_tipe_jaminan tj ON reg.fs_kd_tipe_jaminan = tj.fs_kd_tipe_jaminan
        LEFT JOIN ta_kelas kls_reg ON reg.fs_kd_kelas = kls_reg.fs_kd_kelas
        LEFT JOIN (
            SELECT RegId, KelasId, KelasName,
                   ROW_NUMBER() OVER(PARTITION BY RegId ORDER BY CrtDate DESC) as rn
            FROM BILRG_BedWaitingList
        ) wl ON occ.fs_kd_reg = wl.RegId AND wl.rn = 1
        LEFT JOIN (
            SELECT 
                tb_baby.fs_kd_bed,
                tb_baby.fs_kd_reg AS BabyRegId,
                COALESCE(NULLIF(r_baby.fs_mr, ''), tb_baby.fs_kd_reg) AS BabyMrNo,
                COALESCE(
                    NULLIF(pmr_baby.fs_nm_pasien, ''),
                    'By. Ny. ' + ISNULL(pmr_mother.fs_nm_pasien, '')
                ) AS BabyName,
                ROW_NUMBER() OVER (
                    PARTITION BY tb_baby.fs_kd_bed 
                    ORDER BY tb_baby.fd_tgl_in DESC, tb_baby.fs_jam_in DESC
                ) AS rn
            FROM ta_trs_bed tb_baby
            LEFT JOIN ta_registrasi r_baby ON tb_baby.fs_kd_reg = r_baby.fs_kd_reg
            LEFT JOIN tc_mr pmr_baby ON r_baby.fs_mr = pmr_baby.fs_mr
            LEFT JOIN ta_trs_bed tb_mother ON tb_baby.fs_kd_bed = tb_mother.fs_kd_bed AND tb_mother.fn_tarif > 0
                AND (tb_mother.fd_tgl_out = '' OR tb_mother.fd_tgl_out = '3000-01-01' OR tb_mother.fd_tgl_out IS NULL)
                AND (tb_mother.fd_tgl_void = '' OR tb_mother.fd_tgl_void = '3000-01-01' OR tb_mother.fd_tgl_void IS NULL)
            LEFT JOIN ta_registrasi r_mother ON tb_mother.fs_kd_reg = r_mother.fs_kd_reg
            LEFT JOIN tc_mr pmr_mother ON r_mother.fs_mr = pmr_mother.fs_mr
            WHERE (tb_baby.fd_tgl_out = '' OR tb_baby.fd_tgl_out = '3000-01-01' OR tb_baby.fd_tgl_out IS NULL)
              AND (tb_baby.fd_tgl_void = '' OR tb_baby.fd_tgl_void = '3000-01-01' OR tb_baby.fd_tgl_void IS NULL)
              AND tb_baby.fn_tarif = 0
        ) baby ON bd.fs_kd_bed = baby.fs_kd_bed AND baby.rn = 1
        WHERE km.fs_kd_bangsal = @BangsalId
            AND bd.fb_aktif = 1
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
        INNER JOIN ta_bed bd ON bo.BedId = bd.fs_kd_bed
        INNER JOIN ta_kamar km ON bd.fs_kd_kamar = km.fs_kd_kamar
        WHERE km.fs_kd_bangsal = @BangsalId OR bo.BangsalId = @BangsalId;

        -- 4. Waiting List
        SELECT 
            wl.WaitingListId,
            wl.RegId,
            wl.PasienId AS MrNo,
            ISNULL(pmr.fs_nm_pasien, wl.PasienId) AS NamaPasien,
            ISNULL(pmr.fs_jns_kelamin, '') AS Gender,
            pmr.fd_tgl_lahir AS TglLahir,
            ISNULL(wl.UrgencyLevel, 0) AS ClinicalUrgency,
            ISNULL(wl.EntrySource, 0) AS ArrivalSourceType,
            ISNULL(wl.UnitPengirimName, '') AS ReferringUnitName,
            ISNULL(wl.KelasName, '') AS RequestedKelasName,
            ISNULL(wl.AdvisoryBedId, '') AS PlannedBedId,
            ISNULL(wl.SpecialNeeds, '') AS SpecialRequirements,
            wl.CrtDate
        FROM BILRG_BedWaitingList wl
        LEFT JOIN tc_mr pmr ON wl.PasienId = pmr.fs_mr
        WHERE wl.BangsalId = @BangsalId 
          AND wl.WaitingListStatus IN (0, 1)
        ORDER BY ISNULL(wl.UrgencyLevel, 0) DESC, wl.CrtDate ASC;
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
            COALESCE(
                NULLIF(NULLIF(kls_reg.fs_nm_kelas, 'NON KELAS'), ''),
                NULLIF(wl.KelasName, ''),
                kls.fs_nm_kelas,
                ''
            ) AS BillingKelasName,
            baby.BabyName,
            baby.BabyRegId,
            baby.BabyMrNo,
            reg.fs_kd_jenis_inap AS KdJenisInap
        FROM ta_bed bd
        INNER JOIN ta_kamar km ON bd.fs_kd_kamar = km.fs_kd_kamar
        LEFT JOIN ta_kelas kls ON km.fs_kd_kelas = kls.fs_kd_kelas
        LEFT JOIN (
            SELECT 
                tb.fs_kd_bed,
                tb.fs_kd_trs,
                tb.fs_kd_reg,
                tb.fd_tgl_in,
                tb.fs_jam_in,
                tb.fn_tarif,
                tb.fs_kd_kelas,
                ROW_NUMBER() OVER (
                    PARTITION BY tb.fs_kd_bed 
                    ORDER BY 
                        CASE WHEN tb.fn_tarif > 0 THEN 1 ELSE 2 END,
                        tb.fd_tgl_in ASC,
                        tb.fs_jam_in ASC
                ) AS rn
            FROM ta_trs_bed tb
            WHERE (tb.fd_tgl_out = '' OR tb.fd_tgl_out = '3000-01-01' OR tb.fd_tgl_out IS NULL)
              AND (tb.fd_tgl_void = '' OR tb.fd_tgl_void = '3000-01-01' OR tb.fd_tgl_void IS NULL)
        ) occ ON bd.fs_kd_bed = occ.fs_kd_bed AND occ.rn = 1
        LEFT JOIN ta_registrasi reg ON occ.fs_kd_reg = reg.fs_kd_reg
        LEFT JOIN tc_mr pmr ON reg.fs_mr = pmr.fs_mr
        LEFT JOIN td_peg peg ON reg.fs_kd_medis = peg.fs_kd_peg
        LEFT JOIN ta_tipe_jaminan tj ON reg.fs_kd_tipe_jaminan = tj.fs_kd_tipe_jaminan
        LEFT JOIN ta_kelas kls_reg ON reg.fs_kd_kelas = kls_reg.fs_kd_kelas
        LEFT JOIN (
            SELECT RegId, KelasId, KelasName,
                   ROW_NUMBER() OVER(PARTITION BY RegId ORDER BY CrtDate DESC) as rn
            FROM BILRG_BedWaitingList
        ) wl ON occ.fs_kd_reg = wl.RegId AND wl.rn = 1
        LEFT JOIN (
            SELECT 
                tb_baby.fs_kd_bed,
                tb_baby.fs_kd_reg AS BabyRegId,
                COALESCE(NULLIF(r_baby.fs_mr, ''), tb_baby.fs_kd_reg) AS BabyMrNo,
                COALESCE(
                    NULLIF(pmr_baby.fs_nm_pasien, ''),
                    'By. Ny. ' + ISNULL(pmr_mother.fs_nm_pasien, '')
                ) AS BabyName,
                ROW_NUMBER() OVER (
                    PARTITION BY tb_baby.fs_kd_bed 
                    ORDER BY tb_baby.fd_tgl_in DESC, tb_baby.fs_jam_in DESC
                ) AS rn
            FROM ta_trs_bed tb_baby
            LEFT JOIN ta_registrasi r_baby ON tb_baby.fs_kd_reg = r_baby.fs_kd_reg
            LEFT JOIN tc_mr pmr_baby ON r_baby.fs_mr = pmr_baby.fs_mr
            LEFT JOIN ta_trs_bed tb_mother ON tb_baby.fs_kd_bed = tb_mother.fs_kd_bed AND tb_mother.fn_tarif > 0
                AND (tb_mother.fd_tgl_out = '' OR tb_mother.fd_tgl_out = '3000-01-01' OR tb_mother.fd_tgl_out IS NULL)
                AND (tb_mother.fd_tgl_void = '' OR tb_mother.fd_tgl_void = '3000-01-01' OR tb_mother.fd_tgl_void IS NULL)
            LEFT JOIN ta_registrasi r_mother ON tb_mother.fs_kd_reg = r_mother.fs_kd_reg
            LEFT JOIN tc_mr pmr_mother ON r_mother.fs_mr = pmr_mother.fs_mr
            WHERE (tb_baby.fd_tgl_out = '' OR tb_baby.fd_tgl_out = '3000-01-01' OR tb_baby.fd_tgl_out IS NULL)
              AND (tb_baby.fd_tgl_void = '' OR tb_baby.fd_tgl_void = '3000-01-01' OR tb_baby.fd_tgl_void IS NULL)
              AND tb_baby.fn_tarif = 0
        ) baby ON bd.fs_kd_bed = baby.fs_kd_bed AND baby.rn = 1
        WHERE bd.fs_kd_bed = @BedId
            AND bd.fb_aktif = 1;

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
            ORDER BY CASE WHEN occ.fn_tarif > 0 THEN 1 ELSE 2 END, occ.fd_tgl_in ASC, occ.fs_jam_in ASC
        )
        ORDER BY CASE WHEN JenisID = 'BPJS' THEN 1 WHEN JenisID = 'ASKES' THEN 2 ELSE 3 END;

        -- 3. Tab Tindakan & BHP
        SELECT 
            ISNULL(t.FS_KD_TRS, '') AS TindakanId,
            ISNULL(TRY_CONVERT(DATETIME, t.FD_TGL_TRS + ' ' + t.FS_JAM_TRS), '2000-01-01') AS TglJam,
            ISNULL(tr.FS_NM_TARIF, '') AS ItemTindakan,
            ISNULL(
                NULLIF((SELECT TOP 1 p.FS_NM_PEG FROM TA_TRS_TINDAKAN2 tk LEFT JOIN td_peg p ON tk.FS_KD_PETUGAS_MEDIS = p.FS_KD_PEG WHERE tk.FS_KD_TRS = t.FS_KD_TRS AND ISNULL(tk.FS_KD_PETUGAS_MEDIS, '') <> '' ORDER BY tk.FS_KD_DETIL_TARIF), ''),
                ISNULL(peg.fs_nm_peg, t.CRTUSR)
            ) AS Petugas,
            ISNULL(t.FN_TOTAL, 0) AS Biaya,
            CASE WHEN EXISTS (
                SELECT 1 FROM TA_TRS_TINDAKAN2 tk 
                WHERE tk.FS_KD_TRS = t.FS_KD_TRS 
                  AND ISNULL(tk.FS_KD_PETUGAS_MEDIS, '') <> ''
            ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasJasaDokter
        FROM TA_TRS_TINDAKAN t
        LEFT JOIN td_peg peg ON t.CRTUSR = peg.fs_kd_peg
        LEFT JOIN ta_tarif tr ON t.FS_KD_TARIF = tr.FS_KD_TARIF
        WHERE t.FS_KD_REG = (
            SELECT TOP 1 occ.fs_kd_reg
            FROM ta_bed bd
            INNER JOIN ta_trs_bed occ ON bd.fs_kd_bed = occ.fs_kd_bed
                AND (occ.fd_tgl_out = '' OR occ.fd_tgl_out = '3000-01-01' OR occ.fd_tgl_out IS NULL)
                AND (occ.fd_tgl_void = '' OR occ.fd_tgl_void = '3000-01-01' OR occ.fd_tgl_void IS NULL)
            WHERE bd.fs_kd_bed = @BedId
            ORDER BY CASE WHEN occ.fn_tarif > 0 THEN 1 ELSE 2 END, occ.fd_tgl_in ASC, occ.fs_jam_in ASC
        )
        AND (t.FD_TGL_VOID = '3000-01-01' OR t.FD_TGL_VOID = '' OR t.FD_TGL_VOID IS NULL)

        UNION ALL

        SELECT 
            ISNULL(b.BhpRanapId, '') AS TindakanId,
            b.CrtDate AS TglJam,
            ISNULL(b.BhpItemName, '') AS ItemTindakan,
            ISNULL(peg.fs_nm_peg, b.CrtUser) AS Petugas,
            ISNULL(b.Qty * b.Price, 0) AS Biaya,
            CAST(0 AS BIT) AS HasJasaDokter
        FROM BILRG_BhpRanap b
        LEFT JOIN td_peg peg ON b.CrtUser = peg.fs_kd_peg
        WHERE b.PakaiBedId = (
            SELECT TOP 1 occ.fs_kd_trs
            FROM ta_bed bd
            INNER JOIN ta_trs_bed occ ON bd.fs_kd_bed = occ.fs_kd_bed
                AND (occ.fd_tgl_out = '' OR occ.fd_tgl_out = '3000-01-01' OR occ.fd_tgl_out IS NULL)
                AND (occ.fd_tgl_void = '' OR occ.fd_tgl_void = '3000-01-01' OR occ.fd_tgl_void IS NULL)
            WHERE bd.fs_kd_bed = @BedId
            ORDER BY CASE WHEN occ.fn_tarif > 0 THEN 1 ELSE 2 END, occ.fd_tgl_in ASC, occ.fs_jam_in ASC
        )
        AND (b.VodDate = '3000-01-01' OR b.VodDate IS NULL)
        ORDER BY TglJam DESC;

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
            ORDER BY CASE WHEN occ.fn_tarif > 0 THEN 1 ELSE 2 END, occ.fd_tgl_in ASC, occ.fs_jam_in ASC
        )
        AND (b.fd_tgl_void = '' OR b.fd_tgl_void = '3000-01-01' OR b.fd_tgl_void IS NULL)
        ORDER BY b.fd_tgl_in ASC, b.fs_jam_in ASC;
        """;

    private readonly DatabaseOptions _opt;
    private readonly ITglJamProvider? _tglJamProvider;

    public WardCockpitDal(IOptions<DatabaseOptions> opt, ITglJamProvider? tglJamProvider = null)
    {
        _opt = opt.Value;
        _tglJamProvider = tglJamProvider;
    }

    public async Task<WardCockpitOverviewDto?> GetOverviewAsync(string bangsalId, DateTime? currentTime = null, CancellationToken cancellationToken = default)
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

        var asOf = currentTime ?? _tglJamProvider?.Now ?? DateTime.Now;
        return BuildOverview(bangsalRow, roomBedRows, operationalRows, waitingListRows, asOf);
    }

    public WardCockpitOverviewDto? GetOverview(string bangsalId, DateTime? currentTime = null)
    {
        return GetOverviewAsync(bangsalId, currentTime).GetAwaiter().GetResult();
    }

    public async Task<WardCockpitBedDetailDto?> GetBedDetailAsync(string bedId, DateTime? currentTime = null, CancellationToken cancellationToken = default)
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

        var asOf = currentTime ?? _tglJamProvider?.Now ?? DateTime.Now;
        return BuildBedDetail(bedRow, noKartu, tindakanRows, mutasiRows, asOf);
    }

    public WardCockpitBedDetailDto? GetBedDetail(string bedId, DateTime? currentTime = null)
    {
        return GetBedDetailAsync(bedId, currentTime).GetAwaiter().GetResult();
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

                    var billingKelas = string.IsNullOrWhiteSpace(b.BillingKelasName) || b.BillingKelasName.Equals("NON KELAS", StringComparison.OrdinalIgnoreCase)
                        ? first.KelasKamar
                        : b.BillingKelasName;

                    var normRoomKelas = NormalizeKelasCategory(first.KelasKamar);
                    var normBillingKelas = NormalizeKelasCategory(billingKelas);

                    var isTitipKelas = false;
                    if (!string.IsNullOrEmpty(normRoomKelas) && !string.IsNullOrEmpty(normBillingKelas))
                    {
                        isTitipKelas = !normRoomKelas.Equals(normBillingKelas, StringComparison.OrdinalIgnoreCase);
                    }
                    else if (!string.IsNullOrEmpty(b.TransaksiKelasId) && !string.IsNullOrEmpty(b.RegKelasId) &&
                             b.RegKelasId != "000" && b.RegKelasId != "-")
                    {
                        isTitipKelas = b.TransaksiKelasId != b.RegKelasId;
                    }

                    var isRawatGabung = b.PakaiBedPurpose == 4 || !string.IsNullOrWhiteSpace(b.BabyRegId);

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
                        BillingKelasName: billingKelas,
                        IsTitipKelas: isTitipKelas,
                        IsRawatGabung: isRawatGabung,
                        BabyName: b.BabyName,
                        BabyRegId: b.BabyRegId,
                        BabyMrNo: b.BabyMrNo,
                        KdJenisInap: b.KdJenisInap
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

    private static string CalculateAge(string? birthDateStr, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(birthDateStr) || !DateTime.TryParse(birthDateStr, out var birthDate))
            return "-";

        if (birthDate.Year >= 3000 || birthDate.Year < 1900)
            return "-";

        var years = now.Year - birthDate.Year;
        if (birthDate.Date > now.AddYears(-years).Date)
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

    internal static string NormalizeKelasCategory(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var upper = raw.Trim().ToUpperInvariant();
        if (upper == "NON KELAS" || upper == "000" || upper == "-") return string.Empty;
        if (upper.Contains("VVIP")) return "VVIP";
        if (upper.Contains("VIP")) return "VIP";
        if (System.Text.RegularExpressions.Regex.IsMatch(upper, @"\b(III|3)\b") || upper.Contains("KELAS 3") || upper.Contains("KELAS III")) return "Kelas III";
        if (System.Text.RegularExpressions.Regex.IsMatch(upper, @"\b(II|2)\b") || upper.Contains("KELAS 2") || upper.Contains("KELAS II")) return "Kelas II";
        if (System.Text.RegularExpressions.Regex.IsMatch(upper, @"\b(I|1)\b") || upper.Contains("KELAS 1") || upper.Contains("KELAS I")) return "Kelas I";
        return raw.Trim();
    }

    private static string MapUrgency(int urgency) => urgency switch
    {
        2 => "Cito",
        1 => "Urgent",
        _ => "Elective"
    };

    private static string MapReferralSource(int sourceType, string? unitName) => sourceType switch
    {
        0 => "IGD",
        1 => "Poliklinik",
        2 => "Admisi",
        3 => "Transfer",
        _ => "IGD"
    };

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
            TglLahir: !string.IsNullOrWhiteSpace(bedRow.TglLahir) ? bedRow.TglLahir.Trim() : string.Empty,
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

        var billingKelas = string.IsNullOrWhiteSpace(bedRow.BillingKelasName) || bedRow.BillingKelasName.Equals("NON KELAS", StringComparison.OrdinalIgnoreCase)
            ? bedRow.KelasKamar
            : bedRow.BillingKelasName;

        var isRawatGabung = !string.IsNullOrWhiteSpace(bedRow.BabyRegId) || !string.IsNullOrWhiteSpace(bedRow.BabyName);

        var occupancy = new WardCockpitBedDetailOccupancyDto(
            PakaiBedId: bedRow.PakaiBedId!,
            RegId: bedRow.RegId ?? string.Empty,
            Patient: patient,
            Penjamin: penjamin,
            Dpjp: dpjp,
            CheckInTime: tglJamMasuk,
            LosDays: losDays,
            BillingKelas: billingKelas,
            IsRawatGabung: isRawatGabung,
            BabyName: bedRow.BabyName,
            BabyRegId: bedRow.BabyRegId,
            BabyMrNo: bedRow.BabyMrNo,
            KdJenisInap: bedRow.KdJenisInap
        );

        var tabTindakan = (tindakanRows ?? [])
            .Select(t => new WardCockpitBedDetailTindakanDto(
                TglJam: t.TglJam,
                ItemTindakan: t.ItemTindakan,
                Petugas: t.Petugas,
                Biaya: t.Biaya,
                TindakanId: t.TindakanId ?? string.Empty,
                HasJasaDokter: t.HasJasaDokter
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

    private static string CalculateAgeFull(string? birthDateStr, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(birthDateStr) || !DateTime.TryParse(birthDateStr, out var birthDate))
            return "-";

        if (birthDate.Year >= 3000 || birthDate.Year < 1900)
            return "-";

        var years = now.Year - birthDate.Year;
        if (birthDate.Date > now.AddYears(-years).Date)
            years--;

        return $"{Math.Max(0, years)} Tahun";
    }

    public async Task<IEnumerable<WardCockpitBangsalListItemDto>> ListActiveBangsalAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                b.fs_kd_bangsal AS BangsalId,
                b.fs_nm_bangsal AS BangsalName,
                ISNULL(STRING_AGG(k.fs_nm_kelas, ' / ') WITHIN GROUP (ORDER BY k.fs_nm_kelas), '') AS KelasName
            FROM ta_bangsal b
            LEFT JOIN (
                SELECT DISTINCT fs_kd_bangsal, fs_kd_kelas
                FROM ta_kamar
                WHERE ISNULL(fs_kd_bangsal, '') <> '' AND ISNULL(fs_kd_kelas, '') <> ''
            ) km ON b.fs_kd_bangsal = km.fs_kd_bangsal
            LEFT JOIN ta_kelas k ON km.fs_kd_kelas = k.fs_kd_kelas
            WHERE ISNULL(b.fb_aktif, 1) = 1 AND b.fs_kd_bangsal NOT IN ('ZZ', 'XX')
            GROUP BY b.fs_kd_bangsal, b.fs_nm_bangsal
            ORDER BY b.fs_kd_bangsal;
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var rawResult = await conn.QueryAsync<WardCockpitBangsalListItemDto>(command);
        return rawResult.Select(x => new WardCockpitBangsalListItemDto(
            x.BangsalId,
            x.BangsalName,
            FormatBangsalKelasSummary(x.KelasName)));
    }

    private static string FormatBangsalKelasSummary(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "-";
        var parts = raw.Split(new[] { " / ", "/" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var items = new List<string>();

        bool hasI = false, hasII = false, hasIII = false, hasVIP = false, hasVVIP = false, hasIsolasi = false, hasExec = false;
        var specials = new List<string>();

        foreach (var p in parts)
        {
            var u = p.ToUpperInvariant();
            if (u.StartsWith("VVIP")) hasVVIP = true;
            else if (u.StartsWith("VIP")) hasVIP = true;
            else if (u.StartsWith("EXECUTIVE")) hasExec = true;
            else if (u.StartsWith("ISOLASI")) hasIsolasi = true;
            else if (u.StartsWith("I B") || u.StartsWith("I A") || u == "I" || u.StartsWith("I ") || u.Contains("KELAS I") || u == "1") hasI = true;
            else if (u.StartsWith("II A") || u.StartsWith("II B") || u == "II" || u.StartsWith("II ") || u.Contains("KELAS II") || u == "2") hasII = true;
            else if (u == "III" || u.StartsWith("III ") || u.Contains("KELAS III") || u == "3") hasIII = true;
            else if (u.Contains("ICU") || u.Contains("ICCU") || u.Contains("PICU") || u.Contains("HND"))
            {
                if (!specials.Contains(u)) specials.Add(u);
            }
        }

        if (hasI) items.Add("I");
        if (hasII) items.Add("II");
        if (hasIII) items.Add("III");
        if (hasVIP) items.Add("VIP");
        if (hasVVIP) items.Add("VVIP");
        if (hasIsolasi) items.Add("Isolasi");
        if (hasExec) items.Add("Executive");
        foreach (var s in specials) items.Add(s);

        if (items.Count == 0) return raw;

        if (hasI || hasII || hasIII)
        {
            return "Kelas " + string.Join(" / ", items);
        }

        return string.Join(" / ", items);
    }

    public async Task<string?> SaveRoomingInAsync(
        string motherPakaiBedId,
        string babyRegId,
        string bedId,
        string bangsalId,
        string kamarId,
        string regId,
        string userId,
        DateTime occurredAt,
        string? catatan = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            DECLARE @ResolvedBabyRegId VARCHAR(50) = @BabyRegId;
            DECLARE @BabyMrNo VARCHAR(50) = '';

            SELECT TOP 1 
                @ResolvedBabyRegId = fs_kd_reg,
                @BabyMrNo = ISNULL(fs_mr, '')
            FROM ta_registrasi
            WHERE fs_kd_reg = @BabyRegId 
               OR fs_mr = @BabyRegId 
               OR fs_mr LIKE '%' + @BabyRegId
            ORDER BY fd_tgl_masuk DESC, fs_jam_masuk DESC;

            IF ISNULL(@BabyMrNo, '') = ''
            BEGIN
                SET @BabyMrNo = @BabyRegId;
            END

            -- 1. Hubungkan ta_registrasi bayi ke registrasi ibu jika registrasi bayi ada
            IF EXISTS (SELECT 1 FROM ta_registrasi WHERE fs_kd_reg = @ResolvedBabyRegId)
            BEGIN
                UPDATE ta_registrasi
                SET fs_reg_ibu = @RegId
                WHERE fs_kd_reg = @ResolvedBabyRegId;
            END

            -- 2. Pastikan transaksi pemakaian bed untuk bayi bertarif 0 tercatat di ta_trs_bed
            IF NOT EXISTS (
                SELECT 1 FROM ta_trs_bed 
                WHERE fs_kd_reg = @ResolvedBabyRegId 
                  AND fs_kd_bed = @BedId 
                  AND (fd_tgl_out = '' OR fd_tgl_out = '3000-01-01' OR fd_tgl_out IS NULL)
                  AND (fd_tgl_void = '' OR fd_tgl_void = '3000-01-01' OR fd_tgl_void IS NULL)
            )
            BEGIN
                INSERT INTO ta_trs_bed (
                    fs_kd_trs, fd_tgl_in, fs_jam_in, fs_kd_petugas,
                    fd_tgl_out, fs_jam_out, fs_kd_petugas_out,
                    fs_kd_reg, fs_kd_layanan, fs_kd_layanan_dk,
                    fs_kd_kamar_tipe, fs_kd_bed, fn_tarif,
                    fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                    fd_tgl_entry, fs_jam_entry, fs_kd_kelas
                )
                SELECT TOP 1
                    @BabyPakaiBedId,
                    CONVERT(VARCHAR(10), @OccurredAt, 120),
                    CONVERT(VARCHAR(8), @OccurredAt, 108),
                    @UserId,
                    '3000-01-01', '00:00:00', '',
                    @ResolvedBabyRegId,
                    trs_ibu.fs_kd_layanan,
                    trs_ibu.fs_kd_layanan_dk,
                    trs_ibu.fs_kd_kamar_tipe,
                    @BedId,
                    0,
                    '3000-01-01', '', '',
                    CONVERT(VARCHAR(10), @OccurredAt, 120),
                    CONVERT(VARCHAR(8), @OccurredAt, 108),
                    trs_ibu.fs_kd_kelas
                FROM ta_trs_bed trs_ibu
                WHERE trs_ibu.fs_kd_trs = @MotherPakaiBedId;
            END

            -- 3. Cari tiket antrean aktif bayi dan transisikan ke Placed (2)
            DECLARE @TargetWaitingListId VARCHAR(50) = NULL;
            DECLARE @BabyWaitingListBangsalId VARCHAR(50) = NULL;

            SELECT TOP 1 
                @TargetWaitingListId = WaitingListId,
                @BabyWaitingListBangsalId = BangsalId
            FROM BILRG_BedWaitingList
            WHERE WaitingListStatus IN (0, 1)
              AND (
                  (ISNULL(@ResolvedBabyRegId, '') <> '' AND (RegId = @ResolvedBabyRegId OR RegId = @BabyRegId))
                  OR (ISNULL(@BabyMrNo, '') <> '' AND (PasienId = @BabyMrNo OR PasienId = @BabyRegId))
              )
            ORDER BY CrtDate DESC;

            IF @TargetWaitingListId IS NOT NULL
            BEGIN
                UPDATE BILRG_BedWaitingList
                SET
                    WaitingListStatus = 2,
                    AdmittedBedId = @BedId,
                    AdmittedNurseId = @UserId,
                    AdmittedDate = @OccurredAt,
                    UpdUser = @UserId,
                    UpdDate = @OccurredAt
                WHERE WaitingListId = @TargetWaitingListId;
            END

            SELECT @BabyWaitingListBangsalId AS BabyWaitingListBangsalId;
            """;

        var babyPakaiBedId = "BE" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var dp = new DynamicParameters();
        dp.AddParam("@MotherPakaiBedId", motherPakaiBedId, SqlDbType.VarChar);
        dp.AddParam("@BabyPakaiBedId", babyPakaiBedId, SqlDbType.VarChar);
        dp.AddParam("@BabyRegId", babyRegId, SqlDbType.VarChar);
        dp.AddParam("@BedId", bedId, SqlDbType.VarChar);
        dp.AddParam("@UserId", userId, SqlDbType.VarChar);
        dp.AddParam("@OccurredAt", occurredAt, SqlDbType.DateTime);
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);

        await using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var command = new CommandDefinition(sql, dp, cancellationToken: cancellationToken);
        var bangsalResult = await conn.ExecuteScalarAsync<string?>(command);
        return string.IsNullOrWhiteSpace(bangsalResult) ? null : bangsalResult.Trim();
    }

    public async Task<IEnumerable<WardCockpitBabyLookupDto>> LookupBabyAsync(string keyword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword) || keyword.Trim().Length < 3)
            return Enumerable.Empty<WardCockpitBabyLookupDto>();

        const string sql = """
            SELECT TOP 8
                r.fs_kd_reg AS RegId,
                r.fs_mr AS MrNo,
                ISNULL(p.fs_nm_pasien, '') AS NamaPasien,
                CASE 
                    WHEN p.fs_jns_kelamin IN ('L', 'MALE', 'LAKI-LAKI') THEN 'Laki-laki'
                    WHEN p.fs_jns_kelamin IN ('P', 'FEMALE', 'PEREMPUAN') THEN 'Perempuan'
                    ELSE ISNULL(p.fs_jns_kelamin, 'Laki-laki')
                END AS Gender,
                p.fd_tgl_lahir AS TglLahir
            FROM ta_registrasi r
            LEFT JOIN tc_mr p ON r.fs_mr = p.fs_mr
            WHERE r.fs_kd_jenis_inap = '3'
              AND (
                  r.fs_kd_reg LIKE '%' + @Keyword + '%' 
                  OR r.fs_mr LIKE '%' + @Keyword + '%' 
                  OR p.fs_nm_pasien LIKE '%' + @Keyword + '%'
              )
            ORDER BY r.fd_tgl_masuk DESC;
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@Keyword", keyword.Trim(), SqlDbType.VarChar);

        await using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var command = new CommandDefinition(sql, dp, cancellationToken: cancellationToken);
        return await conn.QueryAsync<WardCockpitBabyLookupDto>(command);
    }
}
