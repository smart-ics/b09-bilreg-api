CREATE TABLE ta_trs_billing2
(
    fs_kd_trs VARCHAR(26) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_trs DEFAULT(''),
    fn_no_urut INT NOT NULL CONSTRAINT DF_ta_trs_billing2_fn_no_urut DEFAULT(0),
    fs_kd_jenis_bayar VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_jenis_bayar DEFAULT(''),
    fn_trs_p DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_billing2_fn_trs_p DEFAULT(0),
    fn_trs_n DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_billing2_fn_trs_n DEFAULT(0),
    
    fs_kd_trs_bayar VARCHAR(26) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_trs_bayar DEFAULT(''),
    fd_tgl_bayar VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_billing2_fd_tgl_bayar DEFAULT('3000-01-01'),
    fs_jam_bayar VARCHAR(8) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_jam_bayar DEFAULT('00:00:00'),
    fs_kd_petugas_kasir VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_petugas_kasir DEFAULT(''),
    
    fs_kd_petugas_medis VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_petugas_medis DEFAULT(''),
    fs_kd_detil_tarif VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_detil_tarif DEFAULT(''),
    fs_kd_grup_rek VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_grup_rek DEFAULT(''),
    
    fs_kd_rek_ppdp VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_rek_ppdp DEFAULT(''),
    fs_kd_rek_pdpt VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_rek_pdpt DEFAULT(''),
    fs_kd_rek_disc VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_rek_disc DEFAULT(''),
    
    fs_kd_rek_pdpt_lain VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_rek_pdpt_lain DEFAULT(''),
    fs_kd_rek_persediaan VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_rek_persediaan DEFAULT(''),
    fs_kd_rek_tax VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_rek_tax DEFAULT(''),
    fs_kd_rek_retur VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_billing2_fs_kd_rek_retur DEFAULT(''),

    CONSTRAINT PK_ta_trs_billing2 PRIMARY KEY CLUSTERED(fs_kd_trs, fn_no_urut)
)