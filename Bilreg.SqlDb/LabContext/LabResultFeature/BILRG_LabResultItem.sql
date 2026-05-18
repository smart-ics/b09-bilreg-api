CREATE TABLE BILRG_LabResultItem (
    ResultDocumentId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabResultItem_ResultDocumentId DEFAULT(''),
    ItemNo INT NOT NULL CONSTRAINT DF_BILRG_LabResultItem_ItemNo DEFAULT(0),

    TestId VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_LabResultItem_TestId DEFAULT(''),
    TestName VARCHAR(120) NOT NULL CONSTRAINT DF_BILRG_LabResultItem_TestName DEFAULT(''),

    ComponentCode VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_LabResultItem_ComponentCode DEFAULT(''),
    ComponentName VARCHAR(120) NOT NULL CONSTRAINT DF_BILRG_LabResultItem_ComponentName DEFAULT(''),

    ResultType INT NOT NULL CONSTRAINT DF_BILRG_LabResultItem_ResultType DEFAULT(1),

    NumericValue DECIMAL(18, 6) NOT NULL CONSTRAINT DF_BILRG_LabResultItem_NumericValue DEFAULT(0),
    TextValue VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_LabResultItem_TextValue DEFAULT(''),
    OptionValue VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_LabResultItem_OptionValue DEFAULT(''),
    NarrativeValue VARCHAR(4000) NOT NULL CONSTRAINT DF_BILRG_LabResultItem_NarrativeValue DEFAULT(''),

    Unit VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_LabResultItem_Unit DEFAULT(''),
    ReferenceRangeText VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_LabResultItem_ReferenceRangeText DEFAULT(''),

    FlagStatus INT NOT NULL CONSTRAINT DF_BILRG_LabResultItem_FlagStatus DEFAULT(1),

    CONSTRAINT PK_BILRG_LabResultItem PRIMARY KEY CLUSTERED (ResultDocumentId, ItemNo)
);
GO

CREATE INDEX IX_BILRG_LabResultItem_ResultDocumentId ON BILRG_LabResultItem(ResultDocumentId);
GO
