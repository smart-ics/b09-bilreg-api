using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.AdmisiContext.LayananSub.InstalasiAgg
{
    public class InstalasiModel : IInstalasiKey
    {
        public string InstalasiId { get; private set; }
        public string InstalasiName { get;  private set; }
        public string InstalasiDkId { get; private set; }
        public string InstalasiDkName { get; private set; }

        public InstalasiModel(string id, string name)
        {

            InstalasiId = id;
            InstalasiName = name;
            InstalasiDkId = string.Empty;
            InstalasiDkName = string.Empty;
        }
        public static InstalasiModel Create(string id, string name) => new InstalasiModel(id, name);
        
        public void Set(InstalasiDkModel instalasiDk)
        {
            if (instalasiDk == null) throw new ArgumentNullException(nameof(instalasiDk));
            InstalasiDkId = instalasiDk.InstalasiDkId;
            InstalasiDkName = instalasiDk.InstalasiDkName;
        }
    }
    public interface IInstalasiKey
    {
        string InstalasiId { get; }
    }


}

