CREATE TABLE ta_kamar (
   fs_kd_kamar VARCHAR(5) NOT NULL CONSTRAINT DF_ta_kamar_fs_kd_kamar DEFAULT(''),
   fs_nm_kamar VARCHAR(30) NOT NULL CONSTRAINT DF_ta_kamar_fs_nm_kamar DEFAULT(''),
   fs_ket1 VARCHAR(50) NOT NULL CONSTRAINT DF_ta_kamar_fs_ket1 DEFAULT(''),
   fs_ket2 VARCHAR(50) NOT NULL CONSTRAINT DF_ta_kamar_fs_ket2 DEFAULT(''),
   fs_ket3 VARCHAR(50) NOT NULL CONSTRAINT DF_ta_kamar_fs_ket3 DEFAULT(''),
   fn_jumlah DECIMAL(12,0) NOT NULL CONSTRAINT DF_ta_kamar_fn_jumlah DEFAULT(0),
   fn_pakai DECIMAL(12,0) NOT NULL CONSTRAINT DF_ta_kamar_fn_pakai DEFAULT(0),
   fn_kotor DECIMAL(20,0) NOT NULL CONSTRAINT DF_ta_kamar_fn_kotor DEFAULT(0),
   fn_rusak DECIMAL(20,0) NOT NULL CONSTRAINT DF_ta_kamar_fn_rusak DEFAULT(0),
   fs_kd_bangsal VARCHAR(2) NOT NULL CONSTRAINT DF_ta_kamar_fs_kd_bangsal DEFAULT(0),
   fs_kd_kelas VARCHAR(2) NOT NULL CONSTRAINT DF_ta_kamar_fs_kd_kelas DEFAULT(0)

    CONSTRAINT PK_ta_kamar PRIMARY KEY CLUSTERED(fs_kd_kamar)
);