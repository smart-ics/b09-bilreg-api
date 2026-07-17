IF COL_LENGTH('BILRG_AdmAdmission', 'KelasDkId') IS NULL
BEGIN
    ALTER TABLE BILRG_AdmAdmission
        ADD KelasDkId VARCHAR(1) NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_KelasDkId DEFAULT('-');
END
GO

IF COL_LENGTH('BILRG_AdmAdmission', 'KelasDkName') IS NULL
BEGIN
    ALTER TABLE BILRG_AdmAdmission
        ADD KelasDkName VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_AdmAdmission_KelasDkName DEFAULT('');
END
GO

IF COL_LENGTH('BILRG_AdmAdmission', 'KelasId') IS NOT NULL
BEGIN
    UPDATE aa
    SET
        aa.KelasDkId = ISNULL(k.fs_kd_kelas_dk, '-'),
        aa.KelasDkName = ISNULL(dk.fs_nm_kelas_dk, '')
    FROM BILRG_AdmAdmission aa
    LEFT JOIN ta_kelas k ON aa.KelasId = k.fs_kd_kelas
    LEFT JOIN ta_kelas_dk dk ON k.fs_kd_kelas_dk = dk.fs_kd_kelas_dk;
END
GO

IF COL_LENGTH('BILRG_AdmAdmission', 'KelasId') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_AdmAdmission DROP CONSTRAINT DF_BILRG_AdmAdmission_KelasId;
    ALTER TABLE BILRG_AdmAdmission DROP COLUMN KelasId;
END
GO

IF COL_LENGTH('BILRG_AdmAdmission', 'KelasName') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_AdmAdmission DROP CONSTRAINT DF_BILRG_AdmAdmission_KelasName;
    ALTER TABLE BILRG_AdmAdmission DROP COLUMN KelasName;
END
GO
