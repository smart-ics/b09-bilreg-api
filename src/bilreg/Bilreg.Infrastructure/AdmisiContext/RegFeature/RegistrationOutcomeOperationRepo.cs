using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public sealed class RegistrationOutcomeOperationRepo : IRegistrationOutcomeOperationRepo
{
    private readonly DatabaseOptions _opt;
    public RegistrationOutcomeOperationRepo(IOptions<DatabaseOptions> opt)=>_opt=opt.Value;
    public bool TryFinalize(RegistrationOutcomeModel o,string loketKey,byte[] version,DateTime doneAt)
    {
        const string sql="""
          INSERT BILRG_RegOutcome(OutcomeId,AntrianId,NoUrut,OutcomeType,RegId,ReasonCode,
            CrtUser,CrtDate,UpdUser,UpdDate,VodUser,VodDate)
          VALUES(@OutcomeId,@AntrianId,@NoUrut,@OutcomeType,@RegId,@ReasonCode,
            @UserId,@CreatedAt,@UserId,@CreatedAt,'','3000-01-01');
          UPDATE BILRG_AntrianEntry SET AntrianStatus=2,DoneAt=@doneAt,UpdUser=@UserId,UpdDate=@doneAt
          WHERE AntrianId=@AntrianId AND NoUrut=@NoUrut AND AntrianStatus=1;
          IF @@ROWCOUNT<>1 SELECT CAST(0 AS BIT); ELSE BEGIN
            UPDATE BILRG_AdmBookingAssistance SET IsActive=0,UpdUser=@UserId,UpdDate=@doneAt
              WHERE AntrianId=@AntrianId AND NoUrut=@NoUrut AND IsActive=1;
            UPDATE BILRG_AdmLoketCurrentCall SET ClaimState=0,IsActive=0,ReleasedAt=@doneAt,
              UpdUser=@UserId,UpdDate=@doneAt
            WHERE LoketKey=@loketKey AND AntrianId=@AntrianId AND NoUrut=@NoUrut
              AND ClaimState=2 AND RowVersion=@version;
            SELECT CAST(IIF(@@ROWCOUNT=1,1,0) AS BIT);
          END
          """;
        try { using var c=new SqlConnection(ConnStringHelper.Get(_opt));
          return c.ExecuteScalar<bool>(sql,new{o.OutcomeId,o.AntrianId,o.NoUrut,
            OutcomeType=(int)o.OutcomeType,o.RegId,o.ReasonCode,o.UserId,o.CreatedAt,loketKey,version,doneAt}); }
        catch(SqlException e) when(e.Number is 2601 or 2627){return false;}
    }

    public bool TryFinalizeEstablished(RegistrationOutcomeModel o,AntrianEntryModel e,
        string loketKey,byte[] version,DateTime doneAt)
    {
        const string sql="""
          INSERT BILRG_RegOutcome(OutcomeId,AntrianId,NoUrut,OutcomeType,RegId,ReasonCode,
            CrtUser,CrtDate,UpdUser,UpdDate,VodUser,VodDate)
          VALUES(@OutcomeId,@AntrianId,@NoUrut,1,@RegId,'',@UserId,@CreatedAt,@UserId,@CreatedAt,'','3000-01-01');
          UPDATE BILRG_AntrianEntry SET AntrianStatus=2,DoneAt=@doneAt,
            PersonName=@PersonName,PasienTrackerId=@PasienTrackerId,ReffId=@RegId,ReffDesc='REG',
            UpdUser=@UserId,UpdDate=@doneAt
          WHERE AntrianId=@AntrianId AND NoUrut=@NoUrut AND AntrianStatus=1;
          IF @@ROWCOUNT<>1 SELECT CAST(0 AS BIT); ELSE BEGIN
            UPDATE BILRG_AdmBookingAssistance SET IsActive=0,UpdUser=@UserId,UpdDate=@doneAt
              WHERE AntrianId=@AntrianId AND NoUrut=@NoUrut AND IsActive=1;
            UPDATE BILRG_AdmLoketCurrentCall SET ClaimState=0,IsActive=0,ReleasedAt=@doneAt,
              UpdUser=@UserId,UpdDate=@doneAt
            WHERE LoketKey=@loketKey AND AntrianId=@AntrianId AND NoUrut=@NoUrut
              AND ClaimState=2 AND IsActive=1 AND RowVersion=@version;
            SELECT CAST(IIF(@@ROWCOUNT=1,1,0) AS BIT);
          END
          """;
        try { using var c=new SqlConnection(ConnStringHelper.Get(_opt));
          return c.ExecuteScalar<bool>(sql,new{o.OutcomeId,o.AntrianId,o.NoUrut,o.RegId,o.UserId,o.CreatedAt,
            PersonName=e.Visitor.PersonName,PasienTrackerId=e.Tracker.PasienTrackerId,
            loketKey,version,doneAt}); }
        catch(SqlException x) when(x.Number is 2601 or 2627){return false;}
    }

    public bool TryRecordLegacyEstablished(RegistrationOutcomeModel o)
    {
        const string sql="""
          INSERT BILRG_RegOutcome(OutcomeId,AntrianId,NoUrut,OutcomeType,RegId,ReasonCode,
            CrtUser,CrtDate,UpdUser,UpdDate,VodUser,VodDate)
          VALUES(@OutcomeId,@AntrianId,@NoUrut,1,@RegId,'',@UserId,@CreatedAt,@UserId,@CreatedAt,'','3000-01-01');
          UPDATE BILRG_AdmBookingAssistance SET IsActive=0,UpdUser=@UserId,UpdDate=@CreatedAt
            WHERE AntrianId=@AntrianId AND NoUrut=@NoUrut AND IsActive=1;
          """;
        try { using var c=new SqlConnection(ConnStringHelper.Get(_opt)); c.Execute(sql,o); return true; }
        catch(SqlException x) when(x.Number is 2601 or 2627){return false;}
    }
}
