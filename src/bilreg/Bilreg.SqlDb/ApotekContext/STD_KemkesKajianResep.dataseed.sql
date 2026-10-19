MERGE INTO STD_KemkesKajianResep AS target
USING (VALUES
    ('1.1', 'Apakah nama, umur, jenis kelamin, berat badan dan tinggi badan pasien sudah sesuai?', '1', 1),
    ('1.2', 'Apakah nama, nomor ijin, alamat dan paraf dokter sudah sesuai?', '1', 1),
    ('1.3', 'Apakah tanggal resep sudah sesuai?', '1', 1),
    ('1.4', 'Apakah ruangan/unit asal resep sudah sesuai?', '1', 1),
    ('2.1', 'Apakah nama obat, bentuk dan kekuatan sediaan sudah sesuai?', '2', 1),
    ('2.2', 'Apakah dosis dan jumlah obat sudah sesuai?', '2', 1),
    ('2.3', 'Apakah stabilitas obat sudah sesuai?', '2', 1),
    ('2.4', 'Apakah aturan dan cara penggunaan obat sudah sesuai?', '2', 1),
    ('3.1', 'Apakah ketepatan indikasi, dosis, dan waktu penggunaan obat sudah sesuai?', '3', 1),
    ('3.2', 'Apakah terdapat duplikasi pengobatan?', '3', 0),
    ('3.3', 'Apakah terdapat alergi dan reaksi obat yang tidak dikehendaki (ROTD)?', '3', 0),
    ('3.4', 'Apakah terdapat kontraindikasi pengobatan?', '3', 0),
    ('3.5', 'Apakah terdapat dampak interaksi obat?', '3', 0)
) AS source (KajianResepId, KajianResepName, GrupKajianResepId, DefaultIsYes)
ON target.KajianResepId = source.KajianResepId
WHEN MATCHED THEN
    UPDATE SET 
        KajianResepName = source.KajianResepName,
        GrupKajianResepId = source.GrupKajianResepId,
        DefaultIsYes = source.DefaultIsYes
WHEN NOT MATCHED THEN
    INSERT (KajianResepId, KajianResepName, GrupKajianResepId, DefaultIsYes)
    VALUES (source.KajianResepId, source.KajianResepName, source.GrupKajianResepId, source.DefaultIsYes);
GO
