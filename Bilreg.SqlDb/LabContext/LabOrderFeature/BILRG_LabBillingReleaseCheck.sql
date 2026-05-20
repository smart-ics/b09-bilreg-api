CREATE TABLE BILRG_LabBillingReleaseCheck (
    CheckId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabBillingReleaseCheck_CheckId DEFAULT(''),
    OrderId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabBillingReleaseCheck_OrderId DEFAULT(''),
    BillingStatus INT NOT NULL CONSTRAINT DF_BILRG_LabBillingReleaseCheck_BillingStatus DEFAULT(0),
    Message VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_LabBillingReleaseCheck_Message DEFAULT(''),
    RequestedByUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabBillingReleaseCheck_RequestedByUserId DEFAULT(''),
    CheckedAt DATETIME NOT NULL CONSTRAINT DF_BILRG_LabBillingReleaseCheck_CheckedAt DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_LabBillingReleaseCheck PRIMARY KEY CLUSTERED (CheckId)
);
GO

CREATE INDEX IX_BILRG_LabBillingReleaseCheck_Order ON BILRG_LabBillingReleaseCheck(OrderId, CheckedAt DESC);
GO
