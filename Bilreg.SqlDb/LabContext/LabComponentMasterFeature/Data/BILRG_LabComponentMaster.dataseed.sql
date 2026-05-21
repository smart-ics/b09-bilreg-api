DELETE FROM BILRG_LabComponentMaster;
GO

INSERT INTO BILRG_LabComponentMaster (
    ComponentId, LoincCode, ComponentCode, ComponentName, ComponentNameIndonesia, ResultType, DefaultUnit,
    IsSystem, IsActive, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
VALUES
    ('MLC0001', '718-7', 'HB', 'Hemoglobin', 'Hemoglobin', 1, 'g/dL', 1, 1, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01'),
    ('MLC0002', '6690-2', 'WBC', 'Leukocyte', 'Leukosit', 1, '10^3/uL', 1, 1, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01'),
    ('MLC0003', '789-8', 'RBC', 'Erythrocyte', 'Eritrosit', 1, '10^6/uL', 1, 1, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01'),
    ('MLC0004', '777-3', 'PLT', 'Platelet', 'Trombosit', 1, '10^3/uL', 1, 1, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01'),
    ('MLC0005', '2345-7', 'GLU', 'Glucose', 'Glukosa Darah', 1, 'mg/dL', 1, 1, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01'),
    ('MLC0006', '3094-0', 'UREA', 'Urea', 'Urea', 1, 'mg/dL', 1, 1, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01'),
    ('MLC0007', '2160-0', 'CREA', 'Creatinine', 'Kreatinin', 1, 'mg/dL', 1, 1, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01'),
    ('MLC0008', '1742-6', 'ALT', 'Alanine Aminotransferase', 'Alanin Aminotransferase', 1, 'U/L', 1, 1, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01'),
    ('MLC0009', '1920-8', 'AST', 'Aspartate Aminotransferase', 'Aspartat Aminotransferase', 1, 'U/L', 1, 1, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01'),
    ('MLC000A', '2093-3', 'CHOL', 'Total Cholesterol', 'Kolesterol Total', 1, 'mg/dL', 1, 1, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01'),
    ('MLC000B', NULL, 'URINE_PH', 'Urine pH', 'pH Urin', 1, '', 1, 1, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01'),
    ('MLC000C', NULL, 'REMARK', 'Remark', 'Catatan', 4, '', 1, 0, 'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01');
GO
