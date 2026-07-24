IF COL_LENGTH('BILRG_Antrian', 'QueuePrefixSnapshot') IS NULL
BEGIN
    ALTER TABLE BILRG_Antrian ADD QueuePrefixSnapshot CHAR(1) NOT NULL
        CONSTRAINT DF_BILRG_Antrian_QueuePrefixSnapshot DEFAULT('');
END;
