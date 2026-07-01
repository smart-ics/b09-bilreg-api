CREATE TABLE ta_reg_trs_ref(
    fs_kd_reg VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_trs_ref_fs_kd_reg DEFAULT(''),
    fs_kd_trs_bl_admin VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_trs_ref_fs_kd_trs_bl_admin DEFAULT(''),
    fn_bl_admin DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_reg_trs_ref_fn_bl_admin DEFAULT(0),
    fs_kd_trs_bl_materai VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_trs_ref_fs_kd_trs_bl_materai DEFAULT(''),
    fn_bl_materai DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_reg_trs_ref_fn_bl_materai DEFAULT(0),
    fs_kd_trs_bl_bulat_jasa VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_trs_ref_fs_kd_trs_bl_bulat_jasa DEFAULT(''),
    fn_bl_bulat_jasa DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_reg_trs_ref_fn_bl_bulat_jasa DEFAULT(0),
    fs_kd_trs_bl_bulat_obat VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_trs_ref_fs_kd_trs_bl_bulat_obat DEFAULT(''),
    fn_bl_bulat_obat DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_reg_trs_ref_fn_bl_bulat_obat DEFAULT(0),
    
    CONSTRAINT PK_ta_reg_trs_ref PRIMARY KEY CLUSTERED(fs_kd_reg)
)
GO