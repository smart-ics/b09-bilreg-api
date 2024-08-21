using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.AdmPasienContext.StatusKawinAgg
{
    public class StatusKawinModel : IStatusKawinKey
    {
        // Constructor
        private StatusKawinModel(string id, string name)
            => (StatusKawinId, StatusKawinName) = (id, name);
        
        // Factory Method
        public static StatusKawinModel Create(string id, string name)
        => new StatusKawinModel(id, name);

        // Properties
        public string StatusKawinId { get; private set; }
        public string StatusKawinName { get; private set; }
    }

    public interface IStatusKawinKey
    {
        string StatusKawinId { get;}
    }
}
