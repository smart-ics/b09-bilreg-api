namespace Bilreg.Domain.BrmContext.MutasiBerkasFeature;

public interface IMutasiBerkasKey
{
    string MutasiBerkasId { get; }
}

public record MutasiBerkasKey(string MutasiBerkasId) : IMutasiBerkasKey;
