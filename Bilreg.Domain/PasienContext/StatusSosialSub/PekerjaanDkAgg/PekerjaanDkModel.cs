namespace Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;

public class PekerjaanDkModel : IPekerjaanDkKey
{
    //  CONSTRUCTORS
    public PekerjaanDkModel(string id, string name)
    {
        PekerjaanDkId = id;
        PekerjaanDkName = name;
    }

    public PekerjaanDkModel()
    {
    }
        
    //  PROPERTIES
    public string PekerjaanDkId { get; protected set; }
    public string PekerjaanDkName { get; protected set; }
}