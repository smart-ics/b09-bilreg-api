CREATE TABLE BILRG_AdmBookingAssistance
(
    BookingId VARCHAR(26) NOT NULL,
    AssistanceCorrelation VARCHAR(80) NOT NULL,
    AntrianId VARCHAR(26) NOT NULL,
    NoUrut INT NOT NULL,
    IsActive BIT NOT NULL,
    FailureCode VARCHAR(50) NOT NULL,
    KioskId VARCHAR(50) NOT NULL,
    CrtUser VARCHAR(50) NOT NULL,
    CrtDate DATETIME NOT NULL,
    UpdUser VARCHAR(50) NOT NULL,
    UpdDate DATETIME NOT NULL,
    VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmBookingAssistance_VodUser DEFAULT(''),
    VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AdmBookingAssistance_VodDate DEFAULT('3000-01-01'),
    CONSTRAINT PK_BILRG_AdmBookingAssistance PRIMARY KEY CLUSTERED(BookingId),
    CONSTRAINT UX_BILRG_AdmBookingAssistance_Correlation UNIQUE(AssistanceCorrelation),
    CONSTRAINT CK_BILRG_AdmBookingAssistance_Entry CHECK(AntrianId<>'' AND NoUrut>0)
);
CREATE INDEX IX_BILRG_AdmBookingAssistance_Active ON BILRG_AdmBookingAssistance(IsActive,BookingId)
    INCLUDE(AntrianId,NoUrut,FailureCode,KioskId);
