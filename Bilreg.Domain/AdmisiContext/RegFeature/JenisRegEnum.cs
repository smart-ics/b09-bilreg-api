namespace Bilreg.Domain.AdmisiContext.RegFeature;

public enum JenisRegEnum
{
    RegJalan,    //  0
    RegInap,     //  1
    External,    //  2
    Darurat,     //  3
    Meninggal,   //  4
    ExternalInap,//  5
    Unknown      //  6
}

public static class JenisRegEnumExtensions
{
    public static string ToNumberString(this JenisRegEnum value)
    {
        return ((int)value).ToString();
    }
    
    public static JenisRegEnum ToJenisRegEnum(this string value)
    {
        if (int.TryParse(value, out int intValue) && 
            Enum.IsDefined(typeof(JenisRegEnum), intValue))
        {
            return (JenisRegEnum)intValue;
        }
        return JenisRegEnum.Unknown;
    }
}