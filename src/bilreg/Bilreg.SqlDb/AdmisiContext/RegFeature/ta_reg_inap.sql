CREATE TABLE ta_reg_inap
(
    fs_kd_reg             VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_inap_fs_kd_reg DEFAULT(' '),
    fs_kd_caramasuk_inap  VARCHAR(3)  NOT NULL CONSTRAINT DF_ta_reg_inap_fs_kd_caramasuk_inap DEFAULT(' '),
    fs_kd_trs_booking_bed VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_inap_fs_kd_trs_booking_bed DEFAULT(' '),
    fs_kd_medis_sekunder  VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_inap_fs_kd_medis_sekunder DEFAULT(' ')
)
