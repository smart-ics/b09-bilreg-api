namespace Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;

public interface IPekerjaanDkKey
{
    string PekerjaanDkId { get; }
}

public record PekerjaanDkKey(string PekerjaanDkId) : IPekerjaanDkKey;