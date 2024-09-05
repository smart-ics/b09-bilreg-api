CREATE TABLE ta_transport2 (
   fs_kd_transport VARCHAR(3) NOT NULL CONSTRAINT DF_ta_transport2_fs_kd_transport DEFAULT (''),
   fs_kd_detil_tarif VARCHAR(3) NOT NULL CONSTRAINT DF_ta_transport2_fs_kd_detil_tarif DEFAULT (''),
   fn_tarif DECIMAL(20) NOT NULL CONSTRAINT DF_ta_transport2_fn_tarif DEFAULT (0),
   fb_tetap BIT NOT NULL CONSTRAINT DF_ta_transport2_fb_tetap DEFAULT (0),
)