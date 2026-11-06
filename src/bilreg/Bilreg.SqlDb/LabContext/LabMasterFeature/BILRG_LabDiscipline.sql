IF OBJECT_ID('BILRG_LabDiscipline', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_LabDiscipline (
        DisciplineId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_DisciplineId DEFAULT(''),
        DisciplineName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_DisciplineName DEFAULT(''),
        CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_CrtUser DEFAULT(''),
        CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_CrtDate DEFAULT('3000-01-01'),
        UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_UpdUser DEFAULT(''),
        UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_UpdDate DEFAULT('3000-01-01'),
        VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_VodUser DEFAULT(''),
        VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_LabDiscipline PRIMARY KEY CLUSTERED (DisciplineId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_BILRG_LabDiscipline_Name' AND object_id = OBJECT_ID('BILRG_LabDiscipline'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_LabDiscipline_Name ON BILRG_LabDiscipline(DisciplineName);
END
GO
