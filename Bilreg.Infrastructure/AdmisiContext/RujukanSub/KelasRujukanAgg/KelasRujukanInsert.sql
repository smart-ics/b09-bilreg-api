INSERT INTO tc_kelas_rs (fs_kd_kelas, fs_nm_kelas, fn_nilai)
SELECT '1', 'Tingkat I', 5 UNION
SELECT '2', 'Tingkat II', 4 UNION
SELECT 'A', 'Kelas A', 3 UNION
SELECT 'B', 'Kelas B', 2 UNION
SELECT 'C', 'Kelas C', 1 UNION
SELECT 'D', 'Kelas D', 6 UNION
SELECT 'X', 'Tanpa Kelas', 0;
