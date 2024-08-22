using System;

namespace Bilreg.Domain.AdmPasienContext.PekerjaanAgg
{
    public class PekerjaanModel : IPekerjaanKey
    {
        //  CONSTRUCTORS
        private PekerjaanModel(string id, string name)
        {
            PekerjaanId = id;
            PekerjaanName = name;
        }

        //  FACTORY METHODS
        public static PekerjaanModel Create(string id, string name)
        {
            return new PekerjaanModel(id, name);
        }

        //  PROPERTIES
        public string PekerjaanId { get; private set; }
        public string PekerjaanName { get; private set; }
    }

    public interface IPekerjaanKey
    {
        string PekerjaanId { get; }
    }
}
