SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

CREATE TABLE BILRG_AdmKioskServicePoint
(
    StationId      VARCHAR(50) NOT NULL,
    ServicePointId VARCHAR(50) NOT NULL,
    SortOrder      INT         NOT NULL CONSTRAINT DF_BILRG_AdmKioskServicePoint_SortOrder DEFAULT(0),
    CONSTRAINT PK_BILRG_AdmKioskServicePoint PRIMARY KEY CLUSTERED (StationId, ServicePointId),
    CONSTRAINT FK_BILRG_AdmKioskServicePoint_Kiosk FOREIGN KEY (StationId)
        REFERENCES BILRG_AdmQueueKiosk(StationId),
    CONSTRAINT FK_BILRG_AdmKioskServicePoint_ServicePoint FOREIGN KEY (ServicePointId)
        REFERENCES BILRG_AdmServicePoint(ServicePointId)
);

CREATE INDEX IX_BILRG_AdmKioskServicePoint_ServicePointId
    ON BILRG_AdmKioskServicePoint(ServicePointId);
