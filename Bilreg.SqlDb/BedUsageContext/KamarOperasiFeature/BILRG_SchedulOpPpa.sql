CREATE TABLE BILRG_ScheduleOpPpa
(
    ScheduleOpId      VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_ScheduleOpPpa_ScheduleOpId DEFAULT(''),
    NoUrut            INT NOT NULL CONSTRAINT DF_BILRG_ScheduleOpPpa_NoUrut DEFAULT(0),
    PpaId             VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_ScheduleOpPpa_PpaId DEFAULT(''),
    ProfesiId         VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_ScheduleOpPpa_ProfesiId DEFAULT(''),
    GroupSpesialisId  VARCHAR(3) NOT NULL CONSTRAINT DF_BILRG_ScheduleOpPpa_GroupSpesialisId DEFAULT(''),

    CONSTRAINT PK_BILRG_ScheduleOpPpa PRIMARY KEY CLUSTERED(ScheduleOpId, NoUrut)
)