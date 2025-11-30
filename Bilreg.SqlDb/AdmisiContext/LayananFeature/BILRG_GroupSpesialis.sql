CREATE TABLE BILRG_GroupSpesialis(
    GroupSpesialisId VARCHAR(3) NOT NULL CONSTRAINT DF_BILRG_GroupSpesialis_GroupSpesialisId DEFAULT(''),
    GroupSpesialisName VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_GroupSpesialis_GroupSpesialisName DEFAULT('')
    
    CONSTRAINT PK_BILRG_GroupSpesialis PRIMARY KEY CLUSTERED(GroupSpesialisId)
)