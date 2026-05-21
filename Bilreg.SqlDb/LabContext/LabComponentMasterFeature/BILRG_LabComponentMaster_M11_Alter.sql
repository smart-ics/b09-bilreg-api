IF COL_LENGTH('BILRG_LabComponentMaster', 'ComponentNameIndonesia') IS NULL
BEGIN
    ALTER TABLE BILRG_LabComponentMaster
        ADD ComponentNameIndonesia VARCHAR(120) NULL;
END
GO
