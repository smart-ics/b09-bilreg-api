namespace Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg;

public interface IStatusKawinDkKey
{
    string StatusKawinDkId { get;}
}

public record StatusKawinDkKey(string StatusKawinDkId) : IStatusKawinDkKey;
