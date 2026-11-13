-- BILRG_LabDiscipline_Seed.sql
-- Seed initial canonical laboratory disciplines

IF NOT EXISTS (SELECT 1 FROM BILRG_LabDiscipline WHERE DisciplineId = 'DISC-HEM')
BEGIN
    INSERT INTO BILRG_LabDiscipline (DisciplineId, DisciplineName, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('DISC-HEM', 'Hematologi', 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabDiscipline WHERE DisciplineId = 'DISC-KIM')
BEGIN
    INSERT INTO BILRG_LabDiscipline (DisciplineId, DisciplineName, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('DISC-KIM', 'Kimia Klinik', 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabDiscipline WHERE DisciplineId = 'DISC-IMM')
BEGIN
    INSERT INTO BILRG_LabDiscipline (DisciplineId, DisciplineName, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('DISC-IMM', 'Imunologi & Serologi', 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabDiscipline WHERE DisciplineId = 'DISC-URI')
BEGIN
    INSERT INTO BILRG_LabDiscipline (DisciplineId, DisciplineName, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('DISC-URI', 'Urinalisis & Feses', 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabDiscipline WHERE DisciplineId = 'DISC-MIK')
BEGIN
    INSERT INTO BILRG_LabDiscipline (DisciplineId, DisciplineName, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('DISC-MIK', 'Mikrobiologi Klinik', 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabDiscipline WHERE DisciplineId = 'DISC-PAH')
BEGIN
    INSERT INTO BILRG_LabDiscipline (DisciplineId, DisciplineName, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('DISC-PAH', 'Patologi Anatomi - Histopatologi', 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabDiscipline WHERE DisciplineId = 'DISC-PAS')
BEGIN
    INSERT INTO BILRG_LabDiscipline (DisciplineId, DisciplineName, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('DISC-PAS', 'Patologi Anatomi - Sitologi', 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;
GO
