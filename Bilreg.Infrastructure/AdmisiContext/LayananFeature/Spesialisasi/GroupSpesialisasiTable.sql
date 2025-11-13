CREATE TABLE BILRG_GroupSpesialisasi(
    GroupSpesialisasiId VARCHAR(3) NOT NULL CONSTRAINT DF_BILRG_GroupSpesialisasi_GroupSpesialisasiId DEFAULT(''),
    GroupSpesialisasiName VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_GroupSpesialisasi_GroupSpesialisasiName DEFAULT('')
    
    CONSTRAINT PK_BILRG_GroupSpesialisasi PRIMARY KEY CLUSTERED(GroupSpesialisasiId)
)