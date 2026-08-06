CREATE TABLE BILRG_AdmOpnameRequestInsurance
    (
        OpnameRequestId     VARCHAR(12)  NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequestInsurance_OpnameRequestId DEFAULT(''),
        TipeJaminanId       VARCHAR(5)   NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequestInsurance_TipeJaminanId DEFAULT('-'),
        TipeJaminanName     VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequestInsurance_TipeJaminanName DEFAULT('-'),
        ReffId              VARCHAR(12)  NOT NULL CONSTRAINT DF_BILRG_AdmOpnameRequestInsurance_ReffId DEFAULT('-'),
        
        CONSTRAINT PK_BILRG_AdmOpnameRequestInsurance PRIMARY KEY CLUSTERED (OpnameRequestId)
    );