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
SELECT 'ANS', 'Anestesi' UNION
SELECT 'MKR', 'Kornea dan Bedah Refraktif' UNION
SELECT 'MIM', 'Infeksi dan Immunologi' UNION
SELECT 'MVR', 'Vitreo-Retina' UNION
SELECT 'MSB', 'Pediatri Onkologi Strabismus' UNION
SELECT 'MNO', 'Neuro Oftalmologi' UNION
SELECT 'MGL', 'Glaukoma' UNION
SELECT 'MPD', 'Pediatrik Oftalmologi' UNION
SELECT 'MLK', 'Refraksi Lensa Kontak' UNION
SELECT 'MOK', 'Rekon Okuloplasti dan Onkologi' UNION
SELECT 'MOM', 'Onkologi Mata' UNION
SELECT 'MKM', 'Oftalmologi Komunitas' UNION
SELECT 'X', 'Lain-Lain'
