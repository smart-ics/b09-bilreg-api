CREATE TABLE ta_bed (
    fs_kd_bed VARCHAR(8) NOT NULL CONSTRAINT DF_ta_bed_fs_kd_bed DEFAULT(''),
    fs_nm_bed VARCHAR(30) NOT NULL CONSTRAINT DF_ta_bed_fs_nm_bed DEFAULT(''),
    fs_kd_kamar VARCHAR(5) NOT NULL CONSTRAINT DF_ta_bed_fs_kd_kamar DEFAULT(''),
    fs_keterangan VARCHAR(60) NOT NULL CONSTRAINT DF_ta_bed_fs_keterangan DEFAULT(''),
    fb_aktif BIT NOT NULL CONSTRAINT DF_ta_bed_fb_aktif DEFAULT(0),
      
    CONSTRAINT PK_ta_bed PRIMARY KEY CLUSTERED (fs_kd_bed)
);