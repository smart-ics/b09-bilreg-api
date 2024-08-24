namespace Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg
{
    public class AgamaModel : IAgamaKey
    {
        //  CONSTRUCTORS
        private AgamaModel(string id, string name)
        {
            AgamaId = id;
            AgamaName = name;
        }

        public static AgamaModel Create(string id, string name)
        {
            return new AgamaModel(id, name);
        }

        //  PROPERTIES
        public string AgamaId { get; private set; }
        public string AgamaName { get; private set; }
    }
    public interface IAgamaKey
    {
        string AgamaId { get; }
    }
}
