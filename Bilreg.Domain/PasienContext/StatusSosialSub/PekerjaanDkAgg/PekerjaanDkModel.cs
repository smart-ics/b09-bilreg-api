namespace Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;

public class PekerjaanDkModel : IPekerjaanDkKey
{
    public PekerjaanDkModel(string id, string name)
    {
        if (id == string.Empty ^ name == string.Empty)
            throw new ArgumentException(("Invalid pekerjaan dk."));
        
        PekerjaanDkId = id;
        PekerjaanDkName = name;
    }

    public static PekerjaanDkModel Default => new PekerjaanDkModel(string.Empty, string.Empty);

    public PekerjaanDkModel()
    {
    }
        
    public string PekerjaanDkId { get; protected set; }
    public string PekerjaanDkName { get; protected set; }
}