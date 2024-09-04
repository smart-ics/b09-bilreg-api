using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakDkAgg;
public class RekapCetakDkModel(string id, string name) : IRekapCetakDkKey
{
   
    public string RekapCetakDkId { get; protected set; } = id;
    public string RekapCetakDkName { get; protected set; } = name;

}
public interface IRekapCetakDkKey
{
    string RekapCetakDkId { get; }
}