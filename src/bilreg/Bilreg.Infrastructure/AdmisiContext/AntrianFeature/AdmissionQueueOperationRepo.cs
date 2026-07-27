using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public sealed class AdmissionQueueOperationRepo : IAdmissionQueueOperationRepo
{
    private readonly DatabaseOptions _opt;
    public AdmissionQueueOperationRepo(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    private SqlConnection Open() => new(ConnStringHelper.Get(_opt));

    public bool TryCall(string q, int n, string loket, string user, DateTime at)
    {
        const string sql = """
            SET XACT_ABORT ON;
            UPDATE BILRG_AntrianEntry SET CallCount=CallCount+1,UpdUser=@user,UpdDate=@at
            WHERE AntrianId=@q AND NoUrut=@n AND AntrianStatus=0
              AND NOT EXISTS (SELECT 1 FROM BILRG_AdmLoketCurrentCall WITH (UPDLOCK,HOLDLOCK)
                  WHERE AntrianId=@q AND NoUrut=@n AND IsActive=1);
            IF @@ROWCOUNT<>1 SELECT CAST(0 AS BIT); ELSE BEGIN
                UPDATE BILRG_AdmLoketCurrentCall SET AntrianId=@q,NoUrut=@n,ClaimState=1,IsActive=1,
                    AnnouncementVersion=AnnouncementVersion+1,CalledAt=@at,ServiceStartedAt='3000-01-01',
                    ReleasedAt='3000-01-01',UpdUser=@user,UpdDate=@at
                WHERE LoketKey=@loket AND ClaimState=0;
                IF @@ROWCOUNT=0 BEGIN
                    IF EXISTS(SELECT 1 FROM BILRG_AdmLoketCurrentCall WHERE LoketKey=@loket)
                        SELECT CAST(0 AS BIT);
                    ELSE BEGIN
                        INSERT BILRG_AdmLoketCurrentCall(LoketKey,AntrianId,NoUrut,ClaimState,IsActive,
                            AnnouncementVersion,CalledAt,ServiceStartedAt,ReleasedAt,CrtUser,CrtDate,
                            UpdUser,UpdDate,VodUser,VodDate)
                        VALUES(@loket,@q,@n,1,1,1,@at,'3000-01-01','3000-01-01',@user,@at,@user,@at,'','3000-01-01');
                        SELECT CAST(1 AS BIT);
                    END
                END ELSE SELECT CAST(1 AS BIT);
            END
            """;
        try { using var c=Open(); return c.ExecuteScalar<bool>(sql,new{q,n,loket,user,at}); }
        catch (SqlException e) when (e.Number is 2601 or 2627) { return false; }
    }

    public bool TryRecall(string q,int n,string loket,byte[] expectedRowVersion,string user,DateTime at)
    {
        const string sql="""
            UPDATE BILRG_AntrianEntry SET CallCount=CallCount+1,UpdUser=@user,UpdDate=@at
            WHERE AntrianId=@q AND NoUrut=@n AND AntrianStatus=0;
            IF @@ROWCOUNT<>1 SELECT CAST(0 AS BIT); ELSE BEGIN
              UPDATE BILRG_AdmLoketCurrentCall SET AnnouncementVersion=AnnouncementVersion+1,
                CalledAt=@at,UpdUser=@user,UpdDate=@at
              WHERE LoketKey=@loket AND AntrianId=@q AND NoUrut=@n AND ClaimState=1
                AND RowVersion=@expectedRowVersion;
              SELECT CAST(IIF(@@ROWCOUNT=1,1,0) AS BIT);
            END
            """;
        using var c=Open(); return c.ExecuteScalar<bool>(sql,new{q,n,loket,expectedRowVersion,user,at});
    }

    public bool TryReturnToWaiting(
        string q,
        int n,
        string loket,
        byte[] expectedRowVersion,
        string user,
        DateTime at)
    {
        const string sql = """
            UPDATE BILRG_AdmLoketCurrentCall SET ClaimState=0,IsActive=0,ReleasedAt=@at,
                UpdUser=@user,UpdDate=@at
            WHERE LoketKey=@loket AND AntrianId=@q AND NoUrut=@n
              AND ClaimState=1 AND IsActive=1 AND RowVersion=@expectedRowVersion
              AND EXISTS (
                  SELECT 1 FROM BILRG_AntrianEntry
                  WHERE AntrianId=@q AND NoUrut=@n AND AntrianStatus=0);
            SELECT CAST(IIF(@@ROWCOUNT=1,1,0) AS BIT);
            """;
        using var c = Open();
        return c.ExecuteScalar<bool>(sql, new { q, n, loket, expectedRowVersion, user, at });
    }

    public bool TryStartService(string q,int n,string loket,byte[] expectedRowVersion,string user,DateTime at)
    {
        const string sql="""
            UPDATE BILRG_AntrianEntry SET AntrianStatus=1,ServedAt=@at,UpdUser=@user,UpdDate=@at
            WHERE AntrianId=@q AND NoUrut=@n AND AntrianStatus=0;
            IF @@ROWCOUNT<>1 SELECT CAST(0 AS BIT); ELSE BEGIN
              UPDATE BILRG_AdmLoketCurrentCall SET ClaimState=2,ServiceStartedAt=@at,UpdUser=@user,UpdDate=@at
              WHERE LoketKey=@loket AND AntrianId=@q AND NoUrut=@n AND ClaimState=1
                AND RowVersion=@expectedRowVersion;
              SELECT CAST(IIF(@@ROWCOUNT=1,1,0) AS BIT);
            END
            """;
        using var c=Open(); return c.ExecuteScalar<bool>(sql,new{q,n,loket,expectedRowVersion,user,at});
    }

    public bool TryReturnToWaiting(string q,int n,string loket,byte[] expectedRowVersion,string user,DateTime at)
    {
        const string sql="""
            UPDATE BILRG_AdmLoketCurrentCall SET ClaimState=0,IsActive=0,ReleasedAt=@at,
              UpdUser=@user,UpdDate=@at
            WHERE LoketKey=@loket AND AntrianId=@q AND NoUrut=@n AND ClaimState=1 AND IsActive=1
              AND RowVersion=@expectedRowVersion
              AND EXISTS (SELECT 1 FROM BILRG_AntrianEntry
                WHERE AntrianId=@q AND NoUrut=@n AND AntrianStatus=0);
            SELECT CAST(IIF(@@ROWCOUNT=1,1,0) AS BIT);
            """;
        using var c=Open(); return c.ExecuteScalar<bool>(sql,new{q,n,loket,expectedRowVersion,user,at});
    }

    public bool TryWithdraw(string q,int n,string reason,string user,DateTime at,string? loketKey,byte[]? expectedRowVersion)
    {
        const string sql="""
            UPDATE BILRG_AntrianEntry SET AntrianStatus=3,WithdrawalReason=@reason,
                WithdrawalUserId=@user,WithdrawnAt=@at,UpdUser=@user,UpdDate=@at
            WHERE AntrianId=@q AND NoUrut=@n AND AntrianStatus=0;
            IF @@ROWCOUNT<>1 SELECT CAST(0 AS BIT); ELSE BEGIN
              UPDATE BILRG_AdmBookingAssistance SET IsActive=0,UpdUser=@user,UpdDate=@at
                WHERE AntrianId=@q AND NoUrut=@n AND IsActive=1;
            IF @loketKey IS NULL BEGIN
              SELECT CAST(IIF(EXISTS(SELECT 1 FROM BILRG_AdmLoketCurrentCall
                WHERE AntrianId=@q AND NoUrut=@n AND IsActive=1),0,1) AS BIT);
            END ELSE BEGIN
              UPDATE BILRG_AdmLoketCurrentCall SET ClaimState=0,IsActive=0,ReleasedAt=@at,
                UpdUser=@user,UpdDate=@at
              WHERE LoketKey=@loketKey AND AntrianId=@q AND NoUrut=@n AND ClaimState=1 AND IsActive=1
                AND RowVersion=@expectedRowVersion;
              SELECT CAST(IIF(@@ROWCOUNT=1,1,0) AS BIT);
            END END
            """;
        using var c=Open(); return c.ExecuteScalar<bool>(sql,new{q,n,reason,user,at,loketKey,expectedRowVersion});
    }

    public bool TryRedirect(string originAntrianId,int originNoUrut,string userId,DateTime at,
        string? loketKey,byte[]? expectedRowVersion,AntrianModel targetQueue,AntrianEntryModel replacement)
    {
        if (!TryWithdraw(originAntrianId,originNoUrut,"Redirected",userId,at,loketKey,expectedRowVersion)) return false;
        const string sql="""
            IF NOT EXISTS(SELECT 1 FROM BILRG_Antrian WHERE AntrianId=@AntrianId)
              INSERT BILRG_Antrian(AntrianId,AntrianDate,StartTime,EndTime,SequenceTag,
                AntrianDescription,ServicePointCode,QueuePrefixSnapshot,CrtUser,CrtDate,UpdUser,UpdDate)
              VALUES(@AntrianId,@AntrianDate,@StartTime,@EndTime,@SequenceTag,
                @AntrianDescription,@ServicePointCode,@QueuePrefixSnapshot,@userId,@CreatedAt,@userId,@CreatedAt);
            INSERT BILRG_AntrianEntry(AntrianId,NoUrut,PersonName,PasienTrackerId,AntrianStatus,
              CreatedAt,ServedAt,DoneAt,ReffId,ReffDesc,Priority,CreationReason,CallCount,
              SourceAntrianId,SourceNoUrut,WithdrawalReason,WithdrawalUserId,WithdrawnAt,CrtUser,CrtDate,UpdUser,UpdDate)
            VALUES(@AntrianId,@NoUrut,'','-',0,@CreatedAt,'3000-01-01','3000-01-01','','',1,1,0,
              @SourceAntrianId,@SourceNoUrut,'','','3000-01-01',@userId,@CreatedAt,@userId,@CreatedAt);
            """;
        try { using var c=Open(); c.Execute(sql,new { targetQueue.AntrianId,
            AntrianDate=targetQueue.AntrianDate.ToDateTime(TimeOnly.MinValue),
            StartTime=targetQueue.StartTime.ToString("HH:mm"),EndTime=targetQueue.EndTime.ToString("HH:mm"),
            targetQueue.SequenceTag,targetQueue.AntrianDescription,ServicePointCode=targetQueue.ServicePoint.ServicePointCode,
            targetQueue.QueuePrefixSnapshot,replacement.NoUrut,replacement.CreatedAt,
            SourceAntrianId=originAntrianId,SourceNoUrut=originNoUrut }); return true; }
        catch (SqlException e) when (e.Number is 2601 or 2627) { return false; }
    }
}
