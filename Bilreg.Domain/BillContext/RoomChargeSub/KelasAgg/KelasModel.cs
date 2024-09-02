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
        public string KelasId { get; protected set; }
        public string KelasName { get; protected set; }
        public bool IsAktif { get; protected set; }
        public string KelasDkId { get; protected set; }
        public string KelasDkName { get; protected set; }

        
        public KelasModel(string id, string name)
        {
            KelasId = id;
            KelasName = name;
            IsAktif = true;
            KelasDkId = string.Empty;
        }

        public void Set(KelasDkModel kelasDk)
        {
            KelasDkId = kelasDk.KelasDkId;
        }
        public void SetAktif() => IsAktif = true;
        public void UnSetAktif() => IsAktif = false;

        public static KelasModel Create(string id, string name)
            => new KelasModel(id, name);

    }

    public interface IKelasKey
    {
        string KelasId { get; }
    }
}
