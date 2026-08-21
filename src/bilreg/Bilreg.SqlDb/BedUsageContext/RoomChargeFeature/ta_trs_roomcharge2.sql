CREATE TABLE ta_trs_roomcharge2
(
    fs_kd_trs          VARCHAR(15)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge2_fs_kd_trs DEFAULT(''),
    fs_kd_detil_tarif  VARCHAR(3)    NOT NULL CONSTRAINT DF_ta_trs_roomcharge2_fs_kd_detil_tarif DEFAULT(''),
    fn_tarif           DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_roomcharge2_fn_tarif DEFAULT(0),
    fn_diskon          DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_roomcharge2_fn_diskon DEFAULT(0),
    fn_total           DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_roomcharge2_fn_total DEFAULT(0),
    fs_kd_rek_pdpt     VARCHAR(20)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge2_fs_kd_rek_pdpt DEFAULT(''),
    fs_kd_rek_diskon   VARCHAR(20)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge2_fs_kd_rek_diskon DEFAULT(''),

    CONSTRAINT PK_ta_trs_roomcharge2 PRIMARY KEY CLUSTERED(fs_kd_trs, fs_kd_detil_tarif)
)