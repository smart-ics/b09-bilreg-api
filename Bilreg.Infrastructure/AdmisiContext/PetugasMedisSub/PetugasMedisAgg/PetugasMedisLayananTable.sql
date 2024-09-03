CREATE TABLE td_peg_layanan(
    fs_kd_peg VARCHAR(10) NOT NULL CONSTRAINT DF_td_peg_layanan_fs_kd_peg DEFAULT(''),
    fs_kd_layanan VARCHAR(5) NOT NULL CONSTRAINT DF_td_peg_layanan_fs_kd_layanan DEFAULT(''),
    fb_utama BIT NOT NULL CONSTRAINT DF_td_peg_layanan_fb_utama DEFAULT(0),
 
    CONSTRAINT PK_td_peg_layanan PRIMARY KEY CLUSTERED(fs_kd_peg, fs_kd_layanan)
)