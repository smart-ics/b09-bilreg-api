--------------------------------------------------------------------------------
-- Migrasi Data Idempotent Pra-Admisi Rawat Inap (Opname Request & Reservasi)
-- Menuju Tabel Terpadu BILRG_AdmInpatientOrder dan Backfill BILRG_AdmAdmission
--------------------------------------------------------------------------------

-- 1. Salin Data Aktif dari BILRG_AdmOpnameRequest
IF OBJECT_ID('BILRG_AdmOpnameRequest', 'U') IS NOT NULL AND OBJECT_ID('BILRG_AdmInpatientOrder', 'U') IS NOT NULL
BEGIN
    INSERT INTO BILRG_AdmInpatientOrder
    (
        InpatientOrderId,
        OrderSource,
        OrderStatus,
        PasienId,
        PasienName,
        TglLahir,
        Gender,
        DokterId,
        DokterName,
        PlannedDate,
        KelasId,
        KelasName,
        BangsalId,
        BangsalName,
        ClinicalNotes,
        TipeJaminanId,
        TipeJaminanName,
        JaminanReffId,
        FulfilledRegId,
        ExternalReferenceId,
        ExternalSystemSource,
        CrtUser,
        CrtDate,
        UpdUser,
        UpdDate,
        VodUser,
        VodDate
    )
    SELECT
        aa.OpnameRequestId,
        1, -- OrderSourceEnum.EmrRawatJalan
        CASE 
            WHEN aa.OpnameRequestStatus = 0 THEN 0 -- Requested
            WHEN aa.OpnameRequestStatus = 1 THEN 2 -- Fulfilled
            WHEN aa.OpnameRequestStatus = 2 THEN 3 -- Cancelled
            ELSE 0
        END,
        aa.PasienId,
        ISNULL(mr.fs_nm_pasien, ''),
        ISNULL(mr.fd_tgl_lahir, '3000-01-01'),
        ISNULL(mr.fs_jns_kelamin, ''),
        aa.DokterId,
        aa.DokterName,
        aa.PlannedDate,
        '-',
        '',
        '-',
        '',
        aa.ClinicalNotes,
        ISNULL(ins.TipeJaminanId, '-'),
        ISNULL(ins.TipeJaminanName, ''),
        ISNULL(ins.ReffId, '-'),
        aa.FulfilledRegId,
        CASE WHEN aa.TrsOrderId = '' OR aa.TrsOrderId IS NULL THEN '-' ELSE aa.TrsOrderId END,
        CASE WHEN aa.TrsOrderId IS NOT NULL AND aa.TrsOrderId <> '-' AND aa.TrsOrderId <> '' THEN 'EMR_RJ' ELSE '' END,
        aa.CrtUser,
        aa.CrtDate,
        aa.UpdUser,
        aa.UpdDate,
        aa.VodUser,
        aa.VodDate
    FROM BILRG_AdmOpnameRequest aa
    LEFT JOIN tc_mr mr ON aa.PasienId = mr.fs_mr
    LEFT JOIN BILRG_AdmOpnameRequestInsurance ins ON aa.OpnameRequestId = ins.OpnameRequestId
    WHERE aa.VodDate = '3000-01-01'
      AND NOT EXISTS (
          SELECT 1 FROM BILRG_AdmInpatientOrder target
          WHERE target.InpatientOrderId = aa.OpnameRequestId
      );
END
GO

-- 2. Salin Data Aktif dari BILRG_AdmReservation
IF OBJECT_ID('BILRG_AdmReservation', 'U') IS NOT NULL AND OBJECT_ID('BILRG_AdmInpatientOrder', 'U') IS NOT NULL
BEGIN
    INSERT INTO BILRG_AdmInpatientOrder
    (
        InpatientOrderId,
        OrderSource,
        OrderStatus,
        PasienId,
        PasienName,
        TglLahir,
        Gender,
        DokterId,
        DokterName,
        PlannedDate,
        KelasId,
        KelasName,
        BangsalId,
        BangsalName,
        ClinicalNotes,
        TipeJaminanId,
        TipeJaminanName,
        JaminanReffId,
        FulfilledRegId,
        ExternalReferenceId,
        ExternalSystemSource,
        CrtUser,
        CrtDate,
        UpdUser,
        UpdDate,
        VodUser,
        VodDate
    )
    SELECT
        aa.ReservationId,
        3, -- OrderSourceEnum.AdmisiBooking
        CASE 
            WHEN aa.ReservationStatus = 0 THEN 1 -- Maintained
            WHEN aa.ReservationStatus = 1 THEN 1 -- Maintained
            WHEN aa.ReservationStatus = 2 THEN 2 -- Fulfilled
            WHEN aa.ReservationStatus = 3 THEN 3 -- Cancelled
            ELSE 1
        END,
        aa.PasienId,
        ISNULL(NULLIF(aa.PasienName, ''), ISNULL(mr.fs_nm_pasien, '')),
        CASE WHEN aa.TglLahir = '3000-01-01' OR aa.TglLahir IS NULL THEN ISNULL(mr.fd_tgl_lahir, '3000-01-01') ELSE aa.TglLahir END,
        ISNULL(NULLIF(aa.Gender, ''), ISNULL(mr.fs_jns_kelamin, '')),
        '-',
        '',
        aa.PlannedDate,
        aa.KelasId,
        aa.KelasName,
        aa.BangsalId,
        aa.BangsalName,
        '',
        '-',
        '',
        '-',
        aa.RealizedRegId,
        '-',
        '',
        aa.CrtUser,
        aa.CrtDate,
        aa.UpdUser,
        aa.UpdDate,
        aa.VodUser,
        aa.VodDate
    FROM BILRG_AdmReservation aa
    LEFT JOIN tc_mr mr ON aa.PasienId = mr.fs_mr
    WHERE aa.VodDate = '3000-01-01'
      AND NOT EXISTS (
          SELECT 1 FROM BILRG_AdmInpatientOrder target
          WHERE target.InpatientOrderId = aa.ReservationId
      );
END
GO

-- 3. Salin Data Terminal / Riwayat (Voided) secara Idempotent
IF OBJECT_ID('BILRG_AdmOpnameRequest', 'U') IS NOT NULL AND OBJECT_ID('BILRG_AdmInpatientOrder', 'U') IS NOT NULL
BEGIN
    INSERT INTO BILRG_AdmInpatientOrder
    (
        InpatientOrderId,
        OrderSource,
        OrderStatus,
        PasienId,
        PasienName,
        TglLahir,
        Gender,
        DokterId,
        DokterName,
        PlannedDate,
        KelasId,
        KelasName,
        BangsalId,
        BangsalName,
        ClinicalNotes,
        TipeJaminanId,
        TipeJaminanName,
        JaminanReffId,
        FulfilledRegId,
        ExternalReferenceId,
        ExternalSystemSource,
        CrtUser,
        CrtDate,
        UpdUser,
        UpdDate,
        VodUser,
        VodDate
    )
    SELECT
        aa.OpnameRequestId,
        1,
        CASE 
            WHEN aa.OpnameRequestStatus = 0 THEN 0
            WHEN aa.OpnameRequestStatus = 1 THEN 2
            WHEN aa.OpnameRequestStatus = 2 THEN 3
            ELSE 0
        END,
        aa.PasienId,
        ISNULL(mr.fs_nm_pasien, ''),
        ISNULL(mr.fd_tgl_lahir, '3000-01-01'),
        ISNULL(mr.fs_jns_kelamin, ''),
        aa.DokterId,
        aa.DokterName,
        aa.PlannedDate,
        '-',
        '',
        '-',
        '',
        aa.ClinicalNotes,
        ISNULL(ins.TipeJaminanId, '-'),
        ISNULL(ins.TipeJaminanName, ''),
        ISNULL(ins.ReffId, '-'),
        aa.FulfilledRegId,
        CASE WHEN aa.TrsOrderId = '' OR aa.TrsOrderId IS NULL THEN '-' ELSE aa.TrsOrderId END,
        CASE WHEN aa.TrsOrderId IS NOT NULL AND aa.TrsOrderId <> '-' AND aa.TrsOrderId <> '' THEN 'EMR_RJ' ELSE '' END,
        aa.CrtUser,
        aa.CrtDate,
        aa.UpdUser,
        aa.UpdDate,
        aa.VodUser,
        aa.VodDate
    FROM BILRG_AdmOpnameRequest aa
    LEFT JOIN tc_mr mr ON aa.PasienId = mr.fs_mr
    LEFT JOIN BILRG_AdmOpnameRequestInsurance ins ON aa.OpnameRequestId = ins.OpnameRequestId
    WHERE aa.VodDate <> '3000-01-01'
      AND NOT EXISTS (
          SELECT 1 FROM BILRG_AdmInpatientOrder target
          WHERE target.InpatientOrderId = aa.OpnameRequestId
      );
END
GO

IF OBJECT_ID('BILRG_AdmReservation', 'U') IS NOT NULL AND OBJECT_ID('BILRG_AdmInpatientOrder', 'U') IS NOT NULL
BEGIN
    INSERT INTO BILRG_AdmInpatientOrder
    (
        InpatientOrderId,
        OrderSource,
        OrderStatus,
        PasienId,
        PasienName,
        TglLahir,
        Gender,
        DokterId,
        DokterName,
        PlannedDate,
        KelasId,
        KelasName,
        BangsalId,
        BangsalName,
        ClinicalNotes,
        TipeJaminanId,
        TipeJaminanName,
        JaminanReffId,
        FulfilledRegId,
        ExternalReferenceId,
        ExternalSystemSource,
        CrtUser,
        CrtDate,
        UpdUser,
        UpdDate,
        VodUser,
        VodDate
    )
    SELECT
        aa.ReservationId,
        3,
        CASE 
            WHEN aa.ReservationStatus = 0 THEN 1
            WHEN aa.ReservationStatus = 1 THEN 1
            WHEN aa.ReservationStatus = 2 THEN 2
            WHEN aa.ReservationStatus = 3 THEN 3
            ELSE 1
        END,
        aa.PasienId,
        ISNULL(NULLIF(aa.PasienName, ''), ISNULL(mr.fs_nm_pasien, '')),
        CASE WHEN aa.TglLahir = '3000-01-01' OR aa.TglLahir IS NULL THEN ISNULL(mr.fd_tgl_lahir, '3000-01-01') ELSE aa.TglLahir END,
        ISNULL(NULLIF(aa.Gender, ''), ISNULL(mr.fs_jns_kelamin, '')),
        '-',
        '',
        aa.PlannedDate,
        aa.KelasId,
        aa.KelasName,
        aa.BangsalId,
        aa.BangsalName,
        '',
        '-',
        '',
        '-',
        aa.RealizedRegId,
        '-',
        '',
        aa.CrtUser,
        aa.CrtDate,
        aa.UpdUser,
        aa.UpdDate,
        aa.VodUser,
        aa.VodDate
    FROM BILRG_AdmReservation aa
    LEFT JOIN tc_mr mr ON aa.PasienId = mr.fs_mr
    WHERE aa.VodDate <> '3000-01-01'
      AND NOT EXISTS (
          SELECT 1 FROM BILRG_AdmInpatientOrder target
          WHERE target.InpatientOrderId = aa.ReservationId
      );
END
GO

-- 4. Backfill InpatientOrderId pada BILRG_AdmAdmission
IF OBJECT_ID('BILRG_AdmAdmission', 'U') IS NOT NULL AND COL_LENGTH('BILRG_AdmAdmission', 'InpatientOrderId') IS NOT NULL
BEGIN
    -- Backfill dari OpnameRequestId
    IF COL_LENGTH('BILRG_AdmAdmission', 'OpnameRequestId') IS NOT NULL
    BEGIN
        UPDATE BILRG_AdmAdmission
        SET InpatientOrderId = OpnameRequestId
        WHERE OpnameRequestId IS NOT NULL 
          AND OpnameRequestId <> '-' 
          AND OpnameRequestId <> ''
          AND (InpatientOrderId = '-' OR InpatientOrderId = '');
    END;

    -- Backfill dari ReservationId
    IF COL_LENGTH('BILRG_AdmAdmission', 'ReservationId') IS NOT NULL
    BEGIN
        UPDATE BILRG_AdmAdmission
        SET InpatientOrderId = ReservationId
        WHERE ReservationId IS NOT NULL 
          AND ReservationId <> '-' 
          AND ReservationId <> ''
          AND (InpatientOrderId = '-' OR InpatientOrderId = '');
    END;
END
GO
