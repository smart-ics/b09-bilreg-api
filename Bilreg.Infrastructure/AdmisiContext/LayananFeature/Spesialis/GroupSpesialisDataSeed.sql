DELETE BILRG_GroupSpesialis
GO

INSERT INTO BILRG_GroupSpesialis
SELECT 'UMM', 'Umum' UNION
SELECT 'INT', 'Penyakit Dalam' UNION
SELECT 'ANK', 'Anak' UNION
SELECT 'GGI', 'Gigi' UNION
SELECT 'OBG', 'Obgyn' UNION
SELECT 'BDH', 'Bedah' UNION
SELECT 'MTA', 'Mata' UNION
SELECT 'THT', 'THT' UNION
SELECT 'KKE', 'Kulit Kelamin dan Estetika' UNION
SELECT 'JTG', 'Jantung' UNION
SELECT 'PRU', 'Paru' UNION
SELECT 'SRF', 'Syaraf' UNION
SELECT 'OTP', 'Orthopedi' UNION
SELECT 'URO', 'Urologi' UNION
SELECT 'RMD', 'Rehab Medik' UNION
SELECT 'PSI', 'Psikiatri' UNION
SELECT 'X', 'Lain-Lain'
