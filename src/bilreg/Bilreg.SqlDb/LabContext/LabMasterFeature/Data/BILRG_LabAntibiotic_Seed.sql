-- BILRG_LabAntibiotic_Seed.sql
-- Seed initial standardized antibiotics for Indonesian hospitals (CLSI M100 / PPRA Kemenkes compliant)
-- Normalized Title Case generic names

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '101')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('101', 'Penicillin G', '10 U', '<=11', '12 - 21', '>= 22', 10, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '102')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('102', 'Ampicillin', '10 ug', '<=19', '20 - 27', '>= 28', 11, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '103')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('103', 'Amoxicillin', '25 ug', '<=10', '11 - 15', '>= 16', 12, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '104')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('104', 'Amoxicillin + Clavulanic Acid', '30 ug', '<=19', '19 - 20', '>= 20', 13, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '105')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('105', 'Sulbenicillin', '100 ug', '<=17', '18 - 22', '>= 23', 14, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '106')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('106', 'Cloxacillin', '5 ug', '<=12', '13 - 14', '>= 15', 15, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '107')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('107', 'Methicillin', '5 ug', '<=9', '10 - 13', '>= 14', 16, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '108')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('108', 'Ticarcillin', '75 ug', '<=11', '12 - 14', '>= 15', 17, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '109')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('109', 'Cephalexin', '30 ug', '<=14', '14 - 17', '>= 18', 18, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '110')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('110', 'Cefamandole', '30 ug', '<=14', '14 - 17', '>= 18', 19, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '111')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('111', 'Cefmetazole', '30 ug', '<=14', '15 - 22', '>= 23', 20, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '112')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('112', 'Cefotiam', '30 ug', '<=15', '16 - 19', '>= 20', 21, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '113')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('113', 'Cefuroxime', '30 ug', '<=14', '15 - 17', '>= 18', 22, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '114')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('114', 'Ceftazidime', '30 ug', '<=14', '15 - 17', '>= 18', 23, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '115')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('115', 'Ceftriaxone', '30 ug', '<=14', '15 - 17', '>= 18', 24, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '116')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('116', 'Cefoperazone', '75 ug', '<=15', '16 - 20', '>= 21', 25, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '117')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('117', 'Cefodizime', '30 ug', '<=14', '15 - 22', '>= 23', 26, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '118')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('118', 'Cefotaxime', '30 ug', '<=14', '15 - 22', '>= 23', 27, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '119')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('119', 'Cefpirome', '30 ug', '<=14', '15 - 17', '>= 18', 28, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '120')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('120', 'Amikacin', '30 ug', '<=14', '15 - 16', '>= 17', 29, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '121')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('121', 'Gentamicin', '30 ug', '<=12', '13 - 14', '>= 15', 30, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '122')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('122', 'Kanamycin', '30 ug', '<=13', '14 - 17', '>= 18', 31, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '123')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('123', 'Neomycin', '30 ug', '<=12', '13 - 16', '>= 17', 32, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '124')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('124', 'Netilmicin', '30 ug', '<=12', '13 - 14', '>= 15', 33, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '125')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('125', 'Streptomycin', '10 ug', '<=11', '12 - 14', '>= 15', 34, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '126')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('126', 'Tobramycin', '10 ug', '<=12', '13 - 14', '>= 15', 35, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '127')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('127', 'Chloramphenicol', '30 ug', '<=11', '12 - 17', '>= 18', 36, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '128')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('128', 'Tetracycline', '30 ug', '<=14', '15 - 18', '>= 19', 37, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '129')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('129', 'Trimethoprim', '5 ug', '<=10', '11 - 15', '>= 16', 38, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '130')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('130', 'Sulfonamide', '300 ug', '<=12', '13 - 16', '>= 17', 39, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '131')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('131', 'Cotrimoxazole', '25 ug', '<=10', '11 - 15', '>= 16', 40, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '132')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('132', 'Erythromycin', '15 ug', '<=13', '14 - 17', '>= 18', 41, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '133')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('133', 'Lincomycin', '15 ug', '<=13', '14 - 17', '>= 18', 42, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '134')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('134', 'Clindamycin', '2 ug', '<=14', '15 - 16', '>= 17', 43, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '135')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('135', 'Nitrofurantoin', '300 ug', '<=14', '15 - 16', '>= 17', 44, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '136')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('136', 'Nalidixic Acid', '30 ug', '<=13', '14 - 18', '>= 19', 45, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '137')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('137', 'Pipemidic Acid', '20 ug', '<=13', '14 - 18', '>= 19', 46, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '138')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('138', 'Bacitracin', '10 ug', '<=8', '9 - 12', '>= 13', 47, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '139')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('139', 'Polymyxin B', '300 ug', '<=8', '9 - 12', '>= 13', 48, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '140')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('140', 'Metronidazole', '5 ug', '<=15', '16 - 20', '>= 21', 49, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '141')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('141', 'Ciprofloxacin', '5 ug', '<=15', '16 - 20', '>= 21', 50, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '142')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('142', 'Pefloxacin', '5 ug', '<=16', '17 - 21', '>= 22', 51, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '143')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('143', 'Ampicillin + Sulbactam', '20 ug', '<=11', '12 - 14', '>= 15', 52, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '144')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('144', 'Cefadroxil', '30 ug', '<=11', '12 - 18', '>= 19', 53, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '145')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('145', 'Cefsulodin', '30 ug', '<=11', '12 - 18', '>= 19', 54, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '146')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('146', 'Aztreonam', '30 ug', '<=15', '16 - 21', '>= 22', 55, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '147')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('147', 'Dibekacin', '30 ug', '<=15', '16 - 21', '>= 22', 56, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '148')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('148', 'Ofloxacin', '5 ug', '<=12', '13 - 14', '>= 15', 57, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '149')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('149', 'Ticarcillin + Clavulanic Acid', '85 ug', '<=11', '12 - 14', '>= 15', 58, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '150')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('150', 'Cefixime', '5 ug', '<=15', '16 - 18', '>= 19', 59, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '151')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('151', 'Clarithromycin', '15 ug', '<=13', '14 - 17', '>= 18', 60, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '152')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('152', 'Piperacillin', '100 ug', '<=14', '15 - 17', '>= 18', 61, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '153')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('153', 'Vancomycin', '30 ug', '<=9', '10 - 11', '>= 12', 62, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '154')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('154', 'Colistin', '10 ug', '<=8', '9 - 10', '>= 11', 63, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '155')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('155', 'Imipenem', '10 ug', '<=18', '19 - 21', '>= 22', 64, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '156')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('156', 'Meropenem', '10 ug', '<=11', '12 - 13', '>= 14', 65, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '157')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('157', 'Cefepime', '30 ug', '<=14', '15 - 17', '>= 18', 66, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '158')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('158', 'Piperacillin + Tazobactam', '110 ug', '<=14', '15 - 17', '>= 18', 67, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '159')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('159', 'Oxacillin', '1 ug', '<=10', '11 - 12', '>= 13', 68, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '160')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('160', 'Teicoplanin', '1 ug', '<=10', '11 - 12', '>= 13', 69, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '161')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('161', 'Cefoperazone + Sulbactam', '75 + 30 ug', '<=15', '16 - 20', '>= 21', 70, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '162')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('162', 'Moxifloxacin', '15 ug', '<=15', '16 - 18', '>= 19', 71, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '163')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('163', 'Levofloxacin', '5 ug', '<=13', '14 - 16', '>= 17', 72, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '164')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('164', 'Linezolid', '30 ug', '<=10', '11 - 12', '>= 13', 73, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '165')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('165', 'Norfloxacin', '30 ug', '<=10', '11 - 12', '>= 13', 74, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '166')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('166', 'Cefazolin', '30 ug', '<=14', '15 - 17', '>= 18', 75, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '167')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('167', 'Azithromycin', '15 ug', '<=13', '14 - 17', '>= 18', 76, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '168')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('168', 'Doxycycline', '30 ug', '<=12', '13 - 15', '>= 16', 77, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '169')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('169', 'Ertapenem', '10 ug', '<=18', '19 - 21', '>= 22', 78, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '170')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('170', 'Doripenem', '10 ug', '<=19', '20 - 22', '>= 23', 79, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '171')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('171', 'Tigecycline', '15 ug', '<=14', '15 - 18', '>= 19', 80, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '172')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('172', 'Fosfomycin', '200 ug', '<=12', '13 - 15', '>= 16', 81, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '173')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('173', 'Daptomycin', '30 ug', '<=11', '12 - 14', '>= 15', 82, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '174')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('174', 'Ceftazidime + Avibactam', '30/20 ug', '<=20', '-', '>= 21', 83, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_LabAntibiotic WHERE AntibioticId = '175')
BEGIN
    INSERT INTO BILRG_LabAntibiotic (AntibioticId, AntibioticName, DiskConcentration, ResistenRange, IntermediateRange, SensitifRange, DisplayOrder, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
    VALUES ('175', 'Ceftolozane + Tazobactam', '30/10 ug', '<=16', '17 - 20', '>= 21', 84, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
END;
GO
