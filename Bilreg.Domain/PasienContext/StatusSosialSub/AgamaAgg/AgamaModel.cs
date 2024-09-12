namespace Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg
{
    public class AgamaModel : IAgamaKey
    {
        //  CONSTRUCTORS
        public AgamaModel(string id, string name)
        {
            AgamaId = id;
            AgamaName = name;
        }

        //  PROPERTIES
        public string AgamaId { get; protected set; }
        public string AgamaName { get; protected set; }
    }
}
