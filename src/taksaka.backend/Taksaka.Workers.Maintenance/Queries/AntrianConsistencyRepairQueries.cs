namespace Taksaka.Workers.Maintenance.Queries;

internal static class AntrianConsistencyRepairQueries
{
    public const string FindInconsistent = """
        SELECT TOP (@BatchSize)
            aa.AntrianId,
            aa.NoUrut,
            aa.PersonName,
            aa.ReffId,
            aa.ReffDesc,

            bb.BookingId      AS BookingUlid,
            cc.BookingId      AS BookingBridgeId,
            dd.fs_kd_booking   AS FsKdBooking,
            dd.fs_kd_reg       AS RegistrationId,
            dd.fd_tgl_masuk    AS FdTglMasuk

        FROM BILRG_AntrianEntry aa

        LEFT JOIN BILRG_BookingExternal bb
               ON aa.ReffId = bb.BookingId

        LEFT JOIN HIDOK_BookingBridge cc
               ON bb.ReffId = cc.BookingId

        LEFT JOIN ta_registrasi dd
               ON cc.KodeTrsBookingRS = dd.fs_kd_booking
              AND dd.fs_kd_booking <> ''

        WHERE
            ISNULL(dd.fs_kd_reg,'??') LIKE 'RG%'
            AND aa.ReffDesc='BOK'

        ORDER BY
            dd.fd_tgl_masuk,
            aa.AntrianId,
            aa.NoUrut;
        """;

    public const string Repair = """
        UPDATE BILRG_AntrianEntry
        SET
            ReffId   = @RegistrationId,
            ReffDesc = 'REG'
        WHERE
            AntrianId = @AntrianId
        AND NoUrut    = @NoUrut;
        """;
}
