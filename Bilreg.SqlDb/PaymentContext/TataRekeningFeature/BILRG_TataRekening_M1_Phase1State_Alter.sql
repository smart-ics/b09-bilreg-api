IF COL_LENGTH('BILRG_TataRekening', 'FinVerifStatus') IS NULL
BEGIN
    ALTER TABLE BILRG_TataRekening
        ADD FinVerifStatus INT NOT NULL
            CONSTRAINT DF_BILRG_TataRekening_FinVerifStatus DEFAULT(0),
            FinVerifPetugas VARCHAR(10) NOT NULL
            CONSTRAINT DF_BILRG_TataRekening_FinVerifPetugas DEFAULT(''),
            FinVerifDate DATETIME NOT NULL
            CONSTRAINT DF_BILRG_TataRekening_FinVerifDate DEFAULT('3000-01-01'),
            IsAllocated BIT NOT NULL
            CONSTRAINT DF_BILRG_TataRekening_IsAllocated DEFAULT(0),
            SettlementInitiated BIT NOT NULL
            CONSTRAINT DF_BILRG_TataRekening_SettlementInitiated DEFAULT(0),
            Version INT NOT NULL
            CONSTRAINT DF_BILRG_TataRekening_Version DEFAULT(1);
END
GO
