CREATE TABLE ta_layanan
(
    fs_kd_layanan VARCHAR(26) NOT NULL CONSTRAINT DF_ta_layanan_fs_kd_layanan DEFAULT(''),
    fs_nm_layanan VARCHAR(128) NOT NULL CONSTRAINT DF_ta_layanan_fs_nm_layanan DEFAULT(''),
    fs_kd_jenis_lokasi VARCHAR(26) NOT NULL CONSTRAINT DF_ta_layanan_fs_kd_jenis_lokasi DEFAULT(''),
    CONSTRAINT PK_ta_layanan PRIMARY KEY CLUSTERED(fs_kd_layanan)
)
GO

ALTER TABLE ta_layanan
    ADD fs_kd_jenis_lokasi VARCHAR(3) NOT NULL CONSTRAINT DF_ta_layanan_fs_kd_jenis_lokasi DEFAULT('')
GO
