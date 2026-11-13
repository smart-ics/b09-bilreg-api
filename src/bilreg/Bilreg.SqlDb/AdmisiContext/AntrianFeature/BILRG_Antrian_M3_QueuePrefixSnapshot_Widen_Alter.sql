-- Widen QueuePrefixSnapshot to VARCHAR(5) to support clinic queue prefix (1-2 uppercase letters)
IF COL_LENGTH('BILRG_Antrian', 'QueuePrefixSnapshot') IS NULL
BEGIN
    ALTER TABLE BILRG_Antrian ADD QueuePrefixSnapshot VARCHAR(5) NOT NULL
        CONSTRAINT DF_BILRG_Antrian_QueuePrefixSnapshot DEFAULT('');
END
ELSE IF EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_Antrian') 
      AND name = 'QueuePrefixSnapshot' 
      AND max_length = 1
)
BEGIN
    ALTER TABLE BILRG_Antrian ALTER COLUMN QueuePrefixSnapshot VARCHAR(5) NOT NULL;
END
GO
