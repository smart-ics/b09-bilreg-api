CREATE TABLE BILRG_NilaiTarifKomponen
(
    NilaiTarifId   VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_NilaiTarifKomponen_NilaiTarifId DEFAULT(''),
    NoUrut         INT NOT NULL CONSTRAINT DF_BILRG_NilaiTarifKomponen_NoUrut DEFAULT(0),
    KomponenId     VARCHAR(3) NOT NULL CONSTRAINT DF_BILRG_NilaiTarifKomponen_KomponenId DEFAULT(''),
    Nilai          DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_NilaiTarifKomponen_Nilai DEFAULT(0),
    
    CONSTRAINT PK_BILRG_NilaiTarifKomponen PRIMARY KEY CLUSTERED(NilaiTarifId, NoUrut)
)