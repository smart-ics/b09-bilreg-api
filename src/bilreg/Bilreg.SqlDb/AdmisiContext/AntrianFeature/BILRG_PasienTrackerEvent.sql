CREATE TABLE BILRG_PasienTrackerEvent(
    PasienTrackerId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_PasienTrackerEvent_PasienTrackerId DEFAULT(''),
    NoUrut INT NOT NULL CONSTRAINT DF_BILRG_PasienTrackerEvent_NoUrut DEFAULT(0),
    EventName VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_PasienTrackerEvent_EventName DEFAULT(''),
    EventDate DATETIME NOT NULL CONSTRAINT DF_BILRG_PasienTrackerEvent_EventDate DEFAULT('3000-01-01'),
    ReffId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_PasienTrackerEvent_ReffId DEFAULT(''),
    
    CONSTRAINT PK_BILRG_PasienTrackerEvent PRIMARY KEY CLUSTERED (PasienTrackerId, NoUrut)
)
