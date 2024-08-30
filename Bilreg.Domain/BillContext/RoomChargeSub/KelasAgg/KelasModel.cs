using Bilreg.Domain.BillContext.RoomChargeSub.KelasDkAgg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg
{
    public class KelasModel : IKelasKey
    {
        public string KelasId { get; private set; }
        public string KelasName { get; private set; }
        public string IsAktif { get; private set; }
        public string KelasDkId { get; private set; }
        public string KelasDkName { get; private set; }

        public KelasModel(string id, string name, string isAktif,string kelasDkId)
        {
            KelasId = id;
            KelasName = name;
            IsAktif = isAktif;
            KelasDkId = kelasDkId;
            KelasDkName = string.Empty;
        }
        public void Set(KelasDkModel kelasDk)
        {
            KelasDkId = kelasDk.KelasDkId;
            KelasDkName = kelasDk.KelasDkName;

        }

        public static KelasModel Create(string id, string name,string isAktif, string kelasDkId)
            => new KelasModel(id, name,isAktif, kelasDkId);
    }

    public interface IKelasKey
    {
        string KelasId { get; }
    }
}
