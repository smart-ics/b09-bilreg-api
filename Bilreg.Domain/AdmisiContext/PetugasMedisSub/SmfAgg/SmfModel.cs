namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg
{
    public class SmfModel : ISmfKey
    {
        // CONSTRUCTOR
        private SmfModel(string id, string name) 
        {
            SmfId = id;
            SmfName = name;
        }

        // FACTORY METHOD
        public static SmfModel Create(string id, string name)
        {
            return new SmfModel(id, name);
        }

        // PROPERTIES
        public string SmfId { get; private set; }
        public string SmfName { get; private set; }
    }

    public interface ISmfKey
    {
        string SmfId { get; }
    }
}
