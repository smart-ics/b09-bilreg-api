CREATE TABLE BILRG_TataRekening
(
    RegId           VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TataRekening_RegId DEFAULT(''),
    Status          INT NOT NULL CONSTRAINT DF_BILRG_TataRekening_Status DEFAULT(0),
    PetugasVerif    VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TataRekening_PetugasVerif DEFAULT(''),
    DischargeDate   DATETIME NOT NULL CONSTRAINT DF_BILRG_TataRekening_DischargeDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_TataRekening PRIMARY KEY CLUSTERED(RegId)
)


