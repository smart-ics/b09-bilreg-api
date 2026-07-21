IF COL_LENGTH('BILRG_AdmOpnameRequest', 'PasienName') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_AdmOpnameRequest DROP CONSTRAINT DF_BILRG_AdmOpnameRequest_PasienName;
    ALTER TABLE BILRG_AdmOpnameRequest DROP COLUMN PasienName;
END
GO

IF COL_LENGTH('BILRG_AdmOpnameRequest', 'TglLahir') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_AdmOpnameRequest DROP CONSTRAINT DF_BILRG_AdmOpnameRequest_TglLahir;
    ALTER TABLE BILRG_AdmOpnameRequest DROP COLUMN TglLahir;
END
GO

IF COL_LENGTH('BILRG_AdmOpnameRequest', 'Gender') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_AdmOpnameRequest DROP CONSTRAINT DF_BILRG_AdmOpnameRequest_Gender;
    ALTER TABLE BILRG_AdmOpnameRequest DROP COLUMN Gender;
END
GO
