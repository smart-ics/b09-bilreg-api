namespace Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;

public interface IPendidikanDkKey
{
    string PendidikanDkId {get;}
}

public record PendidikanDkKey(string PendidikanDkId) : IPendidikanDkKey;