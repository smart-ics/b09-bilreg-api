CREATE TABLE ta_kamar (
   fs_kd_kamar VARCHAR(5) NOT NULL CONSTRAINT DF_ta_kamar_fs_kd_kamar DEFAULT(''),
   fs_nm_kamar VARCHAR(30) NOT NULL CONSTRAINT DF_ta_kamar_fs_nm_kamar DEFAULT(''),
   fs_kd_bangsal VARCHAR(2) NOT NULL CONSTRAINT DF_ta_kamar_fs_kd_bangsal DEFAULT(0),
   fs_kd_kelas VARCHAR(2) NOT NULL CONSTRAINT DF_ta_kamar_fs_kd_kelas DEFAULT(0)

    CONSTRAINT PK_ta_kamar PRIMARY KEY CLUSTERED(fs_kd_kamar)
);