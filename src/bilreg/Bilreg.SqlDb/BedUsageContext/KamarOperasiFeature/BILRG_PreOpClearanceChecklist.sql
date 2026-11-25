CREATE TABLE BILRG_PreOpClearanceChecklist
(
    ClearanceId   VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_PreOpClearanceChecklist_ClearanceId   DEFAULT(''),
    NoUrut        INT          NOT NULL CONSTRAINT DF_BILRG_PreOpClearanceChecklist_NoUrut        DEFAULT(0),
    ChecklistKey  VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_PreOpClearanceChecklist_ChecklistKey  DEFAULT(''),
    Status        INT          NOT NULL CONSTRAINT DF_BILRG_PreOpClearanceChecklist_Status        DEFAULT(0),
    Notes         VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_PreOpClearanceChecklist_Notes         DEFAULT(''),

    CONSTRAINT PK_BILRG_PreOpClearanceChecklist PRIMARY KEY CLUSTERED (ClearanceId, NoUrut)
)
