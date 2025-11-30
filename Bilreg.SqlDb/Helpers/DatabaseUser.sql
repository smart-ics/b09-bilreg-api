CREATE LOGIN bilregLogin WITH PASSWORD = 'bilreg123!'
GO
       
CREATE USER bilregUser FRO LOGIN bilregLogin
GO
       
sp_addrolemember 'db_datareader', 'bilregUser'
GO

sp_addrolemember 'db_datawriter', 'bilregUser'
GO

sp_addrolemember 'db_ddladmin', 'bilregUser'
GO


