CREATE TABLE ta_smf(
    fs_kd_smf VARCHAR(3) NOT NULL CONSTRAINT DF_ta_smf_fs_kd_smf DEFAULT(''),
    fs_nm_smf VARCHAR(30) NOT NULL CONSTRAINT DF_ta_smf_fs_nm_smf DEFAULT(''),

    CONSTRAINT PK_ta_smf PRIMARY KEY CLUSTERED(fs_kd_smf)
)
