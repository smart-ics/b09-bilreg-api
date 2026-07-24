namespace Bilreg.Domain.AdmisiContext.RegFeature;

public enum RegistrationOutcomeTypeEnum { Established = 1, NotEstablished = 2 }

public sealed record RegistrationOutcomeModel
{
    private RegistrationOutcomeModel(string outcomeId,string antrianId,int noUrut,
        RegistrationOutcomeTypeEnum outcomeType,string regId,string reasonCode,string userId,DateTime createdAt)
    {
        if(string.IsNullOrWhiteSpace(outcomeId)) throw new ArgumentException("OutcomeId is required.");
        if(string.IsNullOrWhiteSpace(antrianId)||noUrut<=0) throw new ArgumentException("Queue Entry identity is required.");
        if(string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.");
        if(outcomeType==RegistrationOutcomeTypeEnum.Established && (string.IsNullOrWhiteSpace(regId)||!string.IsNullOrWhiteSpace(reasonCode)))
            throw new ArgumentException("Established requires RegId and prohibits ReasonCode.");
        if(outcomeType==RegistrationOutcomeTypeEnum.NotEstablished && (!string.IsNullOrWhiteSpace(regId)||string.IsNullOrWhiteSpace(reasonCode)))
            throw new ArgumentException("NotEstablished requires ReasonCode and prohibits RegId.");
        OutcomeId=outcomeId;AntrianId=antrianId;NoUrut=noUrut;OutcomeType=outcomeType;
        RegId=regId.Trim();ReasonCode=reasonCode.Trim();UserId=userId.Trim();CreatedAt=createdAt;
    }
    public string OutcomeId{get;} public string AntrianId{get;} public int NoUrut{get;}
    public RegistrationOutcomeTypeEnum OutcomeType{get;} public string RegId{get;}
    public string ReasonCode{get;} public string UserId{get;} public DateTime CreatedAt{get;}
    public static RegistrationOutcomeModel Established(string q,int n,string regId,string user,DateTime at)=>
        new(Ulid.NewUlid().ToString(),q,n,RegistrationOutcomeTypeEnum.Established,regId,"",user,at);
    public static RegistrationOutcomeModel NotEstablished(string q,int n,string reason,string user,DateTime at)=>
        new(Ulid.NewUlid().ToString(),q,n,RegistrationOutcomeTypeEnum.NotEstablished,"",reason,user,at);
}
