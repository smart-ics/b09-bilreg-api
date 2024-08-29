namespace Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg
{
    public class InstalasiDkModel :IInstalasiDkKey
    {
        //  CONSTRUCTOR
        private InstalasiDkModel(string id ,string name)
        {
            InstalasiDkId = id;
            InstalasiDkName = name;
        }

        //  FACOTRY
        public static InstalasiDkModel Create(string id, string name)
        {
            return new InstalasiDkModel(id, name);
        }

        //  PROPERTIES
        public string InstalasiDkId { get; private set; }
        public string InstalasiDkName { get; private set; }
        
    }

    public interface IInstalasiDkKey
    {
        string InstalasiDkId { get; }
    }
}
