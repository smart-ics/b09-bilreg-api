CREATE TABLE ta_trs_bed(
   fs_kd_trs VARCHAR (15) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_kd_trs DEFAULT(''),
   fd_tgl_in VARCHAR (10) NOT NULL CONSTRAINT DF_ta_trs_bed_fd_tgl_in DEFAULT('3000-01-01'),
   fs_jam_in VARCHAR (8) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_jam_in DEFAULT('00:00:00'),
   fs_kd_petugas VARCHAR (10) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_kd_petugas DEFAULT(''),
   fs_kd_reg VARCHAR (10) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_kd_reg DEFAULT(''),
   fs_kd_layanan VARCHAR (5) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_kd_layanan DEFAULT(''),
   fs_kd_layanan_dk VARCHAR (2) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_kd_layanan_dk DEFAULT(''),
   fs_kd_kamar_tipe VARCHAR (1) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_kd_kamar_tipe DEFAULT(''),
   fs_kd_bed VARCHAR (8) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_kd_bed DEFAULT(''),
   fn_tarif DECIMAL (18, 0) NOT NULL CONSTRAINT DF_ta_trs_bed_fn_tarif DEFAULT(0),
   fd_tgl_out VARCHAR (10) NOT NULL CONSTRAINT DF_ta_trs_bed_fd_tgl_out DEFAULT('3000-01-01'),
   fs_jam_out VARCHAR (8) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_jam_out DEFAULT('00:00:00'),
   fs_kd_petugas_out VARCHAR (10) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_kd_petugas_out DEFAULT(''),
   fd_tgl_void VARCHAR (10) NOT NULL CONSTRAINT DF_ta_trs_bed_fd_tgl_void DEFAULT('3000-01-01'),
   fs_jam_void VARCHAR (8) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_jam_void DEFAULT('00:00:00'),
   fs_kd_petugas_void VARCHAR (10) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_kd_petugas_void DEFAULT(''),
   fd_tgl_entry VARCHAR (10) NOT NULL CONSTRAINT DF_ta_trs_bed_fd_tgl_entry DEFAULT('3000-01-01'),
   fs_jam_entry VARCHAR (8) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_jam_entry DEFAULT('00:00:00'),
   fs_kd_kelas VARCHAR (3) NOT NULL CONSTRAINT DF_ta_trs_bed_fs_kd_kelas DEFAULT(''),

   CONSTRAINT PK_ta_trs_bed PRIMARY KEY CLUSTERED(fs_kd_trs)
)