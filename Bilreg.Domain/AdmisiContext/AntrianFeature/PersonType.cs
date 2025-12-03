using System.Text.RegularExpressions;
using Bilreg.Domain.Shared.Helpers;
using F23.StringSimilarity;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record PersonType(string PersonName, DateOnly TglLahir)
{
    public static PersonType Default => new("", new DateOnly(3000,1,1));
    
    public bool IsSimilar(PersonType? other)
    {
        if (other?.TglLahir != TglLahir)
            return false;
        var otherName = other?.PersonName ?? string.Empty;
        var similar = JaroWinklerDistance.AreSimilar(PersonName, otherName, threshold: 0.95);
        return similar;
    }
}