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
        public InstalasiModel(string id, string name)
        {

            InstalasiId = id;
            InstalasiName = name;
            InstalasiDkId = string.Empty;
            InstalaiDkName = string.Empty;

        }
        public static InstalasiModel Create(string id, string name) => new InstalasiModel(id, name);
        public void Set(InstalasiDkModel instalasiDk)
        {
            if (instalasiDk == null) throw new ArgumentNullException(nameof(instalasiDk));
            InstalasiDkId = instalasiDk.InstalasiDkId;
            InstalaiDkName = instalasiDk.InstalasiDkName;

        }

        public string InstalasiId { get; private set; }
        public string InstalasiName { get;  private set; }
        public string InstalasiDkId { get; private set; }
        public string InstalaiDkName { get; private set; }
    }
    public interface IInstalasiKey
    {
        string InstalasiId { get; }
    }


}

