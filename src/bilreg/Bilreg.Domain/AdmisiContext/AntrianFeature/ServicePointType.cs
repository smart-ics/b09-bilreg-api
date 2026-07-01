namespace Bilreg.Domain.AdmisiContext.AntrianFeature;



public record ServicePointType(string ServicePointCode, string ServicePointName) : IServicePointKey
{
    public static ServicePointType Default =>
        new("-", "-");

}

public interface IServicePointKey
{
    string ServicePointCode { get; }
}

