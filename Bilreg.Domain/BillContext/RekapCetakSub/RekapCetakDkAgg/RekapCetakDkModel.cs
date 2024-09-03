using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakDkAgg;
public class RekapCetakDkModel(string rekapCetakDkId, string rekapCetakDkName) : IRekapCetakDkKey
{
   
    public string RekapCetakDkId { get; protected set; } = rekapCetakDkId;
    public string RekapCetakDkName { get; protected set; } = rekapCetakDkName;

}

public interface IRekapCetakDkKey
{
    string RekapCetakDkId { get; }
}