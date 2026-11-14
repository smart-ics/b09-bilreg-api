namespace Bilreg.Domain.AdmisiContext.RegFeature;

public interface IRegKey
{
    string RegId { get; }
}

public record RegKey(string RegId) : IRegKey;
