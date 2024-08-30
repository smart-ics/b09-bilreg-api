using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.BillContext.RoomChargeSub.KelasDkAgg
{
    public class KelasDkModel: IKelasDkKey
    {
        private KelasDkModel(string id, string name) { 
            KelasDkId = id;
            KelasDkName = name;
        }
        // Factory Method
        public static KelasDkModel Create(string id, string name)
        {
            return new KelasDkModel(id,name);
        }
        // Properties
        public string KelasDkId { get; private set; }
        public string KelasDkName { get; private set; }
    }

    public interface IKelasDkKey
    {
        string KelasDkId { get; }
    }
}
