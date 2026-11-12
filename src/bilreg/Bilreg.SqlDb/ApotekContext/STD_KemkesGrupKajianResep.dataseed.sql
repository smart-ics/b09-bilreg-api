MERGE INTO STD_KemkesGrupKajianResep AS target
USING (VALUES
    ('1', 'Persyaratan Administrasi'),
    ('2', 'Persyaratan Farmasetik'),
    ('3', 'Persyaratan Klinis')
) AS source (GrupKajianResepId, GrupKajianResepName)
ON target.GrupKajianResepId = source.GrupKajianResepId
WHEN MATCHED THEN
    UPDATE SET GrupKajianResepName = source.GrupKajianResepName
WHEN NOT MATCHED THEN
    INSERT (GrupKajianResepId, GrupKajianResepName)
    VALUES (source.GrupKajianResepId, source.GrupKajianResepName);
GO
