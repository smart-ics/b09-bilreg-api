namespace Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;

public interface ICaraMasukDkKey
{
    string CaraMasukDkId { get; }
}

public record CaraMasukDkKey(string CaraMasukDkId) : ICaraMasukDkKey;