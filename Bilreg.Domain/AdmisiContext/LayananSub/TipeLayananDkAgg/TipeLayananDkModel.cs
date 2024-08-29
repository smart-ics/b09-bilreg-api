namespace Bilreg.Domain.AdmisiContext.LayananSub.TipeLayananDkAgg
{
    public class TipeLayananDkModel : ITipeLayananDkKey
    {
        // Constructor
        private TipeLayananDkModel(string id, string name)
        {
            TipeLayananDkId = id;
            TipeLayananDkName = name;
        }
        // Factory Method
        public static TipeLayananDkModel Create(string id, string name)
        {
            return new TipeLayananDkModel(id, name);
        }
        // Properties
        public string TipeLayananDkId { get; private set; }
        public string TipeLayananDkName { get; private set; }
    }

    public interface ITipeLayananDkKey
    {
        string TipeLayananDkId { get; }
    }
}
