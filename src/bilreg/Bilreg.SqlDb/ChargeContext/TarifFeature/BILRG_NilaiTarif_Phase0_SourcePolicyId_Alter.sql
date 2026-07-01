-- Phase 0: optional publish traceability on operational projection (empty for import-only rows).

--IF COL_LENGTH('BILRG_NilaiTarif', 'SourcePolicyId') IS NULL
--BEGIN
--    ALTER TABLE BILRG_NilaiTarif ADD
--        SourcePolicyId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_NilaiTarif_SourcePolicyId DEFAULT('');
--END
--GO
