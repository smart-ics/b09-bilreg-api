using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public sealed class BookingAssistanceRepo:IBookingAssistanceRepo
{
    private readonly DatabaseOptions _opt; public BookingAssistanceRepo(IOptions<DatabaseOptions> o)=>_opt=o.Value;
    private SqlConnection Open()=>new(ConnStringHelper.Get(_opt));
    public BookingAssistanceActive? FindActive(string bookingId)
    { const string sql="""
      SELECT a.BookingId,a.AntrianId,a.NoUrut,
        CASE WHEN q.QueuePrefixSnapshot='' THEN NULL ELSE q.QueuePrefixSnapshot+RIGHT('0000'+CONVERT(VARCHAR(4),a.NoUrut),4) END QueueLabel
      FROM BILRG_AdmBookingAssistance a INNER JOIN BILRG_Antrian q ON q.AntrianId=a.AntrianId
      WHERE a.BookingId=@bookingId AND a.IsActive=1
      """;using var c=Open();return c.QuerySingleOrDefault<BookingAssistanceActive>(sql,new{bookingId});}
    public bool TryCreate(string bookingId,string correlation,string? failureCode,string kioskId,string userId,
      DateTime at,AntrianModel q,AntrianEntryModel e)
    { const string sql="""
      IF NOT EXISTS(SELECT 1 FROM BILRG_Antrian WHERE AntrianId=@AntrianId)
        INSERT BILRG_Antrian(AntrianId,AntrianDate,StartTime,EndTime,SequenceTag,AntrianDescription,ServicePointCode,QueuePrefixSnapshot,CrtUser,CrtDate,UpdUser,UpdDate)
        VALUES(@AntrianId,@AntrianDate,@StartTime,@EndTime,@SequenceTag,@AntrianDescription,@ServicePointCode,@QueuePrefixSnapshot,@userId,@at,@userId,@at);
      INSERT BILRG_AntrianEntry(AntrianId,NoUrut,PersonName,PasienTrackerId,AntrianStatus,CreatedAt,ServedAt,DoneAt,ReffId,ReffDesc,CrtUser,CrtDate,UpdUser,UpdDate)
        VALUES(@AntrianId,@NoUrut,'','-',0,@at,'3000-01-01','3000-01-01',@bookingId,'BOK-AST',@userId,@at,@userId,@at);
      UPDATE BILRG_AdmBookingAssistance WITH(UPDLOCK,HOLDLOCK)
        SET AntrianId=@AntrianId,NoUrut=@NoUrut,IsActive=1,FailureCode=@failureCode,
            KioskId=@kioskId,UpdUser=@userId,UpdDate=@at
        WHERE BookingId=@bookingId AND IsActive=0;
      IF @@ROWCOUNT=0
        INSERT BILRG_AdmBookingAssistance(BookingId,AssistanceCorrelation,AntrianId,NoUrut,IsActive,FailureCode,KioskId,CrtUser,CrtDate,UpdUser,UpdDate)
          VALUES(@bookingId,@correlation,@AntrianId,@NoUrut,1,@failureCode,@kioskId,@userId,@at,@userId,@at);
      """;
      try{using var c=Open();c.Execute(sql,new{bookingId,correlation,failureCode=failureCode??"",kioskId,userId,at,q.AntrianId,
        AntrianDate=q.AntrianDate.ToDateTime(TimeOnly.MinValue),StartTime=q.StartTime.ToString("HH:mm"),EndTime=q.EndTime.ToString("HH:mm"),
        q.SequenceTag,q.AntrianDescription,ServicePointCode=q.ServicePoint.ServicePointCode,q.QueuePrefixSnapshot,e.NoUrut});return true;}
      catch(SqlException x)when(x.Number is 2601 or 2627){return false;}
    }
}
