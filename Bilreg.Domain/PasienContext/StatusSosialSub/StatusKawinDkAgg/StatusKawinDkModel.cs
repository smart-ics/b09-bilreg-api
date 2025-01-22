namespace Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg
{
    public class StatusKawinDkModel : IStatusKawinDkKey
    {
        // Constructor
        public StatusKawinDkModel(string id, string name)
            => (StatusKawinDkId, StatusKawinDkName) = (id, name);

        public StatusKawinDkModel()
        {
        }

        // Properties
        public string StatusKawinDkId { get; private set; }
        public string StatusKawinDkName { get; private set; }
    }
}

