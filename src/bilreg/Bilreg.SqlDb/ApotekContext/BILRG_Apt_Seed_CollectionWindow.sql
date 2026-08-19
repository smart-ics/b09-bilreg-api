-- Collection Window default (BC-01). Idempotent.

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM tz_parameter_sistem WHERE fs_kd_parameter = 'APT_COLLECTION_WINDOW_DAYS')
BEGIN
    INSERT INTO tz_parameter_sistem (fs_kd_parameter, fs_nm_parameter, fs_value)
    VALUES ('APT_COLLECTION_WINDOW_DAYS', 'Outpatient pharmacy collection window (days)', '7');
END;
