CREATE TABLE BILRG_AptFinalReview (
    DispensingId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AptFinalReview_DispensingId DEFAULT(''),
    ReviewNo INT NOT NULL CONSTRAINT DF_BILRG_AptFinalReview_ReviewNo DEFAULT(0),
    Outcome INT NOT NULL CONSTRAINT DF_BILRG_AptFinalReview_Outcome DEFAULT(0),
    Reason VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_AptFinalReview_Reason DEFAULT(''),
    PharmacistId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptFinalReview_PharmacistId DEFAULT(''),
    EffectiveAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptFinalReview_EffectiveAt DEFAULT('3000-01-01'),
    AffectedQty DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_AptFinalReview_AffectedQty DEFAULT(0),
    CONSTRAINT PK_BILRG_AptFinalReview PRIMARY KEY CLUSTERED (DispensingId, ReviewNo)
);
GO
