-- =========================================================================================
-- Script Name : Index_BILRG_OpCaseAktif_State_Date.sql
-- Description : Idempotent migration script for composite non-clustered index on BILRG_OpCaseAktif
--               to optimize queue queries filtered by OpCaseState (e.g. Requested) and sorted by OrderOpDate.
-- Reference   : TD-03, PLAN-SURGERY-ORDER-POOL-001 (P1-S01), ISSUE-SURGERY-ORDER-POOL-001
-- =========================================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_BILRG_OpCaseAktif_State_Date' 
      AND object_id = OBJECT_ID('BILRG_OpCaseAktif')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_OpCaseAktif_State_Date 
    ON BILRG_OpCaseAktif(OpCaseState, OrderOpDate) 
    INCLUDE (PasienId);
END
GO
