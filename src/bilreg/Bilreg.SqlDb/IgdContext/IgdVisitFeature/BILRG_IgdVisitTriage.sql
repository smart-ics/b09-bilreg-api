CREATE TABLE BILRG_IgdVisitTriage (
    IgdVisitId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_IgdVisitId DEFAULT(''),
    NoTriage INT NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_NoTriage DEFAULT(0),

    TriageMethod VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_TriageMethod DEFAULT(''),
    TriageLevel VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_TriageLevel DEFAULT(''),
    TriageColor VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_TriageColor DEFAULT(''),
    AirwaysScore INT NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_AirwaysScore DEFAULT(0),
    BreathingScore INT NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_BreathingScore DEFAULT(0),
    BloodCirculationScore INT NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_BloodCirculationScore DEFAULT(0),
    GcsEyeScore INT NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_GcsEyeScore DEFAULT(0),
    GcsMotorScore INT NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_GcsMotorScore DEFAULT(0),
    GcsVoiceScore INT NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_GcsVoiceScore DEFAULT(0),
    IsManualOverrideBlack BIT NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_IsManualOverrideBlack DEFAULT(0),
    OverrideByUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_OverrideByUserId DEFAULT(''),
    OverrideReason VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_OverrideReason DEFAULT(''),
    OverrideDateTime DATETIME NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_OverrideDateTime DEFAULT('3000-01-01'),
    AssessmentDateTime DATETIME NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_AssessmentDateTime DEFAULT('3000-01-01'),
    AssessorUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_AssessorUserId DEFAULT(''),
    Notes VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_IgdVisitTriage_Notes DEFAULT(''),

    CONSTRAINT PK_BILRG_IgdVisitTriage PRIMARY KEY CLUSTERED (IgdVisitId, NoTriage)
);
GO
CREATE INDEX IX_BILRG_IgdVisitTriage_AssessmentDateTime
    ON BILRG_IgdVisitTriage(IgdVisitId, AssessmentDateTime DESC);
GO
