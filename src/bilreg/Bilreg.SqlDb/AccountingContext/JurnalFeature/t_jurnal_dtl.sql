CREATE TABLE t_jurnal_dtl
(
    fs_kd_jurnal VARCHAR(26) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_kd_jurnal DEFAULT(''),
    fn_urut INT NOT NULL CONSTRAINT DF_t_jurnal_dtl_fn_urut DEFAULT(0),
    fs_kd_rek VARCHAR(20) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_kd_rek DEFAULT(''),
    fs_uraian VARCHAR(256) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_uraian DEFAULT(''),
    fn_jurnald DECIMAL(18,2) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fn_jurnald DEFAULT(0),
    fn_jurnalk DECIMAL(18,2) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fn_jurnalk DEFAULT(0),

    fs_kd_unit VARCHAR(10) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_kd_unit DEFAULT(''),
    fs_kd_jk VARCHAR(3) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_kd_jk DEFAULT(''),

    fs_string00 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string00 DEFAULT(''),
    fs_string01 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string01 DEFAULT(''),
    fs_string02 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string02 DEFAULT(''),
    fs_string03 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string03 DEFAULT(''),
    fs_string04 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string04 DEFAULT(''),
    fs_string05 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string05 DEFAULT(''),
    fs_string06 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string06 DEFAULT(''),
    fs_string07 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string07 DEFAULT(''),
    fs_string08 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string08 DEFAULT(''),
    fs_string09 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string09 DEFAULT(''),
    fs_string10 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string10 DEFAULT(''),
    fs_string11 VARCHAR(40) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fs_string11 DEFAULT(''),

    fn_nilai_jasa DECIMAL(18,2) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fn_nilai_jasa DEFAULT(0),
    fn_nilai_obat DECIMAL(18,2) NOT NULL CONSTRAINT DF_t_jurnal_dtl_fn_nilai_obat DEFAULT(0),

    CONSTRAINT PK_t_jurnal_dtl PRIMARY KEY CLUSTERED(fs_kd_jurnal, fn_urut),
)