namespace Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;

public interface ISukuKey
{
    string SukuId {get;}
}

public record SukuKey(string SukuId) : ISukuKey;