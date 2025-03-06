namespace Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;

public interface ICaraBayarDkKey
{
    string CaraBayarDkId { get; }
}

public record CaraBayarDkKey(string CaraBayarDkId) : ICaraBayarDkKey;