CREATE TABLE tb_satuan
(
    fs_kd_satuan      VARCHAR(3)  NOT NULL CONSTRAINT DF_tb_satuan_fs_kd_satuan DEFAULT(''),
    fs_nm_satuan      VARCHAR(10) NOT NULL CONSTRAINT DF_tb_satuan_fs_nm_satuan DEFAULT(''),
    fb_satuan_racik   BIT         NOT NULL CONSTRAINT DF_tb_satuan_fb_satuan_racik DEFAULT(0),
    CONSTRAINT PK_tb_satuan PRIMARY KEY CLUSTERED (fs_kd_satuan)
)