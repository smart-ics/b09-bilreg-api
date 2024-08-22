using System;

namespace Bilreg.Domain.AdmPasienContext.PekerjaanDkAgg
{
    public class PekerjaanDkModel : IPekerjaanDkKey
    {
        //  CONSTRUCTORS
        private PekerjaanDkModel(string id, string name)
        {
            PekerjaanDkId = id;
            PekerjaanDkName = name;
        }

        //  FACTORY METHODS
        public static PekerjaanDkModel Create(string id, string name)
        {
            return new PekerjaanDkModel(id, name);
        }

        //  PROPERTIES
        public string PekerjaanDkId { get; private set; }
        public string PekerjaanDkName { get; private set; }
    }

    public interface IPekerjaanDkKey
    {
        string PekerjaanDkId { get; }
    }
}
