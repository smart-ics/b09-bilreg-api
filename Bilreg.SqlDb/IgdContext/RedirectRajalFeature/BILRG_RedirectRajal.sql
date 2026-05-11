CREATE TABLE BILRG_RedirectRajal (
    RedirectRajalId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_RedirectRajal_RedirectRajalId DEFAULT(''),
    IgdVisitId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_RedirectRajal_IgdVisitId DEFAULT(''),
    VisitorName VARCHAR(60) NOT NULL CONSTRAINT DF_BILRG_RedirectRajal_VisitorName DEFAULT(''),
    RedirectDateTime DATETIME NOT NULL CONSTRAINT DF_BILRG_RedirectRajal_RedirectDateTime DEFAULT('3000-01-01'),
    Reason VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_RedirectRajal_Reason DEFAULT(''),
    RedirectUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_RedirectRajal_RedirectUserId DEFAULT(''),

    CONSTRAINT PK_BILRG_RedirectRajal PRIMARY KEY CLUSTERED (RedirectRajalId)
);
GO

CREATE INDEX IX_BILRG_RedirectRajal_Visit ON BILRG_RedirectRajal(IgdVisitId);
GO
