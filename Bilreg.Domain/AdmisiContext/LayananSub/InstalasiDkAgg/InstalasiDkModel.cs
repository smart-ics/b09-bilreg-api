using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg
{
    public class InstalasiDkModel :IInstalasiDkKey
    {
        // Constructor
        private InstalasiDkModel(string id ,string name)
        {
            InstalasiDkId = id;
            InstalasiDkName = name;
        }

        // Factory
        public static InstalasiDkModel Create(string id, string name)
        {
            return new InstalasiDkModel(id, name);
        }

        //Properties
        public string InstalasiDkId { get; private set; }
        public string InstalasiDkName { get; private set; }
        
    }

    public interface IInstalasiDkKey
    {
        string InstalasiDkId { get; }
    }
}
