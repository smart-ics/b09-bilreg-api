using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.AdmPasienContext.StatusKawinDkAgg
{
    public class StatusKawinDkModel : IStatusKawinDkKey
    {
        // Constructor
        private StatusKawinDkModel(string id, string name)
            => (StatusKawinDkId, StatusKawinDkName) = (id, name);
        
        // Factory Method
        public static StatusKawinDkModel Create(string id, string name)
        => new StatusKawinDkModel(id, name);

        // Properties
        public string StatusKawinDkId { get; private set; }
        public string StatusKawinDkName { get; private set; }
    }

    public interface IStatusKawinDkKey
    {
        string StatusKawinDkId { get;}
    }
}
