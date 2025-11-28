CREATE TABLE td_peg(
    fs_kd_peg VARCHAR(10) NOT NULL CONSTRAINT DF_td_peg_fs_kd_peg DEFAULT(''),
    fs_nm_peg VARCHAR(45) NOT NULL CONSTRAINT DF_td_peg_fs_nm_peg DEFAULT(''),
    fs_nm_alias VARCHAR(40) NOT NULL CONSTRAINT DF_td_peg_fs_nm_alias DEFAULT(''),
    fs_kd_smf VARCHAR(3) NOT NULL CONSTRAINT DF_td_peg_fs_kd_smf DEFAULT(''),
 
    CONSTRAINT PK_td_peg PRIMARY KEY CLUSTERED(fs_kd_peg)   
)
