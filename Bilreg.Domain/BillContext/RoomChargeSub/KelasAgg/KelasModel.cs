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
        public bool IsAktif { get; private set; }
        public string KelasDkId { get; private set; }
        public string KelasDkName { get; private set; }

        public KelasModel(string id, string name, string kelasDkId)
        {
            KelasId = id;
            KelasName = name;
            IsAktif = true;
            KelasDkId = kelasDkId;
        }

        public void Set(KelasDkModel kelasDk)
        {
            KelasDkId = kelasDk.KelasDkId;
        }
        public void SetAktif() => IsAktif = true;
        public void UnSetAktif() => IsAktif = false;

        public static KelasModel Create(string id, string name, string kelasDkId)
            => new KelasModel(id, name, kelasDkId);

    }

    public interface IKelasKey
    {
        string KelasId { get; }
    }
}
