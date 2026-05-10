CREATE TABLE BILRG_IgdVisitTriage (
    IgdVisitId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_IgdVisitId DEFAULT(''),
    NoTriage INT NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_NoTriage DEFAULT(0),

    TriageLevel VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_TriageLevel DEFAULT(''),
    AssessmentDateTime DATETIME NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_AssessmentDateTime DEFAULT('3000-01-01'),
    AssessorUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_AssessorUserId DEFAULT(''),
    Notes VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_Notes DEFAULT(''),

    CONSTRAINT PK_BILRG_IgdVisitTriage PRIMARY KEY CLUSTERED (IgdVisitId, NoTriage)
);
GO
