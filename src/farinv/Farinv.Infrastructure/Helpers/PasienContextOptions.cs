namespace Farinv.Infrastructure.Helpers;

public class PasienContextOptions
{
    public const string SECTION_NAME = "PasienContext";
    
    public GenderTypeOptions GenderType { get; set; } = new GenderTypeOptions();
}

public class GenderTypeOptions
{
    public string Male { get; set; } = "L";
    public string Female { get; set; } = "P";
}